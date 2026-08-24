using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.BusinessCodes;
using VPureLux.Catalog;
using VPureLux.Customers;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;

namespace VPureLux.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
public class ServiceAppService : ApplicationService, IServiceAppService
{
    private const string OrderSequence = "ServiceOrder";
    private const string OrderPrefix = "SVC";

    private readonly IServiceOrderRepository _orders;
    private readonly IServicePaymentRepository _payments;
    private readonly IServiceReadRepository _readRepository;
    private readonly IRepository<CustomerAsset, Guid> _assets;
    private readonly IRepository<CustomerAssetComponent, Guid> _assetComponents;
    private readonly IRepository<AssetMaintenanceEvent, Guid> _maintenanceEvents;
    private readonly IRepository<AssetReplacementReminder, Guid> _reminders;
    private readonly IComponentReplacementPolicyRepository _policies;
    private readonly IRepository<Component, Guid> _components;
    private readonly IServiceWorkRepository _works;
    private readonly IWarehouseRepository _warehouses;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryLotRepository _lots;
    private readonly IInventoryTransactionRepository _inventoryTransactions;
    private readonly IInventoryBalanceRepository _balances;
    private readonly InventoryManager _inventoryManager;
    private readonly IBusinessCodeGenerator _codeGenerator;
    private readonly IClock _clock;

    public ServiceAppService(
        IServiceOrderRepository orders,
        IServicePaymentRepository payments,
        IServiceReadRepository readRepository,
        IRepository<CustomerAsset, Guid> assets,
        IRepository<CustomerAssetComponent, Guid> assetComponents,
        IRepository<AssetMaintenanceEvent, Guid> maintenanceEvents,
        IRepository<AssetReplacementReminder, Guid> reminders,
        IComponentReplacementPolicyRepository policies,
        IRepository<Component, Guid> components,
        IServiceWorkRepository works,
        IWarehouseRepository warehouses,
        IStockItemRepository stockItems,
        IInventoryLotRepository lots,
        IInventoryTransactionRepository inventoryTransactions,
        IInventoryBalanceRepository balances,
        InventoryManager inventoryManager,
        IBusinessCodeGenerator codeGenerator,
        IClock clock)
    {
        _orders = orders;
        _payments = payments;
        _readRepository = readRepository;
        _assets = assets;
        _assetComponents = assetComponents;
        _maintenanceEvents = maintenanceEvents;
        _reminders = reminders;
        _policies = policies;
        _components = components;
        _works = works;
        _warehouses = warehouses;
        _stockItems = stockItems;
        _lots = lots;
        _inventoryTransactions = inventoryTransactions;
        _balances = balances;
        _inventoryManager = inventoryManager;
        _codeGenerator = codeGenerator;
        _clock = clock;
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServiceOrderListDto>> GetListAsync(GetServiceOrderListInput input)
    {
        var filter = new ServiceOrderFilter
        {
            SearchText = input.SearchText,
            CustomerId = input.CustomerId,
            CustomerAssetId = input.CustomerAssetId,
            Status = input.Status,
            FromDate = input.FromDate,
            ToDate = input.ToDate,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };
        var count = await _readRepository.GetOrderCountAsync(filter);
        var items = await _readRepository.GetOrderListAsync(filter);
        return new PagedResultDto<ServiceOrderListDto>(count, items.Select(MapList).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<ServiceOrderDto> GetAsync(Guid id) => await MapAsync(await GetOrderAsync(id));

    [Authorize(VPureLuxPermissions.Service.Create)]
    public async Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto input)
    {
        var asset = await _assets.FindAsync(input.CustomerAssetId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        EnsureAssetUsable(asset);
        await EnsureWarehouseUsableAsync(input.WarehouseId);
        var definitions = await ResolveLinesAsync(asset.Id, input.Lines);
        var orderDate = input.OrderDate ?? _clock.Now;
        var orderNo = await GenerateOrderNoAsync(orderDate);
        var assetName = ResolveAssetName(asset);
        var order = new ServiceOrder(
            GuidGenerator.Create(), orderNo, asset.CustomerId, asset.Id, input.WarehouseId, orderDate,
            asset.CustomerCodeSnapshot, asset.CustomerNameSnapshot, asset.AssetNo, assetName,
            input.ScheduledAt, input.TechnicianUserId, input.ServiceAddress ?? asset.InstallationAddress, input.Note);
        AddLines(order, input.Lines, definitions);
        await _orders.InsertAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Edit)]
    public async Task<ServiceOrderDto> UpdateAsync(Guid id, UpdateServiceOrderDto input)
    {
        var order = await GetOrderAsync(id);
        var definitions = await ResolveLinesAsync(order.CustomerAssetId, input.Lines);
        order.UpdatePlan(input.ScheduledAt, input.TechnicianUserId, input.ServiceAddress, input.Note);
        foreach (var line in order.Lines.ToList())
        {
            order.RemoveLine(line.Id);
        }
        AddLines(order, input.Lines, definitions);
        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Confirm)]
    public async Task<ServiceOrderDto> ConfirmAsync(Guid id)
    {
        var order = await GetOrderAsync(id);
        order.Confirm(_clock.Now);
        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Confirm)]
    public async Task<ServiceOrderDto> StartAsync(Guid id)
    {
        var order = await GetOrderAsync(id);
        order.Start(_clock.Now);
        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Complete)]
    public async Task<ServiceOrderDto> CompleteAsync(Guid id, CompleteServiceOrderDto input)
    {
        var order = await GetOrderAsync(id);
        if (order.Status == ServiceOrderStatus.Completed && order.CompletionIdempotencyKey == input.IdempotencyKey)
        {
            return await MapAsync(order);
        }

        var quantities = input.Lines.GroupBy(x => x.LineId).ToDictionary(x => x.Key, x => x.Single().ActualQuantity);
        if (quantities.Count != order.Lines.Count || order.Lines.Any(line => !quantities.ContainsKey(line.Id)))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        var completedAt = input.CompletedAt ?? _clock.Now;
        var materialLines = order.Lines
            .Where(x => x.LineType == ServiceOrderLineType.Material && quantities[x.Id] > 0)
            .ToList();

        var inventoryTransaction = await CreateInventoryIssueAsync(order, materialLines, quantities, input.IdempotencyKey, completedAt);
        foreach (var line in order.Lines.Where(x => x.LineType == ServiceOrderLineType.Labor))
        {
            order.CompleteLine(line.Id, quantities[line.Id], 0);
        }

        await ApplyMaintenanceScheduleAsync(order, materialLines, completedAt);
        order.Complete(input.IdempotencyKey, completedAt, inventoryTransaction?.Id);
        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Cancel)]
    public async Task CancelAsync(Guid id)
    {
        var order = await GetOrderAsync(id);
        order.Cancel(_clock.Now);
        await _orders.UpdateAsync(order, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Service.ManagePayments)]
    public async Task<ServicePaymentDto> AddPaymentAsync(Guid id, CreateServicePaymentDto input)
    {
        var replay = await _payments.FindByIdempotencyKeyAsync(input.IdempotencyKey);
        if (replay != null)
        {
            if (replay.ServiceOrderId != id || replay.Amount != input.Amount)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ServicePaymentIdempotencyConflict);
            }
            return MapPayment(replay);
        }

        var order = await GetOrderAsync(id);
        if (order.Status == ServiceOrderStatus.Cancelled)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServicePaymentRequiresActiveOrder);
        }
        var existingPaid = (await _payments.GetByOrderIdAsync(id))
            .Where(x => x.Status == ServicePaymentStatus.Posted).Sum(x => x.Amount);
        var receivableCap = order.Status == ServiceOrderStatus.Completed
            ? order.TotalRevenueAmount
            : order.Lines.Sum(x => x.PlannedQuantity * x.UnitPrice);
        if (existingPaid + input.Amount > receivableCap)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServicePaymentOverpaymentNotAllowed);
        }

        var payment = new ServicePayment(
            GuidGenerator.Create(), id, order.CustomerId, input.Amount, input.PaymentDate,
            input.PaymentMethod, input.IdempotencyKey, input.ReferenceNo, input.Note);
        await _payments.InsertAsync(payment, autoSave: true);
        return MapPayment(payment);
    }

    [Authorize(VPureLuxPermissions.Service.ManagePayments)]
    public async Task VoidPaymentAsync(Guid id, Guid paymentId)
    {
        await GetOrderAsync(id);
        var payment = await _payments.FindAsync(paymentId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        if (payment.ServiceOrderId != id)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        }

        payment.Void();
        await _payments.UpdateAsync(payment, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServicePaymentDto>> GetPaymentsAsync(Guid id)
    {
        await GetOrderAsync(id);
        return (await _payments.GetByOrderIdAsync(id)).Select(MapPayment).ToList();
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServiceAssetOptionDto>> GetAssetOptionsAsync(ServiceLookupInput input) =>
        (await _readRepository.GetAssetOptionsAsync(input.CustomerId, input.SearchText, input.MaxResultCount))
        .Select(x => new ServiceAssetOptionDto
        {
            Id = x.Id, CustomerId = x.CustomerId, CustomerCode = x.CustomerCode, CustomerName = x.CustomerName,
            AssetNo = x.AssetNo, AssetName = x.AssetName, ServiceAddress = x.ServiceAddress
        }).ToList();

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<ServiceAssetOptionDto> GetAssetOptionAsync(Guid id)
    {
        var asset = await _assets.FindAsync(id) ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        return new ServiceAssetOptionDto
        {
            Id = asset.Id, CustomerId = asset.CustomerId, CustomerCode = asset.CustomerCodeSnapshot,
            CustomerName = asset.CustomerNameSnapshot, AssetNo = asset.AssetNo, AssetName = ResolveAssetName(asset),
            ServiceAddress = asset.InstallationAddress
        };
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServiceAssetPositionOptionDto>> GetAssetPositionOptionsAsync(Guid customerAssetId)
    {
        var query = await _assetComponents.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(query.Where(x => x.CustomerAssetId == customerAssetId &&
            x.Status != CustomerAssetComponentStatus.Inactive).OrderBy(x => x.PositionCode));
        return items.Select(x => new ServiceAssetPositionOptionDto
        {
            Id = x.Id, PositionCode = x.PositionCode, PositionName = x.PositionName, ComponentId = x.ComponentId,
            ComponentCode = x.ComponentCodeSnapshot, ComponentName = x.ComponentNameSnapshot
        }).ToList();
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServiceMaterialOptionDto>> GetMaterialOptionsAsync(ServiceLookupInput input) =>
        (await _readRepository.GetMaterialOptionsAsync(input.SearchText, input.MaxResultCount)).Select(x => new ServiceMaterialOptionDto
        {
            ComponentId = x.ComponentId, StockItemId = x.StockItemId, Code = x.Code, Name = x.Name,
            Unit = x.Unit, SuggestedPrice = x.SuggestedPrice
        }).ToList();

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServiceWorkOptionDto>> GetWorkOptionsAsync(ServiceLookupInput input) =>
        (await _readRepository.GetWorkOptionsAsync(input.SearchText, input.MaxResultCount)).Select(x => new ServiceWorkOptionDto
        {
            Id = x.Id, Code = x.Code, Name = x.Name, DefaultPrice = x.DefaultPrice
        }).ToList();

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServiceWarehouseOptionDto>> GetWarehouseOptionsAsync()
    {
        var query = await _warehouses.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(query.Where(x => x.Status == InventoryEntityStatus.Active)
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.Code));
        return items.Select(x => new ServiceWarehouseOptionDto
        {
            Id = x.Id, Code = x.Code, Name = x.Name, IsDefault = x.IsDefault
        }).ToList();
    }

    private async Task<InventoryTransaction?> CreateInventoryIssueAsync(
        ServiceOrder order,
        IReadOnlyCollection<ServiceOrderLine> materialLines,
        IReadOnlyDictionary<Guid, int> quantities,
        string completionKey,
        DateTime completedAt)
    {
        if (materialLines.Count == 0)
        {
            return null;
        }

        await EnsureWarehouseUsableAsync(order.WarehouseId);
        var componentIds = materialLines.Select(x => x.ComponentId!.Value).Distinct().ToHashSet();
        var stockQuery = await _stockItems.GetQueryableAsync();
        var stockItems = await AsyncExecuter.ToListAsync(stockQuery.Where(x =>
            x.ItemType == StockItemType.Component && componentIds.Contains(x.CatalogItemId)));
        var stockByComponent = stockItems.ToDictionary(x => x.CatalogItemId);
        if (stockByComponent.Count != componentIds.Count || stockItems.Any(x =>
                x.Status != InventoryEntityStatus.Active || !x.IsInventoryEnabled))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.StockItemNotFound);
        }

        var availableLots = await _lots.GetAvailableFifoLotsAsync(order.WarehouseId, stockItems.Select(x => x.Id).ToHashSet());
        var transactionKey = $"SERVICE-{order.Id:N}";
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join("|", order.Id, completionKey, string.Join(",", materialLines.OrderBy(x => x.Id)
                .Select(x => $"{x.Id:N}:{quantities[x.Id]}"))))));
        var transaction = _inventoryManager.CreateTransaction(
            order.WarehouseId, InventoryTransactionType.ServiceIssue, transactionKey, requestHash,
            nameof(ServiceOrder), order.Id, reason: $"Service {order.OrderNo}");

        var movementByStock = new Dictionary<Guid, (decimal Quantity, decimal Cost)>();
        var allocatedLotIds = new HashSet<Guid>();
        foreach (var serviceLine in materialLines)
        {
            var stockItem = stockByComponent[serviceLine.ComponentId!.Value];
            var quantity = quantities[serviceLine.Id];
            var issueLine = transaction.AddIssueLine(GuidGenerator.Create(), stockItem.Id, quantity);
            var allocations = _inventoryManager.AllocateFifo(transaction, issueLine, availableLots);
            allocatedLotIds.UnionWith(allocations.Select(x => x.InventoryLotId));
            var cost = allocations.Sum(x => x.TotalCost);
            order.CompleteLine(serviceLine.Id, quantity, cost);
            if (movementByStock.TryGetValue(stockItem.Id, out var movement))
            {
                movementByStock[stockItem.Id] = (movement.Quantity + quantity, movement.Cost + cost);
            }
            else
            {
                movementByStock[stockItem.Id] = (quantity, cost);
            }
        }

        transaction.Post(completedAt);
        await _inventoryTransactions.InsertAsync(transaction);
        await _lots.UpdateManyAsync(availableLots.Where(x => allocatedLotIds.Contains(x.Id)));
        foreach (var movement in movementByStock)
        {
            await _balances.ApplyMovementAsync(order.WarehouseId, movement.Key,
                -movement.Value.Quantity, -movement.Value.Cost, completedAt);
        }
        return transaction;
    }

    private async Task ApplyMaintenanceScheduleAsync(
        ServiceOrder order,
        IReadOnlyCollection<ServiceOrderLine> materialLines,
        DateTime completedAt)
    {
        var scheduledLines = materialLines.Where(x => x.CustomerAssetComponentId.HasValue)
            .GroupBy(x => x.CustomerAssetComponentId!.Value).Select(x => x.Single()).ToList();
        if (scheduledLines.Count == 0)
        {
            return;
        }

        var positionIds = scheduledLines.Select(x => x.CustomerAssetComponentId!.Value).ToHashSet();
        var positionQuery = await _assetComponents.GetQueryableAsync();
        var positions = await AsyncExecuter.ToListAsync(positionQuery.Where(x => positionIds.Contains(x.Id)));
        if (positions.Count != positionIds.Count || positions.Any(x => x.CustomerAssetId != order.CustomerAssetId))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceMaterialPositionMismatch);
        }
        var positionById = positions.ToDictionary(x => x.Id);
        var policies = (await _policies.GetEnabledByComponentIdsAsync(
            scheduledLines.Select(x => x.ComponentId!.Value).Distinct().ToList())).ToDictionary(x => x.ComponentId);
        var reminderQuery = await _reminders.GetQueryableAsync();
        var openReminders = await AsyncExecuter.ToListAsync(reminderQuery.Where(x =>
            x.CustomerAssetComponentId.HasValue && positionIds.Contains(x.CustomerAssetComponentId.Value) &&
            x.Status == AssetReplacementReminderStatus.Pending));
        var remindersByPosition = openReminders.GroupBy(x => x.CustomerAssetComponentId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());
        var newReminders = new List<AssetReplacementReminder>();
        var events = new List<AssetMaintenanceEvent>();

        foreach (var line in scheduledLines)
        {
            var position = positionById[line.CustomerAssetComponentId!.Value];
            if (position.ComponentId != line.ComponentId)
            {
                position.MapComponent(line.ComponentId!.Value, line.ItemCodeSnapshot, line.ItemNameSnapshot, line.UnitSnapshot,
                    $"Mapped by service order {order.OrderNo}");
            }
            position.SetReplacementBaseline(completedAt);
            var idempotencyKey = $"SERVICE-{order.Id:N}-{line.Id:N}";
            events.Add(new AssetMaintenanceEvent(
                GuidGenerator.Create(), order.CustomerAssetId, position.Id, AssetMaintenanceEventType.Replacement,
                AssetMaintenanceSourceType.ServiceOrder, completedAt, idempotencyKey, order.Id,
                line.ComponentId, line.ItemCodeSnapshot, line.ItemNameSnapshot, order.OrderNo));

            AssetReplacementReminder? next = null;
            if (policies.TryGetValue(line.ComponentId!.Value, out var policy))
            {
                next = new AssetReplacementReminder(
                    GuidGenerator.Create(), order.CustomerAssetId, position.Id, line.ComponentId.Value,
                    null, null, line.ItemCodeSnapshot, line.ItemNameSnapshot, line.UnitSnapshot,
                    line.ActualQuantity, completedAt.Date.AddMonths(policy.CycleMonths),
                    policy.CycleMonths, policy.WarningDaysBeforeDue,
                    ReplacementReminderTriggerSource.Replacement, nameof(ServiceOrderLine), line.Id,
                    idempotencyKey, $"Service order {order.OrderNo}");
                newReminders.Add(next);
            }
            if (remindersByPosition.TryGetValue(position.Id, out var previous))
            {
                foreach (var reminder in previous)
                {
                    reminder.Complete(completedAt, CurrentUser.Id, next?.Id, $"Completed by service order {order.OrderNo}");
                }
            }
        }

        await _assetComponents.UpdateManyAsync(positions);
        if (openReminders.Count > 0) await _reminders.UpdateManyAsync(openReminders);
        if (newReminders.Count > 0) await _reminders.InsertManyAsync(newReminders);
        await _maintenanceEvents.InsertManyAsync(events);
    }

    private async Task<Dictionary<(ServiceOrderLineType Type, Guid Id), LineDefinition>> ResolveLinesAsync(
        Guid assetId,
        IReadOnlyCollection<ServiceOrderLineInput> inputs)
    {
        if (inputs.Count == 0 || inputs.Any(x => !Enum.IsDefined(x.LineType) || x.CatalogItemId == Guid.Empty || x.Quantity <= 0))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        var duplicatePositions = inputs.Where(x => x.LineType == ServiceOrderLineType.Material && x.CustomerAssetComponentId.HasValue)
            .GroupBy(x => x.CustomerAssetComponentId!.Value).Any(x => x.Count() > 1);
        if (duplicatePositions)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var materialIds = inputs.Where(x => x.LineType == ServiceOrderLineType.Material).Select(x => x.CatalogItemId).Distinct().ToHashSet();
        var workIds = inputs.Where(x => x.LineType == ServiceOrderLineType.Labor).Select(x => x.CatalogItemId).Distinct().ToHashSet();
        var componentQuery = await _components.GetQueryableAsync();
        var components = await AsyncExecuter.ToListAsync(componentQuery.Where(x => materialIds.Contains(x.Id)));
        var workQuery = await _works.GetQueryableAsync();
        var works = await AsyncExecuter.ToListAsync(workQuery.Where(x => workIds.Contains(x.Id)));
        if (components.Count != materialIds.Count || components.Any(x => x.Status != CatalogItemStatus.Active) ||
            works.Count != workIds.Count || works.Any(x => x.Status != ServiceWorkStatus.Active))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceWorkInactive);
        }

        var positionIds = inputs.Where(x => x.CustomerAssetComponentId.HasValue)
            .Select(x => x.CustomerAssetComponentId!.Value).Distinct().ToHashSet();
        if (positionIds.Count > 0)
        {
            var positionQuery = await _assetComponents.GetQueryableAsync();
            var positions = await AsyncExecuter.ToListAsync(positionQuery.Where(x => positionIds.Contains(x.Id)));
            if (positions.Count != positionIds.Count || positions.Any(x => x.CustomerAssetId != assetId))
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ServiceMaterialPositionMismatch);
            }
        }

        return components.Select(x => new KeyValuePair<(ServiceOrderLineType, Guid), LineDefinition>(
                (ServiceOrderLineType.Material, x.Id), new LineDefinition(x.Code, x.Name, x.Unit)))
            .Concat(works.Select(x => new KeyValuePair<(ServiceOrderLineType, Guid), LineDefinition>(
                (ServiceOrderLineType.Labor, x.Id), new LineDefinition(x.Code, x.Name, "Lần"))))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private void AddLines(ServiceOrder order, IEnumerable<ServiceOrderLineInput> inputs,
        IReadOnlyDictionary<(ServiceOrderLineType Type, Guid Id), LineDefinition> definitions)
    {
        foreach (var input in inputs)
        {
            var definition = definitions[(input.LineType, input.CatalogItemId)];
            order.AddLine(GuidGenerator.Create(), input.LineType, input.CatalogItemId,
                input.CustomerAssetComponentId, definition.Code, definition.Name, definition.Unit,
                input.Quantity, input.UnitPrice, input.Note);
        }
    }

    private async Task EnsureWarehouseUsableAsync(Guid warehouseId)
    {
        var warehouse = await _warehouses.FindAsync(warehouseId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseNotFound);
        if (warehouse.Status != InventoryEntityStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseInactive);
        }
    }

    private static void EnsureAssetUsable(CustomerAsset asset)
    {
        if (asset.Status is CustomerAssetStatus.Inactive or CustomerAssetStatus.Cancelled or CustomerAssetStatus.Transferred or CustomerAssetStatus.PendingInstallation)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
    }

    private async Task<ServiceOrder> GetOrderAsync(Guid id) =>
        await _orders.FindAsync(id) ?? throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderNotFound);

    private async Task<string> GenerateOrderNoAsync(DateTime orderDate)
    {
        var prefix = $"{OrderPrefix}-{orderDate:yyyyMMdd}";
        return await _codeGenerator.GenerateAsync(new BusinessCodeGenerationContext
        {
            SequenceName = OrderSequence,
            Prefix = OrderPrefix,
            Date = orderDate,
            ExistsAsync = (candidate, cancellationToken) => _orders.OrderNoExistsAsync(candidate, cancellationToken),
            SeedMaxAsync = async cancellationToken => (int?)await _orders.GetMaxOrderNoSequenceAsync(prefix, cancellationToken)
        });
    }

    private async Task<ServiceOrderDto> MapAsync(ServiceOrder order)
    {
        var paid = (await _payments.GetByOrderIdAsync(order.Id)).Where(x => x.Status == ServicePaymentStatus.Posted).Sum(x => x.Amount);
        var canViewCost = await AuthorizationService.IsGrantedAsync(VPureLuxPermissions.Service.ViewCost);
        var canViewProfit = await AuthorizationService.IsGrantedAsync(VPureLuxPermissions.Service.ViewProfit);
        return new ServiceOrderDto
        {
            Id = order.Id, OrderNo = order.OrderNo, CustomerId = order.CustomerId, CustomerAssetId = order.CustomerAssetId,
            WarehouseId = order.WarehouseId, OrderDate = order.OrderDate, ScheduledAt = order.ScheduledAt,
            TechnicianUserId = order.TechnicianUserId, Status = order.Status, CustomerCode = order.CustomerCodeSnapshot,
            CustomerName = order.CustomerNameSnapshot, AssetNo = order.AssetNoSnapshot, AssetName = order.AssetNameSnapshot,
            ServiceAddress = order.ServiceAddress, Note = order.Note, ConfirmedAt = order.ConfirmedAt,
            StartedAt = order.StartedAt, CompletedAt = order.CompletedAt, CancelledAt = order.CancelledAt,
            TotalRevenueAmount = order.TotalRevenueAmount, TotalCostAmount = canViewCost ? order.TotalCostAmount : 0,
            TotalProfitAmount = canViewProfit ? order.TotalProfitAmount : 0, PaidAmount = paid,
            Lines = order.Lines.OrderBy(x => x.LineNo).Select(x => new ServiceOrderLineDto
            {
                Id = x.Id, LineNo = x.LineNo, LineType = x.LineType, ComponentId = x.ComponentId,
                CustomerAssetComponentId = x.CustomerAssetComponentId, ServiceWorkId = x.ServiceWorkId,
                ItemCode = x.ItemCodeSnapshot, ItemName = x.ItemNameSnapshot, Unit = x.UnitSnapshot,
                PlannedQuantity = x.PlannedQuantity, ActualQuantity = x.ActualQuantity, UnitPrice = x.UnitPrice,
                RevenueAmount = x.RevenueAmount, CostAmount = canViewCost ? x.CostAmountSnapshot : 0, Note = x.Note
            }).ToList()
        };
    }

    private static ServiceOrderListDto MapList(ServiceOrderListItem x) => new()
    {
        Id = x.Id, OrderNo = x.OrderNo, OrderDate = x.OrderDate, ScheduledAt = x.ScheduledAt,
        CompletedAt = x.CompletedAt, Status = x.Status, CustomerId = x.CustomerId,
        CustomerCode = x.CustomerCode, CustomerName = x.CustomerName, CustomerAssetId = x.CustomerAssetId,
        AssetNo = x.AssetNo, AssetName = x.AssetName, TotalRevenueAmount = x.TotalRevenueAmount,
        PaidAmount = x.PaidAmount, LineCount = x.LineCount
    };

    private static ServicePaymentDto MapPayment(ServicePayment x) => new()
    {
        Id = x.Id, Amount = x.Amount, PaymentDate = x.PaymentDate, PaymentMethod = x.PaymentMethod,
        ReferenceNo = x.ReferenceNo, Note = x.Note, Status = x.Status
    };

    private static string ResolveAssetName(CustomerAsset asset) =>
        !string.IsNullOrWhiteSpace(asset.ProductNameSnapshot)
            ? asset.ProductNameSnapshot
            : string.Join(" ", new[] { asset.Brand, asset.Model }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private sealed record LineDefinition(string Code, string Name, string Unit);
}
