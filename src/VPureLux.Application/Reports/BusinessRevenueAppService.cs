using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;

namespace VPureLux.Reports;

public class BusinessRevenueAppService(IBusinessRevenueReadRepository repository, IOptions<ServiceOptions> options)
    : ApplicationService, IBusinessRevenueAppService
{
    [Authorize(VPureLuxPermissions.Reports.Service.View)]
    public Task<PagedResultDto<BusinessRevenueRowDto>> GetServiceListAsync(GetBusinessRevenueListInput input) => ListAsync(input, true);
    [Authorize(VPureLuxPermissions.Reports.Service.View)]
    public Task<BusinessRevenueSummaryDto> GetServiceSummaryAsync(GetBusinessRevenueListInput input) => SummaryAsync(input, true);
    [Authorize(VPureLuxPermissions.Reports.Consolidated.View)]
    public Task<PagedResultDto<BusinessRevenueRowDto>> GetConsolidatedListAsync(GetBusinessRevenueListInput input) => ListAsync(input, false);
    [Authorize(VPureLuxPermissions.Reports.Consolidated.View)]
    public Task<BusinessRevenueSummaryDto> GetConsolidatedSummaryAsync(GetBusinessRevenueListInput input) => SummaryAsync(input, false);

    private async Task<(DateTime End, bool SalesCost, bool SalesProfit, bool ServiceCost, bool ServiceProfit)> PrepareAsync(
        GetBusinessRevenueListInput input, bool serviceOnly)
    {
        if (serviceOnly) input.Source = BusinessRevenueSource.Service;
        if (input.Source.HasValue && !Enum.IsDefined(input.Source.Value)) throw new UserFriendlyException(L["Reports:InvalidSource"]);
        if (input.Source != BusinessRevenueSource.Sales)
        {
            if (!options.Value.IsEnabled) throw new BusinessException(ServiceErrorCodes.Disabled);
            await AuthorizationService.CheckAsync(VPureLuxPermissions.Reports.Service.View);
        }
        if (input.Source != BusinessRevenueSource.Service) await AuthorizationService.CheckAsync(VPureLuxPermissions.Reports.Sales.View);
        var today = Clock.Now.AddHours(7).Date;
        input.FromDate = (input.FromDate ?? input.ToDate ?? new DateTime(today.Year, today.Month, 1)).Date;
        input.ToDate = (input.ToDate ?? today).Date;
        if (input.FromDate > input.ToDate || input.ToDate >= DateTime.MaxValue.Date || input.FromDate < new DateTime(2, 1, 1))
            throw new UserFriendlyException(L["Reports:DateRangeInvalid"]);
        input.SearchText = input.SearchText?.Trim();
        var sc = await AuthorizationService.IsGrantedAsync(VPureLuxPermissions.Sales.ViewCost) &&
                 await AuthorizationService.IsGrantedAsync(VPureLuxPermissions.Reports.Profit.View);
        var sp = sc && await AuthorizationService.IsGrantedAsync(VPureLuxPermissions.Sales.ViewProfit);
        var vc = await AuthorizationService.IsGrantedAsync(VPureLuxPermissions.Service.ViewCost);
        var vp = vc && await AuthorizationService.IsGrantedAsync(VPureLuxPermissions.Service.ViewProfit);
        // Do not expose restricted values indirectly through order-by comparisons.
        var sort = input.Sorting?.Trim().ToLowerInvariant() ?? "";
        if ((sort.StartsWith("totalknowncost") && !All(input.Source, sc, vc)) ||
            (sort.StartsWith("profit") && !All(input.Source, sp, vp))) input.Sorting = null;
        return (input.ToDate.Value.AddDays(1), sc, sp, vc, vp);
    }

    private async Task<PagedResultDto<BusinessRevenueRowDto>> ListAsync(GetBusinessRevenueListInput input, bool serviceOnly)
    {
        var p = await PrepareAsync(input, serviceOnly);
        var count = await repository.GetCountAsync(input, p.End);
        var rows = await repository.GetListAsync(input, p.End);
        foreach (var row in rows)
        {
            var cost = row.Source == BusinessRevenueSource.Sales ? p.SalesCost : p.ServiceCost;
            var profit = row.Source == BusinessRevenueSource.Sales ? p.SalesProfit : p.ServiceProfit;
            if (!cost) { row.MaterialCost = row.LaborCost = row.TotalKnownCost = null; row.LaborCostKnown = row.CostIncomplete = null; }
            if (!profit) row.Profit = null;
        }
        return new PagedResultDto<BusinessRevenueRowDto>(count, rows);
    }

    private async Task<BusinessRevenueSummaryDto> SummaryAsync(GetBusinessRevenueListInput input, bool serviceOnly)
    {
        var p = await PrepareAsync(input, serviceOnly);
        var result = await repository.GetSummaryAsync(input, p.End);
        if (!All(input.Source, p.SalesCost, p.ServiceCost))
        { result.KnownMaterialCost = result.KnownLaborCost = result.TotalKnownCost = null; result.CostIncompleteCount = null; }
        if (!All(input.Source, p.SalesProfit, p.ServiceProfit)) result.Profit = null;
        return result;
    }

    private static bool All(BusinessRevenueSource? source, bool sales, bool service) =>
        source == BusinessRevenueSource.Sales ? sales : source == BusinessRevenueSource.Service ? service : sales && service;
}
