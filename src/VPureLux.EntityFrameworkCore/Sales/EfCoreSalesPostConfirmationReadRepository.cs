using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Sales;

public class EfCoreSalesPostConfirmationReadRepository : ISalesPostConfirmationReadRepository, ITransientDependency
{
    private readonly IDbContextProvider<VPureLuxDbContext> _dbContextProvider;

    public EfCoreSalesPostConfirmationReadRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider) =>
        _dbContextProvider = dbContextProvider;

    public async Task<long> GetReturnTaskCountAsync(
        SalesReturnTaskFilter filter,
        CancellationToken cancellationToken = default) =>
        await (await CreateReturnQueryAsync(filter)).LongCountAsync(cancellationToken);

    public async Task<List<SalesReturnTaskReadItem>> GetReturnTasksAsync(
        SalesReturnTaskFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateReturnQueryAsync(filter);
        query = ApplyReturnSorting(query, filter.Sorting);
        return await query.Skip(filter.SkipCount).Take(filter.MaxResultCount).ToListAsync(cancellationToken);
    }

    public async Task<long> GetRefundTaskCountAsync(
        SalesRefundTaskFilter filter,
        CancellationToken cancellationToken = default) =>
        await (await CreateRefundQueryAsync(filter)).LongCountAsync(cancellationToken);

    public async Task<List<SalesRefundTaskReadItem>> GetRefundTasksAsync(
        SalesRefundTaskFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateRefundQueryAsync(filter);
        query = ApplyRefundSorting(query, filter.Sorting);
        return await query.Skip(filter.SkipCount).Take(filter.MaxResultCount).ToListAsync(cancellationToken);
    }

    private static IQueryable<SalesReturnTaskReadItem> ApplyReturnSorting(
        IQueryable<SalesReturnTaskReadItem> query,
        string? sorting)
    {
        var (field, descending) = ParseSorting(sorting);
        return field switch
        {
            "orderno" => descending
                ? query.OrderByDescending(x => x.OrderNo).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.OrderNo).ThenBy(x => x.CreatedAt),
            "customer" => descending
                ? query.OrderByDescending(x => x.CustomerName).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CustomerName).ThenBy(x => x.CreatedAt),
            "itemname" => descending
                ? query.OrderByDescending(x => x.ItemName).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.ItemName).ThenBy(x => x.CreatedAt),
            "quantity" => descending
                ? query.OrderByDescending(x => x.Quantity).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.Quantity).ThenBy(x => x.CreatedAt),
            "reason" => descending
                ? query.OrderByDescending(x => x.Reason).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.Reason).ThenBy(x => x.CreatedAt),
            _ => descending
                ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.OrderNo)
                : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.OrderNo)
        };
    }

    private static IQueryable<SalesRefundTaskReadItem> ApplyRefundSorting(
        IQueryable<SalesRefundTaskReadItem> query,
        string? sorting)
    {
        var (field, descending) = ParseSorting(sorting);
        return field switch
        {
            "orderno" => descending
                ? query.OrderByDescending(x => x.OrderNo).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.OrderNo).ThenBy(x => x.CreatedAt),
            "customer" => descending
                ? query.OrderByDescending(x => x.CustomerName).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CustomerName).ThenBy(x => x.CreatedAt),
            "remainingamount" => descending
                ? query.OrderByDescending(x => x.RemainingAmount).ThenByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.RemainingAmount).ThenBy(x => x.CreatedAt),
            _ => descending
                ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.OrderNo)
                : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.OrderNo)
        };
    }

    private static (string Field, bool Descending) ParseSorting(string? sorting)
    {
        var parts = sorting?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return (
            parts.FirstOrDefault()?.ToLowerInvariant() ?? "createdat",
            parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IQueryable<SalesReturnTaskReadItem>> CreateReturnQueryAsync(SalesReturnTaskFilter filter)
    {
        var db = await _dbContextProvider.GetDbContextAsync();
        var revisionTasks =
            from revision in db.SalesOrderRevisions.AsNoTracking()
            from revisionLine in revision.Lines
            join order in db.SalesOrders.AsNoTracking() on revision.SalesOrderId equals order.Id
            from sourceLine in order.Lines.Where(x => x.Id == revisionLine.SourceSalesOrderLineId)
            where revision.Status == SalesOrderRevisionStatus.Draft &&
                  revisionLine.SourceSalesOrderLineId.HasValue &&
                  revisionLine.ReturnConfirmedAt == null &&
                  (revisionLine.IsRemoved || revisionLine.ProductId != revisionLine.BeforeProductId ||
                   revisionLine.Quantity < revisionLine.BeforeQuantity)
            select new SalesReturnTaskReadItem
            {
                TaskType = SalesReturnTaskType.RevisionLine,
                OperationId = revision.Id,
                RevisionLineId = revisionLine.Id,
                SalesOrderId = order.Id,
                OrderNo = order.OrderNo,
                CustomerCode = order.CustomerCodeSnapshot,
                CustomerName = order.CustomerNameSnapshot,
                ItemName = sourceLine.ItemCodeSnapshot + " - " + sourceLine.ItemNameSnapshot,
                Quantity = revisionLine.IsRemoved || revisionLine.ProductId != revisionLine.BeforeProductId
                    ? revisionLine.BeforeQuantity ?? 0
                    : (revisionLine.BeforeQuantity ?? 0) - revisionLine.Quantity,
                Reason = revision.Reason,
                IsException = false,
                CreatedAt = revision.CreationTime
            };
        var cancellationTasks =
            from cancellation in db.SalesOrderCancellations.AsNoTracking()
            join order in db.SalesOrders.AsNoTracking() on cancellation.SalesOrderId equals order.Id
            where cancellation.StockStatus == SalesOrderCancellationStockStatus.PendingReturn ||
                  cancellation.StockStatus == SalesOrderCancellationStockStatus.Exception
            select new SalesReturnTaskReadItem
            {
                TaskType = SalesReturnTaskType.Cancellation,
                OperationId = cancellation.Id,
                RevisionLineId = null,
                SalesOrderId = order.Id,
                OrderNo = order.OrderNo,
                CustomerCode = order.CustomerCodeSnapshot,
                CustomerName = order.CustomerNameSnapshot,
                ItemName = string.Empty,
                Quantity = order.Lines.Where(x => x.IsEffective).Sum(x => x.Quantity),
                Reason = cancellation.StockExceptionReason ?? cancellation.Reason,
                IsException = cancellation.StockStatus == SalesOrderCancellationStockStatus.Exception,
                CreatedAt = cancellation.CreationTime
            };
        var query = revisionTasks.Concat(cancellationTasks);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim();
            query = query.Where(x => x.OrderNo.Contains(search) || x.CustomerCode.Contains(search) ||
                                     x.CustomerName.Contains(search) || x.ItemName.Contains(search));
        }
        return query;
    }

    private async Task<IQueryable<SalesRefundTaskReadItem>> CreateRefundQueryAsync(SalesRefundTaskFilter filter)
    {
        var db = await _dbContextProvider.GetDbContextAsync();
        var revisionTasks =
            from revision in db.SalesOrderRevisions.AsNoTracking()
            join order in db.SalesOrders.AsNoTracking() on revision.SalesOrderId equals order.Id
            let refunded = db.SalesOrderRefunds.Where(x => x.SalesOrderRevisionId == revision.Id).Sum(x => (decimal?)x.Amount) ?? 0
            where revision.Status == SalesOrderRevisionStatus.Applied && revision.RefundDue > refunded
            select new SalesRefundTaskReadItem
            {
                TaskType = SalesRefundTaskType.Revision,
                OperationId = revision.Id,
                SalesOrderId = order.Id,
                OrderNo = order.OrderNo,
                CustomerCode = order.CustomerCodeSnapshot,
                CustomerName = order.CustomerNameSnapshot,
                RefundDue = revision.RefundDue,
                RefundedAmount = refunded,
                RemainingAmount = revision.RefundDue - refunded,
                CreatedAt = revision.AppliedAt ?? revision.CreationTime
            };
        var cancellationTasks =
            from cancellation in db.SalesOrderCancellations.AsNoTracking()
            join order in db.SalesOrders.AsNoTracking() on cancellation.SalesOrderId equals order.Id
            where cancellation.PaymentStatus == SalesOrderCancellationPaymentStatus.PendingRefund &&
                  cancellation.RefundDue > cancellation.RefundedAmount
            select new SalesRefundTaskReadItem
            {
                TaskType = SalesRefundTaskType.Cancellation,
                OperationId = cancellation.Id,
                SalesOrderId = order.Id,
                OrderNo = order.OrderNo,
                CustomerCode = order.CustomerCodeSnapshot,
                CustomerName = order.CustomerNameSnapshot,
                RefundDue = cancellation.RefundDue,
                RefundedAmount = cancellation.RefundedAmount,
                RemainingAmount = cancellation.RefundDue - cancellation.RefundedAmount,
                CreatedAt = cancellation.EffectiveAt
            };
        var query = revisionTasks.Concat(cancellationTasks);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim();
            query = query.Where(x => x.OrderNo.Contains(search) || x.CustomerCode.Contains(search) ||
                                     x.CustomerName.Contains(search));
        }
        return query;
    }
}
