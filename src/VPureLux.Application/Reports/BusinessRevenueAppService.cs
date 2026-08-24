using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Reports;

public class BusinessRevenueAppService : ApplicationService, IBusinessRevenueAppService
{
    private readonly IBusinessRevenueReadRepository _repository;
    public BusinessRevenueAppService(IBusinessRevenueReadRepository repository) => _repository = repository;

    [Authorize(VPureLuxPermissions.Reports.Service.View)]
    public async Task<PagedResultDto<BusinessRevenueRowDto>> GetServiceListAsync(GetBusinessRevenueListInput input)
    {
        input.Source = BusinessRevenueSource.Service;
        var result = await GetListInternalAsync(input);
        await HideCostsUnlessGrantedAsync(result, VPureLuxPermissions.Service.ViewCost, VPureLuxPermissions.Service.ViewProfit);
        return result;
    }

    [Authorize(VPureLuxPermissions.Reports.Service.View)]
    public async Task<BusinessRevenueSummaryDto> GetServiceSummaryAsync(GetBusinessRevenueListInput input)
    {
        input.Source = BusinessRevenueSource.Service;
        var result = await GetSummaryInternalAsync(input);
        await HideCostsUnlessGrantedAsync(result, VPureLuxPermissions.Service.ViewCost, VPureLuxPermissions.Service.ViewProfit);
        return result;
    }

    [Authorize(VPureLuxPermissions.Reports.Consolidated.View)]
    public async Task<PagedResultDto<BusinessRevenueRowDto>> GetConsolidatedListAsync(GetBusinessRevenueListInput input)
    {
        var result = await GetListInternalAsync(input);
        await HideCostsUnlessGrantedAsync(result, VPureLuxPermissions.Reports.Profit.View, VPureLuxPermissions.Reports.Profit.View);
        return result;
    }

    [Authorize(VPureLuxPermissions.Reports.Consolidated.View)]
    public async Task<BusinessRevenueSummaryDto> GetConsolidatedSummaryAsync(GetBusinessRevenueListInput input)
    {
        var result = await GetSummaryInternalAsync(input);
        await HideCostsUnlessGrantedAsync(result, VPureLuxPermissions.Reports.Profit.View, VPureLuxPermissions.Reports.Profit.View);
        return result;
    }

    private async Task<PagedResultDto<BusinessRevenueRowDto>> GetListInternalAsync(GetBusinessRevenueListInput input)
    {
        var toExclusive = input.ToDate?.Date.AddDays(1);
        var count = await _repository.GetCountAsync(input, toExclusive);
        return new PagedResultDto<BusinessRevenueRowDto>(count, await _repository.GetListAsync(input, toExclusive));
    }

    private Task<BusinessRevenueSummaryDto> GetSummaryInternalAsync(GetBusinessRevenueListInput input) =>
        _repository.GetSummaryAsync(input, input.ToDate?.Date.AddDays(1));

    private async Task HideCostsUnlessGrantedAsync(
        PagedResultDto<BusinessRevenueRowDto> result,
        string costPermission,
        string profitPermission)
    {
        var canViewCost = await AuthorizationService.IsGrantedAsync(costPermission);
        var canViewProfit = await AuthorizationService.IsGrantedAsync(profitPermission);
        foreach (var row in result.Items)
        {
            if (!canViewCost) row.Cost = 0;
            if (!canViewProfit) row.Profit = 0;
        }
    }

    private async Task HideCostsUnlessGrantedAsync(
        BusinessRevenueSummaryDto result,
        string costPermission,
        string profitPermission)
    {
        if (!await AuthorizationService.IsGrantedAsync(costPermission)) result.TotalCost = 0;
        if (!await AuthorizationService.IsGrantedAsync(profitPermission)) result.TotalProfit = 0;
    }
}
