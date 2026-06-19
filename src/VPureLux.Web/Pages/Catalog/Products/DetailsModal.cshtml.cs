using System;
using System.Threading.Tasks;
using global::VPureLux.Catalog.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Products;

[Authorize(VPureLuxPermissions.Catalog.Products.View)]
public class DetailsModalModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public ProductDto Product { get; private set; } = new();

    public DetailsModalModel(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task OnGetAsync()
    {
        Product = await _productAppService.GetAsync(Id);
    }
}
