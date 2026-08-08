using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Suppliers;
using Volo.Abp;

namespace VPureLux.Web.Pages.Suppliers;

[Authorize(VPureLuxPermissions.Suppliers.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly ISupplierAppService _appService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateSupplierDto Input { get; set; } = new();

    public string Code { get; private set; } = string.Empty;

    public EditModel(ISupplierAppService appService)
    {
        _appService = appService;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCodeAsync();
            return Page();
        }

        try
        {
            await _appService.UpdateAsync(Id, Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadCodeAsync();
            return Page();
        }

        TempData["StatusMessageKey"] = "Suppliers:UpdatedSuccessfully";
        return RedirectToPage("/Suppliers/Index");
    }

    private async Task LoadAsync()
    {
        var supplier = await _appService.GetAsync(Id);
        Code = supplier.Code;
        Input = new UpdateSupplierDto
        {
            Name = supplier.Name,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            TaxCode = supplier.TaxCode,
            Address = supplier.Address,
            Note = supplier.Note
        };
    }

    private async Task LoadCodeAsync()
    {
        Code = (await _appService.GetAsync(Id)).Code;
    }
}
