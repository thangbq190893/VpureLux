using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VPureLux.Web.Pages.Catalog;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Products;

[Authorize(VPureLuxPermissions.Catalog.Products.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public UpdateProductDto Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Image { get; set; }

    public bool HasImage { get; private set; }

    public EditModel(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task OnGetAsync()
    {
        var product = await _productAppService.GetAsync(Id);
        Code = product.Code;
        HasImage = product.HasImage;
        Input = new UpdateProductDto
        {
            Name = product.Name,
            Description = product.Description
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _productAppService.UpdateAsync(Id, Input);
        var image = await CatalogImageUploadHelper.ToDtoAsync(Image);
        if (image != null)
        {
            await _productAppService.SetImageAsync(Id, image);
        }

        return RedirectToPage("/Catalog/Products/Index");
    }

    public async Task<IActionResult> OnPostRemoveImageAsync()
    {
        await _productAppService.RemoveImageAsync(Id);
        TempData["CatalogImageSuccessMessage"] = "Catalog:ImageRemovedSuccessfully";
        return RedirectToPage(new { id = Id });
    }
}
