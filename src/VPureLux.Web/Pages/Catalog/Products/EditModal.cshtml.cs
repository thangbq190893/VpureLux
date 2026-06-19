using System;
using System.Threading.Tasks;
using global::VPureLux.Catalog.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Products;

[Authorize(VPureLuxPermissions.Catalog.Products.Edit)]
public class EditModalModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public string Code { get; set; } = string.Empty;
    [BindProperty] public UpdateProductDto Input { get; set; } = new();

    public EditModalModel(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task OnGetAsync()
    {
        var product = await _productAppService.GetAsync(Id);
        Code = product.Code;
        Input = new UpdateProductDto
        {
            Name = product.Name,
            Description = product.Description
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _productAppService.UpdateAsync(Id, Input);
        return NoContent();
    }
}
