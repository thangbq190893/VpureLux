using System;
using System.Collections.Generic;
using System.Linq;
using VPureLux.Bom.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Bom;

public class BomVersion : FullAuditedAggregateRoot<Guid>
{
    private readonly List<BomItem> _items = new();

    public Guid ProductId { get; private set; }
    public BomVersionNo VersionNo { get; private set; } = null!;
    public BomStatus Status { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public IReadOnlyCollection<BomItem> Items => _items.AsReadOnly();

    protected BomVersion()
    {
    }

    internal BomVersion(Guid id, Guid productId, BomVersionNo versionNo, DateTime effectiveFrom)
        : base(id)
    {
        if (productId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(ProductId), productId);
        }

        ProductId = productId;
        VersionNo = Check.NotNull(versionNo, nameof(versionNo));
        EffectiveFrom = effectiveFrom;
        Status = BomStatus.Draft;

        AddLocalEvent(new BomVersionCreatedEvent(Id, ProductId, VersionNo.Value));
    }

    public BomItem AddItem(Guid itemId, Guid componentId, decimal quantity)
    {
        EnsureModifiable();

        var item = new BomItem(itemId, componentId, quantity);
        _items.Add(item);
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureModifiable();

        var item = FindItem(itemId);
        _items.Remove(item);
    }

    public void UpdateQuantity(Guid itemId, decimal quantity)
    {
        EnsureModifiable();
        FindItem(itemId).UpdateQuantity(quantity);
    }

    public void UpdateItem(Guid itemId, Guid componentId, decimal quantity)
    {
        EnsureModifiable();
        FindItem(itemId).Update(componentId, quantity);
    }

    public void Publish()
    {
        EnsureModifiable();

        if (_items.Count == 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData("Reason", "A BOM must contain at least one item before publishing.");
        }

        Status = BomStatus.Published;
        AddLocalEvent(new BomPublishedEvent(Id, ProductId, VersionNo.Value));
    }

    public void Archive(DateTime effectiveTo)
    {
        if (Status == BomStatus.Archived)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ArchivedBomCannotBeModified);
        }

        if (Status != BomStatus.Published || effectiveTo < EffectiveFrom)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(effectiveTo), effectiveTo);
        }

        Status = BomStatus.Archived;
        EffectiveTo = effectiveTo;
        AddLocalEvent(new BomArchivedEvent(Id, ProductId, VersionNo.Value));
    }

    public BomVersion CloneVersion(
        Guid newBomVersionId,
        BomVersionNo newVersionNo,
        DateTime effectiveFrom,
        Func<Guid> itemIdFactory)
    {
        Check.NotNull(newVersionNo, nameof(newVersionNo));
        Check.NotNull(itemIdFactory, nameof(itemIdFactory));

        var clone = new BomVersion(newBomVersionId, ProductId, newVersionNo, effectiveFrom);
        foreach (var item in _items)
        {
            clone.AddItem(itemIdFactory(), item.ComponentId, item.Quantity);
        }

        clone.AddLocalEvent(
            new BomVersionClonedEvent(Id, clone.Id, ProductId, clone.VersionNo.Value));

        return clone;
    }

    private BomItem FindItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(x => x.Id == itemId);
        if (item == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound)
                .WithData(nameof(itemId), itemId);
        }

        return item;
    }

    private void EnsureModifiable()
    {
        if (Status == BomStatus.Published)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.PublishedBomCannotBeModified);
        }

        if (Status == BomStatus.Archived)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ArchivedBomCannotBeModified);
        }
    }
}
