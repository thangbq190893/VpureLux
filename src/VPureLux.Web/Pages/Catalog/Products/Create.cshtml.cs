using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VPureLux.Web.Pages.Catalog;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Products;

[Authorize(VPureLuxPermissions.Catalog.Products.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;

    [BindProperty]
    public CreateProductDto Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Image { get; set; }

    public CreateModel(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var product = await _productAppService.CreateAsync(Input);
        var image = await CatalogImageUploadHelper.ToDtoAsync(Image);
        if (image != null)
        {
            await _productAppService.SetImageAsync(product.Id, image);
        }

        return RedirectToPage("/Catalog/Products/Index");
    }
}
