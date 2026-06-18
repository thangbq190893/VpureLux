using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Inventory;

[Route("api/inventory")]
public class InventoryQueryController : AbpControllerBase
{
    private readonly IInventoryQueryAppService _appService;
    public InventoryQueryController(IInventoryQueryAppService appService) => _appService = appService;
    [HttpGet("balances")] public Task<List<InventoryBalanceDto>> GetBalancesAsync(Guid? warehouseId = null, Guid? stockItemId = null) => _appService.GetBalancesAsync(warehouseId, stockItemId);
    [HttpGet("lots")] public Task<List<InventoryLotDto>> GetLotsAsync(Guid? warehouseId = null, Guid? stockItemId = null) => _appService.GetLotsAsync(warehouseId, stockItemId);
    [HttpGet("ledger")] public Task<List<InventoryTransactionDto>> GetLedgerAsync(Guid? warehouseId = null, Guid? stockItemId = null) => _appService.GetLedgerAsync(warehouseId, stockItemId);
}
