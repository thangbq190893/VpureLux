using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Inventory;

public class InventoryLotSupplier : FullAuditedEntity<Guid>
{
    public Guid InventoryLotId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierCodeSnapshot { get; private set; } = string.Empty;
    public string SupplierNameSnapshot { get; private set; } = string.Empty;

    protected InventoryLotSupplier()
    {
    }

    internal InventoryLotSupplier(
        Guid id,
        Guid inventoryLotId,
        Guid supplierId,
        string supplierCodeSnapshot,
        string supplierNameSnapshot)
        : base(id)
    {
        InventoryLotId = inventoryLotId;
        SupplierId = supplierId;
        SupplierCodeSnapshot = Check.NotNullOrWhiteSpace(
            supplierCodeSnapshot,
            nameof(supplierCodeSnapshot),
            Suppliers.SupplierConsts.MaxCodeLength);
        SupplierNameSnapshot = Check.NotNullOrWhiteSpace(
            supplierNameSnapshot,
            nameof(supplierNameSnapshot),
            Suppliers.SupplierConsts.MaxNameLength);
    }
}
