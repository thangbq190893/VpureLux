using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Timing;

namespace VPureLux.Inventory;

public class InventoryManager : DomainService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public InventoryManager(
        IWarehouseRepository warehouseRepository,
        IStockItemRepository stockItemRepository,
        IInventoryLotRepository lotRepository,
        IInventoryTransactionRepository transactionRepository,
        IGuidGenerator guidGenerator,
        IClock clock)
    {
        _warehouseRepository = warehouseRepository;
        _stockItemRepository = stockItemRepository;
        _lotRepository = lotRepository;
        _transactionRepository = transactionRepository;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task<Warehouse> CreateWarehouseAsync(string code, string name, string? address, bool isDefault)
    {
        code = Warehouse.NormalizeCode(code);
        if (await _warehouseRepository.CodeExistsAsync(code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseCodeAlreadyExists);
        }

        return new Warehouse(_guidGenerator.Create(), code, name, address, isDefault);
    }

    public async Task EnsureWarehouseAndStockItemUsableAsync(Guid warehouseId, Guid stockItemId)
    {
        var warehouse = await _warehouseRepository.FindAsync(warehouseId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseNotFound);
        if (warehouse.Status != InventoryEntityStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseInactive);
        }

        var stockItem = await _stockItemRepository.FindAsync(stockItemId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.StockItemNotFound);
        if (stockItem.Status != InventoryEntityStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.StockItemInactive);
        }

        if (!stockItem.IsInventoryEnabled)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.StockItemInventoryDisabled);
        }
    }

    public async Task<InventoryTransaction?> FindExistingTransactionAsync(string idempotencyKey) =>
        await _transactionRepository.FindByIdempotencyKeyAsync(idempotencyKey);

    public InventoryTransaction CreateTransaction(
        Guid warehouseId,
        InventoryTransactionType type,
        string idempotencyKey,
        string requestHash,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? bomVersionId = null,
        string? reason = null) =>
        new(
            _guidGenerator.Create(),
            warehouseId,
            type,
            idempotencyKey,
            requestHash,
            referenceType,
            referenceId,
            bomVersionId,
            reason);

    public InventoryLot CreateLot(Guid warehouseId, InventoryTransactionLine line)
    {
        return new InventoryLot(
            _guidGenerator.Create(),
            line.LotNo!,
            warehouseId,
            line.StockItemId,
            line.Id,
            line.ReceivedAt ?? _clock.Now,
            line.Quantity,
            line.UnitCost!.Value);
    }

    public async Task<IReadOnlyList<FifoAllocation>> AllocateFifoAsync(
        InventoryTransaction transaction,
        InventoryTransactionLine line)
    {
        var remaining = line.Quantity;
        var result = new List<FifoAllocation>();
        var lots = await _lotRepository.GetAvailableFifoLotsAsync(transaction.WarehouseId, line.StockItemId);

        foreach (var lot in lots)
        {
            if (remaining == 0)
            {
                break;
            }

            var allocated = Math.Min(remaining, lot.AvailableQuantity);
            lot.Allocate(allocated);
            transaction.AddAllocation(line.Id, _guidGenerator.Create(), lot.Id, allocated, lot.UnitCost);
            result.Add(new FifoAllocation(lot.Id, allocated, lot.UnitCost));
            remaining -= allocated;
        }

        if (remaining > 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InsufficientInventory)
                .WithData(nameof(line.StockItemId), line.StockItemId)
                .WithData("RequestedQuantity", line.Quantity);
        }

        return result;
    }
}
