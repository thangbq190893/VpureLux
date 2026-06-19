using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Catalog;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Authorization;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    private readonly ICustomerAppService _customers;
    private readonly IWarehouseAppService _warehouses;
    private readonly IProductAppService _products;
    private readonly IProductPricingContextAppService _productPricingContext;
    [BindProperty] public CreateSalesOrderDto Input { get; set; } = new() { Lines = [new CreateSalesOrderLineDto()] };
    public List<SelectListItem> Customers { get; private set; } = new();
    public List<SelectListItem> Warehouses { get; private set; } = new();
    public List<SelectListItem> Products { get; private set; } = new();
    public Dictionary<Guid, SalesProductContextViewModel> ProductContexts { get; private set; } = new();

    public CreateModel(
        ISalesOrderAppService service,
        ICustomerAppService customers,
        IWarehouseAppService warehouses,
        IProductAppService products,
        IProductPricingContextAppService productPricingContext)
    {
        _service = service;
        _customers = customers;
        _warehouses = warehouses;
        _products = products;
        _productPricingContext = productPricingContext;
    }

    public async Task OnGetAsync()
    {
        await LoadSelectionsAsync();
        Input.OrderDate ??= Clock.Now;
        if (Input.Lines.Count == 0)
        {
            Input.Lines.Add(new CreateSalesOrderLineDto());
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var order = await _service.CreateAsync(Input);
            return RedirectToPage("/Sales/Details", new { id = order.Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
            await LoadSelectionsAsync();
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

    private async Task LoadSelectionsAsync()
    {
        Customers = (await _customers.GetListAsync(new GetCustomerListInput { MaxResultCount = 500 })).Items
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString())).ToList();
        Warehouses = (await _warehouses.GetListAsync(new GetInventoryListInput { MaxResultCount = 500 })).Items
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString())).ToList();
        var productItems = (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 1000 })).Items;
        Products = productItems
            .Where(x => x.Status == CatalogItemStatus.Active)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString())).ToList();
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
            ProductContexts = (await _productPricingContext.GetListAsync())
                .ToDictionary(
                    x => x.ProductId,
                    x =>
                    {
                        productsById.TryGetValue(x.ProductId, out var product);
                        return new SalesProductContextViewModel
                        {
                            ProductId = x.ProductId,
                            ProductLabel = $"{x.ProductCode} - {x.ProductName}",
                            HasPublishedBom = x.HasPublishedBom,
                            HasImage = product?.HasImage ?? false,
                            SuggestedPrice = x.CurrentProductSuggestedPrice,
                            BomStatusText = x.HasPublishedBom
                                ? L["Sales:PublishedBomAvailable"]
                                : L["Sales:NoPublishedBom"]
                        };
                    });
        }
        catch (AbpAuthorizationException)
        {
            ProductContexts = new Dictionary<Guid, SalesProductContextViewModel>();
        }
    }
}
