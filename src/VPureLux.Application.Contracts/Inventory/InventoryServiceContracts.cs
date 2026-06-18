using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Inventory;

public interface IWarehouseAppService : IApplicationService
{
    Task<PagedResultDto<WarehouseDto>> GetListAsync(GetInventoryListInput input);
    Task<WarehouseDto> GetAsync(Guid id);
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto input);
    Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto input);
    Task ActivateAsync(Guid id);
    Task DeactivateAsync(Guid id);
}

public interface IStockItemAppService : IApplicationService
{
    Task<PagedResultDto<StockItemDto>> GetListAsync(GetInventoryListInput input);
    Task<StockItemDto> GetAsync(Guid id);
}

public interface IInventoryTransactionAppService : IApplicationService
{
    Task<InventoryTransactionDto> GetAsync(Guid id);
    Task<InventoryTransactionDto> PostReceiptAsync(PostReceiptDto input);
    Task<IssueCostResultDto> PostIssueAsync(PostIssueDto input);
    Task<InventoryTransactionDto> PostAdjustmentAsync(PostAdjustmentDto input);
}

public interface IInventoryQueryAppService : IApplicationService
{
    Task<List<InventoryBalanceDto>> GetBalancesAsync(Guid? warehouseId = null, Guid? stockItemId = null);
    Task<List<InventoryLotDto>> GetLotsAsync(Guid? warehouseId = null, Guid? stockItemId = null);
    Task<List<InventoryTransactionDto>> GetLedgerAsync(Guid? warehouseId = null, Guid? stockItemId = null);
}
