using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class CustomerAsset : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public CustomerAssetSource? Source { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public Guid? SalesOrderLineId { get; private set; }
    public int? SalesOrderLineNoSnapshot { get; private set; }
    public int? SourceUnitIndex { get; private set; }
    public string AssetNo { get; private set; } = string.Empty;
    public string? OrderNoSnapshot { get; private set; }
    public string CustomerCodeSnapshot { get; private set; } = string.Empty;
    public string CustomerNameSnapshot { get; private set; } = string.Empty;
    public string? ProductCodeSnapshot { get; private set; }
    public string? ProductNameSnapshot { get; private set; }
    public string? SerialNo { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? ExternalReference { get; private set; }
    public DateTime? SoldDate { get; private set; }
    public DateTime? WarrantyStartDate { get; private set; }
    public DateTime? InstalledAt { get; private set; }
    public string? InstallationAddress { get; private set; }
    public Guid? InstalledByUserId { get; private set; }
    public string? InstallationIdempotencyKey { get; private set; }
    public CustomerAssetStatus Status { get; private set; }
    public string? Note { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    [NotMapped]
    public CustomerAssetStatus EffectiveStatus =>
        Source != CustomerAssetSource.External && InstalledAt == null && Status == CustomerAssetStatus.Active
            ? CustomerAssetStatus.PendingReview
            : Status;

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
        Source = CustomerAssetSource.SoldByCompany;
        Status = CustomerAssetStatus.Active;
    }

    public static CustomerAsset CreateSoldMachine(
        Guid id,
        Guid customerId,
        Guid productId,
        Guid salesOrderId,
        Guid salesOrderLineId,
        int salesOrderLineNo,
        int sourceUnitIndex,
        string assetNo,
        string orderNo,
        string customerCode,
        string customerName,
        string productCode,
        string productName,
        DateTime soldDate,
        string? note = null)
    {
        if (salesOrderLineNo <= 0 || sourceUnitIndex <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        return new CustomerAsset
        {
            Id = id,
            CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId)),
            Source = CustomerAssetSource.SoldByCompany,
            ProductId = Check.NotDefaultOrNull<Guid>(productId, nameof(productId)),
            SalesOrderId = Check.NotDefaultOrNull<Guid>(salesOrderId, nameof(salesOrderId)),
            SalesOrderLineId = Check.NotDefaultOrNull<Guid>(salesOrderLineId, nameof(salesOrderLineId)),
            SalesOrderLineNoSnapshot = salesOrderLineNo,
            SourceUnitIndex = sourceUnitIndex,
            AssetNo = Check.NotNullOrWhiteSpace(assetNo, nameof(assetNo), WarrantyConsts.MaxAssetNoLength),
            OrderNoSnapshot = Check.NotNullOrWhiteSpace(orderNo, nameof(orderNo), WarrantyConsts.MaxCodeLength),
            CustomerCodeSnapshot = Check.NotNullOrWhiteSpace(customerCode, nameof(customerCode), WarrantyConsts.MaxCodeLength),
            CustomerNameSnapshot = Check.NotNullOrWhiteSpace(customerName, nameof(customerName), WarrantyConsts.MaxNameLength),
            ProductCodeSnapshot = Check.NotNullOrWhiteSpace(productCode, nameof(productCode), WarrantyConsts.MaxCodeLength),
            ProductNameSnapshot = Check.NotNullOrWhiteSpace(productName, nameof(productName), WarrantyConsts.MaxNameLength),
            SoldDate = soldDate.Date,
            Status = CustomerAssetStatus.PendingInstallation,
            Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength)
        };
    }

    public static CustomerAsset CreateExternal(
        Guid id,
        Guid customerId,
        string assetNo,
        string customerCode,
        string customerName,
        string model,
        string? brand = null,
        string? serialNo = null,
        Guid? productId = null,
        string? productCode = null,
        string? productName = null,
        string? externalReference = null,
        string? note = null)
    {
        return new CustomerAsset
        {
            Id = id,
            CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId)),
            Source = CustomerAssetSource.External,
            ProductId = productId,
            AssetNo = Check.NotNullOrWhiteSpace(assetNo, nameof(assetNo), WarrantyConsts.MaxAssetNoLength),
            CustomerCodeSnapshot = Check.NotNullOrWhiteSpace(customerCode, nameof(customerCode), WarrantyConsts.MaxCodeLength),
            CustomerNameSnapshot = Check.NotNullOrWhiteSpace(customerName, nameof(customerName), WarrantyConsts.MaxNameLength),
            ProductCodeSnapshot = Check.Length(productCode, nameof(productCode), WarrantyConsts.MaxCodeLength),
            ProductNameSnapshot = Check.Length(productName, nameof(productName), WarrantyConsts.MaxNameLength),
            Brand = Check.Length(brand, nameof(brand), WarrantyConsts.MaxBrandLength),
            Model = Check.NotNullOrWhiteSpace(model, nameof(model), WarrantyConsts.MaxModelLength),
            SerialNo = Check.Length(serialNo, nameof(serialNo), WarrantyConsts.MaxSerialNoLength),
            ExternalReference = Check.Length(externalReference, nameof(externalReference), WarrantyConsts.MaxCodeLength),
            Status = CustomerAssetStatus.PendingReview,
            Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength)
        };
    }

    public void UpdateIdentification(string? serialNo, string? brand, string? model, string? note)
    {
        SerialNo = Check.Length(serialNo, nameof(serialNo), WarrantyConsts.MaxSerialNoLength);
        Brand = Check.Length(brand, nameof(brand), WarrantyConsts.MaxBrandLength);
        Model = Check.Length(model, nameof(model), WarrantyConsts.MaxModelLength);
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    public void CompleteExternalOnboarding(
        string? installationAddress,
        string? externalReference,
        Guid? reviewedByUserId,
        string idempotencyKey)
    {
        if (Source != CustomerAssetSource.External)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        InstallationAddress = Check.Length(
            installationAddress,
            nameof(installationAddress),
            WarrantyConsts.MaxAddressLength);
        ExternalReference = Check.Length(
            externalReference,
            nameof(externalReference),
            WarrantyConsts.MaxCodeLength);
        InstalledByUserId = reviewedByUserId;
        InstallationIdempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey,
            nameof(idempotencyKey),
            WarrantyConsts.MaxIdempotencyKeyLength);
        Status = CustomerAssetStatus.Active;
    }

    public void UpdateExternalProfile(string? installationAddress, string? externalReference)
    {
        if (Source != CustomerAssetSource.External)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        InstallationAddress = Check.Length(
            installationAddress,
            nameof(installationAddress),
            WarrantyConsts.MaxAddressLength);
        ExternalReference = Check.Length(
            externalReference,
            nameof(externalReference),
            WarrantyConsts.MaxCodeLength);
    }

    public void Deactivate(string? note)
    {
        if (Status is CustomerAssetStatus.Cancelled or CustomerAssetStatus.Transferred)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        Status = CustomerAssetStatus.Inactive;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    public void ConfirmInstallation(DateTime installedAt, string? address, Guid? installedByUserId, string idempotencyKey)
    {
        idempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey,
            nameof(idempotencyKey),
            WarrantyConsts.MaxIdempotencyKeyLength);

        if (InstallationIdempotencyKey == idempotencyKey)
        {
            return;
        }

        if (InstallationIdempotencyKey != null ||
            Status is CustomerAssetStatus.Inactive or CustomerAssetStatus.Cancelled or CustomerAssetStatus.Transferred)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        InstalledAt = installedAt;
        WarrantyStartDate = installedAt.Date;
        InstallationAddress = Check.Length(address, nameof(address), WarrantyConsts.MaxAddressLength);
        InstalledByUserId = installedByUserId;
        InstallationIdempotencyKey = idempotencyKey;
        Status = CustomerAssetStatus.Active;
    }

    public void CancelBeforeInstallation(string reason)
    {
        if (InstalledAt.HasValue)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesInstallationLocksModification);
        }
        if (Status == CustomerAssetStatus.Cancelled)
        {
            return;
        }
        Status = CustomerAssetStatus.Cancelled;
        reason = Check.NotNullOrWhiteSpace(reason, nameof(reason)).Trim();
        Note = reason.Length <= WarrantyConsts.MaxNoteLength
            ? reason
            : reason[..WarrantyConsts.MaxNoteLength];
    }
}
