using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp.Application.Services;

namespace VPureLux.Inventory;

[Authorize(VPureLuxPermissions.Inventory.View)]
public class InventoryQueryAppService : ApplicationService, IInventoryQueryAppService
{
    private readonly IInventoryBalanceRepository _repository;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly IInventoryLotSupplierRepository _lotSupplierRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly InventoryApplicationMapper _mapper;

    public InventoryQueryAppService(
        IInventoryBalanceRepository repository,
        IInventoryLotRepository lotRepository,
        IInventoryLotSupplierRepository lotSupplierRepository,
        IInventoryTransactionRepository transactionRepository,
        InventoryApplicationMapper mapper)
    {
        _repository = repository;
        _lotRepository = lotRepository;
        _lotSupplierRepository = lotSupplierRepository;
        _transactionRepository = transactionRepository;
        _mapper = mapper;
    }

    public async Task<List<InventoryBalanceDto>> GetBalancesAsync(Guid? warehouseId = null, Guid? stockItemId = null) =>
        (await _repository.GetListAsync(warehouseId, stockItemId)).Select(_mapper.ToDto).ToList();

    public async Task<List<InventoryLotDto>> GetLotsAsync(Guid? warehouseId = null, Guid? stockItemId = null)
    {
        var lots = await _lotRepository.GetListAsync(warehouseId, stockItemId);
        var supplierMap = (await _lotSupplierRepository.GetListByLotIdsAsync(lots.Select(x => x.Id).ToList()))
            .ToDictionary(x => x.InventoryLotId);

        return lots.Select(lot =>
        {
            var dto = _mapper.ToDto(lot);
            if (supplierMap.TryGetValue(lot.Id, out var supplier))
            {
                dto.SupplierId = supplier.SupplierId;
                dto.SupplierCode = supplier.SupplierCodeSnapshot;
                dto.SupplierName = supplier.SupplierNameSnapshot;
            }

            return dto;
        }).ToList();
    }

    [Authorize(VPureLuxPermissions.Inventory.ViewLedger)]
    public async Task<List<InventoryTransactionDto>> GetLedgerAsync(Guid? warehouseId = null, Guid? stockItemId = null) =>
        (await _transactionRepository.GetLedgerAsync(warehouseId, stockItemId)).Select(_mapper.ToDto).ToList();
}
