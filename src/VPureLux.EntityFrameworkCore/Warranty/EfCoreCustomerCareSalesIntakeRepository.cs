using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using VPureLux.Sales;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Warranty;

public class EfCoreCustomerCareSalesIntakeRepository : ICustomerCareSalesIntakeRepository, ITransientDependency
{
    private readonly IDbContextProvider<VPureLuxDbContext> _dbContextProvider;

    public EfCoreCustomerCareSalesIntakeRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<List<CustomerCareSalesIntakeCandidate>> GetCandidatesAsync(
        DateTime confirmedFrom,
        DateTime retryDueAt,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        var db = await _dbContextProvider.GetDbContextAsync();
        var headers = await (
                from order in db.SalesOrders.AsNoTracking()
                from line in order.Lines
                join setting in db.ProductMachineSettings.AsNoTracking()
                    on line.CatalogItemId equals setting.ProductId
                where order.Status == SalesOrderStatus.Confirmed &&
                      order.ConfirmedAt >= confirmedFrom &&
                      setting.IsMachine &&
                      !db.CustomerCareSyncFailures.Any(failure =>
                          failure.SalesOrderLineId == line.Id &&
                          failure.Status == CustomerCareSyncFailureStatus.Pending &&
                          failure.NextRetryAt > retryDueAt) &&
                      !db.CustomerAssets.Any(asset => asset.SalesOrderLineId == line.Id)
                orderby order.ConfirmedAt, order.Id, line.LineNo
                select new
                {
                    SalesOrderId = order.Id,
                    SalesOrderLineId = line.Id,
                    SalesOrderLineNo = line.LineNo,
                    order.OrderNo,
                    order.CustomerId,
                    CustomerCode = order.CustomerCodeSnapshot,
                    CustomerName = order.CustomerNameSnapshot,
                    ProductId = line.CatalogItemId,
                    ProductCode = line.ItemCodeSnapshot,
                    ProductName = line.ItemNameSnapshot,
                    line.Quantity,
                    SoldAt = order.ConfirmedAt ?? order.OrderDate
                })
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);

        if (headers.Count == 0)
        {
            return [];
        }

        var lineIds = headers.Select(x => x.SalesOrderLineId).ToList();
        var sourceOrders = await db.SalesOrders
            .AsNoTracking()
            .Where(order => order.Lines.Any(line => lineIds.Contains(line.Id)))
            .Include(order => order.Lines.Where(line => lineIds.Contains(line.Id)))
            .ThenInclude(line => line.BomSnapshotItems)
            .ToListAsync(cancellationToken);
        var bomItems = sourceOrders
            .SelectMany(order => order.Lines)
            .SelectMany(line => line.BomSnapshotItems.Select(item => new
            {
                SalesOrderLineId = line.Id,
                item.Id,
                item.ComponentId,
                item.ComponentCode,
                item.ComponentName,
                item.Unit,
                item.QuantityPerProduct
            }))
            .ToList();
        var itemsByLine = bomItems
            .GroupBy(x => x.SalesOrderLineId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CustomerCareSalesIntakeBomItem>)group
                    .OrderBy(x => x.ComponentCode)
                    .ThenBy(x => x.Id)
                    .Select(x => new CustomerCareSalesIntakeBomItem(
                        x.Id,
                        x.ComponentId,
                        x.ComponentCode,
                        x.ComponentName,
                        x.Unit,
                        x.QuantityPerProduct))
                    .ToList());

        return headers.Select(header => new CustomerCareSalesIntakeCandidate(
                header.SalesOrderId,
                header.SalesOrderLineId,
                header.SalesOrderLineNo,
                header.OrderNo,
                header.CustomerId,
                header.CustomerCode,
                header.CustomerName,
                header.ProductId,
                header.ProductCode,
                header.ProductName,
                header.Quantity,
                header.SoldAt,
                itemsByLine.GetValueOrDefault(header.SalesOrderLineId) ?? []))
            .ToList();
    }
}
