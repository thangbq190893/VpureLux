using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Sales;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.ManageRefunds)]
public class VoidPaymentModalModel : VPureLuxPageModel
{
    private readonly ISalesPostConfirmationAppService _postConfirmation;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public ReasonDto Input { get; set; } = new();

    public VoidPaymentModalModel(ISalesPostConfirmationAppService postConfirmation) =>
        _postConfirmation = postConfirmation;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _postConfirmation.VoidPaymentAsync(Id, Input);
        return NoContent();
    }
}
