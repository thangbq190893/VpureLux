using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Inventory;

[Route("api/inventory/transactions")]
public class InventoryTransactionController : AbpControllerBase
{
    private readonly IInventoryTransactionAppService _appService;
    public InventoryTransactionController(IInventoryTransactionAppService appService) => _appService = appService;

    [HttpGet("{id:guid}")]
    public Task<InventoryTransactionDto> GetAsync(Guid id) => _appService.GetAsync(id);
    [HttpPost("receipts")]
    public Task<InventoryTransactionDto> PostReceiptAsync([FromBody] PostReceiptDto input) => _appService.PostReceiptAsync(input);
    [HttpPost("issues")]
    public Task<IssueCostResultDto> PostIssueAsync([FromBody] PostIssueDto input) => _appService.PostIssueAsync(input);
    [HttpPost("adjustments")]
    public Task<InventoryTransactionDto> PostAdjustmentAsync([FromBody] PostAdjustmentDto input) => _appService.PostAdjustmentAsync(input);
}
