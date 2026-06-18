using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPureLux.Inventory;

public class CreateWarehouseDto
{
    [Required, StringLength(InventoryConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;
    [Required, StringLength(InventoryConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    [StringLength(InventoryConsts.MaxAddressLength)]
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateWarehouseDto
{
    [Required, StringLength(InventoryConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    [StringLength(InventoryConsts.MaxAddressLength)]
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

public class ReceiptLineInput
{
    public Guid StockItemId { get; set; }
    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }
    [Required, StringLength(InventoryConsts.MaxLotNoLength)]
    public string LotNo { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal UnitCost { get; set; }
}

public class IssueLineInput
{
    public Guid StockItemId { get; set; }
    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }
}

public abstract class PostInventoryTransactionInput
{
    public Guid WarehouseId { get; set; }
    [Required, StringLength(InventoryConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
    [StringLength(InventoryConsts.MaxReferenceTypeLength)]
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? BomVersionId { get; set; }
}

public class PostReceiptDto : PostInventoryTransactionInput
{
    [Required, MinLength(1)]
    public List<ReceiptLineInput> Lines { get; set; } = new();
}

public class PostIssueDto : PostInventoryTransactionInput
{
    public InventoryTransactionType Type { get; set; } = InventoryTransactionType.SalesIssue;
    [Required, MinLength(1)]
    public List<IssueLineInput> Lines { get; set; } = new();
}

public class PostAdjustmentDto : PostInventoryTransactionInput
{
    public InventoryTransactionType Type { get; set; }
    [Required, StringLength(InventoryConsts.MaxReasonLength)]
    public string Reason { get; set; } = string.Empty;
    public List<ReceiptLineInput> IncreaseLines { get; set; } = new();
    public List<IssueLineInput> DecreaseLines { get; set; } = new();
}
