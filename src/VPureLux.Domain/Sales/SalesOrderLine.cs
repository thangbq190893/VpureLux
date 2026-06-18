using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace VPureLux.Sales;

public sealed record SalesOrderBomSnapshotData(
    Guid ComponentId,
    string ComponentCode,
    string ComponentName,
    string Unit,
    decimal QuantityPerProduct,
    decimal TotalRequiredQuantity);

public class SalesOrderLine : Entity<Guid>
{
    private readonly List<SalesOrderBomSnapshotItem> _bomSnapshotItems = new();

    public int LineNo { get; private set; }
    public SalesOrderLineType LineType { get; private set; }
    public Guid CatalogItemId { get; private set; }
    public Guid ProductId => CatalogItemId;
    public Guid? BomVersionId { get; private set; }
    public int? BomVersionNoSnapshot { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string UnitSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public Guid? SuggestedPriceVersionId { get; private set; }
    public decimal? SuggestedPriceSnapshot { get; private set; }
    public decimal ActualSellingPrice { get; private set; }
    public string? OverrideReason { get; private set; }
    public Guid? InventoryTransactionId { get; private set; }
    public decimal RevenueAmount { get; private set; }
    public decimal CostPriceSnapshot { get; private set; }
    public decimal CostAmountSnapshot { get; private set; }
    public decimal ProfitAmount { get; private set; }
    public decimal MarginPercent { get; private set; }
    public IReadOnlyCollection<SalesOrderBomSnapshotItem> BomSnapshotItems => _bomSnapshotItems.AsReadOnly();
    public bool IsConfirmedSnapshot => InventoryTransactionId.HasValue;

    protected SalesOrderLine() { }

    internal SalesOrderLine(
        Guid id,
        int lineNo,
        Guid productId,
        Guid bomVersionId,
        decimal quantity,
        Guid? suggestedPriceVersionId,
        decimal? suggestedPrice,
        decimal actualSellingPrice,
        string? overrideReason) : base(id)
    {
        if (lineNo <= 0 || productId == Guid.Empty || bomVersionId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        LineNo = lineNo;
        LineType = SalesOrderLineType.Product;
        CatalogItemId = productId;
        BomVersionId = bomVersionId;
        Quantity = NormalizeQuantity(quantity);
        SuggestedPriceVersionId = suggestedPriceVersionId;
        SuggestedPriceSnapshot = NormalizeOptionalMoney(suggestedPrice);
        SetActualSellingPrice(actualSellingPrice, overrideReason);
    }

    internal void UpdateDraft(decimal quantity, decimal actualSellingPrice, string? overrideReason)
    {
        Quantity = NormalizeQuantity(quantity);
        SetActualSellingPrice(actualSellingPrice, overrideReason);
    }

    internal void Renumber(int lineNo)
    {
        if (lineNo <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        LineNo = lineNo;
    }

    internal void ApplyConfirmationSnapshot(
        string itemCode,
        string itemName,
        string unit,
        int? bomVersionNo,
        Guid inventoryTransactionId,
        decimal costAmount,
        IEnumerable<SalesOrderBomSnapshotData>? bomSnapshotItems = null)
    {
        if (IsConfirmedSnapshot || inventoryTransactionId == Guid.Empty || costAmount < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderCannotBeModified);
        }

        ItemCodeSnapshot = Check.NotNullOrWhiteSpace(itemCode, nameof(itemCode), SalesConsts.MaxCodeLength);
        ItemNameSnapshot = Check.NotNullOrWhiteSpace(itemName, nameof(itemName), SalesConsts.MaxNameLength);
        UnitSnapshot = Check.NotNullOrWhiteSpace(unit, nameof(unit), SalesConsts.MaxUnitLength);
        BomVersionNoSnapshot = bomVersionNo;
        InventoryTransactionId = inventoryTransactionId;
        CostAmountSnapshot = RoundMoney(costAmount);
        CostPriceSnapshot = Quantity == 0 ? 0 : RoundMoney(CostAmountSnapshot / Quantity);
        RevenueAmount = RoundMoney(ActualSellingPrice * Quantity);
        ProfitAmount = RoundMoney(RevenueAmount - CostAmountSnapshot);
        MarginPercent = RevenueAmount == 0
            ? 0
            : decimal.Round(ProfitAmount / RevenueAmount * 100, SalesConsts.MarginScale, MidpointRounding.AwayFromZero);

        _bomSnapshotItems.Clear();
        if (bomSnapshotItems != null)
        {
            foreach (var item in bomSnapshotItems)
            {
                _bomSnapshotItems.Add(new SalesOrderBomSnapshotItem(
                    Guid.NewGuid(),
                    item.ComponentId,
                    item.ComponentCode,
                    item.ComponentName,
                    item.Unit,
                    item.QuantityPerProduct,
                    item.TotalRequiredQuantity));
            }
        }
    }

    private void SetActualSellingPrice(decimal value, string? overrideReason)
    {
        value = RoundMoney(value);
        if (value < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var isOverride = SuggestedPriceSnapshot.HasValue && SuggestedPriceSnapshot.Value != value;
        if (isOverride && string.IsNullOrWhiteSpace(overrideReason))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesOverrideReasonRequired);
        }

        ActualSellingPrice = value;
        OverrideReason = isOverride
            ? Check.NotNullOrWhiteSpace(overrideReason, nameof(overrideReason), SalesConsts.MaxOverrideReasonLength).Trim()
            : null;
    }

    private static decimal NormalizeQuantity(decimal value)
    {
        value = decimal.Round(value, SalesConsts.QuantityScale, MidpointRounding.AwayFromZero);
        if (value <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        return value;
    }

    private static decimal? NormalizeOptionalMoney(decimal? value)
    {
        if (!value.HasValue)
        {
            return null;
        }
        var normalized = RoundMoney(value.Value);
        if (normalized < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        return normalized;
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero);
}
