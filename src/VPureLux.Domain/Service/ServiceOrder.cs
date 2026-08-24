using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Service;

public class ServiceOrder : FullAuditedAggregateRoot<Guid>
{
    private readonly List<ServiceOrderLine> _lines = new();

    public string OrderNo { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid CustomerAssetId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public Guid? TechnicianUserId { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public string Currency { get; private set; } = ServiceConsts.Currency;
    public string CustomerCodeSnapshot { get; private set; } = string.Empty;
    public string CustomerNameSnapshot { get; private set; } = string.Empty;
    public string AssetNoSnapshot { get; private set; } = string.Empty;
    public string AssetNameSnapshot { get; private set; } = string.Empty;
    public string? ServiceAddress { get; private set; }
    public string? Note { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CompletionIdempotencyKey { get; private set; }
    public Guid? InventoryTransactionId { get; private set; }
    public decimal TotalRevenueAmount { get; private set; }
    public decimal TotalCostAmount { get; private set; }
    public decimal TotalProfitAmount { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public IReadOnlyCollection<ServiceOrderLine> Lines => _lines.AsReadOnly();

    protected ServiceOrder()
    {
    }

    public ServiceOrder(
        Guid id,
        string orderNo,
        Guid customerId,
        Guid customerAssetId,
        Guid warehouseId,
        DateTime orderDate,
        string customerCode,
        string customerName,
        string assetNo,
        string assetName,
        DateTime? scheduledAt,
        Guid? technicianUserId,
        string? serviceAddress,
        string? note) : base(id)
    {
        OrderNo = Check.NotNullOrWhiteSpace(orderNo, nameof(orderNo), ServiceConsts.MaxOrderNoLength);
        CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId));
        CustomerAssetId = Check.NotDefaultOrNull<Guid>(customerAssetId, nameof(customerAssetId));
        WarehouseId = Check.NotDefaultOrNull<Guid>(warehouseId, nameof(warehouseId));
        OrderDate = orderDate;
        CustomerCodeSnapshot = Check.NotNullOrWhiteSpace(customerCode, nameof(customerCode), ServiceConsts.MaxCodeLength);
        CustomerNameSnapshot = Check.NotNullOrWhiteSpace(customerName, nameof(customerName), ServiceConsts.MaxNameLength);
        AssetNoSnapshot = Check.NotNullOrWhiteSpace(assetNo, nameof(assetNo), ServiceConsts.MaxAssetNoLength);
        AssetNameSnapshot = Check.NotNullOrWhiteSpace(assetName, nameof(assetName), ServiceConsts.MaxNameLength);
        Status = ServiceOrderStatus.Draft;
        UpdatePlan(scheduledAt, technicianUserId, serviceAddress, note);
    }

    public void UpdatePlan(DateTime? scheduledAt, Guid? technicianUserId, string? serviceAddress, string? note)
    {
        EnsureDraft();
        ScheduledAt = scheduledAt;
        TechnicianUserId = technicianUserId;
        ServiceAddress = string.IsNullOrWhiteSpace(serviceAddress)
            ? null
            : Check.Length(serviceAddress.Trim(), nameof(serviceAddress), ServiceConsts.MaxAddressLength);
        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : Check.Length(note.Trim(), nameof(note), ServiceConsts.MaxNoteLength);
    }

    public ServiceOrderLine AddLine(
        Guid lineId,
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
        EnsureDraft();
        var line = new ServiceOrderLine(lineId, _lines.Count + 1, lineType, catalogItemId,
            customerAssetComponentId, itemCode, itemName, unit, quantity, unitPrice, note);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = FindLine(lineId);
        _lines.Remove(line);
        for (var index = 0; index < _lines.Count; index++)
        {
            _lines[index].Renumber(index + 1);
        }
    }

    public void Confirm(DateTime confirmedAt)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        Status = ServiceOrderStatus.Confirmed;
        ConfirmedAt = confirmedAt;
    }

    public void Start(DateTime startedAt)
    {
        if (Status != ServiceOrderStatus.Confirmed)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderCannotBeModified);
        }
        Status = ServiceOrderStatus.InProgress;
        StartedAt = startedAt;
    }

    public void CompleteLine(Guid lineId, int actualQuantity, decimal costAmount) =>
        FindLine(lineId).Complete(actualQuantity, costAmount);

    public void Complete(string idempotencyKey, DateTime completedAt, Guid? inventoryTransactionId)
    {
        idempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey, nameof(idempotencyKey), ServiceConsts.MaxIdempotencyKeyLength);
        if (Status == ServiceOrderStatus.Completed)
        {
            if (CompletionIdempotencyKey == idempotencyKey)
            {
                return;
            }
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderCompletionConflict);
        }
        if (Status is not (ServiceOrderStatus.Confirmed or ServiceOrderStatus.InProgress) ||
            _lines.All(x => x.ActualQuantity == 0))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderCannotBeCompleted);
        }

        var hasMaterials = _lines.Any(x => x.LineType == ServiceOrderLineType.Material && x.ActualQuantity > 0);
        if (hasMaterials != inventoryTransactionId.HasValue)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderCannotBeCompleted);
        }

        CompletionIdempotencyKey = idempotencyKey;
        CompletedAt = completedAt;
        InventoryTransactionId = inventoryTransactionId;
        TotalRevenueAmount = _lines.Sum(x => x.RevenueAmount);
        TotalCostAmount = _lines.Sum(x => x.CostAmountSnapshot);
        TotalProfitAmount = TotalRevenueAmount - TotalCostAmount;
        Status = ServiceOrderStatus.Completed;
    }

    public void Cancel(DateTime cancelledAt)
    {
        if (Status is ServiceOrderStatus.Completed or ServiceOrderStatus.Cancelled)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderCannotBeModified);
        }
        Status = ServiceOrderStatus.Cancelled;
        CancelledAt = cancelledAt;
    }

    private ServiceOrderLine FindLine(Guid lineId) =>
        _lines.SingleOrDefault(x => x.Id == lineId)
        ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);

    private void EnsureDraft()
    {
        if (Status != ServiceOrderStatus.Draft)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderCannotBeModified);
        }
    }
}
