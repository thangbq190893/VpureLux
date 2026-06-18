using System;
using VPureLux.Catalog.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Catalog;

public class Product : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CatalogItemStatus Status { get; private set; }
    public ImageData? Image { get; private set; }

    protected Product()
    {
    }

    internal Product(Guid id, string code, string name, string? description)
        : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), CatalogConsts.MaxCodeLength);
        SetInfo(name, description);
        Status = CatalogItemStatus.Active;
        AddLocalEvent(new ProductCreatedEvent(Id, Code, Name));
    }

    public void UpdateInfo(string name, string? description)
    {
        SetInfo(name, description);
        AddLocalEvent(new ProductUpdatedEvent(Id, Code, Name));
    }

    public void Activate()
    {
        if (Status == CatalogItemStatus.Active)
        {
            return;
        }

        Status = CatalogItemStatus.Active;
        AddLocalEvent(new ProductActivatedEvent(Id, Code));
    }

    public void Deactivate()
    {
        if (Status == CatalogItemStatus.Inactive)
        {
            return;
        }

        Status = CatalogItemStatus.Inactive;
        AddLocalEvent(new ProductDeactivatedEvent(Id, Code));
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
        AddLocalEvent(new ProductImageChangedEvent(
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
        AddLocalEvent(new ProductImageRemovedEvent(Id, Code, previousHash));
    }

    private void SetInfo(string name, string? description)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), CatalogConsts.MaxNameLength);
        Description = Check.Length(description, nameof(description), CatalogConsts.MaxDescriptionLength);
    }
}
