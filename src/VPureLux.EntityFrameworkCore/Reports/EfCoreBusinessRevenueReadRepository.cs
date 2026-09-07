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

public class EfCoreBusinessRevenueReadRepository(IDbContextProvider<VPureLuxDbContext> provider) : IBusinessRevenueReadRepository
{
    public async Task<long> GetCountAsync(GetBusinessRevenueListInput input, DateTime toExclusive, CancellationToken cancellationToken = default) =>
        await BuildQuery(await provider.GetDbContextAsync(), input, toExclusive).LongCountAsync(cancellationToken);

    public async Task<List<BusinessRevenueRowDto>> GetListAsync(GetBusinessRevenueListInput input, DateTime toExclusive, CancellationToken cancellationToken = default) =>
        await Sort(BuildQuery(await provider.GetDbContextAsync(), input, toExclusive), input.Sorting)
            .Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync(cancellationToken);

    public async Task<BusinessRevenueSummaryDto> GetSummaryAsync(GetBusinessRevenueListInput input, DateTime toExclusive, CancellationToken cancellationToken = default) =>
        await Totals(BuildQuery(await provider.GetDbContextAsync(), input, toExclusive)).SingleOrDefaultAsync(cancellationToken)
        ?? new BusinessRevenueSummaryDto { KnownMaterialCost = 0, KnownLaborCost = 0, TotalKnownCost = 0, CostIncompleteCount = 0, Profit = 0 };

    internal static IQueryable<BusinessRevenueSummaryDto> Totals(IQueryable<BusinessRevenueRowDto> rows) =>
        rows.GroupBy(_ => 1).Select(g => new BusinessRevenueSummaryDto
        {
            DocumentCount = g.LongCount(),
            SalesRevenue = g.Sum(x => x.Source == BusinessRevenueSource.Sales ? x.Revenue : 0),
            ServiceRevenue = g.Sum(x => x.Source == BusinessRevenueSource.Service ? x.Revenue : 0),
            KnownMaterialCost = g.Sum(x => x.Source == BusinessRevenueSource.Service ? x.MaterialCost : 0),
            KnownLaborCost = g.Sum(x => x.Source == BusinessRevenueSource.Service ? x.TotalKnownCost - x.MaterialCost : 0),
            TotalKnownCost = g.Sum(x => x.TotalKnownCost),
            // SQL Server cannot COUNT_BIG an untyped NULL when Sales folds this predicate to false.
            CostIncompleteCount = g.Sum(x => x.CostIncomplete == true ? 1L : 0L),
            Profit = g.Any(x => x.CostIncomplete == true) ? null : g.Sum(x => x.Profit)
        });

    internal static IQueryable<BusinessRevenueRowDto> BuildQuery(VPureLuxDbContext db, GetBusinessRevenueListInput input, DateTime end)
    {
        var query = input.Source switch
        {
            BusinessRevenueSource.Sales => Sales(db),
            BusinessRevenueSource.Service => Service(db),
            _ => Sales(db).Concat(Service(db))
        };
        if (input.FromDate.HasValue) query = query.Where(x => x.DocumentDate >= input.FromDate.Value);
        query = query.Where(x => x.DocumentDate < end);
        if (!string.IsNullOrWhiteSpace(input.SearchText))
        {
            var s = input.SearchText.Trim();
            query = query.Where(x => x.DocumentNo.Contains(s) || x.CustomerCode.Contains(s) || x.CustomerName.Contains(s) || x.AssetNo.Contains(s) || x.AssetName.Contains(s));
        }
        return query;
    }

    private static IQueryable<BusinessRevenueRowDto> Service(VPureLuxDbContext db)
    {
        var orders = db.ServiceOrders.AsNoTracking().Where(o => o.Status == ServiceOrderStatus.Completed && o.CompletedAt.HasValue);
        var costs = orders.SelectMany(o => o.Lines.Where(l => l.ActualQuantity > 0), (o, l) => new
        {
            OrderId = o.Id, l.LineType,
            // Historical material CostAmountSnapshot is a factual FIFO amount; historical labor zero is not known cost.
            Cost = l.ActualCostAmount ?? (l.LineType == ServiceOrderLineType.Material && o.CompletionCommandHash == null ? (decimal?)l.CostAmountSnapshot : null)
        }).GroupBy(x => x.OrderId).Select(g => new
        {
            OrderId = g.Key,
            Material = g.Sum(x => x.LineType == ServiceOrderLineType.Material ? x.Cost ?? 0 : 0),
            Labor = g.Sum(x => x.LineType == ServiceOrderLineType.Labor ? x.Cost ?? 0 : 0),
            Unknown = g.Count(x => !x.Cost.HasValue),
            UnknownLabor = g.Count(x => x.LineType == ServiceOrderLineType.Labor && !x.Cost.HasValue)
        });
        var money = EfCoreServiceMoneyReadRepository.SummaryQuery(db, orders);
        return from o in orders
               join c in costs on o.Id equals c.OrderId into cg
               from c in cg.DefaultIfEmpty()
               join m in money on o.Id equals m.ServiceOrderId
               let material = (decimal?)c.Material ?? 0
               let labor = (decimal?)c.Labor ?? 0
               let unknown = ((int?)c.Unknown ?? 0) > 0
               let unknownLabor = ((int?)c.UnknownLabor ?? 0) > 0
               select new BusinessRevenueRowDto
               {
                   Source = BusinessRevenueSource.Service, DocumentId = o.Id, DocumentNo = o.OrderNo,
                   DocumentDate = o.CompletionCommandHash != null ? o.CompletedAt!.Value.AddHours(7) : o.CompletedAt!.Value,
                   CustomerId = o.CustomerId, CustomerCode = o.CustomerCodeSnapshot, CustomerName = o.CustomerNameSnapshot,
                   CustomerAssetId = o.CustomerAssetId, AssetNo = o.AssetNoSnapshot, AssetName = o.AssetNameSnapshot,
                   Revenue = o.TotalRevenueAmount, MaterialCost = material, LaborCostKnown = !unknownLabor,
                   LaborCost = unknownLabor ? null : labor, TotalKnownCost = material + labor, CostIncomplete = unknown,
                   Profit = unknown ? null : o.TotalRevenueAmount - material - labor,
                   GrossPosted = m.GrossPosted, GrossRefunded = m.GrossRefunded, NetPaid = m.NetPaid,
                   Receivable = m.Receivable, CustomerCredit = m.CustomerCredit, RefundDue = m.RefundDue,
                   HasInconsistentLedger = m.HasInconsistentLedger
               };
    }

    private static IQueryable<BusinessRevenueRowDto> Sales(VPureLuxDbContext db)
    {
        var orders = db.SalesOrders.AsNoTracking().Where(o => o.Status == SalesOrderStatus.Confirmed && o.ConfirmedAt.HasValue);
        var lines = orders.SelectMany(o => o.Lines.Where(l => l.IsEffective && l.LineType == SalesOrderLineType.Product), (o, l) => new
        { OrderId = o.Id, l.RevenueAmount, l.CostAmountSnapshot, l.ProfitAmount })
            .GroupBy(x => x.OrderId).Select(g => new { OrderId = g.Key, Revenue = g.Sum(x => x.RevenueAmount), Cost = g.Sum(x => x.CostAmountSnapshot), Profit = g.Sum(x => x.ProfitAmount) });
        var posted = db.SalesOrderPayments.Where(p => p.Status == SalesOrderPaymentStatus.Posted)
            .GroupBy(p => p.SalesOrderId).Select(g => new { OrderId = g.Key, Amount = g.Sum(x => x.Amount) });
        var refunds = db.SalesOrderRefunds.GroupBy(p => p.SalesOrderId)
            .Select(g => new { OrderId = g.Key, Amount = g.Sum(x => x.Amount) });
        return from o in orders
               join l in lines on o.Id equals l.OrderId
               join p in posted on o.Id equals p.OrderId into pg
               from p in pg.DefaultIfEmpty()
               join r in refunds on o.Id equals r.OrderId into rg
               from r in rg.DefaultIfEmpty()
               let paid = (decimal?)p.Amount ?? 0
               let refunded = (decimal?)r.Amount ?? 0
               let net = paid >= refunded ? paid - refunded : 0
               select new BusinessRevenueRowDto
               {
                   Source = BusinessRevenueSource.Sales, DocumentId = o.Id, DocumentNo = o.OrderNo, DocumentDate = o.ConfirmedAt!.Value,
                   CustomerId = o.CustomerId, CustomerCode = o.CustomerCodeSnapshot, CustomerName = o.CustomerNameSnapshot,
                   CustomerAssetId = null, AssetNo = "", AssetName = "", Revenue = l.Revenue, MaterialCost = l.Cost,
                   LaborCostKnown = true, LaborCost = 0, TotalKnownCost = l.Cost, CostIncomplete = false, Profit = l.Profit,
                   GrossPosted = paid, GrossRefunded = refunded, NetPaid = net,
                   Receivable = l.Revenue > net ? l.Revenue - net : 0,
                   CustomerCredit = net > l.Revenue ? net - l.Revenue : 0,
                   RefundDue = net > l.Revenue ? net - l.Revenue : 0, HasInconsistentLedger = refunded > paid
               };
    }

    private static IOrderedQueryable<BusinessRevenueRowDto> Sort(IQueryable<BusinessRevenueRowDto> q, string? sorting)
    {
        var sorted = sorting?.Trim().ToLowerInvariant() switch
        {
            "documentdate" or "documentdate asc" => q.OrderBy(x => x.DocumentDate),
            "documentno" or "documentno asc" => q.OrderBy(x => x.DocumentNo),
            "documentno desc" => q.OrderByDescending(x => x.DocumentNo),
            "customername" or "customername asc" => q.OrderBy(x => x.CustomerName),
            "customername desc" => q.OrderByDescending(x => x.CustomerName),
            "revenue" or "revenue asc" => q.OrderBy(x => x.Revenue),
            "revenue desc" => q.OrderByDescending(x => x.Revenue),
            "totalknowncost" or "totalknowncost asc" => q.OrderBy(x => x.TotalKnownCost),
            "totalknowncost desc" => q.OrderByDescending(x => x.TotalKnownCost),
            "profit" or "profit asc" => q.OrderBy(x => x.Profit),
            "profit desc" => q.OrderByDescending(x => x.Profit),
            _ => q.OrderByDescending(x => x.DocumentDate)
        };
        return sorted.ThenByDescending(x => x.DocumentDate).ThenBy(x => x.Source).ThenBy(x => x.DocumentId);
    }
}
