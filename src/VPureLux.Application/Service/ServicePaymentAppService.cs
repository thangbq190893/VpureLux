using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VPureLux.Audit;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace VPureLux.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
public class ServicePaymentAppService(
    IServiceOrderRepository orders, IRepository<ServicePayment, Guid> payments,
    IRepository<ServiceRefund, Guid> refunds, IServiceMoneyReadRepository read,
    ServiceOrderOperationCoordinator coordinator, IOptions<ServiceOptions> options, IClock clock,
    BusinessAuditManager auditManager, IRepository<BusinessAuditLog, Guid> auditLogs)
    : ApplicationService, IServicePaymentAppService
{
    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<ServiceMoneySummaryDto> GetSummaryAsync(Guid id)
    {
        EnsureEnabled();
        var s = await read.GetSummaryAsync(id);
        return new ServiceMoneySummaryDto
        {
            ServiceOrderId = s.ServiceOrderId, CustomerId = s.CustomerId, Status = s.Status,
            PlannedTotal = s.PlannedTotal, ActualTotal = s.ActualTotal, Revenue = s.Revenue,
            GrossPosted = s.GrossPosted, GrossRefunded = s.GrossRefunded, NetPaid = s.NetPaid,
            AdvancePaid = s.AdvancePaid, PlannedRemaining = s.PlannedRemaining, AdvanceExcess = s.AdvanceExcess,
            Receivable = s.Receivable, CustomerCredit = s.CustomerCredit, RefundDue = s.RefundDue,
            HasInconsistentLedger = s.HasInconsistentLedger
        };
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<ServiceCustomerMoneySummaryDto> GetCustomerSummaryAsync(Guid customerId)
    {
        EnsureEnabled();
        var s = await read.GetCustomerSummaryAsync(customerId);
        return new ServiceCustomerMoneySummaryDto { Receivable = s.Receivable, AdvancePaid = s.AdvancePaid,
            CustomerCredit = s.CustomerCredit, RefundDue = s.RefundDue, InconsistentOrderCount = s.InconsistentOrderCount };
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServicePaymentDto>> GetListAsync(ServiceMoneyHistoryInput input)
    {
        EnsureEnabled();
        var page = await read.GetPaymentsAsync(Filter(input));
        return new PagedResultDto<ServicePaymentDto>(page.Count, page.Items.Select(Map).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServiceRefundDto>> GetRefundListAsync(ServiceMoneyHistoryInput input)
    {
        EnsureEnabled();
        var page = await read.GetRefundsAsync(Filter(input));
        return new PagedResultDto<ServiceRefundDto>(page.Count, page.Items.Select(Map).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.ManagePayments)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<ServicePaymentDto> AddPaymentAsync(Guid id, AddServicePaymentDto input)
    {
        EnsureEnabled();
        var actor = Actor();
        var command = new ServiceMoneyCommand(id, input.Amount, input.PaymentDate, input.PaymentMethod,
            input.IdempotencyKey, input.ReferenceNo, input.Note);
        return await coordinator.ExecuteAsync(id, async () =>
        {
            var order = await ReadOrderAsync(id);
            var existing = await payments.FindAsync(x => x.IdempotencyKey == command.Key);
            if (existing != null) { existing.EnsureReplay(command); return Map(existing); }
            (await read.GetSummaryAsync(id)).EnsurePaymentAllowed(command.Amount);
            var payment = new ServicePayment(GuidGenerator.Create(), order.CustomerId, command, actor, clock.Now);
            await payments.InsertAsync(payment, autoSave: true);
            await AuditAsync(order, payment.Id, "PaymentPosted", command.Key, actor,
                new { payment.Amount, payment.PaymentDate, payment.PaymentMethod, payment.ReferenceNo, payment.Note });
            return Map(payment);
        });
    }

    [Authorize(VPureLuxPermissions.Service.ManagePayments)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<ServicePaymentDto> VoidPaymentAsync(Guid id, VoidServicePaymentDto input)
    {
        EnsureEnabled();
        var actor = Actor();
        var command = new ServiceVoidCommand(input.PaymentId, input.IdempotencyKey, input.Reason);
        return await coordinator.ExecuteAsync(id, async () =>
        {
            var order = await ReadOrderAsync(id);
            var payment = await payments.GetAsync(command.PaymentId);
            if (payment.ServiceOrderId != id) throw new BusinessException(ServiceErrorCodes.InvalidMoney);
            if (payment.IsVoidReplay(command)) return Map(payment);
            if (payment.ConcurrencyStamp != input.ConcurrencyStamp || string.IsNullOrWhiteSpace(input.ConcurrencyStamp))
                throw new BusinessException(ServiceErrorCodes.ConcurrentModification).WithData("PaymentId", payment.Id);
            if (await payments.AnyAsync(x => x.VoidIdempotencyKey == command.Key && x.Id != payment.Id))
                throw new BusinessException(ServiceErrorCodes.MoneyConflict);
            (await read.GetSummaryAsync(id)).EnsureVoidAllowed(payment.Amount);
            payment.Void(command, actor, clock.Now);
            await payments.UpdateAsync(payment, autoSave: true);
            await AuditAsync(order, payment.Id, "PaymentVoided", command.Key, actor,
                new { payment.Amount, payment.VoidedAt, payment.VoidedBy, payment.VoidReason });
            return Map(payment);
        });
    }

    [Authorize(VPureLuxPermissions.Service.ManagePayments)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<ServiceRefundDto> RefundAsync(Guid id, AddServiceRefundDto input)
    {
        EnsureEnabled();
        var actor = Actor();
        var command = new ServiceMoneyCommand(id, input.Amount, input.RefundDate, input.Method,
            input.IdempotencyKey, input.ReferenceNo, input.Reason, refund: true);
        return await coordinator.ExecuteAsync(id, async () =>
        {
            var order = await ReadOrderAsync(id);
            var existing = await refunds.FindAsync(x => x.IdempotencyKey == command.Key);
            if (existing != null) { existing.EnsureReplay(command); return Map(existing); }
            (await read.GetSummaryAsync(id)).EnsureRefundAllowed(command.Amount);
            var refund = new ServiceRefund(GuidGenerator.Create(), order.CustomerId, command, actor, clock.Now);
            await refunds.InsertAsync(refund, autoSave: true);
            await AuditAsync(order, refund.Id, "RefundPosted", command.Key, actor,
                new { refund.Amount, refund.RefundDate, refund.Method, refund.ReferenceNo, refund.Reason });
            return Map(refund);
        });
    }

    private async Task<ServiceOrder> ReadOrderAsync(Guid id)
    {
        // Money changes do not change the header; tracking its FK principal can bump ABP's concurrency stamp.
        using (orders.DisableTracking()) return await orders.GetAsync(id, includeDetails: false);
    }

    private async Task AuditAsync(ServiceOrder order, Guid factId, string action, string key, Guid actor, object facts)
    {
        await auditLogs.InsertAsync(auditManager.Create(new BusinessAuditEnvelope(
            GuidGenerator.Create(), "Service", action, action, "ServiceOrder", order.Id, order.Id.ToString("N"), factId.ToString("N"),
            clock.Now, EntityDisplay: order.OrderNo, NewValueJson: JsonSerializer.Serialize(facts),
            MetadataJson: JsonSerializer.Serialize(new { FactId = factId, IdempotencyKey = key }),
            UserId: actor, UserName: CurrentUser.UserName, ActorType: AuditActorType.User)), autoSave: true);
    }

    private Guid Actor() => CurrentUser.Id is { } id && id != Guid.Empty ? id : throw new AbpAuthorizationException();
    private void EnsureEnabled()
    {
        if (!options.Value.IsEnabled) throw new BusinessException(ServiceErrorCodes.Disabled);
    }

    private static ServiceMoneyHistoryFilter Filter(ServiceMoneyHistoryInput input) => new()
    {
        ServiceOrderId = input.ServiceOrderId, SearchText = input.SearchText, Status = input.Status,
        Sorting = input.Sorting, SkipCount = input.SkipCount, MaxResultCount = Math.Clamp(input.MaxResultCount, 1, 100)
    };

    private static ServicePaymentDto Map(ServicePayment p) => new()
    {
        Id = p.Id, ServiceOrderId = p.ServiceOrderId, Amount = p.Amount, PaymentDate = p.PaymentDate,
        PaymentMethod = p.PaymentMethod, Status = p.Status, ReferenceNo = p.ReferenceNo, Note = p.Note,
        RecordedAt = p.RecordedAt ?? p.CreationTime, RecordedBy = p.RecordedBy ?? p.CreatorId,
        VoidedAt = p.VoidedAt, VoidedBy = p.VoidedBy, VoidReason = p.VoidReason,
        IsLegacy = p.RequestHash == null, ConcurrencyStamp = p.ConcurrencyStamp
    };

    private static ServiceRefundDto Map(ServiceRefund r) => new()
    {
        Id = r.Id, ServiceOrderId = r.ServiceOrderId, Amount = r.Amount, RefundDate = r.RefundDate,
        Method = r.Method, ReferenceNo = r.ReferenceNo, Reason = r.Reason, RecordedAt = r.RecordedAt, RecordedBy = r.RecordedBy
    };
}
