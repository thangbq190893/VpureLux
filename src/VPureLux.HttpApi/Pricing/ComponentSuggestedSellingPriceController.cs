using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Pricing;

[Route("api/pricing/components/{componentId:guid}/suggested-selling-prices")]
public class ComponentSuggestedSellingPriceController : AbpControllerBase
{
    private readonly IComponentSuggestedSellingPriceAppService _appService;

    public ComponentSuggestedSellingPriceController(IComponentSuggestedSellingPriceAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public Task<List<ComponentSuggestedSellingPriceVersionDto>> GetHistoryAsync(Guid componentId) =>
        _appService.GetHistoryAsync(componentId);

    [HttpGet("current")]
    public Task<ComponentSuggestedSellingPriceVersionDto> GetCurrentAsync(Guid componentId) =>
        _appService.GetCurrentAsync(componentId);

    [HttpGet("at-date")]
    public Task<ComponentSuggestedSellingPriceVersionDto> GetAtDateAsync(Guid componentId, [FromQuery] DateTime date) =>
        _appService.GetAtDateAsync(componentId, date);

    [HttpPost]
    public Task<ComponentSuggestedSellingPriceVersionDto> CreateAsync(
        Guid componentId,
        [FromBody] CreateComponentSuggestedSellingPriceVersionDto input) =>
        _appService.CreateAsync(componentId, input);
}
