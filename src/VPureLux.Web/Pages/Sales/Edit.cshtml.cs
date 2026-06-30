using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using VPureLux;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Authorization;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.Edit)]
public class EditModel : VPureLuxPageModel
{
    private static readonly JsonSerializerOptions ProductContextJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ISalesOrderAppService _service;
    private readonly IProductAppService _products;
    private readonly IProductPricingContextLookupService _productPricingContext;
    private readonly IBomVersionRepository _bomVersions;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryBalanceRepository _balances;
    private readonly IComponentRepository _components;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CreateSalesOrderLineDto NewLine { get; set; } = new() { Quantity = 1 };
    [BindProperty] public UpdateSalesOrderLineDto UpdateLine { get; set; } = new();
    [BindProperty] public Guid LineId { get; set; }
    public SalesOrderDto Order { get; private set; } = new();
    public List<SelectListItem> Products { get; private set; } = new();
    public Dictionary<Guid, SalesProductContextViewModel> ProductContexts { get; private set; } = new();
    public Dictionary<Guid, string> ProductLabels { get; private set; } = new();

    public EditModel(
        ISalesOrderAppService service,
        IProductAppService products,
        IProductPricingContextLookupService productPricingContext,
        IBomVersionRepository bomVersions,
        IStockItemRepository stockItems,
        IInventoryBalanceRepository balances,
        IComponentRepository components)
    {
        _service = service;
        _products = products;
        _productPricingContext = productPricingContext;
        _bomVersions = bomVersions;
        _stockItems = stockItems;
        _balances = balances;
        _components = components;
    }

    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostAddAsync()
    {
        await LoadAsync();

        var isValid = ValidateNewLineEligibility();
        isValid = ValidateNewLinePricingOverride() && isValid;
        isValid = await ValidateNewLineStockAvailabilityAsync() && isValid;

        if (!isValid)
        {
            return Page();
        }

        try
        {
            await _service.AddLineAsync(Id, NewLine);
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception)
        {
            AddNewLineBusinessError(exception);
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        await LoadAsync();

        var line = Order.Lines.SingleOrDefault(x => x.Id == LineId);
        if (line == null)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound)));
            return Page();
        }

        var isValid = ValidateUpdateLinePricingOverride(line);
        isValid = await ValidateUpdateLineStockAvailabilityAsync(line) && isValid;

        if (!isValid)
        {
            return Page();
        }

        try
        {
            await _service.UpdateLineAsync(Id, LineId, UpdateLine);
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception)
        {
            AddUpdateLineBusinessError(exception);
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

    public async Task<JsonResult> OnGetStockAvailabilityAsync(string? lines)
    {
        await LoadAsync();
        var requestLines = ParseStockAvailabilityLines(lines);
        var availability = await CalculateStockAvailabilityAsync(Order.WarehouseId, requestLines);
        return new JsonResult(availability, ProductContextJsonOptions);
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

    private bool ValidateNewLineEligibility()
    {
        if (NewLine.ProductId == Guid.Empty)
        {
            return true;
        }

        if (ProductContexts.TryGetValue(NewLine.ProductId, out var context) && context.HasPublishedBom)
        {
            return true;
        }

        ModelState.AddModelError(nameof(NewLine) + "." + nameof(NewLine.ProductId), L["Sales:ProductStockSaleNotSupported"].Value);
        return false;
    }

    private bool ValidateNewLinePricingOverride()
    {
        if (NewLine.ProductId == Guid.Empty ||
            !NewLine.ActualSellingPrice.HasValue ||
            !ProductContexts.TryGetValue(NewLine.ProductId, out var context) ||
            !context.SuggestedPrice.HasValue)
        {
            return true;
        }

        return ValidateOverrideReason(
            context.SuggestedPrice.Value,
            NewLine.ActualSellingPrice.Value,
            NewLine.OverrideReason,
            nameof(NewLine) + "." + nameof(NewLine.OverrideReason));
    }

    private bool ValidateUpdateLinePricingOverride(SalesOrderLineDto line)
    {
        if (!line.SuggestedPriceSnapshot.HasValue)
        {
            return true;
        }

        return ValidateOverrideReason(
            line.SuggestedPriceSnapshot.Value,
            UpdateLine.ActualSellingPrice,
            UpdateLine.OverrideReason,
            nameof(UpdateLine) + "." + nameof(UpdateLine.OverrideReason));
    }

    private bool ValidateOverrideReason(decimal suggestedPrice, decimal actualPrice, string? overrideReason, string modelStateKey)
    {
        var roundedSuggested = decimal.Round(suggestedPrice, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero);
        var roundedActual = decimal.Round(actualPrice, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero);

        if (roundedSuggested == roundedActual || !string.IsNullOrWhiteSpace(overrideReason))
        {
            return true;
        }

        ModelState.AddModelError(modelStateKey, L[VPureLuxDomainErrorCodes.SalesOverrideReasonRequired].Value);
        return false;
    }

    private async Task<bool> ValidateNewLineStockAvailabilityAsync()
    {
        if (NewLine.ProductId == Guid.Empty || NewLine.Quantity <= 0)
        {
            return true;
        }

        var newLineIndex = Order.Lines.Count;
        var requestLines = Order.Lines
            .Select((line, index) => new SalesStockAvailabilityLineRequest
            {
                LineIndex = index,
                ProductId = line.ProductId,
                Quantity = line.Quantity
            })
            .Append(new SalesStockAvailabilityLineRequest
            {
                LineIndex = newLineIndex,
                ProductId = NewLine.ProductId,
                Quantity = NewLine.Quantity
            })
            .ToList();
        var availability = await CalculateStockAvailabilityAsync(Order.WarehouseId, requestLines);
        var newLineAvailability = availability.Lines.SingleOrDefault(x => x.LineIndex == newLineIndex);
        if (newLineAvailability?.IsShortage != true)
        {
            return true;
        }

        ModelState.AddModelError(nameof(NewLine) + "." + nameof(NewLine.Quantity), FormatStockShortageMessage(newLineAvailability));
        ModelState.AddModelError(string.Empty, L["Sales:StockIssueGlobal"].Value);
        return false;
    }

    private async Task<bool> ValidateUpdateLineStockAvailabilityAsync(SalesOrderLineDto updatedLine)
    {
        if (UpdateLine.Quantity <= 0)
        {
            return true;
        }

        var updatedLineIndex = Order.Lines.FindIndex(x => x.Id == updatedLine.Id);
        var requestLines = Order.Lines
            .Select((line, index) => new SalesStockAvailabilityLineRequest
            {
                LineIndex = index,
                ProductId = line.ProductId,
                Quantity = line.Id == updatedLine.Id ? UpdateLine.Quantity : line.Quantity
            })
            .ToList();
        var availability = await CalculateStockAvailabilityAsync(Order.WarehouseId, requestLines);
        var lineAvailability = availability.Lines.SingleOrDefault(x => x.LineIndex == updatedLineIndex);
        if (lineAvailability?.IsShortage != true)
        {
            return true;
        }

        ModelState.AddModelError(nameof(UpdateLine) + "." + nameof(UpdateLine.Quantity), FormatStockShortageMessage(lineAvailability));
        ModelState.AddModelError(string.Empty, L["Sales:StockIssueGlobal"].Value);
        return false;
    }

    private void AddNewLineBusinessError(BusinessException exception)
    {
        if (exception.Code == VPureLuxDomainErrorCodes.SalesBomMustBePublished)
        {
            ModelState.AddModelError(nameof(NewLine) + "." + nameof(NewLine.ProductId), L["Sales:ProductStockSaleNotSupported"].Value);
            return;
        }

        if (exception.Code == VPureLuxDomainErrorCodes.SalesOverrideReasonRequired)
        {
            ModelState.AddModelError(nameof(NewLine) + "." + nameof(NewLine.OverrideReason), L[VPureLuxDomainErrorCodes.SalesOverrideReasonRequired].Value);
            return;
        }

        ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
    }

    private void AddUpdateLineBusinessError(BusinessException exception)
    {
        if (exception.Code == VPureLuxDomainErrorCodes.SalesOverrideReasonRequired)
        {
            ModelState.AddModelError(nameof(UpdateLine) + "." + nameof(UpdateLine.OverrideReason), L[VPureLuxDomainErrorCodes.SalesOverrideReasonRequired].Value);
            return;
        }

        ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
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
}
