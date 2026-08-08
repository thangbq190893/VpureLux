using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Uow;

namespace VPureLux.Bom;

public interface IBomStandardCostLookupService
{
    Task<BomStandardCostRangeDto> GetAsync(Guid bomVersionId);

    Task<IReadOnlyDictionary<Guid, BomStandardCostRangeDto>> FindMapAsync(IReadOnlyCollection<Guid> bomVersionIds);
}

public class BomStandardCostLookupService : IBomStandardCostLookupService, ITransientDependency
{
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IInventoryLotRepository _inventoryLotRepository;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public BomStandardCostLookupService(
        IBomVersionRepository bomVersionRepository,
        IStockItemRepository stockItemRepository,
        IInventoryLotRepository inventoryLotRepository,
        IPermissionChecker permissionChecker,
        IAsyncQueryableExecuter asyncExecuter,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _bomVersionRepository = bomVersionRepository;
        _stockItemRepository = stockItemRepository;
        _inventoryLotRepository = inventoryLotRepository;
        _permissionChecker = permissionChecker;
        _asyncExecuter = asyncExecuter;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<BomStandardCostRangeDto> GetAsync(Guid bomVersionId)
    {
        var map = await FindMapCoreAsync([bomVersionId]);
        return map.TryGetValue(bomVersionId, out var range)
            ? range
            : BomStandardCostRangeDto.Empty(bomVersionId);
    }

    public async Task<IReadOnlyDictionary<Guid, BomStandardCostRangeDto>> FindMapAsync(
        IReadOnlyCollection<Guid> bomVersionIds)
    {
        return await FindMapCoreAsync(bomVersionIds);
    }

    private async Task<IReadOnlyDictionary<Guid, BomStandardCostRangeDto>> FindMapCoreAsync(
        IReadOnlyCollection<Guid> bomVersionIds)
    {
        await EnsureCanViewBomAsync();
        if (bomVersionIds.Count == 0)
        {
            return new Dictionary<Guid, BomStandardCostRangeDto>();
        }

        using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var bomIdSet = bomVersionIds.Distinct().ToHashSet();
        var boms = await _asyncExecuter.ToListAsync(
            (await _bomVersionRepository.WithDetailsAsync())
            .Where(x => bomIdSet.Contains(x.Id)));
        var componentIds = boms
            .SelectMany(x => x.OrderedItems)
            .Select(x => x.ComponentId)
            .Distinct()
            .ToArray();

        var stockItemMap = await GetComponentStockItemMapAsync(componentIds);
        var stockItemIds = stockItemMap.Values.Select(x => x.Id).Distinct().ToArray();
        var lotCostMap = await GetAvailableLotCostMapAsync(stockItemIds);
        var result = new Dictionary<Guid, BomStandardCostRangeDto>(boms.Count);

        foreach (var bom in boms)
        {
            result[bom.Id] = BuildRange(bom, stockItemMap, lotCostMap);
        }

        foreach (var missingId in bomIdSet.Where(x => !result.ContainsKey(x)))
        {
            result[missingId] = BomStandardCostRangeDto.Empty(missingId);
        }

        await unitOfWork.CompleteAsync();
        return result;
    }

    private async Task<Dictionary<Guid, StockItem>> GetComponentStockItemMapAsync(IReadOnlyCollection<Guid> componentIds)
    {
        if (componentIds.Count == 0)
        {
            return new Dictionary<Guid, StockItem>();
        }

        var componentIdSet = componentIds.ToHashSet();
        var stockItems = await _asyncExecuter.ToListAsync(
            (await _stockItemRepository.GetQueryableAsync())
            .Where(x => x.ItemType == StockItemType.Component &&
                        x.IsInventoryEnabled &&
                        componentIdSet.Contains(x.CatalogItemId)));

        return stockItems
            .GroupBy(x => x.CatalogItemId)
            .ToDictionary(x => x.Key, x => x.OrderBy(item => item.CodeSnapshot).First());
    }

    private async Task<Dictionary<Guid, AvailableLotCostRange>> GetAvailableLotCostMapAsync(
        IReadOnlyCollection<Guid> stockItemIds)
    {
        if (stockItemIds.Count == 0)
        {
            return new Dictionary<Guid, AvailableLotCostRange>();
        }

        var stockItemIdSet = stockItemIds.ToHashSet();
        var lots = await _asyncExecuter.ToListAsync(
            (await _inventoryLotRepository.GetQueryableAsync())
            .Where(x => stockItemIdSet.Contains(x.StockItemId) && x.AvailableQuantity > 0));

        return lots
            .GroupBy(x => x.StockItemId)
            .ToDictionary(
                x => x.Key,
                x => new AvailableLotCostRange(x.Min(lot => lot.UnitCost), x.Max(lot => lot.UnitCost)));
    }

    private static BomStandardCostRangeDto BuildRange(
        BomVersion bom,
        IReadOnlyDictionary<Guid, StockItem> stockItemMap,
        IReadOnlyDictionary<Guid, AvailableLotCostRange> lotCostMap)
    {
        var dto = BomStandardCostRangeDto.Empty(bom.Id);
        foreach (var item in bom.OrderedItems)
        {
            stockItemMap.TryGetValue(item.ComponentId, out var stockItem);
            AvailableLotCostRange? lotCost = null;
            var hasCost = stockItem != null && lotCostMap.TryGetValue(stockItem.Id, out lotCost);
            var minUnitCost = hasCost ? lotCost!.MinUnitCost : (decimal?)null;
            var maxUnitCost = hasCost ? lotCost!.MaxUnitCost : (decimal?)null;
            var line = new BomStandardCostItemDto
            {
                BomItemId = item.Id,
                ComponentId = item.ComponentId,
                Quantity = item.Quantity,
                MinUnitCost = minUnitCost,
                MaxUnitCost = maxUnitCost,
                MinLineCost = minUnitCost.HasValue ? item.Quantity * minUnitCost.Value : null,
                MaxLineCost = maxUnitCost.HasValue ? item.Quantity * maxUnitCost.Value : null,
                HasAvailableInventoryCost = hasCost
            };

            dto.Items.Add(line);
            if (!hasCost)
            {
                dto.MissingComponentCount++;
                continue;
            }

            dto.MinTotalCost += line.MinLineCost!.Value;
            dto.MaxTotalCost += line.MaxLineCost!.Value;
        }

        if (dto.MissingComponentCount > 0)
        {
            dto.MinTotalCost = null;
            dto.MaxTotalCost = null;
        }

        return dto;
    }

    private async Task EnsureCanViewBomAsync()
    {
        if (!await _permissionChecker.IsGrantedAsync(VPureLuxPermissions.Bom.View))
        {
            throw new AbpAuthorizationException();
        }
    }

    private sealed record AvailableLotCostRange(decimal MinUnitCost, decimal MaxUnitCost);
}

public class BomStandardCostRangeDto
{
    public Guid BomVersionId { get; set; }
    public decimal? MinTotalCost { get; set; }
    public decimal? MaxTotalCost { get; set; }
    public int MissingComponentCount { get; set; }
    public List<BomStandardCostItemDto> Items { get; set; } = new();

    public bool HasCompleteCost => MinTotalCost.HasValue && MaxTotalCost.HasValue;

    public static BomStandardCostRangeDto Empty(Guid bomVersionId) => new()
    {
        BomVersionId = bomVersionId,
        MinTotalCost = 0,
        MaxTotalCost = 0
    };
}

public class BomStandardCostItemDto
{
    public Guid BomItemId { get; set; }
    public Guid ComponentId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? MinUnitCost { get; set; }
    public decimal? MaxUnitCost { get; set; }
    public decimal? MinLineCost { get; set; }
    public decimal? MaxLineCost { get; set; }
    public bool HasAvailableInventoryCost { get; set; }
}
