using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Warranty;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.ManagePolicies)]
public class PolicyModalModel : VPureLuxPageModel
{
    private readonly IWarrantyAppService _warrantyAppService;

    [BindProperty(SupportsGet = true)]
    public Guid ComponentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ComponentCode { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ComponentName { get; set; } = string.Empty;

    [BindProperty]
    public SetComponentReplacementPolicyDto Input { get; set; } = new();

    public PolicyModalModel(IWarrantyAppService warrantyAppService)
    {
        _warrantyAppService = warrantyAppService;
    }

    public async Task OnGetAsync()
    {
        var policy = await _warrantyAppService.GetPolicyByComponentIdAsync(ComponentId);
        if (policy != null)
        {
            Input = new SetComponentReplacementPolicyDto
            {
                IsEnabled = policy.IsEnabled,
                CycleMonths = policy.CycleMonths,
                WarningDaysBeforeDue = policy.WarningDaysBeforeDue,
                Note = policy.Note
            };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _warrantyAppService.SetPolicyAsync(ComponentId, Input);
        return NoContent();
    }
}
