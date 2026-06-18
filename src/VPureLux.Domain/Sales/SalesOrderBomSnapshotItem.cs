using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace VPureLux.Sales;

public class SalesOrderBomSnapshotItem : Entity<Guid>
{
    public Guid ComponentId { get; private set; }
    public string ComponentCode { get; private set; } = string.Empty;
    public string ComponentName { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal QuantityPerProduct { get; private set; }
    public decimal TotalRequiredQuantity { get; private set; }

    protected SalesOrderBomSnapshotItem() { }

    internal SalesOrderBomSnapshotItem(
        Guid id,
        Guid componentId,
        string componentCode,
        string componentName,
        string unit,
        decimal quantityPerProduct,
        decimal totalRequiredQuantity) : base(id)
    {
        ComponentId = Check.NotDefaultOrNull<Guid>(componentId, nameof(componentId));
        ComponentCode = Check.NotNullOrWhiteSpace(componentCode, nameof(componentCode), SalesConsts.MaxCodeLength);
        ComponentName = Check.NotNullOrWhiteSpace(componentName, nameof(componentName), SalesConsts.MaxNameLength);
        Unit = Check.NotNullOrWhiteSpace(unit, nameof(unit), SalesConsts.MaxUnitLength);
        QuantityPerProduct = NormalizeQuantity(quantityPerProduct);
        TotalRequiredQuantity = NormalizeQuantity(totalRequiredQuantity);
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
}
