using System;

namespace VPureLux.Sales;

public class SalesReturnTaskFilter
{
    public string? SearchText { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}

public class SalesReturnTaskReadItem
{
    public SalesReturnTaskType TaskType { get; set; }
    public Guid OperationId { get; set; }
    public Guid? RevisionLineId { get; set; }
    public Guid SalesOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsException { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SalesRefundTaskFilter
{
    public string? SearchText { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}

public class SalesRefundTaskReadItem
{
    public SalesRefundTaskType TaskType { get; set; }
    public Guid OperationId { get; set; }
    public Guid SalesOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal RefundDue { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
