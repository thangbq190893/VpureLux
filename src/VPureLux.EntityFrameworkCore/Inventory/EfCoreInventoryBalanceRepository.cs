using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Inventory;

public class EfCoreInventoryBalanceRepository : IInventoryBalanceRepository
{
    private readonly IDbContextProvider<VPureLuxDbContext> _provider;
    public EfCoreInventoryBalanceRepository(IDbContextProvider<VPureLuxDbContext> provider) => _provider = provider;

    public async Task<InventoryBalance?> FindAsync(Guid warehouseId, Guid stockItemId, CancellationToken cancellationToken = default) =>
        await (await _provider.GetDbContextAsync()).InventoryBalances.FindAsync([warehouseId, stockItemId], cancellationToken);

    public async Task ApplyMovementAsync(Guid warehouseId, Guid stockItemId, decimal quantityDelta, decimal valueDelta, DateTime movementAt, CancellationToken cancellationToken = default)
    {
        var dbContext = await _provider.GetDbContextAsync();
        var balance = await dbContext.InventoryBalances.FindAsync([warehouseId, stockItemId], cancellationToken);
        if (balance == null)
        {
            balance = new InventoryBalance(warehouseId, stockItemId);
            await dbContext.InventoryBalances.AddAsync(balance, cancellationToken);
        }
        balance.ApplyMovement(quantityDelta, valueDelta, movementAt);
    }

    public async Task<List<InventoryBalance>> GetListAsync(Guid? warehouseId = null, Guid? stockItemId = null, CancellationToken cancellationToken = default) =>
        await (await _provider.GetDbContextAsync()).InventoryBalances
            .Where(x => (!warehouseId.HasValue || x.WarehouseId == warehouseId) && (!stockItemId.HasValue || x.StockItemId == stockItemId))
            .OrderBy(x => x.WarehouseId).ThenBy(x => x.StockItemId)
            .ToListAsync(cancellationToken);
}
