using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class CustomerAssetComponent : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerAssetId { get; private set; }
    public string PositionCode { get; private set; } = string.Empty;
    public string PositionName { get; private set; } = string.Empty;
    public Guid? ComponentId { get; private set; }
    public string? ComponentCodeSnapshot { get; private set; }
    public string? ComponentNameSnapshot { get; private set; }
    public string? ComponentUnitSnapshot { get; private set; }
    public int Quantity { get; private set; }
    public DateTime? ReplacementBaselineDate { get; private set; }
    public CustomerAssetComponentStatus Status { get; private set; }
    public string? Note { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected CustomerAssetComponent()
    {
    }

    public CustomerAssetComponent(
        Guid id,
        Guid customerAssetId,
        string positionCode,
        string positionName,
        Guid? componentId,
        string? componentCode,
        string? componentName,
        string? componentUnit,
        int quantity,
        DateTime? replacementBaselineDate,
        bool pendingInstallation,
        string? note = null)
        : base(id)
    {
        CustomerAssetId = Check.NotDefaultOrNull<Guid>(customerAssetId, nameof(customerAssetId));
        PositionCode = Check.NotNullOrWhiteSpace(positionCode, nameof(positionCode), WarrantyConsts.MaxPositionCodeLength);
        PositionName = Check.NotNullOrWhiteSpace(positionName, nameof(positionName), WarrantyConsts.MaxNameLength);
        if (quantity <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        Quantity = quantity;
        SetMapping(componentId, componentCode, componentName, componentUnit);
        ReplacementBaselineDate = replacementBaselineDate?.Date;
        Status = pendingInstallation
            ? CustomerAssetComponentStatus.PendingInstallation
            : ResolveReviewStatus();
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    public void MapComponent(Guid componentId, string code, string name, string unit, string? note)
    {
        SetMapping(componentId, code, name, unit);
        Status = ReplacementBaselineDate.HasValue
            ? CustomerAssetComponentStatus.Active
            : CustomerAssetComponentStatus.MissingBaseline;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    public void UpdatePosition(string positionCode, string positionName, int quantity, string? note)
    {
        if (quantity <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        PositionCode = Check.NotNullOrWhiteSpace(
            positionCode,
            nameof(positionCode),
            WarrantyConsts.MaxPositionCodeLength);
        PositionName = Check.NotNullOrWhiteSpace(
            positionName,
            nameof(positionName),
            WarrantyConsts.MaxNameLength);
        Quantity = quantity;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    public void ClearMapping(string? note)
    {
        SetMapping(null, null, null, null);
        Status = CustomerAssetComponentStatus.MissingMapping;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    public void SetReplacementBaseline(DateTime baselineDate)
    {
        ReplacementBaselineDate = baselineDate.Date;
        Status = ComponentId.HasValue
            ? CustomerAssetComponentStatus.Active
            : CustomerAssetComponentStatus.MissingMapping;
    }

    public void ClearReplacementBaseline()
    {
        ReplacementBaselineDate = null;
        Status = ComponentId.HasValue
            ? CustomerAssetComponentStatus.MissingBaseline
            : CustomerAssetComponentStatus.MissingMapping;
    }

    public void Deactivate(string? note)
    {
        Status = CustomerAssetComponentStatus.Inactive;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    private void SetMapping(Guid? componentId, string? code, string? name, string? unit)
    {
        if (!componentId.HasValue)
        {
            ComponentId = null;
            ComponentCodeSnapshot = null;
            ComponentNameSnapshot = null;
            ComponentUnitSnapshot = null;
            return;
        }

        ComponentId = Check.NotDefaultOrNull<Guid>(componentId.Value, nameof(componentId));
        ComponentCodeSnapshot = Check.NotNullOrWhiteSpace(code, nameof(code), WarrantyConsts.MaxCodeLength);
        ComponentNameSnapshot = Check.NotNullOrWhiteSpace(name, nameof(name), WarrantyConsts.MaxNameLength);
        ComponentUnitSnapshot = Check.NotNullOrWhiteSpace(unit, nameof(unit), WarrantyConsts.MaxUnitLength);
    }

    private CustomerAssetComponentStatus ResolveReviewStatus()
    {
        if (!ComponentId.HasValue)
        {
            return CustomerAssetComponentStatus.MissingMapping;
        }

        return ReplacementBaselineDate.HasValue
            ? CustomerAssetComponentStatus.Active
            : CustomerAssetComponentStatus.MissingBaseline;
    }
}
