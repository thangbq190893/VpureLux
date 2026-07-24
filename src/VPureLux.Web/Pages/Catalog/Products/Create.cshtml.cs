using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VPureLux;
using VPureLux.Web.Pages.Catalog;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using Volo.Abp;

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
        if (!ModelState.IsValid)
        {
            return Page();
        }

        ProductDto product;
        try
        {
            product = await _productAppService.CreateAsync(Input);
        }
        catch (BusinessException exception) when (exception.Code is VPureLuxDomainErrorCodes.ProductCodeRequired or VPureLuxDomainErrorCodes.ProductCodeAlreadyExists)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Code)}", L[exception.Code].Value);
            return Page();
        }

        var image = await CatalogImageUploadHelper.ToDtoAsync(Image);
        if (image != null)
        {
            await _productAppService.SetImageAsync(product.Id, image);
        }

        return RedirectToPage("/Catalog/Products/Index");
    }
}
