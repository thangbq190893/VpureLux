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

    public IReadOnlyList<BomProductSummaryRow> Rows { get; private set; } = Array.Empty<BomProductSummaryRow>();
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
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostOpenProductAsync()
    {
        if (!ProductId.HasValue)
        {
            ModelState.AddModelError(nameof(ProductId), L["Bom:SelectProduct"]);
            await LoadPageAsync();
            return Page();
        }

        return RedirectToPage("/Bom/Product", new { productId = ProductId.Value });
    }

    private async Task LoadPageAsync()
    {
        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            Keyword = SearchTerm,
            MaxResultCount = 1000
        });

        var filteredProducts = products.Items
            .Where(MatchesSearch)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .ToList();

        var rows = new List<BomProductSummaryRow>();

        foreach (var product in filteredProducts)
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
                product.Status,
                versions.Count,
                currentVersion));
        }

        Rows = rows;
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Bom.Create)).Succeeded;
    }

    private bool MatchesSearch(ProductDto product)
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            return true;
        }

        return product.Code.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
               product.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase);
    }

    public sealed record BomProductSummaryRow(
        Guid ProductId,
        string ProductCode,
        string ProductName,
        CatalogItemStatus ProductStatus,
        int VersionCount,
        BomVersionDto? CurrentVersion);
}
