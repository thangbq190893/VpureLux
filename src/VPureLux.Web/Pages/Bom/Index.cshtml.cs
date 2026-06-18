using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;

    [BindProperty]
    [Required]
    public Guid? ProductId { get; set; }

    public List<SelectListItem> ProductOptions { get; private set; } = new();

    public IndexModel(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task OnGetAsync()
    {
        await LoadProductOptionsAsync();
    }

    public async Task<IActionResult> OnPostOpenProductAsync()
    {
        await LoadProductOptionsAsync();
        if (!ProductId.HasValue)
        {
            ModelState.AddModelError(nameof(ProductId), L["Bom:SelectProduct"]);
            return Page();
        }

        return RedirectToPage("/Bom/Product", new { productId = ProductId.Value });
    }

    private async Task LoadProductOptionsAsync()
    {
        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            MaxResultCount = 1000
        });
        ProductOptions = products.Items
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }
}
