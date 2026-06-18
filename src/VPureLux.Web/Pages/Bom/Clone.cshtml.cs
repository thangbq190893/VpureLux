using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Bom;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.Create)]
public class CloneModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CloneBomVersionDto Input { get; set; } = new() { EffectiveFrom = DateTime.Today };

    [BindProperty]
    public string EffectiveFromText { get; set; } = string.Empty;

    public CloneModel(IBomAppService bomAppService)
    {
        _bomAppService = bomAppService;
    }

    public void OnGet()
    {
        EffectiveFromText = BomUi.FormatDate(Input.EffectiveFrom);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!BomUi.TryParseDate(EffectiveFromText, out var effectiveFrom))
        {
            ModelState.AddModelError(nameof(EffectiveFromText), L["Bom:InvalidDateFormat"]);
            return Page();
        }

        Input.EffectiveFrom = effectiveFrom;
        var result = await _bomAppService.CloneAsync(Id, Input);
        return RedirectToPage("/Bom/Details", new { id = result.NewBomVersionId });
    }
}
