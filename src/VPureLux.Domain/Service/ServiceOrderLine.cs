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
    public decimal? StandardCostSnapshot { get; private set; }
    public decimal? ActualCostAmount { get; private set; }
    public Guid? InventoryTransactionLineId { get; private set; }
    public string? Note { get; private set; }

    protected ServiceOrderLine()
    {
    }

    internal ServiceOrderLine(
        Guid id, int lineNo, ServiceOrderLineType lineType, Guid catalogItemId,
        Guid? customerAssetComponentId, string itemCode, string itemName, string unit,
        int quantity, decimal unitPrice, decimal? standardCostSnapshot, string? note) : base(id)
    {
        Renumber(lineNo);
        ReplaceSelection(
            lineType, catalogItemId, customerAssetComponentId, itemCode, itemName,
            unit, quantity, unitPrice, standardCostSnapshot, note);
    }

    internal void UpdateDraft(Guid? customerAssetComponentId, int quantity, decimal unitPrice, string? note)
    {
        ValidateMutable(quantity, unitPrice);
        CustomerAssetComponentId = LineType == ServiceOrderLineType.Material ? customerAssetComponentId : null;
        PlannedQuantity = quantity;
        UnitPrice = RoundMoney(unitPrice);
        Note = NormalizeNote(note);
    }

    internal void ReplaceSelection(
        ServiceOrderLineType lineType, Guid catalogItemId, Guid? customerAssetComponentId,
        string itemCode, string itemName, string unit, int quantity, decimal unitPrice,
        decimal? standardCostSnapshot, string? note)
    {
        if (!Enum.IsDefined(lineType) || catalogItemId == Guid.Empty)
        {
            throw InvalidLine("CatalogItem");
        }

        ValidateMutable(quantity, unitPrice);
        LineType = lineType;
        ComponentId = lineType == ServiceOrderLineType.Material ? catalogItemId : null;
        CustomerAssetComponentId = lineType == ServiceOrderLineType.Material ? customerAssetComponentId : null;
        ServiceWorkId = lineType == ServiceOrderLineType.Labor ? catalogItemId : null;
        ItemCodeSnapshot = Check.NotNullOrWhiteSpace(itemCode, nameof(itemCode), ServiceConsts.MaxCodeLength).Trim();
        ItemNameSnapshot = Check.NotNullOrWhiteSpace(itemName, nameof(itemName), ServiceConsts.MaxNameLength).Trim();
        UnitSnapshot = Check.NotNullOrWhiteSpace(unit, nameof(unit), ServiceConsts.MaxUnitLength).Trim();
        PlannedQuantity = quantity;
        UnitPrice = RoundMoney(unitPrice);
        StandardCostSnapshot = lineType == ServiceOrderLineType.Labor
            ? standardCostSnapshot.HasValue ? ValidateMoney(standardCostSnapshot.Value, nameof(standardCostSnapshot)) : null
            : null;
        Note = NormalizeNote(note);
        ActualQuantity = 0;
        RevenueAmount = 0;
        CostAmountSnapshot = 0;
    }

    internal void RecordCompletion(int quantity, decimal? materialCost, Guid? inventoryLineId)
    {
        ActualQuantity = quantity;
        RevenueAmount = RoundMoney(quantity * UnitPrice);
        ActualCostAmount = quantity == 0 ? null : LineType == ServiceOrderLineType.Material
            ? materialCost : StandardCostSnapshot * quantity;
        CostAmountSnapshot = ActualCostAmount ?? 0m;
        InventoryTransactionLineId = inventoryLineId;
    }

    internal void Renumber(int lineNo)
    {
        if (lineNo <= 0)
        {
            throw InvalidLine(nameof(lineNo));
        }

        LineNo = lineNo;
    }

    private static void ValidateMutable(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw InvalidLine(nameof(quantity));
        }

        ValidateMoney(unitPrice, nameof(unitPrice));
    }

    private static decimal ValidateMoney(decimal value, string field)
    {
        if (value < 0 || value > 9999999999999999.99m)
        {
            throw InvalidLine(field);
        }

        return RoundMoney(value);
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, ServiceConsts.MoneyScale, MidpointRounding.AwayFromZero);

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : Check.Length(note.Trim(), nameof(note), ServiceConsts.MaxNoteLength);

    private static BusinessException InvalidLine(string field) =>
        new BusinessException(ServiceErrorCodes.InvalidLine).WithData("Field", field);
}
