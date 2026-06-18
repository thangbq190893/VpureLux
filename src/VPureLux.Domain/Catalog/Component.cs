using System;
using VPureLux.Catalog.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Catalog;

public class Component : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public CatalogItemStatus Status { get; private set; }
    public ImageData? Image { get; private set; }

    protected Component()
    {
    }

    internal Component(Guid id, string code, string name, string? description, string unit)
        : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), CatalogConsts.MaxCodeLength);
        SetInfo(name, description, unit);
        Status = CatalogItemStatus.Active;
        AddLocalEvent(new ComponentCreatedEvent(Id, Code, Name));
    }

    public void UpdateInfo(string name, string? description, string unit)
    {
        SetInfo(name, description, unit);
        AddLocalEvent(new ComponentUpdatedEvent(Id, Code, Name));
    }

    public void Activate()
    {
        if (Status == CatalogItemStatus.Active)
        {
            return;
        }

        Status = CatalogItemStatus.Active;
        AddLocalEvent(new ComponentActivatedEvent(Id, Code));
    }

    public void Deactivate()
    {
        if (Status == CatalogItemStatus.Inactive)
        {
            return;
        }

        Status = CatalogItemStatus.Inactive;
        AddLocalEvent(new ComponentDeactivatedEvent(Id, Code));
    }

    public void SetImage(ImageData image)
    {
        Check.NotNull(image, nameof(image));
        if (Image?.ImageHash == image.ImageHash)
        {
            return;
        }

        var previousHash = Image?.ImageHash;
        Image = image;
        AddLocalEvent(new ComponentImageChangedEvent(
            Id,
            Code,
            previousHash,
            image.ImageHash,
            image.MimeType,
            image.FileName));
    }

    public void RemoveImage()
    {
        if (Image == null)
        {
            return;
        }

        var previousHash = Image.ImageHash;
        Image = null;
        AddLocalEvent(new ComponentImageRemovedEvent(Id, Code, previousHash));
    }

    private void SetInfo(string name, string? description, string unit)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), CatalogConsts.MaxNameLength);
        Description = Check.Length(description, nameof(description), CatalogConsts.MaxDescriptionLength);
        Unit = Check.NotNullOrWhiteSpace(unit, nameof(unit), CatalogConsts.MaxUnitLength);
    }
}
