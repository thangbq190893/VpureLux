using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace VPureLux.Service;

public class ServiceOrderLine : Entity<Guid>
{
    public int LineNo { get; private set; }
    public ServiceOrderLineType LineType { get; private set; }
    public Guid? ComponentId { get; private set; }
    public Guid? CustomerAssetComponentId { get; private set; }
    public Guid? ServiceWorkId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string UnitSnapshot { get; private set; } = string.Empty;
    public int PlannedQuantity { get; private set; }
    public int ActualQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal RevenueAmount { get; private set; }
    public decimal CostAmountSnapshot { get; private set; }
    public string? Note { get; private set; }

    protected ServiceOrderLine()
    {
    }

    internal ServiceOrderLine(
        Guid id,
        int lineNo,
        ServiceOrderLineType lineType,
        Guid catalogItemId,
        Guid? customerAssetComponentId,
        string itemCode,
        string itemName,
        string unit,
        int quantity,
        decimal unitPrice,
        string? note) : base(id)
    {
        if (lineNo <= 0 || catalogItemId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        LineNo = lineNo;
        Set(lineType, catalogItemId, customerAssetComponentId, itemCode, itemName, unit, quantity, unitPrice, note);
    }

    internal void UpdateDraft(
        ServiceOrderLineType lineType,
        Guid catalogItemId,
        Guid? customerAssetComponentId,
        string itemCode,
        string itemName,
        string unit,
        int quantity,
        decimal unitPrice,
        string? note) =>
        Set(lineType, catalogItemId, customerAssetComponentId, itemCode, itemName, unit, quantity, unitPrice, note);

    internal void Renumber(int lineNo) => LineNo = lineNo > 0
        ? lineNo
        : throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);

    internal void Complete(int actualQuantity, decimal costAmount)
    {
        if (actualQuantity < 0 || actualQuantity > PlannedQuantity || costAmount < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        if (LineType == ServiceOrderLineType.Labor && costAmount != 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        ActualQuantity = actualQuantity;
        RevenueAmount = RoundMoney(UnitPrice * actualQuantity);
        CostAmountSnapshot = RoundMoney(costAmount);
    }

    private void Set(
        ServiceOrderLineType lineType,
        Guid catalogItemId,
        Guid? customerAssetComponentId,
        string itemCode,
        string itemName,
        string unit,
        int quantity,
        decimal unitPrice,
        string? note)
    {
        if (!Enum.IsDefined(lineType) || quantity <= 0 || unitPrice < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        LineType = lineType;
        ComponentId = lineType == ServiceOrderLineType.Material ? catalogItemId : null;
        CustomerAssetComponentId = lineType == ServiceOrderLineType.Material ? customerAssetComponentId : null;
        ServiceWorkId = lineType == ServiceOrderLineType.Labor ? catalogItemId : null;
        ItemCodeSnapshot = Check.NotNullOrWhiteSpace(itemCode, nameof(itemCode), ServiceConsts.MaxCodeLength);
        ItemNameSnapshot = Check.NotNullOrWhiteSpace(itemName, nameof(itemName), ServiceConsts.MaxNameLength);
        UnitSnapshot = Check.NotNullOrWhiteSpace(unit, nameof(unit), ServiceConsts.MaxUnitLength);
        PlannedQuantity = quantity;
        UnitPrice = RoundMoney(unitPrice);
        Note = string.IsNullOrWhiteSpace(note) ? null : Check.Length(note.Trim(), nameof(note), ServiceConsts.MaxNoteLength);
        ActualQuantity = 0;
        RevenueAmount = 0;
        CostAmountSnapshot = 0;
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, ServiceConsts.MoneyScale, MidpointRounding.AwayFromZero);
}
