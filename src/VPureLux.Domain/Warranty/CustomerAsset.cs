using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class CustomerAsset : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public Guid SalesOrderLineId { get; private set; }
    public int SalesOrderLineNoSnapshot { get; private set; }
    public string AssetNo { get; private set; } = string.Empty;
    public string OrderNoSnapshot { get; private set; } = string.Empty;
    public string CustomerCodeSnapshot { get; private set; } = string.Empty;
    public string CustomerNameSnapshot { get; private set; } = string.Empty;
    public string ProductCodeSnapshot { get; private set; } = string.Empty;
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public DateTime SoldDate { get; private set; }
    public DateTime WarrantyStartDate { get; private set; }
    public CustomerAssetStatus Status { get; private set; }
    public string? Note { get; private set; }

    protected CustomerAsset()
    {
    }

    public CustomerAsset(
        Guid id,
        Guid customerId,
        Guid productId,
        Guid salesOrderId,
        Guid salesOrderLineId,
        int salesOrderLineNoSnapshot,
        string assetNo,
        string orderNoSnapshot,
        string customerCodeSnapshot,
        string customerNameSnapshot,
        string productCodeSnapshot,
        string productNameSnapshot,
        DateTime soldDate,
        DateTime warrantyStartDate,
        string? note = null)
        : base(id)
    {
        CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId));
        ProductId = Check.NotDefaultOrNull<Guid>(productId, nameof(productId));
        SalesOrderId = Check.NotDefaultOrNull<Guid>(salesOrderId, nameof(salesOrderId));
        SalesOrderLineId = Check.NotDefaultOrNull<Guid>(salesOrderLineId, nameof(salesOrderLineId));
        if (salesOrderLineNoSnapshot <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        SalesOrderLineNoSnapshot = salesOrderLineNoSnapshot;
        AssetNo = Check.NotNullOrWhiteSpace(assetNo, nameof(assetNo), WarrantyConsts.MaxAssetNoLength);
        OrderNoSnapshot = Check.NotNullOrWhiteSpace(orderNoSnapshot, nameof(orderNoSnapshot), WarrantyConsts.MaxCodeLength);
        CustomerCodeSnapshot = Check.NotNullOrWhiteSpace(customerCodeSnapshot, nameof(customerCodeSnapshot), WarrantyConsts.MaxCodeLength);
        CustomerNameSnapshot = Check.NotNullOrWhiteSpace(customerNameSnapshot, nameof(customerNameSnapshot), WarrantyConsts.MaxNameLength);
        ProductCodeSnapshot = Check.NotNullOrWhiteSpace(productCodeSnapshot, nameof(productCodeSnapshot), WarrantyConsts.MaxCodeLength);
        ProductNameSnapshot = Check.NotNullOrWhiteSpace(productNameSnapshot, nameof(productNameSnapshot), WarrantyConsts.MaxNameLength);
        SoldDate = soldDate;
        WarrantyStartDate = warrantyStartDate;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        Status = CustomerAssetStatus.Active;
    }
}
