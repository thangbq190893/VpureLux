using System;
using System.Text.Json;
using System.Threading.Tasks;
using VPureLux.Bom.Events;
using VPureLux.Catalog.Events;
using VPureLux.Customers.Events;
using VPureLux.Inventory.Events;
using VPureLux.Pricing.Events;
using VPureLux.Sales.Events;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace VPureLux.Audit;

public class BusinessAuditEventHandler :
    ILocalEventHandler<ProductCreatedEvent>, ILocalEventHandler<ProductUpdatedEvent>,
    ILocalEventHandler<ProductActivatedEvent>, ILocalEventHandler<ProductDeactivatedEvent>, ILocalEventHandler<ProductImageChangedEvent>,
    ILocalEventHandler<ProductImageRemovedEvent>, ILocalEventHandler<ComponentCreatedEvent>,
    ILocalEventHandler<ComponentUpdatedEvent>, ILocalEventHandler<ComponentActivatedEvent>, ILocalEventHandler<ComponentDeactivatedEvent>,
    ILocalEventHandler<ComponentImageChangedEvent>, ILocalEventHandler<ComponentImageRemovedEvent>,
    ILocalEventHandler<BomVersionCreatedEvent>, ILocalEventHandler<BomPublishedEvent>,
    ILocalEventHandler<BomArchivedEvent>, ILocalEventHandler<BomVersionClonedEvent>,
    ILocalEventHandler<CustomerCreatedEvent>, ILocalEventHandler<CustomerUpdatedEvent>,
    ILocalEventHandler<CustomerActivatedEvent>, ILocalEventHandler<CustomerDeactivatedEvent>,
    ILocalEventHandler<ComponentSuggestedSellingPriceVersionCreatedEvent>,
    ILocalEventHandler<ProductSuggestedPriceVersionCreatedEvent>,
    ILocalEventHandler<InventoryReceiptPostedEvent>, ILocalEventHandler<InventoryIssuePostedEvent>,
    ILocalEventHandler<InventoryAdjustedEvent>, ILocalEventHandler<SalesOrderCreatedEvent>,
    ILocalEventHandler<SalesOrderConfirmedEvent>, ILocalEventHandler<SalesOrderCancelledEvent>,
    ITransientDependency
{
    private readonly IBusinessAuditLogRepository _repository;
    private readonly BusinessAuditManager _manager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;
    private readonly IAuditingManager _auditingManager;
    private readonly ICurrentUser _currentUser;

    public BusinessAuditEventHandler(
        IBusinessAuditLogRepository repository, BusinessAuditManager manager, IGuidGenerator guidGenerator,
        IClock clock, IAuditingManager auditingManager, ICurrentUser currentUser)
    {
        _repository = repository;
        _manager = manager;
        _guidGenerator = guidGenerator;
        _clock = clock;
        _auditingManager = auditingManager;
        _currentUser = currentUser;
    }

    public Task HandleEventAsync(ProductCreatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Create, "Product", e.ProductId, e.Code);
    public Task HandleEventAsync(ProductUpdatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Update, "Product", e.ProductId, e.Code);
    public Task HandleEventAsync(ProductActivatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Activate, "Product", e.ProductId, e.Code);
    public Task HandleEventAsync(ProductDeactivatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Deactivate, "Product", e.ProductId, e.Code, AuditSeverity.Important);
    public Task HandleEventAsync(ProductImageChangedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.ImageChange, "Product", e.ProductId, e.Code);
    public Task HandleEventAsync(ProductImageRemovedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.ImageRemove, "Product", e.ProductId, e.Code);
    public Task HandleEventAsync(ComponentCreatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Create, "Component", e.ComponentId, e.Code);
    public Task HandleEventAsync(ComponentUpdatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Update, "Component", e.ComponentId, e.Code);
    public Task HandleEventAsync(ComponentActivatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Activate, "Component", e.ComponentId, e.Code);
    public Task HandleEventAsync(ComponentDeactivatedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.Deactivate, "Component", e.ComponentId, e.Code, AuditSeverity.Important);
    public Task HandleEventAsync(ComponentImageChangedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.ImageChange, "Component", e.ComponentId, e.Code);
    public Task HandleEventAsync(ComponentImageRemovedEvent e) => WriteAsync(e, "Catalog", AuditActionTypes.ImageRemove, "Component", e.ComponentId, e.Code);
    public Task HandleEventAsync(BomVersionCreatedEvent e) => WriteAsync(e, "BOM", AuditActionTypes.Create, "BomVersion", e.BomVersionId, $"v{e.VersionNo}");
    public Task HandleEventAsync(BomPublishedEvent e) => WriteAsync(e, "BOM", AuditActionTypes.Publish, "BomVersion", e.BomVersionId, $"v{e.VersionNo}", AuditSeverity.Important);
    public Task HandleEventAsync(BomArchivedEvent e) => WriteAsync(e, "BOM", AuditActionTypes.Archive, "BomVersion", e.BomVersionId, $"v{e.VersionNo}", AuditSeverity.Important);
    public Task HandleEventAsync(BomVersionClonedEvent e) => WriteAsync(e, "BOM", AuditActionTypes.Clone, "BomVersion", e.NewBomVersionId, $"v{e.VersionNo}");
    public Task HandleEventAsync(CustomerCreatedEvent e) => WriteAsync(e, "Customer", AuditActionTypes.Create, "Customer", e.CustomerId, e.Code);
    public Task HandleEventAsync(CustomerUpdatedEvent e) => WriteAsync(e, "Customer", AuditActionTypes.Update, "Customer", e.CustomerId, e.Code);
    public Task HandleEventAsync(CustomerActivatedEvent e) => WriteAsync(e, "Customer", AuditActionTypes.Activate, "Customer", e.CustomerId, e.Code);
    public Task HandleEventAsync(CustomerDeactivatedEvent e) => WriteAsync(e, "Customer", AuditActionTypes.Deactivate, "Customer", e.CustomerId, e.Code, AuditSeverity.Important);
    public Task HandleEventAsync(ComponentSuggestedSellingPriceVersionCreatedEvent e) => WriteAsync(e, "Pricing", AuditActionTypes.Create, "ComponentSuggestedSellingPriceVersion", e.PriceVersionId, $"v{e.VersionNo}", AuditSeverity.Important);
    public Task HandleEventAsync(ProductSuggestedPriceVersionCreatedEvent e) => WriteAsync(e, "Pricing", AuditActionTypes.Create, "ProductSuggestedPriceVersion", e.PriceVersionId, $"v{e.VersionNo}", AuditSeverity.Important);
    public Task HandleEventAsync(InventoryReceiptPostedEvent e) => WriteAsync(e, "Inventory", AuditActionTypes.Post, "InventoryTransaction", e.TransactionId, null, AuditSeverity.Important);
    public Task HandleEventAsync(InventoryIssuePostedEvent e) => WriteAsync(e, "Inventory", AuditActionTypes.Post, "InventoryTransaction", e.TransactionId, null, AuditSeverity.Important);
    public Task HandleEventAsync(InventoryAdjustedEvent e) => WriteAsync(e, "Inventory", AuditActionTypes.Post, "InventoryTransaction", e.TransactionId, null, AuditSeverity.Critical);
    public Task HandleEventAsync(SalesOrderCreatedEvent e) => WriteAsync(e, "Sales", AuditActionTypes.Create, "SalesOrder", e.SalesOrderId, e.OrderNo);
    public Task HandleEventAsync(SalesOrderConfirmedEvent e) => WriteAsync(e, "Sales", AuditActionTypes.Confirm, "SalesOrder", e.SalesOrderId, e.OrderNo, AuditSeverity.Critical);
    public Task HandleEventAsync(SalesOrderCancelledEvent e) => WriteAsync(e, "Sales", AuditActionTypes.Cancel, "SalesOrder", e.SalesOrderId, e.OrderNo, AuditSeverity.Important);

    private async Task WriteAsync<T>(
        T eventData, string module, string action, string entityType, Guid entityId,
        string? display, AuditSeverity severity = AuditSeverity.Informational)
    {
        var correlationId = _auditingManager.Current?.Log.CorrelationId ?? _guidGenerator.Create().ToString("N");
        var envelope = new BusinessAuditEnvelope(
            _guidGenerator.Create(), module, typeof(T).Name, action, entityType, entityId,
            correlationId, correlationId, _clock.Now, severity, display,
            MetadataJson: JsonSerializer.Serialize(eventData), UserId: _currentUser.Id,
            UserName: _currentUser.UserName, ActorType: _currentUser.Id.HasValue ? AuditActorType.User : AuditActorType.System,
            IsSystemGenerated: !_currentUser.Id.HasValue);
        await _repository.InsertAsync(_manager.Create(envelope));
    }
}
