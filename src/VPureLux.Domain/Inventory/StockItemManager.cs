using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.Inventory;

public class StockItemManager : DomainService
{
    private readonly IStockItemRepository _repository;
    private readonly IGuidGenerator _guidGenerator;

    public StockItemManager(IStockItemRepository repository, IGuidGenerator guidGenerator)
    {
        _repository = repository;
        _guidGenerator = guidGenerator;
    }

    public async Task<StockItem> GetOrCreateAsync(
        StockItemType itemType,
        Guid catalogItemId,
        string code,
        string name,
        string unit)
    {
        var existing = await _repository.FindByCatalogItemAsync(itemType, catalogItemId);
        if (existing != null)
        {
            existing.UpdateSnapshot(code, name, unit);
            return existing;
        }

        return new StockItem(
            _guidGenerator.Create(),
            itemType,
            catalogItemId,
            code,
            name,
            unit,
            isInventoryEnabled: itemType == StockItemType.Component);
    }
}
