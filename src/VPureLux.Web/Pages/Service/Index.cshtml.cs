using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.View)]
public class IndexModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IServiceOrderAppService _service;
    private readonly IAuthorizationService _authorization;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;
    private readonly IOptions<ServiceOptions> _options;

    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanManageWorks { get; private set; }

    public IndexModel(
        IServiceOrderAppService service,
        IAuthorizationService authorization,
        IStringLocalizer<VPureLuxResource> localizer,
        IOptions<ServiceOptions> options)
    {
        _service = service;
        _authorization = authorization;
        _localizer = localizer;
        _options = options;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        CanCreate = await GrantedAsync(VPureLuxPermissions.Service.Create);
        CanEdit = await GrantedAsync(VPureLuxPermissions.Service.Edit);
        CanManageWorks = await GrantedAsync(VPureLuxPermissions.Service.ManageWorks);
        return Page();
    }

    public async Task<JsonResult> OnGetListAsync(GetServiceOrderListInput input)
    {
        var canEdit = await GrantedAsync(VPureLuxPermissions.Service.Edit);
        var result = await _service.GetListAsync(input);
        return new JsonResult(new PagedResultDto<ServiceOrderRow>(result.TotalCount, result.Items.Select(order =>
            new ServiceOrderRow(
                order.Id,
                order.OrderNo,
                order.OrderDate.ToString("dd/MM/yyyy", Vi),
                $"{order.CustomerCode} - {order.CustomerName}",
                $"{order.AssetNo} - {order.AssetName}",
                order.ScheduledAt?.ToString("dd/MM/yyyy HH:mm", Vi) ?? "-",
                _localizer[$"Service:Status:{order.Status}"].Value,
                GetBadge(order.Status),
                FormatMoney(order.PlannedAmount),
                canEdit && order.Status == ServiceOrderStatus.Draft)).ToList()));
    }

    private async Task<bool> GrantedAsync(string permission) =>
        (await _authorization.AuthorizeAsync(User, permission)).Succeeded;

    private static string FormatMoney(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero).ToString("#,0", Vi) + " ₫";

    private static string GetBadge(ServiceOrderStatus status) => status switch
    {
        ServiceOrderStatus.Completed => "text-bg-success",
        ServiceOrderStatus.Cancelled => "text-bg-secondary",
        ServiceOrderStatus.InProgress => "text-bg-warning",
        ServiceOrderStatus.Confirmed => "text-bg-info",
        _ => "text-bg-light"
    };

    public sealed record ServiceOrderRow(
        Guid Id,
        string OrderNo,
        string OrderDate,
        string Customer,
        string Asset,
        string ScheduledAt,
        string StatusLabel,
        string StatusBadge,
        string PlannedAmount,
        bool CanEdit);
}
