using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Audit;
using VPureLux.EntityFrameworkCore.Warranty;
using VPureLux.Permissions;
using VPureLux.Sales;
using VPureLux.Service;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

public partial class ServiceOrderWorkflowTests
{
    private IServicePaymentAppService Money => GetRequiredService<IServicePaymentAppService>();
    private static AddServicePaymentDto Pay(decimal amount) => new()
    {
        Amount = amount, PaymentDate = new(2026, 9, 7, 9, 0, 0, TimeSpan.FromHours(7)),
        PaymentMethod = SalesPaymentMethod.Cash, IdempotencyKey = Guid.NewGuid().ToString("N"), ReferenceNo = "receipt", Note = "paid"
    };
    private static AddServiceRefundDto RefundInput(decimal amount) => new()
    {
        Amount = amount, RefundDate = new(2026, 9, 7, 10, 0, 0, TimeSpan.FromHours(7)), Method = SalesPaymentMethod.Cash,
        IdempotencyKey = Guid.NewGuid().ToString("N"), ReferenceNo = "return", Reason = "Actual charge lower"
    };

    [Fact]
    public async Task Money_actual_lower_than_advance_partial_refunds_preserve_revenue_and_audit()
    {
        var f = await CreateFixtureAsync("S004-ACTUAL");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 10, 1000000));
        var input = Pay(8000000);
        var p = await Money.AddPaymentAsync(o.Id, input);
        (await Money.GetSummaryAsync(o.Id)).Revenue.ShouldBe(0);
        var complete = Completion(o);
        complete.Lines[0].ActualQuantity = 6;
        await Orders.CompleteAsync(o.Id, complete);
        var s = await Money.GetSummaryAsync(o.Id);
        s.Revenue.ShouldBe(6000000); s.RefundDue.ShouldBe(2000000); s.Receivable.ShouldBe(0);
        (await Money.GetListAsync(new() { ServiceOrderId = o.Id })).Items.Single().Status.ShouldBe(ServicePaymentStatus.Posted);
        (await Should.ThrowAsync<BusinessException>(() => Money.AddPaymentAsync(o.Id, Pay(1)))).Code.ShouldBe(ServiceErrorCodes.PaymentExceedsObligation);
        var rInput = RefundInput(1000000);
        var r = await Money.RefundAsync(o.Id, rInput);
        (await Money.RefundAsync(o.Id, rInput)).Id.ShouldBe(r.Id);
        s = await Money.GetSummaryAsync(o.Id); s.RefundDue.ShouldBe(1000000); s.Revenue.ShouldBe(6000000);
        rInput.Amount = 1000001;
        (await Should.ThrowAsync<BusinessException>(() => Money.RefundAsync(o.Id, rInput))).Code.ShouldBe(ServiceErrorCodes.MoneyConflict);
        (await Should.ThrowAsync<BusinessException>(() => Money.RefundAsync(o.Id, RefundInput(1000001)))).Code.ShouldBe(ServiceErrorCodes.RefundExceedsCredit);
        await Money.RefundAsync(o.Id, RefundInput(1000000));
        s = await Money.GetSummaryAsync(o.Id);
        s.NetPaid.ShouldBe(6000000); s.GrossPosted.ShouldBe(8000000); s.GrossRefunded.ShouldBe(2000000); s.RefundDue.ShouldBe(0);
        (await Should.ThrowAsync<BusinessException>(() => Money.VoidPaymentAsync(o.Id, new()
        { PaymentId = p.Id, ConcurrencyStamp = p.ConcurrencyStamp, IdempotencyKey = "void", Reason = "wrong" })))
            .Code.ShouldBe(ServiceErrorCodes.VoidWouldInvalidateRefund);
        await WithUnitOfWorkAsync(async () =>
        {
            var logs = await GetRequiredService<IRepository<BusinessAuditLog, Guid>>().GetListAsync(x => x.EntityId == o.Id);
            logs.Count.ShouldBe(3);
            logs.All(x => x.UserId.HasValue).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Money_cancel_preserves_advance_and_full_settlement_does_not_void()
    {
        var f = await CreateFixtureAsync("S004-CANCEL");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 5000000));
        await Money.AddPaymentAsync(o.Id, Pay(5000000));
        await Orders.CancelAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, Reason = "Customer cancelled" });
        var s = await Money.GetSummaryAsync(o.Id);
        s.Status.ShouldBe(ServiceOrderStatus.Cancelled); s.RefundDue.ShouldBe(5000000); s.Revenue.ShouldBe(0);
        (await Money.GetRefundListAsync(new() { ServiceOrderId = o.Id })).TotalCount.ShouldBe(0);
        await Money.RefundAsync(o.Id, RefundInput(5000000));
        (await Money.GetSummaryAsync(o.Id)).RefundDue.ShouldBe(0);
        (await Money.GetListAsync(new() { ServiceOrderId = o.Id })).Items.Single().Status.ShouldBe(ServicePaymentStatus.Posted);
    }

    [Fact]
    public async Task Money_replay_conflicts_void_audit_and_retry_after_void()
    {
        var f = await CreateFixtureAsync("S004-REPLAY");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 500));
        var input = Pay(100);
        var p = await Money.AddPaymentAsync(o.Id, input);
        (await Money.AddPaymentAsync(o.Id, input)).Id.ShouldBe(p.Id);
        input.Note = "other";
        (await Should.ThrowAsync<BusinessException>(() => Money.AddPaymentAsync(o.Id, input))).Code.ShouldBe(ServiceErrorCodes.MoneyConflict);
        input.Note = "paid";
        var v = new VoidServicePaymentDto { PaymentId = p.Id, ConcurrencyStamp = p.ConcurrencyStamp, IdempotencyKey = "void-key", Reason = "Wrong receipt" };
        var voided = await Money.VoidPaymentAsync(o.Id, v);
        (await Money.VoidPaymentAsync(o.Id, v)).VoidedAt.ShouldBe(voided.VoidedAt);
        (await Money.AddPaymentAsync(o.Id, input)).Status.ShouldBe(ServicePaymentStatus.Voided);
        (await Money.GetSummaryAsync(o.Id)).NetPaid.ShouldBe(0);
        v.Reason = "Different";
        (await Should.ThrowAsync<BusinessException>(() => Money.VoidPaymentAsync(o.Id, v))).Code.ShouldBe(ServiceErrorCodes.MoneyConflict);
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<IRepository<BusinessAuditLog, Guid>>().CountAsync(x => x.EntityId == o.Id)).ShouldBe(2));
    }

    [Fact]
    public async Task Money_customer_summary_is_per_order_not_cross_order_credit_netting()
    {
        var f = await CreateFixtureAsync("S004-CUSTOMER");
        var a = await StartOrderAsync(f, Labor(f.Work.Id, 1, 100));
        var b = await StartOrderAsync(f, Labor(f.Work.Id, 1, 200));
        var c = await StartOrderAsync(f, Labor(f.Work.Id, 1, 300));
        await Money.AddPaymentAsync(a.Id, Pay(50));
        await Money.AddPaymentAsync(b.Id, Pay(80));
        await Money.AddPaymentAsync(c.Id, Pay(70));
        await Orders.CompleteAsync(b.Id, Completion(b));
        await Orders.CancelAsync(c.Id, new() { ConcurrencyStamp = c.ConcurrencyStamp, Reason = "cancel" });
        var total = await Money.GetCustomerSummaryAsync(a.CustomerId);
        total.AdvancePaid.ShouldBe(50); total.Receivable.ShouldBe(120); total.RefundDue.ShouldBe(70); total.CustomerCredit.ShouldBe(70);
        (await Money.GetCustomerSummaryAsync(Guid.NewGuid())).Receivable.ShouldBe(0);
    }

    [Fact]
    public async Task Money_histories_filter_sort_and_page_two_in_database()
    {
        var f = await CreateFixtureAsync("S004-PAGE");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 100));
        for (var i = 0; i < 13; i++)
        {
            var p = Pay(1); p.PaymentDate = p.PaymentDate.AddMinutes(i); p.ReferenceNo = $"PAY-{i:D2}";
            await Money.AddPaymentAsync(o.Id, p);
        }
        var page = await Money.GetListAsync(new() { ServiceOrderId = o.Id, Sorting = "PaymentDate asc", SkipCount = 10, MaxResultCount = 10 });
        page.TotalCount.ShouldBe(13); page.Items.Count.ShouldBe(3); page.Items[0].ReferenceNo.ShouldBe("PAY-10");
        (await Money.GetListAsync(new() { ServiceOrderId = o.Id, SearchText = "PAY-12" })).TotalCount.ShouldBe(1);
        await Orders.CancelAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, Reason = "cancel" });
        for (var i = 0; i < 13; i++)
        {
            var r = RefundInput(1); r.RefundDate = r.RefundDate.AddMinutes(i); r.ReferenceNo = $"REF-{i:D2}";
            await Money.RefundAsync(o.Id, r);
        }
        var rp = await Money.GetRefundListAsync(new() { ServiceOrderId = o.Id, Sorting = "RefundDate asc", SkipCount = 10, MaxResultCount = 10 });
        rp.TotalCount.ShouldBe(13); rp.Items.Count.ShouldBe(3); rp.Items[0].ReferenceNo.ShouldBe("REF-10");
        (await Money.GetRefundListAsync(new() { ServiceOrderId = o.Id, SearchText = "REF-12" })).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Money_permissions_and_feature_are_enforced_on_all_mutations()
    {
        var f = await CreateFixtureAsync("S004-AUTH");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 100));
        var p = await Money.AddPaymentAsync(o.Id, Pay(50));
        Func<Task>[] actions = [
            async () => await Money.AddPaymentAsync(o.Id, Pay(1)),
            async () => await Money.VoidPaymentAsync(o.Id, new() { PaymentId = p.Id, ConcurrencyStamp = p.ConcurrencyStamp, IdempotencyKey = "void", Reason = "wrong" }),
            async () => await Money.RefundAsync(o.Id, RefundInput(1)) ];
        foreach (var action in actions)
        {
            using (WarrantyMatrixAuthorizationService.Deny(VPureLuxPermissions.Service.ManagePayments))
                await Should.ThrowAsync<AbpAuthorizationException>(action);
            GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = false;
            (await Should.ThrowAsync<BusinessException>(action)).Code.ShouldBe(ServiceErrorCodes.Disabled);
            Enable();
        }
        (await Money.GetSummaryAsync(o.Id)).NetPaid.ShouldBe(50);
    }
}
