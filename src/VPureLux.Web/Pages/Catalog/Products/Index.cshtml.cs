using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Products;

public class IndexModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    public IReadOnlyList<ProductDto> Products { get; private set; } = Array.Empty<ProductDto>();
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }

    public IndexModel(IProductAppService productAppService, IAuthorizationService authorizationService)
    {
        _productAppService = productAppService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        var result = await _productAppService.GetListAsync(new GetProductListInput
        {
            Keyword = Keyword,
            MaxResultCount = 100
        });

        Products = result.Items;
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Products.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Products.Edit)).Succeeded;
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _productAppService.DeactivateAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _productAppService.ActivateAsync(id);
        return RedirectToPage();
    }
}
