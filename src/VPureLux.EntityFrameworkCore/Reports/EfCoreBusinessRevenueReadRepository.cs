using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using VPureLux.Sales;
using VPureLux.Service;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Reports;

public class EfCoreBusinessRevenueReadRepository : IBusinessRevenueReadRepository
{
    private readonly IDbContextProvider<VPureLuxDbContext> _provider;
    public EfCoreBusinessRevenueReadRepository(IDbContextProvider<VPureLuxDbContext> provider) => _provider = provider;

    public async Task<long> GetCountAsync(GetBusinessRevenueListInput input, DateTime? toDateExclusive, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await BuildQuery(db, input, toDateExclusive).LongCountAsync(cancellationToken);
    }

    public async Task<List<BusinessRevenueRowDto>> GetListAsync(GetBusinessRevenueListInput input, DateTime? toDateExclusive, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        var query = BuildQuery(db, input, toDateExclusive);
        query = input.Sorting?.Contains("DocumentDate asc", StringComparison.OrdinalIgnoreCase) == true
            ? query.OrderBy(x => x.DocumentDate).ThenBy(x => x.DocumentNo)
            : query.OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.DocumentNo);
        return await query.Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync(cancellationToken);
    }

    public async Task<BusinessRevenueSummaryDto> GetSummaryAsync(GetBusinessRevenueListInput input, DateTime? toDateExclusive, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        var rows = BuildQuery(db, input, toDateExclusive);
        return await rows.GroupBy(_ => 1).Select(group => new BusinessRevenueSummaryDto
        {
            SalesRevenue = group.Where(x => x.Source == BusinessRevenueSource.Sales).Sum(x => x.Revenue),
            ServiceRevenue = group.Where(x => x.Source == BusinessRevenueSource.Service).Sum(x => x.Revenue),
            TotalCost = group.Sum(x => x.Cost),
            TotalProfit = group.Sum(x => x.Profit),
            TotalPaid = group.Sum(x => x.Paid),
            TotalRemaining = group.Sum(x => x.Remaining),
            DocumentCount = group.LongCount()
        }).FirstOrDefaultAsync(cancellationToken) ?? new BusinessRevenueSummaryDto();
    }

    private static IQueryable<BusinessRevenueRowDto> BuildQuery(VPureLuxDbContext db, GetBusinessRevenueListInput input, DateTime? toDateExclusive)
    {
        var search = input.SearchText?.Trim();
        var sales = db.SalesOrders.AsNoTracking()
            .Where(x => x.Status == SalesOrderStatus.Confirmed && x.ConfirmedAt.HasValue)
            .Select(x => new BusinessRevenueRowDto
            {
                Source = BusinessRevenueSource.Sales, DocumentId = x.Id, DocumentNo = x.OrderNo,
                DocumentDate = x.ConfirmedAt!.Value, CustomerCode = x.CustomerCodeSnapshot,
                CustomerName = x.CustomerNameSnapshot, Revenue = x.TotalRevenueAmount, Cost = x.TotalCostAmount,
                Profit = x.TotalProfitAmount,
                Paid = db.SalesOrderPayments.Where(p => p.SalesOrderId == x.Id && p.Status == SalesOrderPaymentStatus.Posted)
                    .Sum(p => (decimal?)p.Amount) ?? 0,
                Remaining = x.TotalRevenueAmount - (db.SalesOrderPayments.Where(p => p.SalesOrderId == x.Id && p.Status == SalesOrderPaymentStatus.Posted)
                    .Sum(p => (decimal?)p.Amount) ?? 0)
            });
        var service = db.ServiceOrders.AsNoTracking()
            .Where(x => x.Status == ServiceOrderStatus.Completed && x.CompletedAt.HasValue)
            .Select(x => new BusinessRevenueRowDto
            {
                Source = BusinessRevenueSource.Service, DocumentId = x.Id, DocumentNo = x.OrderNo,
                DocumentDate = x.CompletedAt!.Value, CustomerCode = x.CustomerCodeSnapshot,
                CustomerName = x.CustomerNameSnapshot, Revenue = x.TotalRevenueAmount, Cost = x.TotalCostAmount,
                Profit = x.TotalProfitAmount,
                Paid = db.ServicePayments.Where(p => p.ServiceOrderId == x.Id && p.Status == ServicePaymentStatus.Posted)
                    .Sum(p => (decimal?)p.Amount) ?? 0,
                Remaining = x.TotalRevenueAmount - (db.ServicePayments.Where(p => p.ServiceOrderId == x.Id && p.Status == ServicePaymentStatus.Posted)
                    .Sum(p => (decimal?)p.Amount) ?? 0)
            });
        var query = sales.Concat(service);
        return query
            .Where(x => !input.Source.HasValue || x.Source == input.Source.Value)
            .Where(x => !input.FromDate.HasValue || x.DocumentDate >= input.FromDate.Value.Date)
            .Where(x => !toDateExclusive.HasValue || x.DocumentDate < toDateExclusive.Value)
            .Where(x => string.IsNullOrEmpty(search) || x.DocumentNo.Contains(search) || x.CustomerCode.Contains(search) || x.CustomerName.Contains(search));
    }
}
