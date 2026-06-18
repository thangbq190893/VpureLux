using System.Threading.Tasks;
using VPureLux.Catalog;
using VPureLux.Catalog.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;

namespace VPureLux.Inventory;

public class CatalogStockItemSynchronizationHandler :
    ILocalEventHandler<ComponentCreatedEvent>,
    ILocalEventHandler<ComponentUpdatedEvent>,
    ILocalEventHandler<ComponentDeactivatedEvent>,
    ILocalEventHandler<ComponentActivatedEvent>,
    ILocalEventHandler<ProductCreatedEvent>,
    ILocalEventHandler<ProductUpdatedEvent>,
    ILocalEventHandler<ProductDeactivatedEvent>,
    ILocalEventHandler<ProductActivatedEvent>,
    ILocalEventHandler<EntityDeletedEventData<Component>>,
    ILocalEventHandler<EntityDeletedEventData<Product>>,
    ITransientDependency
{
    private readonly IComponentRepository _componentRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly StockItemManager _stockItemManager;

    public CatalogStockItemSynchronizationHandler(
        IComponentRepository componentRepository,
        IProductRepository productRepository,
        IStockItemRepository stockItemRepository,
        StockItemManager stockItemManager)
    {
        _componentRepository = componentRepository;
        _productRepository = productRepository;
        _stockItemRepository = stockItemRepository;
        _stockItemManager = stockItemManager;
    }

    public Task HandleEventAsync(ComponentCreatedEvent eventData) =>
        SynchronizeComponentAsync(eventData.ComponentId);

    public Task HandleEventAsync(ComponentUpdatedEvent eventData) =>
        SynchronizeComponentAsync(eventData.ComponentId);

    public Task HandleEventAsync(ComponentDeactivatedEvent eventData) =>
        DeactivateAsync(StockItemType.Component, eventData.ComponentId);

    public Task HandleEventAsync(ComponentActivatedEvent eventData) =>
        SynchronizeComponentAsync(eventData.ComponentId);

    public Task HandleEventAsync(ProductCreatedEvent eventData) =>
        SynchronizeProductAsync(eventData.ProductId);

    public Task HandleEventAsync(ProductUpdatedEvent eventData) =>
        SynchronizeProductAsync(eventData.ProductId);

    public Task HandleEventAsync(ProductDeactivatedEvent eventData) =>
        DeactivateAsync(StockItemType.Product, eventData.ProductId);

    public Task HandleEventAsync(ProductActivatedEvent eventData) =>
        SynchronizeProductAsync(eventData.ProductId);

    public Task HandleEventAsync(EntityDeletedEventData<Component> eventData) =>
        DeactivateAsync(StockItemType.Component, eventData.Entity.Id);

    public Task HandleEventAsync(EntityDeletedEventData<Product> eventData) =>
        DeactivateAsync(StockItemType.Product, eventData.Entity.Id);

    private async Task SynchronizeComponentAsync(System.Guid componentId)
    {
        var component = await _componentRepository.FindAsync(componentId);
        if (component == null)
        {
            return;
        }
        await SynchronizeAsync(
            StockItemType.Component,
            component.Id,
            component.Code,
            component.Name,
            component.Unit);
    }

    private async Task SynchronizeProductAsync(System.Guid productId)
    {
        var product = await _productRepository.FindAsync(productId);
        if (product == null)
        {
            return;
        }
        await SynchronizeAsync(
            StockItemType.Product,
            product.Id,
            product.Code,
            product.Name,
            InventoryConsts.DefaultProductUnit);
    }

    private async Task SynchronizeAsync(
        StockItemType type,
        System.Guid catalogItemId,
        string code,
        string name,
        string unit)
    {
        var existing = await _stockItemRepository.FindByCatalogItemAsync(type, catalogItemId);
        var stockItem = await _stockItemManager.GetOrCreateAsync(type, catalogItemId, code, name, unit);
        if (existing == null)
        {
            await _stockItemRepository.InsertAsync(stockItem);
        }
        else
        {
            stockItem.Activate();
            await _stockItemRepository.UpdateAsync(stockItem);
        }
    }

    private async Task DeactivateAsync(StockItemType type, System.Guid catalogItemId)
    {
        var stockItem = await _stockItemRepository.FindByCatalogItemAsync(type, catalogItemId);
        if (stockItem == null)
        {
            return;
        }

        stockItem.Deactivate();
        await _stockItemRepository.UpdateAsync(stockItem);
    }
}
