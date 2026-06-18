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
    private readonly IProductPricingContextAppService _productPricingContext;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CreateSalesOrderLineDto NewLine { get; set; } = new();
    [BindProperty] public UpdateSalesOrderLineDto UpdateLine { get; set; } = new();
    [BindProperty] public Guid LineId { get; set; }
    public SalesOrderDto Order { get; private set; } = new();
    public List<SelectListItem> Products { get; private set; } = new();
    public Dictionary<Guid, SalesProductContextViewModel> ProductContexts { get; private set; } = new();

    public EditModel(
        ISalesOrderAppService service,
        IProductAppService products,
        IProductPricingContextAppService productPricingContext)
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
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception));
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
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception));
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
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception));
            await LoadAsync();
            return Page();
        }
    }

    public async Task<JsonResult> OnGetProductContextAsync(Guid productId)
    {
        await LoadProductContextsAsync();
        if (ProductContexts.TryGetValue(productId, out var context))
        {
            return new JsonResult(context);
        }

        return new JsonResult(new SalesProductContextViewModel
        {
            ProductId = productId,
            ProductLabel = L["Sales:ProductContextUnavailable"],
            BomStatusText = L["Sales:ProductContextUnavailable"]
        });
    }

    public string GetProductLabel(SalesOrderLineDto line)
    {
        if (!string.IsNullOrWhiteSpace(line.ItemCodeSnapshot) || !string.IsNullOrWhiteSpace(line.ItemNameSnapshot))
        {
            return $"{line.ItemCodeSnapshot} - {line.ItemNameSnapshot}".Trim(' ', '-');
        }

        return ProductContexts.TryGetValue(line.ProductId, out var product)
            ? product.ProductLabel
            : L["Sales:ProductContextUnavailable"];
    }

    private async Task LoadAsync()
    {
        Order = await _service.GetAsync(Id);
        Products = (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 500 })).Items
            .Where(x => x.Status == CatalogItemStatus.Active)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
        await LoadProductContextsAsync();
    }

    private async Task LoadProductContextsAsync()
    {
        if (ProductContexts.Count > 0)
        {
            return;
        }

        try
        {
            ProductContexts = (await _productPricingContext.GetListAsync())
                .ToDictionary(
                    x => x.ProductId,
                    x => new SalesProductContextViewModel
                    {
                        ProductId = x.ProductId,
                        ProductLabel = $"{x.ProductCode} - {x.ProductName}",
                        SuggestedPrice = x.CurrentProductSuggestedPrice,
                        BomStatusText = x.HasPublishedBom
                            ? L["Sales:PublishedBomAvailable"]
                            : L["Sales:NoPublishedBom"]
                    });
        }
        catch (AbpAuthorizationException)
        {
            ProductContexts = new Dictionary<Guid, SalesProductContextViewModel>();
        }
    }

    private string GetFriendlyErrorMessage(BusinessException exception)
    {
        return string.IsNullOrWhiteSpace(exception.Code)
            ? exception.Message
            : L[exception.Code].Value;
    }

    public class SalesProductContextViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductLabel { get; set; } = string.Empty;
        public decimal? SuggestedPrice { get; set; }
        public string BomStatusText { get; set; } = string.Empty;
    }
}
