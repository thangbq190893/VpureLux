using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Inventory;

public class EfCoreInventoryLotSupplierRepository
    : EfCoreRepository<VPureLuxDbContext, InventoryLotSupplier, Guid>, IInventoryLotSupplierRepository
{
    public EfCoreInventoryLotSupplierRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider)
    {
    }

    public async Task<List<InventoryLotSupplier>> GetListByLotIdsAsync(
        IReadOnlyCollection<Guid> inventoryLotIds,
        CancellationToken cancellationToken = default)
    {
        if (inventoryLotIds.Count == 0)
        {
            return new List<InventoryLotSupplier>();
        }

        return await (await GetDbSetAsync())
            .Where(x => inventoryLotIds.Contains(x.InventoryLotId))
            .ToListAsync(GetCancellationToken(cancellationToken));
    }
}
