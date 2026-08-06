using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IProductAppService _productAppService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty]
    [Required]
    public Guid? ProductId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public bool CanCreate { get; private set; }

    public IndexModel(
        IBomAppService bomAppService,
        IProductAppService productAppService,
        IAuthorizationService authorizationService)
    {
        _bomAppService = bomAppService;
        _productAppService = productAppService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await SetPermissionsAsync();
    }

    public async Task<IActionResult> OnPostOpenProductAsync()
    {
        if (!ProductId.HasValue)
        {
            ModelState.AddModelError(nameof(ProductId), L["Bom:SelectProduct"]);
            await SetPermissionsAsync();
            return Page();
        }

        return RedirectToPage("/Bom/Product", new { productId = ProductId.Value });
    }

    public async Task<JsonResult> OnGetListAsync(GetProductListInput input)
    {
        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            Keyword = input.Keyword,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        });

        var rows = new List<BomProductSummaryRow>();

        foreach (var product in products.Items)
        {
            var versions = await _bomAppService.GetListAsync(product.Id);
            var currentVersion = versions
                .Where(x => x.Status == BomStatus.Published)
                .OrderByDescending(x => x.VersionNo)
                .FirstOrDefault();

            rows.Add(new BomProductSummaryRow(
                product.Id,
                product.Code,
                product.Name,
                product.Status.ToString(),
                versions.Count,
                currentVersion));
        }

        return new JsonResult(new PagedResultDto<BomProductSummaryRow>(products.TotalCount, rows));
    }

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Bom.Create)).Succeeded;
    }

    public sealed record BomProductSummaryRow(
        Guid ProductId,
        string ProductCode,
        string ProductName,
        string ProductStatus,
        int VersionCount,
        BomVersionDto? CurrentVersion);
}
