using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IComponentAppService _componentAppService;
    private readonly IProductAppService _productAppService;
    private readonly IProductPricingContextLookupService _productPricingContextLookupService;

    [BindProperty(SupportsGet = true)]
    public Guid ProductId { get; set; }

    [BindProperty]
    public string EffectiveFromText { get; set; } = string.Empty;

    [BindProperty]
    public List<BomItemSelectionModel> Items { get; set; } = new() { new() };

    public List<SelectListItem> ComponentOptions { get; private set; } = new();
    public ProductDto? Product { get; private set; }
    public ProductPricingContextDto? PricingContext { get; private set; }

    public CreateModel(
        IBomAppService bomAppService,
        IComponentAppService componentAppService,
        IProductAppService productAppService,
        IProductPricingContextLookupService productPricingContextLookupService)
    {
        _bomAppService = bomAppService;
        _componentAppService = componentAppService;
        _productAppService = productAppService;
        _productPricingContextLookupService = productPricingContextLookupService;
    }

    public async Task OnGetAsync()
    {
        EffectiveFromText = BomUi.FormatDate(DateTime.Today);
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadPageAsync();

        if (!BomUi.TryParseDate(EffectiveFromText, out var effectiveFrom))
        {
            ModelState.AddModelError(nameof(EffectiveFromText), L["Bom:InvalidDateFormat"]);
            return Page();
        }

        var items = Items.Where(x => x.ComponentId.HasValue).ToList();
        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, L["Bom:AtLeastOneItem"]);
            return Page();
        }

        var result = await _bomAppService.CreateAsync(ProductId, new CreateBomVersionDto
        {
            EffectiveFrom = effectiveFrom,
            Items = items.Select(x => new CreateBomItemDto
            {
                ComponentId = x.ComponentId!.Value,
                Quantity = x.Quantity
            }).ToList()
        });

        return RedirectToPage("/Bom/Details", new { id = result.Id });
    }

    private async Task LoadPageAsync()
    {
        await LoadProductContextAsync();
        await LoadComponentOptionsAsync();
    }

    private async Task LoadProductContextAsync()
    {
        Product = await _productAppService.GetAsync(ProductId);
        var contexts = await _productPricingContextLookupService.FindMapAsync([ProductId], Clock.Now);
        PricingContext = contexts.GetValueOrDefault(ProductId);
    }

    private async Task LoadComponentOptionsAsync()
    {
        var components = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            MaxResultCount = 1000
        });

        ComponentOptions = components.Items
            .Where(x => x.Status == CatalogItemStatus.Active)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }
}
