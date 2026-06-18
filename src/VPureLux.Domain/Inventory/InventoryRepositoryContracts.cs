using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Inventory;

public interface IStockItemRepository : IRepository<StockItem, Guid>
{
    Task<StockItem?> FindByCatalogItemAsync(StockItemType itemType, Guid catalogItemId, CancellationToken cancellationToken = default);
}

public interface IWarehouseRepository : IRepository<Warehouse, Guid>
{
    Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default);
}

public interface IInventoryLotRepository : IRepository<InventoryLot, Guid>
{
    Task<List<InventoryLot>> GetAvailableFifoLotsAsync(
        Guid warehouseId,
        Guid stockItemId,
        CancellationToken cancellationToken = default);
    Task<List<InventoryLot>> GetListAsync(
        Guid? warehouseId = null,
        Guid? stockItemId = null,
        CancellationToken cancellationToken = default);
}

public interface IInventoryTransactionRepository : IRepository<InventoryTransaction, Guid>
{
    Task<InventoryTransaction?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task<List<InventoryTransaction>> GetLedgerAsync(
        Guid? warehouseId = null,
        Guid? stockItemId = null,
        CancellationToken cancellationToken = default);
}

public interface IInventoryBalanceRepository
{
    Task<InventoryBalance?> FindAsync(Guid warehouseId, Guid stockItemId, CancellationToken cancellationToken = default);
    Task ApplyMovementAsync(
        Guid warehouseId,
        Guid stockItemId,
        decimal quantityDelta,
        decimal valueDelta,
        DateTime movementAt,
        CancellationToken cancellationToken = default);
    Task<List<InventoryBalance>> GetListAsync(Guid? warehouseId = null, Guid? stockItemId = null, CancellationToken cancellationToken = default);
}
