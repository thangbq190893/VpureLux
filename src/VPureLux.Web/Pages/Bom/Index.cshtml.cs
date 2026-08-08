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
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IProductAppService _productAppService;
    private readonly IBomStandardCostLookupService _standardCostLookupService;
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
        IBomStandardCostLookupService standardCostLookupService,
        IAuthorizationService authorizationService)
    {
        _bomAppService = bomAppService;
        _productAppService = productAppService;
        _standardCostLookupService = standardCostLookupService;
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

    public async Task<IActionResult> OnPostEditCurrentAsync(Guid id)
    {
        try
        {
            var result = await _bomAppService.CreateEditableDraftFromCurrentAsync(id);
            return RedirectToPage("/Bom/Edit", new { id = result.NewBomVersionId });
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await SetPermissionsAsync();
            return Page();
        }
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

        var summaries = new List<BomProductSummary>();

        foreach (var product in products.Items)
        {
            var versions = await _bomAppService.GetListAsync(product.Id);
            var currentVersion = versions
                .Where(x => x.Status == BomStatus.Published)
                .OrderByDescending(x => x.VersionNo)
                .FirstOrDefault();

            summaries.Add(new BomProductSummary(
                product.Id,
                product.Code,
                product.Name,
                product.Status.ToString(),
                versions.Count,
                currentVersion));
        }

        var costMap = await _standardCostLookupService.FindMapAsync(
            summaries
                .Where(x => x.CurrentVersion != null)
                .Select(x => x.CurrentVersion!.Id)
                .ToArray());
        var rows = summaries.Select(summary => new BomProductSummaryRow(
                summary.ProductId,
                summary.ProductCode,
                summary.ProductName,
                summary.ProductStatus,
                summary.VersionCount,
                summary.CurrentVersion,
                FormatStandardCost(summary.CurrentVersion, costMap)))
            .ToList();

        return new JsonResult(new PagedResultDto<BomProductSummaryRow>(products.TotalCount, rows));
    }

    private string FormatStandardCost(
        BomVersionDto? currentVersion,
        IReadOnlyDictionary<Guid, BomStandardCostRangeDto> costMap)
    {
        if (currentVersion == null)
        {
            return L["Bom:NoPublishedBom"].Value;
        }

        if (!costMap.TryGetValue(currentVersion.Id, out var cost) || !cost.HasCompleteCost)
        {
            return L["Bom:MissingInventoryCost", cost?.MissingComponentCount ?? 0].Value;
        }

        return BomUi.FormatMoneyRange(cost.MinTotalCost!.Value, cost.MaxTotalCost!.Value);
    }

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Bom.Create)).Succeeded;
    }

    private sealed record BomProductSummary(
        Guid ProductId,
        string ProductCode,
        string ProductName,
        string ProductStatus,
        int VersionCount,
        BomVersionDto? CurrentVersion);

    public sealed record BomProductSummaryRow(
        Guid ProductId,
        string ProductCode,
        string ProductName,
        string ProductStatus,
        int VersionCount,
        BomVersionDto? CurrentVersion,
        string StandardCostRange);
}
