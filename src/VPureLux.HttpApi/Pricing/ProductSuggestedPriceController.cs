using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Pricing;

[Route("api/pricing/products/{productId:guid}/suggested-prices")]
public class ProductSuggestedPriceController : AbpControllerBase
{
    private readonly IProductSuggestedPriceAppService _appService;

    public ProductSuggestedPriceController(IProductSuggestedPriceAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public Task<List<ProductSuggestedPriceVersionDto>> GetHistoryAsync(Guid productId) =>
        _appService.GetHistoryAsync(productId);

    [HttpGet("current")]
    public Task<ProductSuggestedPriceVersionDto> GetCurrentAsync(Guid productId) =>
        _appService.GetCurrentAsync(productId);

    [HttpGet("at-date")]
    public Task<ProductSuggestedPriceVersionDto> GetAtDateAsync(Guid productId, [FromQuery] DateTime date) =>
        _appService.GetAtDateAsync(productId, date);

    [HttpPost]
    public Task<ProductSuggestedPriceVersionDto> CreateAsync(
        Guid productId,
        [FromBody] CreateProductSuggestedPriceVersionDto input) =>
        _appService.CreateAsync(productId, input);
}
