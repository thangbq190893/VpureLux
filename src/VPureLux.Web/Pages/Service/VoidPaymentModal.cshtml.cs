using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.ManagePayments)]
public class VoidPaymentModalModel(IServicePaymentAppService money, IOptions<ServiceOptions> options) : VPureLuxPageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public VoidServicePaymentDto Input { get; set; } = new();

    public IActionResult OnGet(Guid paymentId, string concurrencyStamp)
    {
        if (!options.Value.IsEnabled) return NotFound();
        Input = new() { PaymentId = paymentId, ConcurrencyStamp = concurrencyStamp, IdempotencyKey = Guid.NewGuid().ToString("N") };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!options.Value.IsEnabled) return NotFound();
        if (!ModelState.IsValid) return Page();
        try { await money.VoidPaymentAsync(Id, Input); return NoContent(); }
        catch (BusinessException e)
        {
            ModelState.AddModelError(string.Empty, L[e.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]);
            return Page();
        }
    }
}
