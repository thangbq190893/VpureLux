using System.Threading.Tasks;
using global::VPureLux.Catalog.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Products;

[Authorize(VPureLuxPermissions.Catalog.Products.Create)]
public class CreateModalModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;

    [BindProperty] public CreateProductDto Input { get; set; } = new();

    public CreateModalModel(IProductAppService productAppService)
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

        await _productAppService.CreateAsync(Input);
        return NoContent();
    }
}
