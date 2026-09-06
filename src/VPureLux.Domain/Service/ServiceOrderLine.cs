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

    // Persistence-only compatibility shell. Operational commands belong to later Service phases.
    protected ServiceOrderLine() { }
}
