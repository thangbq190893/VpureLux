using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Sales;

public class EfCoreSalesOrderPaymentRepository :
    EfCoreRepository<VPureLuxDbContext, SalesOrderPayment, Guid>,
    ISalesOrderPaymentRepository
{
    public EfCoreSalesOrderPaymentRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider)
    {
    }

    public async Task<SalesOrderPayment?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync())
            .FirstOrDefaultAsync(
                x => x.IdempotencyKey == idempotencyKey,
                GetCancellationToken(cancellationToken));

    public async Task<List<SalesOrderPayment>> GetListBySalesOrderIdAsync(
        Guid salesOrderId,
        CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync())
            .Where(x => x.SalesOrderId == salesOrderId)
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.CreationTime)
            .ThenByDescending(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));

    public async Task<Dictionary<Guid, decimal>> GetPostedPaidAmountsAsync(
        IEnumerable<Guid> salesOrderIds,
        CancellationToken cancellationToken = default)
    {
        var ids = salesOrderIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        return await (await GetDbSetAsync())
            .Where(x => ids.Contains(x.SalesOrderId) && x.Status == SalesOrderPaymentStatus.Posted)
            .GroupBy(x => x.SalesOrderId)
            .Select(x => new { SalesOrderId = x.Key, PaidAmount = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.SalesOrderId, x => x.PaidAmount, GetCancellationToken(cancellationToken));
    }
}
