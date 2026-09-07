using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Sales;

public class ReasonDto
{
    [Required, StringLength(SalesConsts.MaxReasonLength)]
    public string Reason { get; set; } = string.Empty;
}

public class OpenSalesOrderRevisionDto : ReasonDto { }

public class UpdateSalesOrderRevisionDto
{
    public Guid CustomerId { get; set; }
    [Required]
    public List<UpdateSalesOrderRevisionLineDto> Lines { get; set; } = new();
}

public class UpdateSalesOrderRevisionLineDto
{
    public Guid? RevisionLineId { get; set; }
    public bool IsRemoved { get; set; }
    public Guid ProductId { get; set; }
    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }
    public decimal ActualSellingPrice { get; set; }
    [StringLength(SalesConsts.MaxOverrideReasonLength)]
    public string? OverrideReason { get; set; }
}

public class ConfirmRevisionReturnedGoodsDto : ReasonDto
{
    [Required]
    public List<Guid> RevisionLineIds { get; set; } = new();
}

public class ApplySalesOrderRevisionDto
{
    [Required, StringLength(SalesConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class SubmitSalesOrderRevisionDto : UpdateSalesOrderRevisionDto
{
    [Required, StringLength(SalesConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CancelConfirmedSalesOrderDto : ReasonDto
{
    [Required, StringLength(SalesConsts.MaxReasonLength)]
    public string ReasonGroup { get; set; } = string.Empty;
}

public class ConfirmCancellationReturnedGoodsDto : ReasonDto
{
    public bool IsEligibleForRestock { get; set; }
    [Required, StringLength(SalesConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class RecordSalesOrderRefundDto : ReasonDto
{
    public decimal Amount { get; set; }
    public DateTime RefundedAt { get; set; }
    public SalesPaymentMethod PaymentMethod { get; set; }
    [StringLength(SalesConsts.MaxPaymentReferenceNoLength)]
    public string? ReferenceNo { get; set; }
    [Required, StringLength(SalesConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class SalesOrderRevisionDto : EntityDto<Guid>
{
    public Guid SalesOrderId { get; set; }
    public int RevisionNo { get; set; }
    public SalesOrderRevisionStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal BeforeTotal { get; set; }
    public decimal? AppliedTotal { get; set; }
    public decimal RefundDue { get; set; }
    public DateTime? AppliedAt { get; set; }
    public List<SalesOrderRevisionLineDto> Lines { get; set; } = new();
}

public class SalesOrderRevisionLineDto : EntityDto<Guid>
{
    public Guid? SourceSalesOrderLineId { get; set; }
    public Guid? EffectiveSalesOrderLineId { get; set; }
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ActualSellingPrice { get; set; }
    public bool IsRemoved { get; set; }
    public bool RequiresReturnConfirmation { get; set; }
    public DateTime? ReturnConfirmedAt { get; set; }
    public Guid? IssueInventoryTransactionId { get; set; }
    public Guid? ReversalInventoryTransactionId { get; set; }
    public decimal AppliedCostAmount { get; set; }
}

public class SalesOrderCancellationDto : EntityDto<Guid>
{
    public Guid SalesOrderId { get; set; }
    public string ReasonGroup { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveAt { get; set; }
    public SalesOrderCancellationStockStatus StockStatus { get; set; }
    public SalesOrderCancellationPaymentStatus PaymentStatus { get; set; }
    public decimal RefundDue { get; set; }
    public decimal RefundedAmount { get; set; }
    public Guid? StockReversalTransactionId { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class SalesOrderRefundDto : EntityDto<Guid>
{
    public Guid SalesOrderId { get; set; }
    public Guid? SalesOrderRevisionId { get; set; }
    public Guid? SalesOrderCancellationId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RefundedAt { get; set; }
    public SalesPaymentMethod PaymentMethod { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class SalesPostConfirmationStateDto
{
    public Guid SalesOrderId { get; set; }
    public SalesOrderStatus Status { get; set; }
    public bool HasInstalledMachine { get; set; }
    public Guid? ActiveRevisionId { get; set; }
    public Guid? CancellationId { get; set; }
    public bool CanAdjust { get; set; }
    public bool CanCancel { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal NetPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal RefundDue { get; set; }
    public List<SalesPostConfirmationLineSummaryDto> Lines { get; set; } = new();
}

public class SalesPostConfirmationLineSummaryDto
{
    public int LineNo { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class SalesRevisionPreviewDto
{
    public Guid RevisionId { get; set; }
    public decimal BeforeTotal { get; set; }
    public decimal NewTotal { get; set; }
    public decimal NetPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal RefundDue { get; set; }
    public int PendingReturnCount { get; set; }
    public List<SalesRevisionImpactDto> Impacts { get; set; } = new();
}

public class SalesRevisionImpactDto
{
    public SalesRevisionImpactType ImpactType { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class GetSalesReturnTasksInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
}

public class SalesReturnTaskDto
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

public class GetSalesRefundTasksInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
}

public class SalesRefundTaskDto
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
