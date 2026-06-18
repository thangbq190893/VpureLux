using System;
using System.Collections.Generic;
using System.Linq;
using VPureLux.Sales.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Sales;

public class SalesOrder : FullAuditedAggregateRoot<Guid>
{
    private readonly List<SalesOrderLine> _lines = new();

    public string OrderNo { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public string Currency { get; private set; } = SalesConsts.Currency;
    public string CustomerCodeSnapshot { get; private set; } = string.Empty;
    public string CustomerNameSnapshot { get; private set; } = string.Empty;
    public Guid? CustomerGroupIdSnapshot { get; private set; }
    public string CustomerGroupCodeSnapshot { get; private set; } = string.Empty;
    public string CustomerGroupNameSnapshot { get; private set; } = string.Empty;
    public string? ConfirmationIdempotencyKey { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public decimal TotalRevenueAmount { get; private set; }
    public decimal TotalCostAmount { get; private set; }
    public decimal TotalProfitAmount { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public IReadOnlyCollection<SalesOrderLine> Lines => _lines.AsReadOnly();

    protected SalesOrder() { }

    internal SalesOrder(Guid id, string orderNo, Guid customerId, Guid warehouseId, DateTime orderDate) : base(id)
    {
        OrderNo = Check.NotNullOrWhiteSpace(orderNo, nameof(orderNo), SalesConsts.MaxOrderNoLength);
        CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId));
        WarehouseId = Check.NotDefaultOrNull<Guid>(warehouseId, nameof(warehouseId));
        OrderDate = orderDate;
        Status = SalesOrderStatus.Draft;
        AddLocalEvent(new SalesOrderCreatedEvent(Id, OrderNo, CustomerId));
    }

    public SalesOrderLine AddLine(
        Guid lineId,
        Guid productId,
        Guid bomVersionId,
        decimal quantity,
        Guid? suggestedPriceVersionId,
        decimal? suggestedPrice,
        decimal actualSellingPrice,
        string? overrideReason)
    {
        EnsureDraft();
        var line = new SalesOrderLine(
            lineId, _lines.Count + 1, productId, bomVersionId, quantity,
            suggestedPriceVersionId, suggestedPrice, actualSellingPrice, overrideReason);
        _lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, decimal quantity, decimal actualSellingPrice, string? overrideReason)
    {
        EnsureDraft();
        FindLine(lineId).UpdateDraft(quantity, actualSellingPrice, overrideReason);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        _lines.Remove(FindLine(lineId));
        for (var index = 0; index < _lines.Count; index++)
        {
            _lines[index].Renumber(index + 1);
        }
    }

    public void ApplyCustomerSnapshot(
        string customerCode,
        string customerName,
        Guid customerGroupId,
        string customerGroupCode,
        string customerGroupName)
    {
        EnsureDraft();
        CustomerCodeSnapshot = Check.NotNullOrWhiteSpace(customerCode, nameof(customerCode), SalesConsts.MaxCodeLength);
        CustomerNameSnapshot = Check.NotNullOrWhiteSpace(customerName, nameof(customerName), SalesConsts.MaxNameLength);
        CustomerGroupIdSnapshot = Check.NotDefaultOrNull<Guid>(customerGroupId, nameof(customerGroupId));
        CustomerGroupCodeSnapshot = Check.NotNullOrWhiteSpace(customerGroupCode, nameof(customerGroupCode), SalesConsts.MaxCodeLength);
        CustomerGroupNameSnapshot = Check.NotNullOrWhiteSpace(customerGroupName, nameof(customerGroupName), SalesConsts.MaxNameLength);
    }

    public void ApplyLineConfirmationSnapshot(
        Guid lineId,
        string itemCode,
        string itemName,
        string unit,
        int? bomVersionNo,
        Guid inventoryTransactionId,
        decimal costAmount,
        IEnumerable<SalesOrderBomSnapshotData>? bomSnapshotItems = null)
    {
        EnsureDraft();
        FindLine(lineId).ApplyConfirmationSnapshot(
            itemCode, itemName, unit, bomVersionNo, inventoryTransactionId, costAmount, bomSnapshotItems);
    }

    public void Confirm(string idempotencyKey, DateTime confirmedAt)
    {
        if (Status == SalesOrderStatus.Confirmed)
        {
            if (ConfirmationIdempotencyKey == idempotencyKey)
            {
                return;
            }
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesConfirmationIdempotencyConflict);
        }
        EnsureDraft();
        if (_lines.Count == 0 || _lines.Any(x => !x.IsConfirmedSnapshot) || !CustomerGroupIdSnapshot.HasValue)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        ConfirmationIdempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey, nameof(idempotencyKey), SalesConsts.MaxIdempotencyKeyLength);
        ConfirmedAt = confirmedAt;
        TotalRevenueAmount = _lines.Sum(x => x.RevenueAmount);
        TotalCostAmount = _lines.Sum(x => x.CostAmountSnapshot);
        TotalProfitAmount = _lines.Sum(x => x.ProfitAmount);
        Status = SalesOrderStatus.Confirmed;
        AddLocalEvent(new SalesOrderConfirmedEvent(
            Id, OrderNo, CustomerId, TotalRevenueAmount, TotalProfitAmount));
    }

    public void CancelDraft(DateTime cancelledAt)
    {
        EnsureDraft();
        Status = SalesOrderStatus.Cancelled;
        CancelledAt = cancelledAt;
        AddLocalEvent(new SalesOrderCancelledEvent(Id, OrderNo, CustomerId));
    }

    private SalesOrderLine FindLine(Guid lineId) =>
        _lines.SingleOrDefault(x => x.Id == lineId)
        ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);

    private void EnsureDraft()
    {
        if (Status == SalesOrderStatus.Confirmed)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderAlreadyConfirmed);
        }
        if (Status == SalesOrderStatus.Cancelled)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderAlreadyCancelled);
        }
    }
}
