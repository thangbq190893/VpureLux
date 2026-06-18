using System;
using VPureLux.Inventory.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Inventory;

public class StockItem : FullAuditedAggregateRoot<Guid>
{
    public StockItemType ItemType { get; private set; }
    public Guid CatalogItemId { get; private set; }
    public string CodeSnapshot { get; private set; } = string.Empty;
    public string NameSnapshot { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public bool IsInventoryEnabled { get; private set; }
    public InventoryEntityStatus Status { get; private set; }

    protected StockItem()
    {
    }

    internal StockItem(
        Guid id,
        StockItemType itemType,
        Guid catalogItemId,
        string code,
        string name,
        string unit,
        bool isInventoryEnabled)
        : base(id)
    {
        if (catalogItemId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        ItemType = itemType;
        CatalogItemId = catalogItemId;
        UpdateSnapshot(code, name, unit);
        IsInventoryEnabled = isInventoryEnabled;
        Status = InventoryEntityStatus.Active;
        AddLocalEvent(new StockItemCreatedEvent(Id, ItemType, CatalogItemId));
    }

    public void UpdateSnapshot(string code, string name, string unit)
    {
        CodeSnapshot = Check.NotNullOrWhiteSpace(code, nameof(code), InventoryConsts.MaxCodeLength);
        NameSnapshot = Check.NotNullOrWhiteSpace(name, nameof(name), InventoryConsts.MaxNameLength);
        Unit = Check.NotNullOrWhiteSpace(unit, nameof(unit), InventoryConsts.MaxCodeLength);
    }

    public void Activate()
    {
        Status = InventoryEntityStatus.Active;
    }

    public void Deactivate()
    {
        if (Status == InventoryEntityStatus.Inactive)
        {
            return;
        }

        Status = InventoryEntityStatus.Inactive;
        AddLocalEvent(new StockItemDeactivatedEvent(Id, ItemType, CatalogItemId));
    }

    public void EnableInventory()
    {
        IsInventoryEnabled = true;
    }

    public void DisableInventory()
    {
        IsInventoryEnabled = false;
    }
}
