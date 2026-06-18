using System;
using VPureLux.Inventory.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Inventory;

public class Warehouse : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public InventoryEntityStatus Status { get; private set; }
    public bool IsDefault { get; private set; }

    protected Warehouse()
    {
    }

    internal Warehouse(Guid id, string code, string name, string? address, bool isDefault)
        : base(id)
    {
        Code = NormalizeCode(code);
        UpdateInfo(name, address, isDefault);
        Status = InventoryEntityStatus.Active;
        AddLocalEvent(new WarehouseCreatedEvent(Id, Code));
    }

    public void UpdateInfo(string name, string? address, bool isDefault)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), InventoryConsts.MaxNameLength);
        Address = string.IsNullOrWhiteSpace(address)
            ? null
            : Check.Length(address.Trim(), nameof(address), InventoryConsts.MaxAddressLength);
        IsDefault = isDefault;
    }

    public void Activate() => Status = InventoryEntityStatus.Active;
    public void Deactivate() => Status = InventoryEntityStatus.Inactive;

    internal static string NormalizeCode(string code) =>
        Check.NotNullOrWhiteSpace(code, nameof(code), InventoryConsts.MaxCodeLength)
            .Trim()
            .ToUpperInvariant();
}
