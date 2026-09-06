using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VPureLux.BusinessCodes;
using VPureLux.Catalog;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace VPureLux.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
public class ServiceOrderAppService : ApplicationService, IServiceOrderAppService
{
    private const string OrderSequence = "ServiceOrder";
    private const string OrderPrefix = "SVC";

    private readonly IServiceOrderRepository _orders;
    private readonly IServiceReadRepository _readRepository;
    private readonly IRepository<CustomerAsset, Guid> _assets;
    private readonly IRepository<CustomerAssetComponent, Guid> _assetComponents;
    private readonly IRepository<Component, Guid> _components;
    private readonly IServiceWorkRepository _works;
    private readonly IStockItemRepository _stockItems;
    private readonly IWarehouseRepository _warehouses;
    private readonly IRepository<IdentityUser, Guid> _users;
    private readonly IBusinessCodeGenerator _codeGenerator;
    private readonly IOptions<ServiceOptions> _options;
    private readonly IClock _clock;

    public ServiceOrderAppService(
        IServiceOrderRepository orders,
        IServiceReadRepository readRepository,
        IRepository<CustomerAsset, Guid> assets,
        IRepository<CustomerAssetComponent, Guid> assetComponents,
        IRepository<Component, Guid> components,
        IServiceWorkRepository works,
        IStockItemRepository stockItems,
        IWarehouseRepository warehouses,
        IRepository<IdentityUser, Guid> users,
        IBusinessCodeGenerator codeGenerator,
        IOptions<ServiceOptions> options,
        IClock clock)
    {
        _orders = orders;
        _readRepository = readRepository;
        _assets = assets;
        _assetComponents = assetComponents;
        _components = components;
        _works = works;
        _stockItems = stockItems;
        _warehouses = warehouses;
        _users = users;
        _codeGenerator = codeGenerator;
        _options = options;
        _clock = clock;
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServiceOrderListDto>> GetListAsync(GetServiceOrderListInput input)
    {
        EnsureEnabled();
        var filter = new ServiceOrderFilter
        {
            SearchText = input.SearchText,
            CustomerId = input.CustomerId,
            Status = input.Status,
            FromDate = input.FromDate,
            ToDate = input.ToDate,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = NormalizePageSize(input.MaxResultCount)
        };
        var count = await _readRepository.GetOrderCountAsync(filter);
        var items = await _readRepository.GetOrderListAsync(filter);
        return new PagedResultDto<ServiceOrderListDto>(count, items.Select(MapList).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<ServiceOrderDto> GetAsync(Guid id)
    {
        EnsureEnabled();
        return await MapAsync(await GetOrderAsync(id));
    }

    [Authorize(VPureLuxPermissions.Service.Create)]
    public async Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto input)
    {
        EnsureEnabled();
        EnsureLinesPresent(input.Lines);
        var asset = await GetUsableAssetAsync(input.CustomerAssetId);
        await EnsureWarehouseUsableAsync(input.WarehouseId);
        await EnsureTechnicianExistsAsync(input.TechnicianUserId);
        var selections = await ResolveSelectionsAsync(input.Lines);
        await ValidatePositionsAsync(asset.Id, input.Lines);

        var orderDate = input.OrderDate ?? _clock.Now;
        var order = new ServiceOrder(
            GuidGenerator.Create(),
            await GenerateOrderNoAsync(orderDate),
            asset.CustomerId,
            asset.Id,
            input.WarehouseId,
            orderDate,
            asset.CustomerCodeSnapshot,
            asset.CustomerNameSnapshot,
            asset.AssetNo,
            ResolveAssetName(asset),
            input.ScheduledAt,
            input.TechnicianUserId,
            input.ServiceAddress ?? asset.InstallationAddress,
            input.Note);

        foreach (var line in input.Lines)
        {
            var selection = selections[(line.LineType, line.CatalogItemId)];
            order.AddLine(
                GuidGenerator.Create(), line.LineType, line.CatalogItemId, line.CustomerAssetComponentId,
                selection.Code, selection.Name, selection.Unit, line.Quantity, line.UnitPrice,
                selection.StandardCost, line.Note);
        }

        await _orders.InsertAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Edit)]
    public async Task<ServiceOrderDto> UpdateAsync(Guid id, UpdateServiceOrderDto input)
    {
        EnsureEnabled();
        EnsureLinesPresent(input.Lines);
        var order = await GetOrderAsync(id);
        EnsureExpectedVersion(order, input.ConcurrencyStamp);
        await EnsureTechnicianExistsAsync(input.TechnicianUserId);

        var existingById = order.Lines.ToDictionary(line => line.Id);
        var suppliedIds = input.Lines.Where(line => line.Id.HasValue).Select(line => line.Id!.Value).ToList();
        if (suppliedIds.Count != suppliedIds.Distinct().Count() || suppliedIds.Any(lineId => !existingById.ContainsKey(lineId)))
        {
            throw new BusinessException(ServiceErrorCodes.InvalidLine).WithData("ServiceOrderId", id);
        }

        var changedSelections = input.Lines.Where(line =>
        {
            if (!line.Id.HasValue)
            {
                return true;
            }

            var existing = existingById[line.Id.Value];
            var currentCatalogId = existing.ComponentId ?? existing.ServiceWorkId;
            return existing.LineType != line.LineType || currentCatalogId != line.CatalogItemId;
        }).ToList();
        var selections = await ResolveSelectionsAsync(changedSelections);
        await ValidatePositionsAsync(order.CustomerAssetId, input.Lines);

        order.UpdateDraft(input.OrderDate, input.ScheduledAt, input.TechnicianUserId, input.ServiceAddress, input.Note);
        var retainedIds = suppliedIds.ToHashSet();
        foreach (var removed in order.Lines.Where(line => !retainedIds.Contains(line.Id)).ToList())
        {
            order.RemoveLine(removed.Id);
        }

        foreach (var line in input.Lines)
        {
            if (!line.Id.HasValue)
            {
                var added = selections[(line.LineType, line.CatalogItemId)];
                order.AddLine(
                    GuidGenerator.Create(), line.LineType, line.CatalogItemId, line.CustomerAssetComponentId,
                    added.Code, added.Name, added.Unit, line.Quantity, line.UnitPrice, added.StandardCost, line.Note);
                continue;
            }

            var existing = existingById[line.Id.Value];
            var currentCatalogId = existing.ComponentId ?? existing.ServiceWorkId;
            if (existing.LineType == line.LineType && currentCatalogId == line.CatalogItemId)
            {
                order.UpdateLine(existing.Id, line.CustomerAssetComponentId, line.Quantity, line.UnitPrice, line.Note);
                continue;
            }

            var replacement = selections[(line.LineType, line.CatalogItemId)];
            order.ReplaceLineSelection(
                existing.Id, line.LineType, line.CatalogItemId, line.CustomerAssetComponentId,
                replacement.Code, replacement.Name, replacement.Unit, line.Quantity, line.UnitPrice,
                replacement.StandardCost, line.Note);
        }

        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Confirm)]
    public async Task<ServiceOrderDto> ConfirmAsync(Guid id, ServiceOrderTransitionDto input)
    {
        EnsureEnabled();
        var order = await GetOrderAsync(id);
        EnsureExpectedVersion(order, input.ConcurrencyStamp);
        order.Confirm(_clock.Now);
        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Confirm)]
    public async Task<ServiceOrderDto> StartAsync(Guid id, ServiceOrderTransitionDto input)
    {
        EnsureEnabled();
        var order = await GetOrderAsync(id);
        EnsureExpectedVersion(order, input.ConcurrencyStamp);
        order.Start(_clock.Now);
        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.Cancel)]
    public async Task<ServiceOrderDto> CancelAsync(Guid id, CancelServiceOrderDto input)
    {
        EnsureEnabled();
        var order = await GetOrderAsync(id);
        EnsureExpectedVersion(order, input.ConcurrencyStamp);
        order.Cancel(_clock.Now, input.Reason);
        await _orders.UpdateAsync(order, autoSave: true);
        return await MapAsync(order);
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServiceAssetOptionDto>> GetAssetOptionsAsync(ServiceLookupInput input)
    {
        EnsureEnabled();
        var pageSize = NormalizePageSize(input.MaxResultCount);
        var count = await _readRepository.GetAssetCountAsync(input.CustomerId, input.SearchText);
        var items = await _readRepository.GetAssetOptionsAsync(
            input.CustomerId, input.SearchText, input.SkipCount, pageSize);
        return new PagedResultDto<ServiceAssetOptionDto>(count, items.Select(MapAsset).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<ServiceAssetOptionDto> GetAssetOptionAsync(Guid id)
    {
        EnsureEnabled();
        return MapAsset(await GetUsableAssetAsync(id));
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServiceAssetPositionOptionDto>> GetAssetPositionOptionsAsync(Guid customerAssetId)
    {
        EnsureEnabled();
        await GetUsableAssetAsync(customerAssetId);
        var query = await _assetComponents.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(query
            .Where(position => position.CustomerAssetId == customerAssetId &&
                               position.Status != CustomerAssetComponentStatus.Inactive)
            .OrderBy(position => position.PositionCode)
            .ThenBy(position => position.Id));
        return items.Select(position => new ServiceAssetPositionOptionDto
        {
            Id = position.Id,
            PositionCode = position.PositionCode,
            PositionName = position.PositionName,
            ComponentId = position.ComponentId,
            ComponentCode = position.ComponentCodeSnapshot,
            ComponentName = position.ComponentNameSnapshot
        }).ToList();
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServiceMaterialOptionDto>> GetMaterialOptionsAsync(ServiceLookupInput input)
    {
        EnsureEnabled();
        var pageSize = NormalizePageSize(input.MaxResultCount);
        var count = await _readRepository.GetMaterialCountAsync(input.SearchText);
        var items = await _readRepository.GetMaterialOptionsAsync(input.SearchText, input.SkipCount, pageSize);
        return new PagedResultDto<ServiceMaterialOptionDto>(count, items.Select(item => new ServiceMaterialOptionDto
        {
            Id = item.Id, Code = item.Code, Name = item.Name, Unit = item.Unit, SuggestedPrice = item.SuggestedPrice
        }).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServiceWorkOptionDto>> GetWorkOptionsAsync(ServiceLookupInput input)
    {
        EnsureEnabled();
        var pageSize = NormalizePageSize(input.MaxResultCount);
        var count = await _readRepository.GetWorkCountAsync(input.SearchText);
        var items = await _readRepository.GetWorkOptionsAsync(input.SearchText, input.SkipCount, pageSize);
        return new PagedResultDto<ServiceWorkOptionDto>(count, items.Select(item => new ServiceWorkOptionDto
        {
            Id = item.Id, Code = item.Code, Name = item.Name, Unit = item.Unit,
            DefaultPrice = item.DefaultPrice, StandardCost = item.StandardCost
        }).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<PagedResultDto<ServiceTechnicianOptionDto>> GetTechnicianOptionsAsync(ServiceLookupInput input)
    {
        EnsureEnabled();
        var pageSize = NormalizePageSize(input.MaxResultCount);
        var count = await _readRepository.GetTechnicianCountAsync(input.SearchText);
        var items = await _readRepository.GetTechnicianOptionsAsync(input.SearchText, input.SkipCount, pageSize);
        return new PagedResultDto<ServiceTechnicianOptionDto>(count, items.Select(item => new ServiceTechnicianOptionDto
        {
            Id = item.Id, Name = item.Name
        }).ToList());
    }

    [Authorize(VPureLuxPermissions.Service.View)]
    public async Task<List<ServiceWarehouseOptionDto>> GetWarehouseOptionsAsync()
    {
        EnsureEnabled();
        var query = await _warehouses.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(query
            .Where(warehouse => warehouse.Status == InventoryEntityStatus.Active)
            .OrderByDescending(warehouse => warehouse.IsDefault)
            .ThenBy(warehouse => warehouse.Code));
        return items.Select(warehouse => new ServiceWarehouseOptionDto
        {
            Id = warehouse.Id, Code = warehouse.Code, Name = warehouse.Name, IsDefault = warehouse.IsDefault
        }).ToList();
    }

    private async Task<ServiceOrder> GetOrderAsync(Guid id) => await _orders.GetAsync(id, includeDetails: true);

    private async Task<CustomerAsset> GetUsableAssetAsync(Guid id)
    {
        var asset = await _assets.FindAsync(id);
        if (asset == null || asset.Status is not (CustomerAssetStatus.Active or CustomerAssetStatus.PendingReview))
        {
            throw new BusinessException(ServiceErrorCodes.AssetUnavailable).WithData("CustomerAssetId", id);
        }

        return asset;
    }

    private async Task EnsureWarehouseUsableAsync(Guid id)
    {
        var warehouse = await _warehouses.FindAsync(id);
        if (warehouse == null || warehouse.Status != InventoryEntityStatus.Active)
        {
            throw new BusinessException(ServiceErrorCodes.InvalidLine)
                .WithData("Field", nameof(ServiceOrder.WarehouseId))
                .WithData("WarehouseId", id);
        }
    }

    private async Task EnsureTechnicianExistsAsync(Guid? id)
    {
        if (id.HasValue && await _users.FindAsync(id.Value) is not { IsActive: true })
        {
            throw new BusinessException(ServiceErrorCodes.InvalidLine)
                .WithData("Field", nameof(ServiceOrder.TechnicianUserId))
                .WithData("TechnicianUserId", id.Value);
        }
    }

    private async Task<Dictionary<(ServiceOrderLineType Type, Guid Id), LineSelection>> ResolveSelectionsAsync(
        IReadOnlyCollection<ServiceOrderLineInput> lines)
    {
        if (lines.Count == 0)
        {
            return [];
        }

        if (lines.Any(line => !Enum.IsDefined(line.LineType) || line.CatalogItemId == Guid.Empty ||
                              (line.LineType == ServiceOrderLineType.Labor && line.CustomerAssetComponentId.HasValue)))
        {
            throw new BusinessException(ServiceErrorCodes.InvalidLine).WithData("Reason", "InvalidLineTypeOrCatalogItem");
        }

        var result = new Dictionary<(ServiceOrderLineType Type, Guid Id), LineSelection>();
        var componentIds = lines.Where(line => line.LineType == ServiceOrderLineType.Material)
            .Select(line => line.CatalogItemId).Distinct().ToList();
        if (componentIds.Count > 0)
        {
            var componentQuery = await _components.GetQueryableAsync();
            var components = await AsyncExecuter.ToListAsync(componentQuery
                .Where(component => componentIds.Contains(component.Id)));
            var stockQuery = await _stockItems.GetQueryableAsync();
            var stockItems = await AsyncExecuter.ToListAsync(stockQuery.Where(stock =>
                stock.ItemType == StockItemType.Component && componentIds.Contains(stock.CatalogItemId)));
            var stockByComponent = stockItems.GroupBy(stock => stock.CatalogItemId)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (var componentId in componentIds)
            {
                var component = components.SingleOrDefault(item => item.Id == componentId);
                stockByComponent.TryGetValue(componentId, out var stock);
                if (component == null || component.Status != CatalogItemStatus.Active || stock == null ||
                    stock.Status != InventoryEntityStatus.Active || !stock.IsInventoryEnabled)
                {
                    throw new BusinessException(ServiceErrorCodes.MaterialUnavailable).WithData("ComponentId", componentId);
                }

                result[(ServiceOrderLineType.Material, componentId)] =
                    new LineSelection(component.Code, component.Name, component.Unit, null);
            }
        }

        var workIds = lines.Where(line => line.LineType == ServiceOrderLineType.Labor)
            .Select(line => line.CatalogItemId).Distinct().ToList();
        if (workIds.Count > 0)
        {
            var workQuery = await _works.GetQueryableAsync();
            var works = await AsyncExecuter.ToListAsync(workQuery.Where(work => workIds.Contains(work.Id)));
            foreach (var workId in workIds)
            {
                var work = works.SingleOrDefault(item => item.Id == workId);
                if (work == null || work.Status != ServiceWorkStatus.Active || string.IsNullOrWhiteSpace(work.Unit))
                {
                    throw new BusinessException(ServiceErrorCodes.WorkUnavailable).WithData("ServiceWorkId", workId);
                }

                result[(ServiceOrderLineType.Labor, workId)] =
                    new LineSelection(work.Code, work.Name, work.Unit, work.StandardCost);
            }
        }

        return result;
    }

    private async Task ValidatePositionsAsync(Guid assetId, IReadOnlyCollection<ServiceOrderLineInput> lines)
    {
        var positionedLines = lines.Where(line => line.LineType == ServiceOrderLineType.Material &&
                                                  line.CustomerAssetComponentId.HasValue).ToList();
        if (positionedLines.Count == 0)
        {
            return;
        }

        var positionIds = positionedLines.Select(line => line.CustomerAssetComponentId!.Value).Distinct().ToList();
        var query = await _assetComponents.GetQueryableAsync();
        var positions = await AsyncExecuter.ToListAsync(query.Where(position => positionIds.Contains(position.Id)));
        var byId = positions.ToDictionary(position => position.Id);
        foreach (var line in positionedLines)
        {
            if (!byId.TryGetValue(line.CustomerAssetComponentId!.Value, out var position) ||
                position.CustomerAssetId != assetId ||
                position.Status == CustomerAssetComponentStatus.Inactive ||
                position.ComponentId != line.CatalogItemId)
            {
                throw new BusinessException(ServiceErrorCodes.MaterialPositionMismatch)
                    .WithData("CustomerAssetId", assetId)
                    .WithData("CustomerAssetComponentId", line.CustomerAssetComponentId.Value)
                    .WithData("ComponentId", line.CatalogItemId);
            }
        }
    }

    private async Task<string> GenerateOrderNoAsync(DateTime orderDate)
    {
        var datePrefix = $"{OrderPrefix}-{orderDate:yyyyMMdd}";
        return await _codeGenerator.GenerateAsync(new BusinessCodeGenerationContext
        {
            SequenceName = OrderSequence,
            Prefix = OrderPrefix,
            Date = orderDate,
            ExistsAsync = (candidate, cancellationToken) => _orders.OrderNoExistsAsync(candidate, cancellationToken),
            SeedMaxAsync = async cancellationToken =>
                await _orders.GetMaxOrderNoSequenceAsync(datePrefix, cancellationToken)
        });
    }

    private async Task<ServiceOrderDto> MapAsync(ServiceOrder order)
    {
        string? technicianName = null;
        if (order.TechnicianUserId.HasValue)
        {
            var technician = await _users.FindAsync(order.TechnicianUserId.Value);
            technicianName = technician?.Name ?? technician?.UserName;
        }

        var lines = order.Lines.OrderBy(line => line.LineNo).Select(line => new ServiceOrderLineDto
        {
            Id = line.Id,
            LineNo = line.LineNo,
            LineType = line.LineType,
            ComponentId = line.ComponentId,
            CustomerAssetComponentId = line.CustomerAssetComponentId,
            ServiceWorkId = line.ServiceWorkId,
            ItemCode = line.ItemCodeSnapshot,
            ItemName = line.ItemNameSnapshot,
            Unit = line.UnitSnapshot,
            PlannedQuantity = line.PlannedQuantity,
            UnitPrice = line.UnitPrice,
            StandardCostSnapshot = line.StandardCostSnapshot,
            Note = line.Note
        }).ToList();
        return new ServiceOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            CustomerId = order.CustomerId,
            CustomerAssetId = order.CustomerAssetId,
            WarehouseId = order.WarehouseId,
            OrderDate = order.OrderDate,
            ScheduledAt = order.ScheduledAt,
            TechnicianUserId = order.TechnicianUserId,
            TechnicianName = technicianName,
            Status = order.Status,
            CustomerCode = order.CustomerCodeSnapshot,
            CustomerName = order.CustomerNameSnapshot,
            AssetNo = order.AssetNoSnapshot,
            AssetName = order.AssetNameSnapshot,
            ServiceAddress = order.ServiceAddress,
            Note = order.Note,
            ConfirmedAt = order.ConfirmedAt,
            StartedAt = order.StartedAt,
            CancelledAt = order.CancelledAt,
            CancellationReason = order.CancellationReason,
            PlannedAmount = lines.Sum(line => line.UnitPrice * line.PlannedQuantity),
            ConcurrencyStamp = order.ConcurrencyStamp,
            Lines = lines
        };
    }

    private static ServiceOrderListDto MapList(ServiceOrderListItem item) => new()
    {
        Id = item.Id,
        OrderNo = item.OrderNo,
        OrderDate = item.OrderDate,
        ScheduledAt = item.ScheduledAt,
        Status = item.Status,
        CustomerId = item.CustomerId,
        CustomerCode = item.CustomerCode,
        CustomerName = item.CustomerName,
        CustomerAssetId = item.CustomerAssetId,
        AssetNo = item.AssetNo,
        AssetName = item.AssetName,
        PlannedAmount = item.PlannedAmount,
        LineCount = item.LineCount
    };

    private static ServiceAssetOptionDto MapAsset(ServiceAssetOption item) => new()
    {
        Id = item.Id,
        CustomerId = item.CustomerId,
        CustomerCode = item.CustomerCode,
        CustomerName = item.CustomerName,
        AssetNo = item.AssetNo,
        AssetName = item.AssetName,
        ServiceAddress = item.ServiceAddress
    };

    private static ServiceAssetOptionDto MapAsset(CustomerAsset asset) => new()
    {
        Id = asset.Id,
        CustomerId = asset.CustomerId,
        CustomerCode = asset.CustomerCodeSnapshot,
        CustomerName = asset.CustomerNameSnapshot,
        AssetNo = asset.AssetNo,
        AssetName = ResolveAssetName(asset),
        ServiceAddress = asset.InstallationAddress
    };

    private static void EnsureLinesPresent(IReadOnlyCollection<ServiceOrderLineInput> lines)
    {
        if (lines.Count == 0)
        {
            throw new BusinessException(ServiceErrorCodes.InvalidLine).WithData("Reason", "AtLeastOneLineRequired");
        }
    }

    private static void EnsureExpectedVersion(ServiceOrder order, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected) || !string.Equals(order.ConcurrencyStamp, expected, StringComparison.Ordinal))
        {
            throw new BusinessException(ServiceErrorCodes.ConcurrentModification)
                .WithData("ServiceOrderId", order.Id)
                .WithData("OrderNo", order.OrderNo)
                .WithData("Status", order.Status);
        }
    }

    private void EnsureEnabled()
    {
        if (!_options.Value.IsEnabled)
        {
            throw new BusinessException(ServiceErrorCodes.Disabled);
        }
    }

    private static int NormalizePageSize(int requested) => Math.Clamp(requested, 1, 100);

    private static string ResolveAssetName(CustomerAsset asset)
    {
        var name = asset.ProductNameSnapshot ?? string.Join(" ", new[] { asset.Brand, asset.Model }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? asset.AssetNo : name.Trim();
    }

    private sealed record LineSelection(string Code, string Name, string Unit, decimal? StandardCost);
}
