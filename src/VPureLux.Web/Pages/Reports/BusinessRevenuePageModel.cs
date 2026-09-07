using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Reports;

public abstract class BusinessRevenuePageModel(IBusinessRevenueAppService reports, IAuthorizationService authorization,
    IOptions<ServiceOptions> options) : VPureLuxPageModel
{
    public abstract bool ServiceOnly { get; }
    public bool ShowSales { get; private set; }
    public bool ShowService { get; private set; }
    public bool ShowCost { get; private set; }
    public bool ShowProfit { get; private set; }
    public bool LinkSales { get; private set; }
    public bool LinkService { get; private set; }
    public string FromDateText { get; private set; } = "";
    public string ToDateText { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        ShowSales = !ServiceOnly && await Granted(VPureLuxPermissions.Reports.Sales.View);
        ShowService = options.Value.IsEnabled && await Granted(VPureLuxPermissions.Reports.Service.View);
        if (ServiceOnly && !options.Value.IsEnabled) return NotFound();
        if ((!ShowSales && !ShowService) || (ServiceOnly && !ShowService)) return Forbid();
        var salesCost = ShowSales && await Granted(VPureLuxPermissions.Sales.ViewCost) && await Granted(VPureLuxPermissions.Reports.Profit.View);
        var serviceCost = ShowService && await Granted(VPureLuxPermissions.Service.ViewCost);
        ShowCost = salesCost || serviceCost;
        ShowProfit = (salesCost && await Granted(VPureLuxPermissions.Sales.ViewProfit)) ||
                     (serviceCost && await Granted(VPureLuxPermissions.Service.ViewProfit));
        LinkSales = await Granted(VPureLuxPermissions.Sales.View);
        LinkService = await Granted(VPureLuxPermissions.Service.View);
        var today = DateTime.UtcNow.AddHours(7).Date;
        FromDateText = ReportDateParser.ToInputValue(new DateTime(today.Year, today.Month, 1));
        ToDateText = ReportDateParser.ToInputValue(today);
        return Page();
    }

    public async Task<JsonResult> OnGetListAsync(GetBusinessRevenueListInput input, string? fromDateText, string? toDateText)
    {
        Dates(input, fromDateText, toDateText);
        return new JsonResult(ServiceOnly ? await reports.GetServiceListAsync(input) : await reports.GetConsolidatedListAsync(input));
    }

    public async Task<JsonResult> OnGetSummaryAsync(GetBusinessRevenueListInput input, string? fromDateText, string? toDateText)
    {
        Dates(input, fromDateText, toDateText);
        return new JsonResult(ServiceOnly ? await reports.GetServiceSummaryAsync(input) : await reports.GetConsolidatedSummaryAsync(input));
    }

    private void Dates(GetBusinessRevenueListInput input, string? from, string? to)
    {
        if (!ModelState.IsValid || !ReportDateParser.TryParseRange(from, to, out var start, out var end))
            throw new UserFriendlyException(L["Reports:DateRangeInvalid"]);
        input.FromDate = start; input.ToDate = end;
    }
    private async Task<bool> Granted(string permission) => (await authorization.AuthorizeAsync(User, permission)).Succeeded;
}
