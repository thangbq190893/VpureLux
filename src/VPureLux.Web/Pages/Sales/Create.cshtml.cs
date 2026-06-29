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
using VPureLux.Bom;
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
    private readonly IBomVersionRepository _bomVersions;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryBalanceRepository _balances;
    private readonly IComponentRepository _components;
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
        IProductPricingContextLookupService productPricingContext,
        IBomVersionRepository bomVersions,
        IStockItemRepository stockItems,
        IInventoryBalanceRepository balances,
        IComponentRepository components)
    {
        _service = service;
        _customers = customers;
        _warehouses = warehouses;
        _products = products;
        _productPricingContext = productPricingContext;
        _bomVersions = bomVersions;
        _stockItems = stockItems;
        _balances = balances;
        _components = components;
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
        isValid = await ValidateLineStockAvailabilityAsync() && isValid;

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

    public async Task<JsonResult> OnGetStockAvailabilityAsync(Guid warehouseId, string? lines)
    {
        var requestLines = ParseStockAvailabilityLines(lines);
        var availability = await CalculateStockAvailabilityAsync(warehouseId, requestLines);
        return new JsonResult(availability, ProductContextJsonOptions);
    }

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

    private async Task<bool> ValidateLineStockAvailabilityAsync()
    {
        if (Input.WarehouseId == Guid.Empty)
        {
            return true;
        }

        var requestLines = Input.Lines
            .Select((line, index) => new SalesStockAvailabilityLineRequest
            {
                LineIndex = index,
                ProductId = line.ProductId,
                Quantity = line.Quantity
            })
            .ToList();
        var availability = await CalculateStockAvailabilityAsync(Input.WarehouseId, requestLines);
        var isValid = true;

        foreach (var line in availability.Lines.Where(x => x.IsShortage))
        {
            ModelState.AddModelError(
                $"Input.Lines[{line.LineIndex}].Quantity",
                FormatStockShortageMessage(line));
            isValid = false;
        }

        if (!isValid)
        {
            ModelState.AddModelError(string.Empty, L["Sales:StockIssueGlobal"].Value);
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

        ModelState.AddModelError($"Input.Lines[{lineIndex}].ProductId", L["Sales:ProductStockSaleNotSupported"].Value);
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

    private async Task<SalesStockAvailabilityResponse> CalculateStockAvailabilityAsync(
        Guid warehouseId,
        IReadOnlyCollection<SalesStockAvailabilityLineRequest> requestLines)
    {
        var normalizedLines = requestLines
            .Where(x => x.ProductId != Guid.Empty && x.Quantity > 0)
            .Select(x => new SalesStockAvailabilityLineRequest
            {
                LineIndex = x.LineIndex,
                ProductId = x.ProductId,
                Quantity = decimal.Round(x.Quantity, InventoryConsts.QuantityScale, MidpointRounding.AwayFromZero)
            })
            .ToList();
        var response = new SalesStockAvailabilityResponse();
        if (warehouseId == Guid.Empty || normalizedLines.Count == 0)
        {
            return response;
        }

        var publishedBomMap = await _bomVersions.GetPublishedMapByProductIdsAsync(
            normalizedLines.Select(x => x.ProductId).Distinct().ToArray());
        var componentIds = publishedBomMap.Values
            .SelectMany(x => x.Items.Select(item => item.ComponentId))
            .Distinct()
            .ToArray();
        var componentMap = componentIds.Length == 0
            ? new Dictionary<Guid, Component>()
            : (await _components.GetListAsync(x => componentIds.Contains(x.Id))).ToDictionary(x => x.Id);
        var stockItems = componentIds.Length == 0
            ? new List<StockItem>()
            : await _stockItems.GetListAsync(x => x.ItemType == StockItemType.Component && componentIds.Contains(x.CatalogItemId));
        var stockItemByComponentId = stockItems
            .GroupBy(x => x.CatalogItemId)
            .ToDictionary(x => x.Key, x => x.First());
        var balances = componentIds.Length == 0
            ? new List<InventoryBalance>()
            : await _balances.GetListAsync(warehouseId);
        var quantityByStockItemId = balances.ToDictionary(x => x.StockItemId, x => x.QuantityOnHand);
        var availableByComponentId = componentIds.ToDictionary(
            x => x,
            x => stockItemByComponentId.TryGetValue(x, out var stockItem) &&
                 quantityByStockItemId.TryGetValue(stockItem.Id, out var quantity)
                ? quantity
                : 0m);
        var aggregateDemandByComponentId = new Dictionary<Guid, decimal>();

        foreach (var line in normalizedLines)
        {
            if (!publishedBomMap.TryGetValue(line.ProductId, out var bom))
            {
                continue;
            }

            foreach (var item in bom.Items)
            {
                aggregateDemandByComponentId[item.ComponentId] =
                    aggregateDemandByComponentId.GetValueOrDefault(item.ComponentId) + (item.Quantity * line.Quantity);
            }
        }

        foreach (var line in normalizedLines)
        {
            if (!publishedBomMap.TryGetValue(line.ProductId, out var bom))
            {
                response.Lines.Add(new SalesStockAvailabilityLineResult
                {
                    LineIndex = line.LineIndex,
                    ProductId = line.ProductId,
                    Status = SalesStockAvailabilityStatus.NoBom
                });
                continue;
            }

            var componentLimits = bom.Items
                .Select(item =>
                {
                    var availableQuantity = availableByComponentId.GetValueOrDefault(item.ComponentId);
                    var aggregateRequiredQuantity = aggregateDemandByComponentId.GetValueOrDefault(item.ComponentId);
                    stockItemByComponentId.TryGetValue(item.ComponentId, out var stockItem);
                    return new SalesComponentAvailabilityLimit(
                        item.ComponentId,
                        GetComponentLabel(item.ComponentId, componentMap, stockItem),
                        availableQuantity,
                        item.Quantity,
                        aggregateRequiredQuantity,
                        item.Quantity <= 0 ? 0 : decimal.Floor(availableQuantity / item.Quantity));
                })
                .ToList();

            if (componentLimits.Count == 0)
            {
                response.Lines.Add(new SalesStockAvailabilityLineResult
                {
                    LineIndex = line.LineIndex,
                    ProductId = line.ProductId,
                    Status = SalesStockAvailabilityStatus.NoBom
                });
                continue;
            }

            var limitingComponent = componentLimits
                .OrderBy(x => x.AvailableToSell)
                .ThenBy(x => x.ComponentLabel)
                .First();
            var aggregateShortage = componentLimits
                .Where(x => x.AggregateRequiredQuantity > x.AvailableQuantity)
                .OrderByDescending(x => x.AggregateRequiredQuantity - x.AvailableQuantity)
                .ThenBy(x => x.ComponentLabel)
                .FirstOrDefault();
            var selectedLimit = aggregateShortage ?? limitingComponent;
            var availableToSell = limitingComponent.AvailableToSell;
            var isShortage = line.Quantity > availableToSell || aggregateShortage != null;

            response.Lines.Add(new SalesStockAvailabilityLineResult
            {
                LineIndex = line.LineIndex,
                ProductId = line.ProductId,
                Status = isShortage ? SalesStockAvailabilityStatus.Shortage : SalesStockAvailabilityStatus.Available,
                AvailableToSell = availableToSell,
                RequestedQuantity = line.Quantity,
                IsShortage = isShortage,
                LimitingComponentId = selectedLimit.ComponentId,
                LimitingComponentLabel = selectedLimit.ComponentLabel,
                LimitingComponentAvailableQuantity = selectedLimit.AvailableQuantity,
                LimitingComponentRequiredQuantity = selectedLimit.AggregateRequiredQuantity
            });
        }

        return response;
    }

    private static List<SalesStockAvailabilityLineRequest> ParseStockAvailabilityLines(string? lines)
    {
        if (string.IsNullOrWhiteSpace(lines))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<SalesStockAvailabilityLineRequest>>(lines, ProductContextJsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private string FormatStockShortageMessage(SalesStockAvailabilityLineResult line)
    {
        var message = L["Sales:InsufficientStockForRequestedQuantity"].Value;
        if (!string.IsNullOrWhiteSpace(line.LimitingComponentLabel))
        {
            message += " " + L["Sales:MissingComponentStock", line.LimitingComponentLabel].Value;
        }

        return message;
    }

    private static string GetComponentLabel(
        Guid componentId,
        IReadOnlyDictionary<Guid, Component> componentMap,
        StockItem? stockItem)
    {
        if (componentMap.TryGetValue(componentId, out var component))
        {
            return $"{component.Code} - {component.Name}";
        }

        if (stockItem != null)
        {
            return $"{stockItem.CodeSnapshot} - {stockItem.NameSnapshot}";
        }

        return componentId.ToString("D");
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

public static class SalesStockAvailabilityStatus
{
    public const string Available = "available";
    public const string Shortage = "shortage";
    public const string NoBom = "noBom";
}

public class SalesStockAvailabilityLineRequest
{
    public int LineIndex { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class SalesStockAvailabilityResponse
{
    public List<SalesStockAvailabilityLineResult> Lines { get; set; } = [];
}

public class SalesStockAvailabilityLineResult
{
    public int LineIndex { get; set; }
    public Guid ProductId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal AvailableToSell { get; set; }
    public decimal RequestedQuantity { get; set; }
    public bool IsShortage { get; set; }
    public Guid? LimitingComponentId { get; set; }
    public string LimitingComponentLabel { get; set; } = string.Empty;
    public decimal LimitingComponentAvailableQuantity { get; set; }
    public decimal LimitingComponentRequiredQuantity { get; set; }
}

public sealed record SalesComponentAvailabilityLimit(
    Guid ComponentId,
    string ComponentLabel,
    decimal AvailableQuantity,
    decimal RequiredQuantityPerProduct,
    decimal AggregateRequiredQuantity,
    decimal AvailableToSell);
