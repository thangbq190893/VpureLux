using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using VPureLux.Suppliers;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace VPureLux.Inventory;

[Authorize(VPureLuxPermissions.Inventory.Receive)]
public class InventoryLotAppService : ApplicationService, IInventoryLotAppService
{
    private readonly IInventoryLotRepository _lotRepository;
    private readonly IInventoryLotSupplierRepository _lotSupplierRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IInventoryBalanceRepository _balanceRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly InventoryManager _manager;
    private readonly InventoryApplicationMapper _mapper;

    public InventoryLotAppService(
        IInventoryLotRepository lotRepository,
        IInventoryLotSupplierRepository lotSupplierRepository,
        IInventoryTransactionRepository transactionRepository,
        IInventoryBalanceRepository balanceRepository,
        ISupplierRepository supplierRepository,
        InventoryManager manager,
        InventoryApplicationMapper mapper)
    {
        _lotRepository = lotRepository;
        _lotSupplierRepository = lotSupplierRepository;
        _transactionRepository = transactionRepository;
        _balanceRepository = balanceRepository;
        _supplierRepository = supplierRepository;
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<InventoryLotDto> UpdateSupplierAsync(Guid id, UpdateInventoryLotSupplierDto input)
    {
        var lot = await _lotRepository.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.InventoryLotNotFound)
                .WithData(nameof(id), id);
        var supplier = await _supplierRepository.FindAsync(input.SupplierId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.SupplierNotFound)
                .WithData(nameof(input.SupplierId), input.SupplierId);

        var existing = (await _lotSupplierRepository.GetListByLotIdsAsync([id])).SingleOrDefault();
        if (existing == null)
        {
            await _lotSupplierRepository.InsertAsync(_manager.CreateLotSupplier(lot, supplier), autoSave: true);
        }
        else
        {
            existing.ChangeSupplier(supplier);
            await _lotSupplierRepository.UpdateAsync(existing, autoSave: true);
        }

        var dto = _mapper.ToDto(lot);
        dto.SupplierId = supplier.Id;
        dto.SupplierCode = supplier.Code;
        dto.SupplierName = supplier.Name;
        return dto;
    }

    public async Task<InventoryLotDto> UpdateUnitCostAsync(Guid id, UpdateInventoryLotUnitCostDto input)
    {
        var lot = await _lotRepository.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.InventoryLotNotFound)
                .WithData(nameof(id), id);
        var previousUnitCost = lot.UnitCost;

        lot.UpdateUnitCost(input.UnitCost);
        await _lotRepository.UpdateAsync(lot);

        var valueDelta = lot.AvailableQuantity * (lot.UnitCost - previousUnitCost);
        if (valueDelta != 0)
        {
            await _balanceRepository.ApplyMovementAsync(
                lot.WarehouseId,
                lot.StockItemId,
                0,
                valueDelta,
                Clock.Now);
        }

        return _mapper.ToDto(lot);
    }

    public async Task DeleteUnusedReceiptAsync(Guid id)
    {
        var selectedLot = await _lotRepository.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.InventoryLotNotFound)
                .WithData(nameof(id), id);
        var transaction = await _transactionRepository.FindByReceiptLineIdAsync(selectedLot.ReceiptTransactionLineId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.InventoryTransactionNotFound)
                .WithData(nameof(selectedLot.ReceiptTransactionLineId), selectedLot.ReceiptTransactionLineId);

        if (transaction.Type != InventoryTransactionType.PurchaseReceipt ||
            transaction.Status != InventoryTransactionStatus.Posted)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InventoryReceiptCannotBeDeletedAfterUse);
        }

        var lineIds = transaction.Lines.Select(x => x.Id).ToList();
        var receiptLots = await _lotRepository.GetListByReceiptLineIdsAsync(lineIds);
        if (receiptLots.Count != lineIds.Count ||
            receiptLots.Any(x => x.AvailableQuantity != x.ReceivedQuantity))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InventoryReceiptCannotBeDeletedAfterUse)
                .WithData(nameof(transaction.Id), transaction.Id)
                .WithData(nameof(selectedLot.LotNo), selectedLot.LotNo);
        }

        var lotSuppliers = await _lotSupplierRepository.GetListByLotIdsAsync(receiptLots.Select(x => x.Id).ToList());
        foreach (var lotSupplier in lotSuppliers)
        {
            await _lotSupplierRepository.DeleteAsync(lotSupplier);
        }

        foreach (var lot in receiptLots)
        {
            await _balanceRepository.ApplyMovementAsync(
                lot.WarehouseId,
                lot.StockItemId,
                -lot.ReceivedQuantity,
                -(lot.ReceivedQuantity * lot.UnitCost),
                Clock.Now);
            await _lotRepository.DeleteAsync(lot);
        }

        await _transactionRepository.DeleteAsync(transaction, autoSave: true);
    }
}
