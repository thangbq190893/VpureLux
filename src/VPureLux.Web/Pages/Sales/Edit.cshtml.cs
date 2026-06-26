using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using VPureLux.Catalog;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Authorization;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    private readonly IProductAppService _products;
    private readonly IProductPricingContextLookupService _productPricingContext;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CreateSalesOrderLineDto NewLine { get; set; } = new();
    [BindProperty] public UpdateSalesOrderLineDto UpdateLine { get; set; } = new();
    [BindProperty] public Guid LineId { get; set; }
    public SalesOrderDto Order { get; private set; } = new();
    public List<SelectListItem> Products { get; private set; } = new();
    public Dictionary<Guid, SalesProductContextViewModel> ProductContexts { get; private set; } = new();
    public Dictionary<Guid, string> ProductLabels { get; private set; } = new();

    public EditModel(
        ISalesOrderAppService service,
        IProductAppService products,
        IProductPricingContextLookupService productPricingContext)
    {
        _service = service;
        _products = products;
        _productPricingContext = productPricingContext;
    }

    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostAddAsync()
    {
        try
        {
            await _service.AddLineAsync(Id, NewLine);
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        try
        {
            await _service.UpdateLineAsync(Id, LineId, UpdateLine);
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRemoveAsync(Guid lineId)
    {
        try
        {
            await _service.RemoveLineAsync(Id, lineId);
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
            await LoadAsync();
            return Page();
        }
    }

    public async Task<JsonResult> OnGetProductContextAsync(Guid productId)
    {
        try
        {
            var contexts = await _productPricingContext.FindMapAsync([productId], Clock.Now);
            if (contexts.TryGetValue(productId, out var context))
            {
                var product = await _products.GetAsync(productId);
                return new JsonResult(ToViewModel(context, product));
            }
        }
        catch (AbpAuthorizationException)
        {
        }

        return new JsonResult(new SalesProductContextViewModel
        {
            ProductId = productId,
            ProductLabel = L["Sales:ProductContextUnavailable"],
            BomStatusText = L["Sales:ProductContextUnavailable"]
        });
    }

    public string GetProductLabel(SalesOrderLineDto line) =>
        SalesUiFormatter.GetProductLabel(line, ProductLabels, L);

    public string GetBomBadgeClass(SalesOrderLineDto line)
    {
        if (line.BomVersionNoSnapshot.HasValue)
        {
            return SalesUiFormatter.GetBomBadgeClass(true);
        }

        return ProductContexts.TryGetValue(line.ProductId, out var context)
            ? SalesUiFormatter.GetBomBadgeClass(context.HasPublishedBom)
            : "badge bg-secondary";
    }

    public string GetBomStatusText(SalesOrderLineDto line)
    {
        if (line.BomVersionNoSnapshot.HasValue)
        {
            return L["Sales:PublishedBomVersion", line.BomVersionNoSnapshot.Value];
        }

        return ProductContexts.TryGetValue(line.ProductId, out var context)
            ? context.BomStatusText
            : L["Sales:ProductContextUnavailable"];
    }

    private async Task LoadAsync()
    {
        Order = await _service.GetAsync(Id);
        var productItems = (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 1000 })).Items;
        Products = productItems
            .Where(x => x.Status == CatalogItemStatus.Active)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
        await LoadProductContextsAsync(productItems.ToDictionary(x => x.Id));
    }

    private async Task LoadProductContextsAsync(IReadOnlyDictionary<Guid, ProductDto>? productsById = null)
    {
        if (ProductContexts.Count > 0)
        {
            return;
        }

        try
        {
            productsById ??= (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 1000 })).Items
                .ToDictionary(x => x.Id);
            ProductContexts = (await _productPricingContext.FindMapAsync(productsById.Keys.ToArray(), Clock.Now))
                .Values
                .ToDictionary(
                    x => x.ProductId,
                    x =>
                    {
                        productsById.TryGetValue(x.ProductId, out var product);
                        return ToViewModel(x, product);
                    });
            ProductLabels = ProductContexts.ToDictionary(x => x.Key, x => x.Value.ProductLabel);
        }
        catch (AbpAuthorizationException)
        {
            ProductContexts = new Dictionary<Guid, SalesProductContextViewModel>();
            ProductLabels = new Dictionary<Guid, string>();
        }
    }

    private SalesProductContextViewModel ToViewModel(ProductPricingContextDto context, ProductDto? product) =>
        new()
        {
            ProductId = context.ProductId,
            ProductLabel = $"{context.ProductCode} - {context.ProductName}",
            HasPublishedBom = context.HasPublishedBom,
            HasImage = product?.HasImage ?? false,
            SuggestedPrice = context.CurrentProductSuggestedPrice,
            BomStatusText = context.HasPublishedBom
                ? L["Sales:PublishedBomAvailable"]
                : L["Sales:NoPublishedBom"]
        };
}
