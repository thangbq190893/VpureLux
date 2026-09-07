using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Audit;
using VPureLux.Service;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

public sealed class MoneyAuditProbe { public bool FailAfterWrite { get; set; } }

[Volo.Abp.DependencyInjection.DisableConventionalRegistration]
public class MoneyAuditRepository(IDbContextProvider<VPureLuxDbContext> provider, MoneyAuditProbe probe)
    : EfCoreRepository<VPureLuxDbContext, BusinessAuditLog, Guid>(provider)
{
    public override async Task<BusinessAuditLog> InsertAsync(BusinessAuditLog entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var result = await base.InsertAsync(entity, autoSave, cancellationToken);
        if (probe.FailAfterWrite && entity.Module == "Service") throw new InvalidOperationException("S004 injected audit failure after save");
        return result;
    }
}

public partial class ServiceOrderWorkflowTests
{
    [Fact]
    public async Task Money_all_mutations_roll_back_with_their_business_audit()
    {
        var f = await CreateFixtureAsync("S004-ROLLBACK");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 100));
        var probe = GetRequiredService<MoneyAuditProbe>();
        probe.FailAfterWrite = true;
        var command = Pay(100);
        await Should.ThrowAsync<InvalidOperationException>(() => Money.AddPaymentAsync(o.Id, command));
        (await Money.GetSummaryAsync(o.Id)).NetPaid.ShouldBe(0);
        (await Money.GetListAsync(new() { ServiceOrderId = o.Id })).TotalCount.ShouldBe(0);
        probe.FailAfterWrite = false;
        var payment = await Money.AddPaymentAsync(o.Id, command);
        (await Orders.GetAsync(o.Id)).ConcurrencyStamp.ShouldBe(o.ConcurrencyStamp);
        probe.FailAfterWrite = true;
        await Should.ThrowAsync<InvalidOperationException>(() => Money.VoidPaymentAsync(o.Id, new()
            { PaymentId = payment.Id, ConcurrencyStamp = payment.ConcurrencyStamp, IdempotencyKey = "void", Reason = "wrong" }));
        (await Money.GetListAsync(new() { ServiceOrderId = o.Id })).Items.Single().Status.ShouldBe(ServicePaymentStatus.Posted);
        await Orders.CancelAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, Reason = "cancel" });
        var refund = RefundInput(100);
        await Should.ThrowAsync<InvalidOperationException>(() => Money.RefundAsync(o.Id, refund));
        (await Money.GetSummaryAsync(o.Id)).RefundDue.ShouldBe(100);
        (await Money.GetRefundListAsync(new() { ServiceOrderId = o.Id })).TotalCount.ShouldBe(0);
        probe.FailAfterWrite = false;
        await Money.RefundAsync(o.Id, refund);
        (await Money.GetSummaryAsync(o.Id)).NetPaid.ShouldBe(0);
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<IRepository<BusinessAuditLog, Guid>>().CountAsync(x => x.EntityId == o.Id)).ShouldBe(2));
    }

    [Fact]
    public async Task Money_legacy_missing_hash_void_reason_and_overpayment_are_read_without_rewrite()
    {
        var f = await CreateFixtureAsync("S004-LEGACY");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 10));
        var paidId = Guid.NewGuid(); var voidId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            foreach (var (id, amount, status) in new[] { (paidId, 12m, ServicePaymentStatus.Posted), (voidId, 3m, ServicePaymentStatus.Voided) })
            {
                var p = (ServicePayment)Activator.CreateInstance(typeof(ServicePayment), nonPublic: true)!;
                var values = db.Entry(p).CurrentValues;
                values[nameof(p.Id)] = id; values[nameof(p.ServiceOrderId)] = o.Id; values[nameof(p.CustomerId)] = o.CustomerId;
                values[nameof(p.Amount)] = amount; values[nameof(p.Status)] = status;
                values[nameof(p.PaymentDate)] = new DateTime(2026, 8, 24);
                values[nameof(p.PaymentMethod)] = VPureLux.Sales.SalesPaymentMethod.Cash;
                values[nameof(p.IdempotencyKey)] = id.ToString("N");
                db.ServicePayments.Add(p);
            }
            await db.SaveChangesAsync();
        });
        var s = await Money.GetSummaryAsync(o.Id);
        s.GrossPosted.ShouldBe(12); s.AdvancePaid.ShouldBe(12); s.AdvanceExcess.ShouldBe(2); s.RefundDue.ShouldBe(2);
        (await Should.ThrowAsync<BusinessException>(() => Money.AddPaymentAsync(o.Id, Pay(1)))).Code.ShouldBe(ServiceErrorCodes.PaymentExceedsObligation);
        var history = await Money.GetListAsync(new() { ServiceOrderId = o.Id });
        history.Items.All(x => x.IsLegacy).ShouldBeTrue();
        history.Items.Single(x => x.Id == voidId).VoidReason.ShouldBeNull();
        var retry = Pay(12); retry.IdempotencyKey = paidId.ToString("N");
        (await Should.ThrowAsync<BusinessException>(() => Money.AddPaymentAsync(o.Id, retry))).Code.ShouldBe(ServiceErrorCodes.MoneyConflict);
        retry.PaymentDate = new DateTimeOffset(2026, 8, 24, 0, 0, 0, TimeSpan.Zero);
        retry.ReferenceNo = string.Empty; retry.Note = null;
        (await Money.AddPaymentAsync(o.Id, retry)).Id.ShouldBe(paidId);
        retry.Amount = 3; retry.IdempotencyKey = voidId.ToString("N");
        (await Money.AddPaymentAsync(o.Id, retry)).Status.ShouldBe(ServicePaymentStatus.Voided);
        await Money.RefundAsync(o.Id, RefundInput(2));
        await WithUnitOfWorkAsync(async () =>
        {
            var rows = await GetRequiredService<IRepository<ServicePayment, Guid>>().GetListAsync(x => x.ServiceOrderId == o.Id);
            rows.Single(x => x.Id == paidId).Amount.ShouldBe(12);
            rows.All(x => x.RequestHash == null && x.VoidReason == null && x.VoidedAt == null).ShouldBeTrue();
            rows.Single(x => x.Id == voidId).Status.ShouldBe(ServicePaymentStatus.Voided);
        });
    }

    [Fact]
    public async Task Money_payment_request_all_facts_and_cross_order_keys_are_checked()
    {
        var f = await CreateFixtureAsync("S004-PAYLOAD");
        var a = await StartOrderAsync(f, Labor(f.Work.Id, 1, 100));
        var b = await StartOrderAsync(f, Labor(f.Work.Id, 1, 100));
        var input = Pay(10);
        await Money.AddPaymentAsync(a.Id, input);
        foreach (var change in new Action<AddServicePaymentDto>[] {
            x => x.Amount = 11, x => x.PaymentDate = x.PaymentDate.AddMinutes(1),
            x => x.PaymentMethod = VPureLux.Sales.SalesPaymentMethod.BankTransfer, x => x.ReferenceNo = "other", x => x.Note = "other" })
        {
            var conflicting = Pay(10); conflicting.IdempotencyKey = input.IdempotencyKey; change(conflicting);
            (await Should.ThrowAsync<BusinessException>(() => Money.AddPaymentAsync(a.Id, conflicting))).Code.ShouldBe(ServiceErrorCodes.MoneyConflict);
        }
        (await Should.ThrowAsync<BusinessException>(() => Money.AddPaymentAsync(b.Id, input))).Code.ShouldBe(ServiceErrorCodes.MoneyConflict);
        (await Money.GetSummaryAsync(b.Id)).GrossPosted.ShouldBe(0);
        var complete = Completion(a); await Orders.CompleteAsync(a.Id, complete);
        await Money.AddPaymentAsync(a.Id, Pay(90));
        (await Money.GetSummaryAsync(a.Id)).Receivable.ShouldBe(0);
    }
}
