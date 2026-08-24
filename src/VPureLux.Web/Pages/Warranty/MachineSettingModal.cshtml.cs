using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Warranty;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.ManageMachines)]
public class MachineSettingModalModel : VPureLuxPageModel
{
    private readonly IWarrantyAppService _warrantyAppService;

    [BindProperty(SupportsGet = true)]
    public Guid ProductId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ProductCode { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string ProductName { get; set; } = string.Empty;

    [BindProperty]
    public SetProductMachineSettingDto Input { get; set; } = new();

    public MachineSettingModalModel(IWarrantyAppService warrantyAppService)
    {
        _warrantyAppService = warrantyAppService;
    }

    public async Task OnGetAsync()
    {
        var setting = await _warrantyAppService.GetMachineSettingByProductIdAsync(ProductId);
        Input = new SetProductMachineSettingDto
        {
            IsMachine = setting?.IsMachine ?? false,
            Note = setting?.Note
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _warrantyAppService.SetMachineSettingAsync(ProductId, Input);
        return NoContent();
    }
}
