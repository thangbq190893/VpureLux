using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Service;

internal static class ServiceMoneyPersistence
{
    public static bool IsDuplicate(Exception exception)
    {
        for (var e = exception; e != null; e = e.InnerException)
            if (e.Message.Contains("UX_ServicePayments_", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("UX_ServiceRefunds_", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("AppServicePayments.IdempotencyKey", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("AppServicePayments.VoidIdempotencyKey", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("AppServiceRefunds.IdempotencyKey", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}

public class EfCoreServicePaymentRepository(IDbContextProvider<VPureLuxDbContext> provider)
    : EfCoreRepository<VPureLuxDbContext, ServicePayment, Guid>(provider)
{
    public override async Task<ServicePayment> InsertAsync(ServicePayment entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        try { return await base.InsertAsync(entity, autoSave, cancellationToken); }
        catch (DbUpdateException e) when (ServiceMoneyPersistence.IsDuplicate(e))
        { throw new BusinessException(ServiceErrorCodes.MoneyConflict, innerException: e).WithData("ServiceOrderId", entity.ServiceOrderId); }
    }

    public override async Task<ServicePayment> UpdateAsync(ServicePayment entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        try { return await base.UpdateAsync(entity, autoSave, cancellationToken); }
        catch (DbUpdateException e) when (ServiceMoneyPersistence.IsDuplicate(e))
        { throw new BusinessException(ServiceErrorCodes.MoneyConflict, innerException: e).WithData("ServiceOrderId", entity.ServiceOrderId); }
    }
}

public class EfCoreServiceRefundRepository(IDbContextProvider<VPureLuxDbContext> provider)
    : EfCoreRepository<VPureLuxDbContext, ServiceRefund, Guid>(provider)
{
    public override async Task<ServiceRefund> InsertAsync(ServiceRefund entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        try { return await base.InsertAsync(entity, autoSave, cancellationToken); }
        catch (DbUpdateException e) when (ServiceMoneyPersistence.IsDuplicate(e))
        { throw new BusinessException(ServiceErrorCodes.MoneyConflict, innerException: e).WithData("ServiceOrderId", entity.ServiceOrderId); }
    }
}

public class EfCoreServiceMoneyReadRepository(IDbContextProvider<VPureLuxDbContext> provider) : IServiceMoneyReadRepository
{
    internal static IQueryable<ServiceMoneySummary> SummaryQuery(VPureLuxDbContext db, IQueryable<ServiceOrder> orders)
    {
        var posted = db.ServicePayments.Where(p => p.Status == ServicePaymentStatus.Posted)
            .GroupBy(p => p.ServiceOrderId).Select(g => new { OrderId = g.Key, Total = g.Sum(x => x.Amount) });
        var refunded = db.ServiceRefunds.GroupBy(r => r.ServiceOrderId)
            .Select(g => new { OrderId = g.Key, Total = g.Sum(x => x.Amount) });
        var planned = db.ServiceOrders.SelectMany(o => o.Lines, (o, l) => new { OrderId = o.Id, Amount = l.UnitPrice * l.PlannedQuantity })
            .GroupBy(x => x.OrderId).Select(g => new { OrderId = g.Key, Total = g.Sum(x => x.Amount) });
        return (from o in orders
                join p in posted on o.Id equals p.OrderId into paidGroup
                from p in paidGroup.DefaultIfEmpty()
                join r in refunded on o.Id equals r.OrderId into refundGroup
                from r in refundGroup.DefaultIfEmpty()
                join l in planned on o.Id equals l.OrderId into plannedGroup
                from l in plannedGroup.DefaultIfEmpty()
                select new ServiceMoneyFacts
                {
                    ServiceOrderId = o.Id, CustomerId = o.CustomerId, Status = o.Status,
                    PlannedTotal = (decimal?)l.Total ?? 0, CompletedTotal = o.TotalRevenueAmount,
                    GrossPosted = (decimal?)p.Total ?? 0, GrossRefunded = (decimal?)r.Total ?? 0
                }).Select(ServiceMoneySummary.Projection);
    }

    public async Task<ServiceMoneySummary> GetSummaryAsync(Guid orderId)
    {
        var db = await provider.GetDbContextAsync();
        return await SummaryQuery(db, db.ServiceOrders.AsNoTracking().Where(o => o.Id == orderId)).SingleOrDefaultAsync()
            ?? throw new BusinessException(ServiceErrorCodes.OrderNotFound).WithData("ServiceOrderId", orderId);
    }

    public async Task<ServiceCustomerMoneySummary> GetCustomerSummaryAsync(Guid customerId)
    {
        var db = await provider.GetDbContextAsync();
        return await SummaryQuery(db, db.ServiceOrders.AsNoTracking().Where(o => o.CustomerId == customerId))
            .GroupBy(x => x.CustomerId).Select(g => new ServiceCustomerMoneySummary
            {
                Receivable = g.Sum(x => x.Receivable), AdvancePaid = g.Sum(x => x.AdvancePaid),
                CustomerCredit = g.Sum(x => x.CustomerCredit), RefundDue = g.Sum(x => x.RefundDue),
                InconsistentOrderCount = g.LongCount(x => x.HasInconsistentLedger)
            }).SingleOrDefaultAsync() ?? new ServiceCustomerMoneySummary();
    }

    public async Task<(long Count, List<ServicePayment> Items)> GetPaymentsAsync(ServiceMoneyHistoryFilter filter)
    {
        var db = await provider.GetDbContextAsync();
        var q = db.ServicePayments.AsNoTracking().Where(x => x.ServiceOrderId == filter.ServiceOrderId);
        if (filter.Status.HasValue) q = q.Where(x => x.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            q = q.Where(x => x.ReferenceNo.Contains(term) || (x.Note != null && x.Note.Contains(term)) ||
                (x.VoidReason != null && x.VoidReason.Contains(term)));
        }
        var count = await q.LongCountAsync();
        var sorted = filter.Sorting?.Trim().ToLowerInvariant() switch
        {
            "paymentdate" or "paymentdate asc" => q.OrderBy(x => x.PaymentDate).ThenBy(x => x.CreationTime).ThenBy(x => x.Id),
            "status" or "status asc" => q.OrderBy(x => x.Status).ThenByDescending(x => x.PaymentDate).ThenByDescending(x => x.CreationTime).ThenByDescending(x => x.Id),
            "status desc" => q.OrderByDescending(x => x.Status).ThenByDescending(x => x.PaymentDate).ThenByDescending(x => x.CreationTime).ThenByDescending(x => x.Id),
            _ => q.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.CreationTime).ThenByDescending(x => x.Id)
        };
        return (count, await sorted.Skip(filter.SkipCount).Take(filter.MaxResultCount).ToListAsync());
    }

    public async Task<(long Count, List<ServiceRefund> Items)> GetRefundsAsync(ServiceMoneyHistoryFilter filter)
    {
        var db = await provider.GetDbContextAsync();
        var q = db.ServiceRefunds.AsNoTracking().Where(x => x.ServiceOrderId == filter.ServiceOrderId);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            q = q.Where(x => x.ReferenceNo.Contains(term) || x.Reason.Contains(term));
        }
        var count = await q.LongCountAsync();
        var sorted = filter.Sorting?.Trim().ToLowerInvariant() is "refunddate" or "refunddate asc"
            ? q.OrderBy(x => x.RefundDate).ThenBy(x => x.CreationTime).ThenBy(x => x.Id)
            : q.OrderByDescending(x => x.RefundDate).ThenByDescending(x => x.CreationTime).ThenByDescending(x => x.Id);
        return (count, await sorted.Skip(filter.SkipCount).Take(filter.MaxResultCount).ToListAsync());
    }
}
