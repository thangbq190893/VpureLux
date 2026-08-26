using System;
using System.Collections.Generic;
using System.Linq;
using VPureLux.Inventory;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace VPureLux.Sales;

public class SalesOrderRevisionLine : Entity<Guid>
{
    private readonly List<SalesOrderRevisionAllocation> _reversedAllocations = new();

    public Guid? SourceSalesOrderLineId { get; private set; }
    public Guid? EffectiveSalesOrderLineId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid BomVersionId { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid? SuggestedPriceVersionId { get; private set; }
    public decimal? SuggestedPriceSnapshot { get; private set; }
    public decimal ActualSellingPrice { get; private set; }
    public string? OverrideReason { get; private set; }
    public bool IsRemoved { get; private set; }
    public Guid? BeforeProductId { get; private set; }
    public Guid? BeforeBomVersionId { get; private set; }
    public decimal? BeforeQuantity { get; private set; }
    public decimal? BeforeActualSellingPrice { get; private set; }
    public decimal BeforeCostAmount { get; private set; }
    public Guid? BeforeInventoryTransactionId { get; private set; }
    public DateTime? ReturnConfirmedAt { get; private set; }
    public Guid? ReturnConfirmedBy { get; private set; }
    public string? ReturnReason { get; private set; }
    public Guid? IssueInventoryTransactionId { get; private set; }
    public Guid? ReversalInventoryTransactionId { get; private set; }
    public decimal AppliedCostAmount { get; private set; }
    public IReadOnlyCollection<SalesOrderRevisionAllocation> ReversedAllocations => _reversedAllocations.AsReadOnly();

    public bool RequiresReturnConfirmation => SourceSalesOrderLineId.HasValue &&
        (IsRemoved || ProductId != BeforeProductId || Quantity < BeforeQuantity);

    protected SalesOrderRevisionLine() { }

    private SalesOrderRevisionLine(Guid id) : base(id) { }

    internal static SalesOrderRevisionLine FromEffective(Guid id, SalesOrderLine source) => new(id)
    {
        SourceSalesOrderLineId = source.Id,
        LineNo = source.LineNo,
        ProductId = source.ProductId,
        BomVersionId = source.BomVersionId!.Value,
        Quantity = source.Quantity,
        SuggestedPriceVersionId = source.SuggestedPriceVersionId,
        SuggestedPriceSnapshot = source.SuggestedPriceSnapshot,
        ActualSellingPrice = source.ActualSellingPrice,
        OverrideReason = source.OverrideReason,
        BeforeProductId = source.ProductId,
        BeforeBomVersionId = source.BomVersionId,
        BeforeQuantity = source.Quantity,
        BeforeActualSellingPrice = source.ActualSellingPrice,
        BeforeCostAmount = source.CostAmountSnapshot,
        BeforeInventoryTransactionId = source.InventoryTransactionId
    };

    internal static SalesOrderRevisionLine CreateAdded(
        Guid id,
        int lineNo,
        Guid productId,
        Guid bomVersionId,
        decimal quantity,
        Guid? suggestedPriceVersionId,
        decimal? suggestedPrice,
        decimal actualSellingPrice,
        string? overrideReason)
    {
        var line = new SalesOrderRevisionLine(id) { LineNo = lineNo };
        line.Update(productId, bomVersionId, quantity, suggestedPriceVersionId, suggestedPrice, actualSellingPrice, overrideReason);
        return line;
    }

    internal void Update(
        Guid productId,
        Guid bomVersionId,
        decimal quantity,
        Guid? suggestedPriceVersionId,
        decimal? suggestedPrice,
        decimal actualSellingPrice,
        string? overrideReason)
    {
        if (productId == Guid.Empty || bomVersionId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        quantity = decimal.Round(quantity, SalesConsts.QuantityScale, MidpointRounding.AwayFromZero);
        actualSellingPrice = SalesOrderRevision.RoundMoney(actualSellingPrice);
        if (quantity <= 0 || actualSellingPrice < 0 || suggestedPrice < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        var isOverride = suggestedPrice.HasValue && suggestedPrice.Value != actualSellingPrice;
        if (isOverride && string.IsNullOrWhiteSpace(overrideReason))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesOverrideReasonRequired);
        }

        ProductId = productId;
        BomVersionId = bomVersionId;
        Quantity = quantity;
        SuggestedPriceVersionId = suggestedPriceVersionId;
        SuggestedPriceSnapshot = suggestedPrice.HasValue ? SalesOrderRevision.RoundMoney(suggestedPrice.Value) : null;
        ActualSellingPrice = actualSellingPrice;
        OverrideReason = isOverride
            ? Check.NotNullOrWhiteSpace(overrideReason, nameof(overrideReason), SalesConsts.MaxOverrideReasonLength).Trim()
            : null;
        IsRemoved = false;
    }

    internal void MarkRemoved() => IsRemoved = true;

    internal void Renumber(int lineNo) => LineNo = lineNo > 0
        ? lineNo
        : throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);

    internal void ConfirmReturnedGoods(Guid? actorId, DateTime confirmedAt, string reason)
    {
        if (!RequiresReturnConfirmation)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        ReturnConfirmedBy = actorId;
        ReturnConfirmedAt = confirmedAt;
        ReturnReason = SalesOrderRevision.NormalizeReason(reason);
    }

    public void MarkApplied(
        Guid? effectiveSalesOrderLineId,
        Guid? issueTransactionId,
        Guid? reversalTransactionId,
        decimal appliedCost,
        IEnumerable<SalesOrderRevisionAllocation>? reversedAllocations = null)
    {
        EffectiveSalesOrderLineId = effectiveSalesOrderLineId;
        IssueInventoryTransactionId = issueTransactionId;
        ReversalInventoryTransactionId = reversalTransactionId;
        AppliedCostAmount = SalesOrderRevision.RoundMoney(appliedCost);
        _reversedAllocations.Clear();
        if (reversedAllocations != null)
        {
            _reversedAllocations.AddRange(reversedAllocations);
        }
    }

    public bool IsUnchanged() => SourceSalesOrderLineId.HasValue && !IsRemoved &&
        ProductId == BeforeProductId && Quantity == BeforeQuantity &&
        ActualSellingPrice == BeforeActualSellingPrice && BomVersionId == BeforeBomVersionId;
}

public class SalesOrderRevisionAllocation : Entity<Guid>
{
    public Guid StockItemId { get; private set; }
    public Guid InventoryLotId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost => Quantity * UnitCost;

    protected SalesOrderRevisionAllocation() { }

    public SalesOrderRevisionAllocation(Guid id, Guid stockItemId, Guid inventoryLotId, decimal quantity, decimal unitCost) : base(id)
    {
        if (stockItemId == Guid.Empty || inventoryLotId == Guid.Empty || quantity <= 0 || unitCost <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        StockItemId = stockItemId;
        InventoryLotId = inventoryLotId;
        Quantity = decimal.Round(quantity, InventoryConsts.QuantityScale, MidpointRounding.AwayFromZero);
        UnitCost = decimal.Round(unitCost, InventoryConsts.CostScale, MidpointRounding.AwayFromZero);
    }
}
