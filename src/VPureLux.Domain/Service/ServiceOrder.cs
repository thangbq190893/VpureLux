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

    // Persistence-only compatibility shell. Operational commands belong to later Service phases.
    protected ServiceOrder() { }
}
