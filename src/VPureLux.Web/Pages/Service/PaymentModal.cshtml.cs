using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Sales;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.ManagePayments)]
public class PaymentModalModel : VPureLuxPageModel
{
    private readonly IServiceAppService _service;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CreateServicePaymentDto Input { get; set; } = new();
    public PaymentModalModel(IServiceAppService service) => _service = service;
    public void OnGet() { Input.PaymentDate = Clock.Now.Date; Input.PaymentMethod = SalesPaymentMethod.Cash; Input.IdempotencyKey = Guid.NewGuid().ToString("N"); }
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); try { await _service.AddPaymentAsync(Id, Input); return NoContent(); } catch (BusinessException exception) { ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]); return Page(); } }
}
