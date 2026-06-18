using System;
using System.Collections.Generic;
using System.Linq;
using VPureLux.Inventory.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Inventory;

public class InventoryTransaction : FullAuditedAggregateRoot<Guid>
{
    private readonly List<InventoryTransactionLine> _lines = new();

    public Guid WarehouseId { get; private set; }
    public InventoryTransactionType Type { get; private set; }
    public InventoryTransactionStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public Guid? BomVersionId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public IReadOnlyCollection<InventoryTransactionLine> Lines => _lines.AsReadOnly();
    public decimal TotalIssueCost => _lines.Sum(x => x.TotalIssueCost);

    protected InventoryTransaction()
    {
    }

    internal InventoryTransaction(
        Guid id,
        Guid warehouseId,
        InventoryTransactionType type,
        string idempotencyKey,
        string requestHash,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? bomVersionId = null,
        string? reason = null)
        : base(id)
    {
        if (warehouseId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        WarehouseId = warehouseId;
        Type = type;
        IdempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey, nameof(idempotencyKey), InventoryConsts.MaxIdempotencyKeyLength);
        RequestHash = Check.NotNullOrWhiteSpace(
            requestHash, nameof(requestHash), InventoryConsts.RequestHashLength);
        ReferenceType = Check.Length(referenceType, nameof(referenceType), InventoryConsts.MaxReferenceTypeLength);
        ReferenceId = referenceId;
        BomVersionId = bomVersionId;
        Reason = NormalizeReason(type, reason);
        Status = InventoryTransactionStatus.Draft;
    }

    public InventoryTransactionLine AddReceiptLine(
        Guid lineId,
        Guid stockItemId,
        decimal quantity,
        string lotNo,
        DateTime receivedAt,
        decimal unitCost)
    {
        EnsureDraft();
        var line = new InventoryTransactionLine(
            lineId,
            stockItemId,
            InventoryMovementDirection.Increase,
            quantity,
            Check.NotNullOrWhiteSpace(lotNo, nameof(lotNo), InventoryConsts.MaxLotNoLength),
            receivedAt,
            unitCost);
        _lines.Add(line);
        return line;
    }

    public InventoryTransactionLine AddIssueLine(Guid lineId, Guid stockItemId, decimal quantity)
    {
        EnsureDraft();
        var line = new InventoryTransactionLine(
            lineId, stockItemId, InventoryMovementDirection.Decrease, quantity);
        _lines.Add(line);
        return line;
    }

    public void AddAllocation(
        Guid lineId,
        Guid allocationId,
        Guid lotId,
        decimal quantity,
        decimal unitCost)
    {
        EnsureDraft();
        var line = _lines.SingleOrDefault(x => x.Id == lineId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        line.AddAllocation(allocationId, lotId, quantity, unitCost);
    }

    public void Post(DateTime postedAt)
    {
        EnsureDraft();
        if (_lines.Count == 0 ||
            _lines.Any(x => x.Direction == InventoryMovementDirection.Decrease &&
                            x.Allocations.Sum(a => a.Quantity) != x.Quantity))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        Status = InventoryTransactionStatus.Posted;
        PostedAt = postedAt;

        if (Type == InventoryTransactionType.PurchaseReceipt)
        {
            AddLocalEvent(new InventoryReceiptPostedEvent(Id, WarehouseId, postedAt));
        }
        else if (Type is InventoryTransactionType.AdjustmentIncrease or InventoryTransactionType.AdjustmentDecrease)
        {
            AddLocalEvent(new InventoryAdjustedEvent(Id, WarehouseId, Type, Reason!));
        }
        else
        {
            AddLocalEvent(new InventoryIssuePostedEvent(Id, WarehouseId, TotalIssueCost, postedAt));
        }
    }

    private void EnsureDraft()
    {
        if (Status == InventoryTransactionStatus.Posted)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InventoryTransactionAlreadyPosted);
        }
    }

    private static string? NormalizeReason(InventoryTransactionType type, string? reason)
    {
        if (type is not (InventoryTransactionType.AdjustmentIncrease or InventoryTransactionType.AdjustmentDecrease))
        {
            return string.IsNullOrWhiteSpace(reason)
                ? null
                : Check.Length(reason.Trim(), nameof(reason), InventoryConsts.MaxReasonLength);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.AdjustmentReasonRequired);
        }

        return Check.NotNullOrWhiteSpace(reason, nameof(reason), InventoryConsts.MaxReasonLength).Trim();
    }
}
