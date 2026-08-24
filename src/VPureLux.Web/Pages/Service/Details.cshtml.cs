using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Service;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.View)]
public class DetailsModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IServiceAppService _service;
    private readonly IAuthorizationService _authorization;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public ServiceOrderDto Order { get; private set; } = new();
    public List<ServicePaymentDto> Payments { get; private set; } = [];
    public bool CanEdit { get; private set; }
    public bool CanConfirm { get; private set; }
    public bool CanComplete { get; private set; }
    public bool CanCancel { get; private set; }
    public bool CanPay { get; private set; }
    public bool CanViewCost { get; private set; }
    public bool CanViewProfit { get; private set; }
    public string StatusBadge => Order.Status switch { ServiceOrderStatus.Completed => "bg-success", ServiceOrderStatus.Cancelled => "bg-secondary", ServiceOrderStatus.InProgress => "bg-warning text-dark", ServiceOrderStatus.Confirmed => "bg-info text-dark", _ => "bg-light text-dark" };

    public DetailsModel(IServiceAppService service, IAuthorizationService authorization) { _service = service; _authorization = authorization; }
    public async Task OnGetAsync() { await LoadAsync(); }
    public async Task<IActionResult> OnPostConfirmAsync() { await _service.ConfirmAsync(Id); return new JsonResult(new { success = true }); }
    public async Task<IActionResult> OnPostStartAsync() { await _service.StartAsync(Id); return new JsonResult(new { success = true }); }
    public async Task<IActionResult> OnPostCancelAsync() { await _service.CancelAsync(Id); return new JsonResult(new { success = true }); }
    public async Task<IActionResult> OnPostVoidPaymentAsync(Guid paymentId) { await _service.VoidPaymentAsync(Id, paymentId); return new JsonResult(new { success = true }); }
    public string FormatDate(DateTime value) => value.ToString("dd/MM/yyyy", Vi);
    public string FormatDateTime(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm", Vi) ?? "-";
    public string FormatMoney(decimal value) => value.ToString("N0", Vi);

    private async Task LoadAsync()
    {
        Order = await _service.GetAsync(Id);
        Payments = await _service.GetPaymentsAsync(Id);
        CanEdit = await Granted(VPureLuxPermissions.Service.Edit); CanConfirm = await Granted(VPureLuxPermissions.Service.Confirm);
        CanComplete = await Granted(VPureLuxPermissions.Service.Complete); CanCancel = await Granted(VPureLuxPermissions.Service.Cancel);
        CanPay = await Granted(VPureLuxPermissions.Service.ManagePayments); CanViewCost = await Granted(VPureLuxPermissions.Service.ViewCost);
        CanViewProfit = await Granted(VPureLuxPermissions.Service.ViewProfit);
    }
    private async Task<bool> Granted(string permission) => (await _authorization.AuthorizeAsync(User, permission)).Succeeded;
}
