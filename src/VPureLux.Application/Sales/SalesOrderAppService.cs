using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Customers;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Pricing;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class SalesOrderAppService : ApplicationService, ISalesOrderAppService
{
    private readonly ISalesOrderRepository _salesOrders;
    private readonly ICustomerRepository _customers;
    private readonly ICustomerGroupRepository _customerGroups;
    private readonly IProductRepository _products;
    private readonly IComponentRepository _components;
    private readonly IBomVersionRepository _bomVersions;
    private readonly IProductSuggestedPriceVersionRepository _suggestedPrices;
    private readonly IWarehouseRepository _warehouses;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryLotRepository _lots;
    private readonly IInventoryTransactionRepository _inventoryTransactions;
    private readonly IInventoryBalanceRepository _balances;
    private readonly InventoryManager _inventoryManager;
    private readonly SalesManager _salesManager;
    private readonly SalesApplicationMapper _mapper;

    public SalesOrderAppService(
        ISalesOrderRepository salesOrders,
        ICustomerRepository customers,
        ICustomerGroupRepository customerGroups,
        IProductRepository products,
        IComponentRepository components,
        IBomVersionRepository bomVersions,
        IProductSuggestedPriceVersionRepository suggestedPrices,
        IWarehouseRepository warehouses,
        IStockItemRepository stockItems,
        IInventoryLotRepository lots,
        IInventoryTransactionRepository inventoryTransactions,
        IInventoryBalanceRepository balances,
        InventoryManager inventoryManager,
        SalesManager salesManager,
        SalesApplicationMapper mapper)
    {
        _salesOrders = salesOrders;
        _customers = customers;
        _customerGroups = customerGroups;
        _products = products;
        _components = components;
        _bomVersions = bomVersions;
        _suggestedPrices = suggestedPrices;
        _warehouses = warehouses;
        _stockItems = stockItems;
        _lots = lots;
        _inventoryTransactions = inventoryTransactions;
        _balances = balances;
        _inventoryManager = inventoryManager;
        _salesManager = salesManager;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<SalesOrderDto>> GetListAsync(GetSalesOrderListInput input)
    {
        var count = await _salesOrders.GetCountAsync(input.CustomerId, input.Status);
        var page = await _salesOrders.GetListAsync(
            input.CustomerId, input.Status, input.Sorting, input.MaxResultCount, input.SkipCount);
        var visibility = await GetFinancialVisibilityAsync();
        return new PagedResultDto<SalesOrderDto>(
            count,
            page.Select(x => _mapper.ToDto(x, visibility.Cost, visibility.Profit)).ToList());
    }

    public async Task<SalesOrderDto> GetAsync(Guid id)
    {
        var visibility = await GetFinancialVisibilityAsync();
        return _mapper.ToDto(await GetOrderAsync(id), visibility.Cost, visibility.Profit);
    }

    [Authorize(VPureLuxPermissions.Sales.Create)]
    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderDto input)
    {
        await EnsureActiveCustomerAsync(input.CustomerId);
        await EnsureActiveWarehouseAsync(input.WarehouseId);
        var orderDate = input.OrderDate ?? Clock.Now;
        var order = await _salesManager.CreateAsync(input.CustomerId, input.WarehouseId, orderDate);
        foreach (var inputLine in input.Lines)
        {
            await AddInputLineAsync(order, inputLine);
        }
        await _salesOrders.InsertAsync(order, autoSave: true);
        return _mapper.ToDto(order, includeCost: false, includeProfit: false);
    }

    [Authorize(VPureLuxPermissions.Sales.Edit)]
    public async Task<SalesOrderDto> AddLineAsync(Guid id, CreateSalesOrderLineDto input)
    {
        var order = await GetOrderAsync(id);
        await AddInputLineAsync(order, input);
        await _salesOrders.UpdateAsync(order, autoSave: true);
        return _mapper.ToDto(order, includeCost: false, includeProfit: false);
    }

    [Authorize(VPureLuxPermissions.Sales.Edit)]
    public async Task<SalesOrderDto> UpdateLineAsync(Guid id, Guid lineId, UpdateSalesOrderLineDto input)
    {
        var order = await GetOrderAsync(id);
        var line = order.Lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        await EnsureOverridePermissionAsync(line.SuggestedPriceSnapshot, input.ActualSellingPrice);
        order.UpdateLine(lineId, input.Quantity, input.ActualSellingPrice, input.OverrideReason);
        await _salesOrders.UpdateAsync(order, autoSave: true);
        return _mapper.ToDto(order, includeCost: false, includeProfit: false);
    }

    [Authorize(VPureLuxPermissions.Sales.Edit)]
    public async Task<SalesOrderDto> RemoveLineAsync(Guid id, Guid lineId)
    {
        var order = await GetOrderAsync(id);
        order.RemoveLine(lineId);
        await _salesOrders.UpdateAsync(order, autoSave: true);
        return _mapper.ToDto(order, includeCost: false, includeProfit: false);
    }

    [Authorize(VPureLuxPermissions.Sales.Confirm)]
    public async Task<ConfirmSalesOrderResultDto> ConfirmAsync(Guid id, ConfirmSalesOrderDto input)
    {
        var order = await GetOrderAsync(id);
        if (order.Status == SalesOrderStatus.Confirmed)
        {
            order.Confirm(input.IdempotencyKey, order.ConfirmedAt ?? Clock.Now);
            var visibility = await GetFinancialVisibilityAsync();
            return ToConfirmationResult(order, visibility.Cost, visibility.Profit);
        }

        var existing = await _salesOrders.FindByConfirmationIdempotencyKeyAsync(input.IdempotencyKey);
        if (existing != null && existing.Id != id)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesConfirmationIdempotencyConflict);
        }

        var customer = await EnsureActiveCustomerAsync(order.CustomerId);
        var customerGroup = await EnsureActiveCustomerGroupAsync(customer.CustomerGroupId);
        await EnsureActiveWarehouseAsync(order.WarehouseId);
        foreach (var line in order.Lines.OrderBy(x => x.LineNo))
        {
            await ConfirmLineAsync(order, line);
        }

        order.ApplyCustomerSnapshot(customer.Code, customer.Name, customerGroup.Id, customerGroup.Code, customerGroup.Name);
        order.Confirm(input.IdempotencyKey, Clock.Now);
        await _salesOrders.UpdateAsync(order, autoSave: true);
        var resultVisibility = await GetFinancialVisibilityAsync();
        return ToConfirmationResult(order, resultVisibility.Cost, resultVisibility.Profit);
    }

    [Authorize(VPureLuxPermissions.Sales.Cancel)]
    public async Task CancelAsync(Guid id)
    {
        var order = await GetOrderAsync(id);
        order.CancelDraft(Clock.Now);
        await _salesOrders.UpdateAsync(order, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Sales.ViewCustomerHistory)]
    [Authorize(VPureLuxPermissions.Sales.ViewProfit)]
    public async Task<List<CustomerPurchaseHistoryDto>> GetCustomerHistoryAsync(Guid customerId)
    {
        if (await _customers.FindAsync(customerId) == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerNotFound);
        }
        return (await _salesOrders.GetCustomerPurchaseHistoryAsync(customerId)).Select(_mapper.ToDto).ToList();
    }

    private async Task AddInputLineAsync(SalesOrder order, CreateSalesOrderLineDto input)
    {
        var product = await EnsureActiveProductAsync(input.ProductId);
        var bom = await EnsurePublishedBomAsync(product.Id);
        var price = await _suggestedPrices.FindAtDateAsync(product.Id, order.OrderDate);
        var priceVersionId = price?.Id;
        var suggestedPrice = price?.Price.Amount;

        var actualPrice = input.ActualSellingPrice ?? suggestedPrice
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData("Reason", "Actual selling price is required when no suggested price exists.");
        await EnsureOverridePermissionAsync(suggestedPrice, actualPrice);
        order.AddLine(
            GuidGenerator.Create(), product.Id, bom.Id,
            input.Quantity, priceVersionId, suggestedPrice, actualPrice, input.OverrideReason);
    }

    private async Task ConfirmLineAsync(SalesOrder order, SalesOrderLine line)
    {
        var product = await EnsureActiveProductAsync(line.ProductId);
        var bom = await EnsurePublishedBomAsync(line.ProductId);
        if (line.BomVersionId != bom.Id)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesBomMustBePublished);
        }
        var snapshotItems = new List<SalesOrderBomSnapshotData>();
        var requirements = new List<(Component Component, decimal Quantity)>();
        foreach (var item in bom.Items)
        {
            var component = await EnsureActiveComponentAsync(item.ComponentId);
            var required = item.Quantity * line.Quantity;
            requirements.Add((component, required));
            snapshotItems.Add(new SalesOrderBomSnapshotData(
                component.Id, component.Code, component.Name, component.Unit, item.Quantity, required));
        }
        var result = await PostInventoryIssueAsync(order, line, bom.Id, requirements);
        order.ApplyLineConfirmationSnapshot(
            line.Id, product.Code, product.Name, SalesConsts.DefaultProductUnit, bom.VersionNo.Value,
            result.Id, result.Cost, snapshotItems);
    }

    private async Task<(Guid Id, decimal Cost)> PostInventoryIssueAsync(
        SalesOrder order,
        SalesOrderLine salesLine,
        Guid? bomVersionId,
        IEnumerable<(Component Component, decimal Quantity)> requirements)
    {
        var idempotencyKey = $"sales-confirm:{order.Id}:line:{salesLine.Id}";
        var consolidated = requirements.GroupBy(x => x.Component.Id)
            .Select(x => (Component: x.First().Component, Quantity: x.Sum(y => y.Quantity)))
            .OrderBy(x => x.Component.Id)
            .ToList();
        var hash = Hash($"{order.Id}|{salesLine.Id}|{order.WarehouseId}|{bomVersionId}|" +
                        string.Join(";", consolidated.Select(x => $"{x.Component.Id}:{x.Quantity}")));
        var existing = await _inventoryManager.FindExistingTransactionAsync(idempotencyKey);
        if (existing != null)
        {
            if (existing.RequestHash != hash)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesConfirmationIdempotencyConflict);
            }
            return (existing.Id, existing.TotalIssueCost);
        }

        var transaction = _inventoryManager.CreateTransaction(
            order.WarehouseId, InventoryTransactionType.SalesIssue, idempotencyKey, hash,
            "SalesOrderLine", salesLine.Id, bomVersionId);
        try
        {
            foreach (var requirement in consolidated)
            {
                var stockItem = await _stockItems.FindByCatalogItemAsync(StockItemType.Component, requirement.Component.Id)
                    ?? throw new BusinessException(VPureLuxDomainErrorCodes.StockItemNotFound);
                await _inventoryManager.EnsureWarehouseAndStockItemUsableAsync(order.WarehouseId, stockItem.Id);
                var issueLine = transaction.AddIssueLine(GuidGenerator.Create(), stockItem.Id, requirement.Quantity);
                var allocations = await _inventoryManager.AllocateFifoAsync(transaction, issueLine);
                foreach (var allocation in allocations)
                {
                    await _lots.UpdateAsync(await _lots.GetAsync(allocation.InventoryLotId));
                }
                await _balances.ApplyMovementAsync(
                    order.WarehouseId, stockItem.Id, -requirement.Quantity,
                    -allocations.Sum(x => x.TotalCost), Clock.Now);
            }
        }
        catch (BusinessException exception) when (
            exception.Code?.StartsWith("INV_", StringComparison.Ordinal) == true)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesInventoryValidationFailed)
                .WithData("InventoryErrorCode", exception.Code);
        }

        transaction.Post(Clock.Now);
        await _inventoryTransactions.InsertAsync(transaction);
        return (transaction.Id, transaction.TotalIssueCost);
    }

    private async Task<Customer> EnsureActiveCustomerAsync(Guid id)
    {
        var customer = await _customers.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.CustomerNotFound);
        if (customer.Status != CustomerStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerInactive);
        }
        return customer;
    }

    private async Task<CustomerGroup> EnsureActiveCustomerGroupAsync(Guid id)
    {
        var group = await _customerGroups.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupNotFound);
        if (group.Status != CustomerGroupStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupInactive);
        }
        return group;
    }

    private async Task EnsureActiveWarehouseAsync(Guid id)
    {
        var warehouse = await _warehouses.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseNotFound);
        if (warehouse.Status != InventoryEntityStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseInactive);
        }
    }

    private async Task<Product> EnsureActiveProductAsync(Guid id)
    {
        var product = await _products.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.ProductNotFound);
        if (product.Status != CatalogItemStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        return product;
    }

    private async Task<Component> EnsureActiveComponentAsync(Guid id)
    {
        var component = await _components.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.ComponentNotFound);
        if (component.Status != CatalogItemStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ComponentNotActive);
        }
        return component;
    }

    private async Task<BomVersion> EnsurePublishedBomAsync(Guid productId)
    {
        var bom = (await _bomVersions.GetListByProductIdAsync(productId))
            .FirstOrDefault(x => x.Status == BomStatus.Published);
        if (bom == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesBomMustBePublished);
        }
        return bom;
    }

    private async Task EnsureOverridePermissionAsync(decimal? suggested, decimal actual)
    {
        if (suggested.HasValue && suggested.Value != actual &&
            !(await AuthorizationService.AuthorizeAsync(VPureLuxPermissions.Sales.OverridePrice)).Succeeded)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.AccessDenied);
        }
    }

    private async Task<SalesOrder> GetOrderAsync(Guid id) =>
        await _salesOrders.FindAsync(id, includeDetails: true)
        ?? throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderNotFound);

    private async Task<(bool Cost, bool Profit)> GetFinancialVisibilityAsync() =>
        ((await AuthorizationService.AuthorizeAsync(VPureLuxPermissions.Sales.ViewCost)).Succeeded,
         (await AuthorizationService.AuthorizeAsync(VPureLuxPermissions.Sales.ViewProfit)).Succeeded);

    private static ConfirmSalesOrderResultDto ToConfirmationResult(
        SalesOrder order,
        bool includeCost,
        bool includeProfit) => new()
    {
        SalesOrderId = order.Id,
        OrderNo = order.OrderNo,
        TotalRevenueAmount = order.TotalRevenueAmount,
        TotalCostAmount = includeCost ? order.TotalCostAmount : null,
        TotalProfitAmount = includeProfit ? order.TotalProfitAmount : null
    };

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
