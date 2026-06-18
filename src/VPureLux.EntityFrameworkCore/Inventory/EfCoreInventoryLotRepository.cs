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

public class EfCoreInventoryLotRepository : EfCoreRepository<VPureLuxDbContext, InventoryLot, Guid>, IInventoryLotRepository
{
    public EfCoreInventoryLotRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }

    public async Task<List<InventoryLot>> GetAvailableFifoLotsAsync(Guid warehouseId, Guid stockItemId, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).Where(x => x.WarehouseId == warehouseId && x.StockItemId == stockItemId && x.AvailableQuantity > 0)
            .OrderBy(x => x.ReceivedAt).ThenBy(x => x.CreationTime).ThenBy(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));

    public async Task<List<InventoryLot>> GetListAsync(Guid? warehouseId = null, Guid? stockItemId = null, CancellationToken cancellationToken = default)
    {
        var query = await GetDbSetAsync();
        return await query.Where(x => (!warehouseId.HasValue || x.WarehouseId == warehouseId) && (!stockItemId.HasValue || x.StockItemId == stockItemId))
            .OrderBy(x => x.ReceivedAt).ThenBy(x => x.CreationTime).ThenBy(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }
}
