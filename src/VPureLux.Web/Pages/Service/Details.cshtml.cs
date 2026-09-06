using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.View)]
public class DetailsModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IServiceOrderAppService _service;
    private readonly IAuthorizationService _authorization;
    private readonly IOptions<ServiceOptions> _options;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public ServiceOrderDto Order { get; private set; } = new();
    public bool CanEdit { get; private set; }
    public bool CanConfirm { get; private set; }
    public bool CanCancel { get; private set; }
    public string StatusBadge => Order.Status switch
    {
        ServiceOrderStatus.Completed => "text-bg-success",
        ServiceOrderStatus.Cancelled => "text-bg-secondary",
        ServiceOrderStatus.InProgress => "text-bg-warning",
        ServiceOrderStatus.Confirmed => "text-bg-info",
        _ => "text-bg-light"
    };

    public DetailsModel(
        IServiceOrderAppService service,
        IAuthorizationService authorization,
        IOptions<ServiceOptions> options,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _service = service;
        _authorization = authorization;
        _options = options;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(string concurrencyStamp) =>
        await RunTransitionAsync(() => _service.ConfirmAsync(Id, new ServiceOrderTransitionDto { ConcurrencyStamp = concurrencyStamp }));

    public async Task<IActionResult> OnPostStartAsync(string concurrencyStamp) =>
        await RunTransitionAsync(() => _service.StartAsync(Id, new ServiceOrderTransitionDto { ConcurrencyStamp = concurrencyStamp }));

    public string FormatDate(DateTime value) => value.ToString("dd/MM/yyyy", Vi);
    public string FormatDateTime(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm", Vi) ?? "-";
    public string FormatMoney(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero).ToString("#,0", Vi) + " ₫";

    private async Task<IActionResult> RunTransitionAsync(Func<Task<ServiceOrderDto>> action)
    {
        if (!_options.Value.IsEnabled) return NotFound();
        try
        {
            var order = await action();
            return new JsonResult(new { success = true, concurrencyStamp = order.ConcurrencyStamp });
        }
        catch (BusinessException exception)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return new JsonResult(new { error = new { message = _localizer[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed].Value } });
        }
    }

    private async Task LoadAsync()
    {
        Order = await _service.GetAsync(Id);
        CanEdit = await GrantedAsync(VPureLuxPermissions.Service.Edit);
        CanConfirm = await GrantedAsync(VPureLuxPermissions.Service.Confirm);
        CanCancel = await GrantedAsync(VPureLuxPermissions.Service.Cancel);
    }

    private async Task<bool> GrantedAsync(string permission) =>
        (await _authorization.AuthorizeAsync(User, permission)).Succeeded;
}
