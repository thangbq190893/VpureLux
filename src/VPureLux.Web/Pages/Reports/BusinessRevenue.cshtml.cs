using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Reports;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Reports;

[Authorize(VPureLuxPermissions.Reports.Consolidated.View)]
public class BusinessRevenueModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IBusinessRevenueAppService _service;
    public BusinessRevenuePageViewModel View { get; private set; } = new();
    public BusinessRevenueModel(IBusinessRevenueAppService service) => _service = service;
    public void OnGet() { var today = Clock.Now.Date; View.FromDate = new DateTime(today.Year, today.Month, 1).ToString("yyyy-MM-dd"); View.ToDate = today.ToString("yyyy-MM-dd"); }
    public async Task<JsonResult> OnGetListAsync(GetBusinessRevenueListInput input) => ToJson(await _service.GetConsolidatedListAsync(input));
    public async Task<JsonResult> OnGetSummaryAsync(GetBusinessRevenueListInput input) => new(await _service.GetConsolidatedSummaryAsync(input));
    private JsonResult ToJson(PagedResultDto<BusinessRevenueRowDto> result) => new(new PagedResultDto<object>(result.TotalCount, result.Items.Select(x => (object)new { x.DocumentId, x.DocumentNo, DocumentDate = x.DocumentDate.ToString("dd/MM/yyyy", Vi), Source = x.Source.ToString(), SourceLabel = L[$"Reports:RevenueSource:{x.Source}"].Value, Customer = $"{x.CustomerCode} - {x.CustomerName}", Revenue = Money(x.Revenue), Cost = Money(x.Cost), Profit = Money(x.Profit), Paid = Money(x.Paid), Remaining = Money(x.Remaining) }).ToList()));
    private static string Money(decimal value) => value.ToString("N0", Vi);
}
