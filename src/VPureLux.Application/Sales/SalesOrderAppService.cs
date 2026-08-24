using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
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
    private readonly ISalesOrderPaymentRepository _payments;
    private readonly SalesApplicationMapper _mapper;
    private readonly ILogger<SalesOrderAppService> _logger;

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
        ISalesOrderPaymentRepository payments,
        SalesApplicationMapper mapper,
        ILogger<SalesOrderAppService> logger)
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
        _payments = payments;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<SalesOrderDto>> GetListAsync(GetSalesOrderListInput input)
    {
        if (input.PaymentStatus.HasValue)
        {
            return await GetListFilteredByPaymentStatusAsync(input);
        }

        var count = await _salesOrders.GetCountAsync(input.CustomerId, input.Status);
        var page = await _salesOrders.GetListAsync(
            input.CustomerId, input.Status, input.Sorting, input.MaxResultCount, input.SkipCount);
        var visibility = await GetFinancialVisibilityAsync();
        var summaries = await GetPaymentSummariesAsync(page);
        var items = page.Select(x => _mapper.ToDto(x, visibility.Cost, visibility.Profit, summaries[x.Id])).ToList();
        await FillMissingCustomerSnapshotsAsync(items);
        return new PagedResultDto<SalesOrderDto>(
            count,
            items);
    }

    public async Task<SalesOrderDto> GetAsync(Guid id)
    {
        var visibility = await GetFinancialVisibilityAsync();
        var order = await GetOrderAsync(id);
        var dto = _mapper.ToDto(order, visibility.Cost, visibility.Profit, await GetPaymentSummaryAsync(order));
        await FillMissingCustomerSnapshotsAsync([dto]);
        return dto;
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
        var productId = input.ProductId == Guid.Empty ? line.ProductId : input.ProductId;
        var product = await EnsureActiveProductAsync(productId);
        var bom = await EnsurePublishedBomAsync(product.Id);
        var price = await _suggestedPrices.FindAtDateAsync(product.Id, order.OrderDate);
        var priceVersionId = price?.Id;
        var suggestedPrice = price?.Price.Amount;
        await EnsureOverridePermissionAsync(suggestedPrice, input.ActualSellingPrice);
        order.UpdateLine(
            lineId,
            product.Id,
            bom.Id,
            input.Quantity,
            priceVersionId,
            suggestedPrice,
            input.ActualSellingPrice,
            input.OverrideReason);
        await _salesOrders.UpdateAsync(order, autoSave: true);
        return _mapper.ToDto(order, includeCost: false, includeProfit: false);
    }

    [Authorize(VPureLuxPermissions.Sales.Edit)]
    public async Task<SalesOrderDto> UpdateLinesAsync(Guid id, UpdateSalesOrderLinesDto input)
    {
        var order = await GetOrderAsync(id);
        var seenLineIds = new HashSet<Guid>();
        var preparedLines = new List<PreparedDraftLineUpdate>();

        foreach (var inputLine in input.Lines)
        {
            if (inputLine.LineId == Guid.Empty || !seenLineIds.Add(inputLine.LineId))
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
            }

            var line = order.Lines.SingleOrDefault(x => x.Id == inputLine.LineId)
                ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
            var productId = inputLine.ProductId == Guid.Empty ? line.ProductId : inputLine.ProductId;
            var product = await EnsureActiveProductAsync(productId);
            var bom = await EnsurePublishedBomAsync(product.Id);
            var price = await _suggestedPrices.FindAtDateAsync(product.Id, order.OrderDate);
            var suggestedPrice = price?.Price.Amount;
            await EnsureOverridePermissionAsync(suggestedPrice, inputLine.ActualSellingPrice);
            preparedLines.Add(new PreparedDraftLineUpdate(
                inputLine.LineId,
                product.Id,
                bom.Id,
                inputLine.Quantity,
                price?.Id,
                suggestedPrice,
                inputLine.ActualSellingPrice,
                inputLine.OverrideReason));
        }

        foreach (var line in preparedLines)
        {
            order.UpdateLine(
                line.LineId,
                line.ProductId,
                line.BomVersionId,
                line.Quantity,
                line.SuggestedPriceVersionId,
                line.SuggestedPrice,
                line.ActualSellingPrice,
                line.OverrideReason);
        }

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
        if (order.Status == SalesOrderStatus.Draft)
        {
            order.CancelDraft(Clock.Now);
        }
        else if (order.Status == SalesOrderStatus.Confirmed)
        {
            var summary = await GetPaymentSummaryAsync(order);
            if (summary.PaymentStatus != SalesOrderReceivableStatus.Unpaid || summary.PaidAmount != 0)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesConfirmedOrderCancelRequiresUnpaid);
            }

            await RollbackConfirmedOrderInventoryAsync(order);
            order.CancelConfirmedUnpaid(Clock.Now);
        }
        else
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderAlreadyCancelled);
        }

        await _salesOrders.UpdateAsync(order, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Sales.Payments.Manage)]
    public async Task<SalesOrderPaymentDto> AddPaymentAsync(Guid id, CreateSalesOrderPaymentDto input)
    {
        var order = await GetOrderAsync(id);
        if (order.Status != SalesOrderStatus.Confirmed)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesPaymentRequiresConfirmedOrder);
        }
        if (input.Amount <= 0 || input.PaymentDate == default || !Enum.IsDefined(input.PaymentMethod))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var idempotencyKey = input.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var existing = await _payments.FindByIdempotencyKeyAsync(idempotencyKey);
        if (existing != null)
        {
            if (existing.SalesOrderId != order.Id)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesPaymentIdempotencyConflict);
            }
            return _mapper.ToDto(existing);
        }

        var summary = await GetPaymentSummaryAsync(order);
        if (input.Amount > summary.RemainingAmount)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesPaymentOverpaymentNotAllowed);
        }

        var payment = new SalesOrderPayment(
            GuidGenerator.Create(),
            order.Id,
            order.CustomerId,
            input.Amount,
            input.PaymentDate,
            input.PaymentMethod,
            input.ReferenceNo,
            input.Note,
            idempotencyKey);
        await _payments.InsertAsync(payment, autoSave: true);
        return _mapper.ToDto(payment);
    }

    public async Task<SalesOrderPaymentSummaryDto> GetPaymentSummaryAsync(Guid id)
    {
        var order = await GetOrderAsync(id);
        return _mapper.ToDto(await GetPaymentSummaryAsync(order));
    }

    public async Task<List<SalesOrderPaymentDto>> GetPaymentsAsync(Guid id)
    {
        var order = await GetOrderAsync(id);
        var payments = await _payments.GetListBySalesOrderIdAsync(order.Id);
        return payments.Select(_mapper.ToDto).ToList();
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

    [Authorize(VPureLuxPermissions.Sales.ViewCustomerHistory)]
    [Authorize(VPureLuxPermissions.Sales.ViewProfit)]
    public async Task<CustomerReceivableSummaryDto> GetCustomerReceivableSummaryAsync(Guid customerId)
    {
        if (await _customers.FindAsync(customerId) == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerNotFound);
        }

        var orders = await _salesOrders.GetListAsync(customerId, SalesOrderStatus.Confirmed);
        var summaries = await GetPaymentSummariesAsync(orders);
        return new CustomerReceivableSummaryDto
        {
            CustomerId = customerId,
            ConfirmedSalesTotal = summaries.Values.Sum(x => x.TotalAmount),
            PaidTotal = summaries.Values.Sum(x => x.PaidAmount),
            RemainingDebt = summaries.Values.Sum(x => Math.Max(x.RemainingAmount, 0)),
            UnpaidOrPartialOrderCount = summaries.Values.Count(x =>
                x.PaymentStatus is SalesOrderReceivableStatus.Unpaid or SalesOrderReceivableStatus.PartiallyPaid)
        };
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
        foreach (var item in bom.OrderedItems)
        {
            var component = await EnsureActiveComponentAsync(item.ComponentId);
            var required = item.Quantity * line.Quantity;
            requirements.Add((component, required));
            snapshotItems.Add(new SalesOrderBomSnapshotData(
                component.Id, component.Code, component.Name, component.Unit, item.Quantity, required));
        }
        var result = await PostInventoryIssueAsync(order, line, product, bom.Id, requirements);
        order.ApplyLineConfirmationSnapshot(
            line.Id, product.Code, product.Name, SalesConsts.DefaultProductUnit, bom.VersionNo.Value,
            result.Id, result.Cost, snapshotItems);
    }

    private async Task<(Guid Id, decimal Cost)> PostInventoryIssueAsync(
        SalesOrder order,
        SalesOrderLine salesLine,
        Product product,
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
        foreach (var requirement in consolidated)
        {
            StockItem? stockItem = null;
            IReadOnlyList<InventoryLot> availableLots = Array.Empty<InventoryLot>();

            try
            {
                stockItem = await _stockItems.FindByCatalogItemAsync(StockItemType.Component, requirement.Component.Id)
                    ?? throw new BusinessException(VPureLuxDomainErrorCodes.StockItemNotFound);
                await _inventoryManager.EnsureWarehouseAndStockItemUsableAsync(order.WarehouseId, stockItem.Id);
                availableLots = await _lots.GetAvailableFifoLotsAsync(order.WarehouseId, stockItem.Id);
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
            catch (BusinessException exception) when (IsSalesInventoryContextException(exception))
            {
                throw CreateSalesInventoryValidationException(
                    exception,
                    order,
                    salesLine,
                    product,
                    requirement.Component,
                    stockItem,
                    requirement.Quantity,
                    availableLots);
            }
        }

        transaction.Post(Clock.Now);
        await _inventoryTransactions.InsertAsync(transaction);
        return (transaction.Id, transaction.TotalIssueCost);
    }

    private static bool IsSalesInventoryContextException(BusinessException exception) =>
        exception.Code == VPureLuxDomainErrorCodes.ValidationFailed ||
        exception.Code?.StartsWith("INV_", StringComparison.Ordinal) == true;

    private BusinessException CreateSalesInventoryValidationException(
        BusinessException exception,
        SalesOrder order,
        SalesOrderLine salesLine,
        Product product,
        Component component,
        StockItem? stockItem,
        decimal requiredQuantity,
        IReadOnlyList<InventoryLot> availableLots)
    {
        var totalAvailable = availableLots.Sum(x => x.AvailableQuantity);
        var firstLot = availableLots.FirstOrDefault();
        var invalidField = exception.Data.Keys.OfType<string>().FirstOrDefault();
        var invalidValue = invalidField == null ? null : exception.Data[invalidField];

        _logger.LogWarning(
            exception,
            "Sales inventory validation failed. OrderId={SalesOrderId}, LineId={SalesOrderLineId}, LineNo={SalesLineNo}, ProductId={ProductId}, ProductCode={ProductCode}, ProductName={ProductName}, ComponentId={ComponentId}, ComponentCode={ComponentCode}, ComponentName={ComponentName}, StockItemId={StockItemId}, RequiredQuantity={RequiredQuantity}, AvailableQuantity={AvailableQuantity}, InventoryErrorCode={InventoryErrorCode}, InvalidField={InvalidField}, InvalidValue={InvalidValue}, FirstLotId={FirstLotId}, FirstLotNo={FirstLotNo}, FirstLotAvailableQuantity={FirstLotAvailableQuantity}.",
            order.Id,
            salesLine.Id,
            salesLine.LineNo,
            product.Id,
            product.Code,
            product.Name,
            component.Id,
            component.Code,
            component.Name,
            stockItem?.Id,
            requiredQuantity,
            totalAvailable,
            exception.Code,
            invalidField,
            invalidValue,
            firstLot?.Id,
            firstLot?.LotNo,
            firstLot?.AvailableQuantity);

        var salesException = new BusinessException(VPureLuxDomainErrorCodes.SalesInventoryValidationFailed)
            .WithData("InventoryErrorCode", exception.Code ?? string.Empty)
            .WithData("SalesOrderId", order.Id)
            .WithData("SalesOrderLineId", salesLine.Id)
            .WithData("SalesLineNo", salesLine.LineNo)
            .WithData("SalesLineQuantity", salesLine.Quantity)
            .WithData("ProductId", product.Id)
            .WithData("ProductCode", product.Code)
            .WithData("ProductName", product.Name)
            .WithData("ComponentId", component.Id)
            .WithData("ComponentCode", component.Code)
            .WithData("ComponentName", component.Name)
            .WithData("ComponentUnit", component.Unit)
            .WithData("RequiredQuantity", requiredQuantity)
            .WithData("AvailableQuantity", totalAvailable);

        if (stockItem != null)
        {
            salesException.WithData("StockItemId", stockItem.Id);
        }

        if (firstLot != null)
        {
            salesException
                .WithData("InventoryLotId", firstLot.Id)
                .WithData("LotNo", firstLot.LotNo)
                .WithData("LotAvailableQuantity", firstLot.AvailableQuantity);
        }

        if (invalidField != null)
        {
            salesException.WithData("InvalidField", invalidField);
            if (invalidValue != null)
            {
                salesException.WithData("InvalidValue", invalidValue);
            }
        }

        return salesException;
    }

    private async Task RollbackConfirmedOrderInventoryAsync(SalesOrder order)
    {
        foreach (var line in order.Lines.OrderBy(x => x.LineNo))
        {
            if (!line.InventoryTransactionId.HasValue)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderCannotBeModified);
            }

            var issue = await _inventoryTransactions.FindAsync(line.InventoryTransactionId.Value, includeDetails: true)
                ?? throw new BusinessException(VPureLuxDomainErrorCodes.InventoryTransactionNotFound);
            if (issue.Type != InventoryTransactionType.SalesIssue ||
                issue.ReferenceType != "SalesOrderLine" ||
                issue.ReferenceId != line.Id)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderCannotBeModified);
            }

            await RollbackSalesIssueAsync(order, line.Id, issue);
        }
    }

    private async Task RollbackSalesIssueAsync(SalesOrder order, Guid salesLineId, InventoryTransaction issue)
    {
        var allocations = issue.Lines
            .SelectMany(line => line.Allocations.Select(allocation => new
            {
                line.StockItemId,
                allocation.InventoryLotId,
                allocation.Quantity,
                allocation.UnitCost,
                allocation.TotalCost
            }))
            .OrderBy(x => x.InventoryLotId)
            .ThenBy(x => x.StockItemId)
            .ToList();
        if (allocations.Count == 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderCannotBeModified);
        }

        var idempotencyKey = $"sales-cancel:{order.Id}:line:{salesLineId}";
        var hash = Hash($"{order.Id}|{salesLineId}|{issue.Id}|" +
                        string.Join(";", allocations.Select(x =>
                            $"{x.StockItemId}:{x.InventoryLotId}:{x.Quantity}:{x.UnitCost}")));
        var existing = await _inventoryManager.FindExistingTransactionAsync(idempotencyKey);
        if (existing != null)
        {
            if (existing.RequestHash != hash)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict);
            }

            return;
        }

        var transaction = _inventoryManager.CreateTransaction(
            order.WarehouseId,
            InventoryTransactionType.AdjustmentIncrease,
            idempotencyKey,
            hash,
            "SalesOrderLine",
            salesLineId,
            issue.BomVersionId,
            "Hủy đơn bán hàng chưa thanh toán");
        var postedAt = Clock.Now;

        foreach (var allocation in allocations)
        {
            var lot = await _lots.GetAsync(allocation.InventoryLotId);
            if (lot.WarehouseId != order.WarehouseId || lot.StockItemId != allocation.StockItemId)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderCannotBeModified);
            }

            lot.Restore(allocation.Quantity);
            await _lots.UpdateAsync(lot);
            transaction.AddReceiptLine(
                GuidGenerator.Create(),
                allocation.StockItemId,
                allocation.Quantity,
                lot.LotNo,
                postedAt,
                allocation.UnitCost);
            await _balances.ApplyMovementAsync(
                order.WarehouseId,
                allocation.StockItemId,
                allocation.Quantity,
                allocation.TotalCost,
                postedAt);
        }

        transaction.Post(postedAt);
        await _inventoryTransactions.InsertAsync(transaction);
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

    private async Task<PagedResultDto<SalesOrderDto>> GetListFilteredByPaymentStatusAsync(GetSalesOrderListInput input)
    {
        var orders = await _salesOrders.GetListAsync(input.CustomerId, input.Status, input.Sorting);
        var visibility = await GetFinancialVisibilityAsync();
        var summaries = await GetPaymentSummariesAsync(orders);
        var filtered = orders
            .Where(x => summaries[x.Id].PaymentStatus == input.PaymentStatus)
            .ToList();
        var page = filtered
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();
        var items = page.Select(x => _mapper.ToDto(x, visibility.Cost, visibility.Profit, summaries[x.Id])).ToList();
        await FillMissingCustomerSnapshotsAsync(items);
        return new PagedResultDto<SalesOrderDto>(
            filtered.Count,
            items);
    }

    private async Task FillMissingCustomerSnapshotsAsync(List<SalesOrderDto> orders)
    {
        var missingCustomerIds = orders
            .Where(x => string.IsNullOrWhiteSpace(x.CustomerNameSnapshot) || string.IsNullOrWhiteSpace(x.CustomerCodeSnapshot))
            .Select(x => x.CustomerId)
            .Distinct()
            .ToList();
        if (missingCustomerIds.Count == 0)
        {
            return;
        }

        var customers = new Dictionary<Guid, Customer>();
        foreach (var customerId in missingCustomerIds)
        {
            var customer = await _customers.FindAsync(customerId);
            if (customer != null)
            {
                customers[customer.Id] = customer;
            }
        }

        foreach (var order in orders)
        {
            if (!customers.TryGetValue(order.CustomerId, out var customer))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(order.CustomerCodeSnapshot))
            {
                order.CustomerCodeSnapshot = customer.Code;
            }

            if (string.IsNullOrWhiteSpace(order.CustomerNameSnapshot))
            {
                order.CustomerNameSnapshot = customer.Name;
            }
        }
    }

    private async Task<Dictionary<Guid, SalesOrderPaymentSummary>> GetPaymentSummariesAsync(List<SalesOrder> orders)
    {
        var paidAmounts = await _payments.GetPostedPaidAmountsAsync(orders.Select(x => x.Id));
        return orders.ToDictionary(
            x => x.Id,
            x => x.Status == SalesOrderStatus.Confirmed
                ? SalesOrderPaymentSummary.From(
                    x.TotalRevenueAmount,
                    paidAmounts.TryGetValue(x.Id, out var paidAmount) ? paidAmount : 0)
                : new SalesOrderPaymentSummary(0, 0, 0, SalesOrderReceivableStatus.NotApplicable));
    }

    private async Task<SalesOrderPaymentSummary> GetPaymentSummaryAsync(SalesOrder order)
    {
        if (order.Status != SalesOrderStatus.Confirmed)
        {
            return new SalesOrderPaymentSummary(0, 0, 0, SalesOrderReceivableStatus.NotApplicable);
        }

        var paidAmounts = await _payments.GetPostedPaidAmountsAsync([order.Id]);
        return SalesOrderPaymentSummary.From(
            order.TotalRevenueAmount,
            paidAmounts.TryGetValue(order.Id, out var paidAmount) ? paidAmount : 0);
    }

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

    private sealed record PreparedDraftLineUpdate(
        Guid LineId,
        Guid ProductId,
        Guid BomVersionId,
        decimal Quantity,
        Guid? SuggestedPriceVersionId,
        decimal? SuggestedPrice,
        decimal ActualSellingPrice,
        string? OverrideReason);
}
