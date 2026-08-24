using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Warranty;

public class EfCoreProductMachineSettingRepository :
    EfCoreRepository<VPureLuxDbContext, ProductMachineSetting, Guid>,
    IProductMachineSettingRepository
{
    public EfCoreProductMachineSettingRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<ProductMachineSetting?> FindByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.ProductId == productId,
            GetCancellationToken(cancellationToken));

    public async Task<List<ProductMachineSetting>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        return await (await GetDbSetAsync())
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(GetCancellationToken(cancellationToken));
    }
}
