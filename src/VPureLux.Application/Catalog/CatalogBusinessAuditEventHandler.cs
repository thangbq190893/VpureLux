using System.Threading.Tasks;
using VPureLux.Catalog.Events;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace VPureLux.Catalog;

public class CatalogBusinessAuditEventHandler :
    ILocalEventHandler<ComponentCreatedEvent>,
    ILocalEventHandler<ComponentUpdatedEvent>,
    ILocalEventHandler<ComponentActivatedEvent>,
    ILocalEventHandler<ComponentDeactivatedEvent>,
    ILocalEventHandler<ProductCreatedEvent>,
    ILocalEventHandler<ProductUpdatedEvent>,
    ILocalEventHandler<ProductActivatedEvent>,
    ILocalEventHandler<ProductDeactivatedEvent>,
    ILocalEventHandler<ProductImageChangedEvent>,
    ILocalEventHandler<ProductImageRemovedEvent>,
    ILocalEventHandler<ComponentImageChangedEvent>,
    ILocalEventHandler<ComponentImageRemovedEvent>,
    ITransientDependency
{
    private readonly IAuditingManager _auditingManager;

    public CatalogBusinessAuditEventHandler(IAuditingManager auditingManager)
    {
        _auditingManager = auditingManager;
    }

    public Task HandleEventAsync(ComponentCreatedEvent eventData) =>
        AddCommentAsync("Component", eventData.ComponentId, "Created", eventData.Code);

    public Task HandleEventAsync(ComponentUpdatedEvent eventData) =>
        AddCommentAsync("Component", eventData.ComponentId, "Updated", eventData.Code);

    public Task HandleEventAsync(ComponentActivatedEvent eventData) =>
        AddCommentAsync("Component", eventData.ComponentId, "Activated", eventData.Code);

    public Task HandleEventAsync(ComponentDeactivatedEvent eventData) =>
        AddCommentAsync("Component", eventData.ComponentId, "Deactivated", eventData.Code);

    public Task HandleEventAsync(ProductCreatedEvent eventData) =>
        AddCommentAsync("Product", eventData.ProductId, "Created", eventData.Code);

    public Task HandleEventAsync(ProductUpdatedEvent eventData) =>
        AddCommentAsync("Product", eventData.ProductId, "Updated", eventData.Code);

    public Task HandleEventAsync(ProductActivatedEvent eventData) =>
        AddCommentAsync("Product", eventData.ProductId, "Activated", eventData.Code);

    public Task HandleEventAsync(ProductDeactivatedEvent eventData) =>
        AddCommentAsync("Product", eventData.ProductId, "Deactivated", eventData.Code);

    public Task HandleEventAsync(ProductImageChangedEvent eventData) =>
        AddImageCommentAsync("Product", eventData.ProductId, "ImageChanged", eventData.Code, eventData.PreviousHash, eventData.NewHash);

    public Task HandleEventAsync(ProductImageRemovedEvent eventData) =>
        AddImageCommentAsync("Product", eventData.ProductId, "ImageRemoved", eventData.Code, eventData.PreviousHash, null);

    public Task HandleEventAsync(ComponentImageChangedEvent eventData) =>
        AddImageCommentAsync("Component", eventData.ComponentId, "ImageChanged", eventData.Code, eventData.PreviousHash, eventData.NewHash);

    public Task HandleEventAsync(ComponentImageRemovedEvent eventData) =>
        AddImageCommentAsync("Component", eventData.ComponentId, "ImageRemoved", eventData.Code, eventData.PreviousHash, null);

    private Task AddCommentAsync(string entityName, object entityId, string action, string code)
    {
        _auditingManager.Current?.Log.Comments.Add(
            $"Catalog:{entityName}:{action};Id={entityId};Code={code}");

        return Task.CompletedTask;
    }

    private Task AddImageCommentAsync(
        string entityName,
        object entityId,
        string action,
        string code,
        string? previousHash,
        string? newHash)
    {
        _auditingManager.Current?.Log.Comments.Add(
            $"Catalog:{entityName}:{action};Id={entityId};Code={code};PreviousHash={previousHash};NewHash={newHash}");

        return Task.CompletedTask;
    }
}
