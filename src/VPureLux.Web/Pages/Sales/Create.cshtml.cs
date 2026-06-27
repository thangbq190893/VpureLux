using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux;
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
    private static readonly JsonSerializerOptions ProductContextJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISalesOrderAppService _service;
    private readonly ICustomerAppService _customers;
    private readonly IWarehouseAppService _warehouses;
    private readonly IProductAppService _products;
    private readonly IProductPricingContextLookupService _productPricingContext;
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
        IProductPricingContextLookupService productPricingContext)
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
        await LoadSelectionsAsync();

        var isValid = ValidateLineEligibility();
        isValid = ValidateLinePricingOverrides() && isValid;

        if (!isValid)
        {
            return Page();
        }

        try
        {
            var order = await _service.CreateAsync(Input);
            return RedirectToPage("/Sales/Details", new { id = order.Id });
        }
        catch (BusinessException exception) when (exception.Code == VPureLuxDomainErrorCodes.SalesBomMustBePublished)
        {
            AddBomValidationErrors();
            return Page();
        }
        catch (BusinessException exception) when (exception.Code == VPureLuxDomainErrorCodes.SalesOverrideReasonRequired)
        {
            AddOverrideReasonValidationErrors();
            return Page();
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
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

    public string GetProductContextsJson() =>
        JsonSerializer.Serialize(
            ProductContexts.ToDictionary(
                x => x.Key.ToString(),
                x => new
                {
                    x.Value.HasPublishedBom,
                    x.Value.HasImage,
                    x.Value.SuggestedPrice
                }),
            ProductContextJsonOptions);

    private bool ValidateLineEligibility()
    {
        var isValid = true;

        for (var i = 0; i < Input.Lines.Count; i++)
        {
            if (!TryAddLineEligibilityError(i))
            {
                continue;
            }

            isValid = false;
        }

        return isValid;
    }

    private void AddBomValidationErrors()
    {
        for (var i = 0; i < Input.Lines.Count; i++)
        {
            TryAddLineEligibilityError(i);
        }

        if (!ModelState.IsValid)
        {
            return;
        }

        ModelState.AddModelError(
            string.Empty,
            SalesUiFormatter.GetFriendlyErrorMessage(
                L,
                new BusinessException(VPureLuxDomainErrorCodes.SalesBomMustBePublished)));
    }

    private bool ValidateLinePricingOverrides()
    {
        var isValid = true;

        for (var i = 0; i < Input.Lines.Count; i++)
        {
            if (!TryAddLineOverrideReasonError(i))
            {
                continue;
            }

            isValid = false;
        }

        return isValid;
    }

    private void AddOverrideReasonValidationErrors()
    {
        for (var i = 0; i < Input.Lines.Count; i++)
        {
            TryAddLineOverrideReasonError(i);
        }

        if (!ModelState.IsValid)
        {
            return;
        }

        ModelState.AddModelError(
            string.Empty,
            SalesUiFormatter.GetFriendlyErrorMessage(
                L,
                new BusinessException(VPureLuxDomainErrorCodes.SalesOverrideReasonRequired)));
    }

    private bool TryAddLineEligibilityError(int lineIndex)
    {
        var line = Input.Lines[lineIndex];
        if (line.ProductId == Guid.Empty)
        {
            return false;
        }

        if (ProductContexts.TryGetValue(line.ProductId, out var context) && context.HasPublishedBom)
        {
            return false;
        }

        ModelState.AddModelError($"Input.Lines[{lineIndex}].ProductId", L["Sales:ProductNotSaleEligible"].Value);
        return true;
    }

    private bool TryAddLineOverrideReasonError(int lineIndex)
    {
        var line = Input.Lines[lineIndex];
        if (line.ProductId == Guid.Empty ||
            !line.ActualSellingPrice.HasValue ||
            !ProductContexts.TryGetValue(line.ProductId, out var context) ||
            !context.SuggestedPrice.HasValue)
        {
            return false;
        }

        var suggestedPrice = decimal.Round(context.SuggestedPrice.Value, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero);
        var actualPrice = decimal.Round(line.ActualSellingPrice.Value, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero);

        if (suggestedPrice == actualPrice || !string.IsNullOrWhiteSpace(line.OverrideReason))
        {
            return false;
        }

        ModelState.AddModelError(
            $"Input.Lines[{lineIndex}].OverrideReason",
            L[VPureLuxDomainErrorCodes.SalesOverrideReasonRequired].Value);
        return true;
    }

    private async Task LoadSelectionsAsync()
    {
        Customers = (await _customers.GetListAsync(new GetCustomerListInput { MaxResultCount = 500 })).Items
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString())).ToList();
        Warehouses = (await _warehouses.GetListAsync(new GetInventoryListInput { MaxResultCount = 500 })).Items
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString())).ToList();
        var productItems = (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 1000 })).Items;
        Products = [new SelectListItem(L["Select"], string.Empty)];
        Products.AddRange(productItems
            .Where(x => x.Status == CatalogItemStatus.Active)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString())));
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
        }
        catch (AbpAuthorizationException)
        {
            ProductContexts = new Dictionary<Guid, SalesProductContextViewModel>();
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
