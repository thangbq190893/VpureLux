using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.View)]
public class IndexModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IServiceAppService _service;
    private readonly IAuthorizationService _authorization;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    public bool CanCreate { get; private set; }
    public bool CanCancel { get; private set; }
    public bool CanManageWorks { get; private set; }

    public IndexModel(IServiceAppService service, IAuthorizationService authorization, IStringLocalizer<VPureLuxResource> localizer)
    {
        _service = service;
        _authorization = authorization;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        CanCreate = await IsGrantedAsync(VPureLuxPermissions.Service.Create);
        CanCancel = await IsGrantedAsync(VPureLuxPermissions.Service.Cancel);
        CanManageWorks = await IsGrantedAsync(VPureLuxPermissions.Service.ManageWorks);
    }

    public async Task<JsonResult> OnGetListAsync(GetServiceOrderListInput input)
    {
        var result = await _service.GetListAsync(input);
        return new JsonResult(new PagedResultDto<ServiceOrderRow>(result.TotalCount, result.Items.Select(x => new ServiceOrderRow(
            x.Id, x.OrderNo, x.OrderDate.ToString("dd/MM/yyyy", Vi),
            $"{x.CustomerCode} - {x.CustomerName}", $"{x.AssetNo} - {x.AssetName}",
            _localizer[$"Service:Status:{x.Status}"].Value, GetBadge(x.Status), x.Status,
            x.TotalRevenueAmount.ToString("N0", Vi), x.PaidAmount.ToString("N0", Vi), x.RemainingAmount.ToString("N0", Vi))).ToList()));
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        await _service.CancelAsync(id);
        return new JsonResult(new { success = true });
    }

    private async Task<bool> IsGrantedAsync(string permission) =>
        (await _authorization.AuthorizeAsync(User, permission)).Succeeded;

    private static string GetBadge(ServiceOrderStatus status) => status switch
    {
        ServiceOrderStatus.Completed => "bg-success",
        ServiceOrderStatus.Cancelled => "bg-secondary",
        ServiceOrderStatus.InProgress => "bg-warning text-dark",
        ServiceOrderStatus.Confirmed => "bg-info text-dark",
        _ => "bg-light text-dark"
    };

    public sealed record ServiceOrderRow(Guid Id, string OrderNo, string OrderDate, string Customer, string Asset,
        string StatusLabel, string StatusBadge, ServiceOrderStatus Status, string Revenue, string Paid, string Remaining);
}
