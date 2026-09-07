using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Service;

public interface IServicePaymentAppService : IApplicationService
{
    Task<ServiceMoneySummaryDto> GetSummaryAsync(Guid id);
    Task<ServiceCustomerMoneySummaryDto> GetCustomerSummaryAsync(Guid customerId);
    Task<PagedResultDto<ServicePaymentDto>> GetListAsync(ServiceMoneyHistoryInput input);
    Task<PagedResultDto<ServiceRefundDto>> GetRefundListAsync(ServiceMoneyHistoryInput input);
    Task<ServicePaymentDto> AddPaymentAsync(Guid id, AddServicePaymentDto input);
    Task<ServicePaymentDto> VoidPaymentAsync(Guid id, VoidServicePaymentDto input);
    Task<ServiceRefundDto> RefundAsync(Guid id, AddServiceRefundDto input);
}

public class ServiceMoneyHistoryInput : PagedAndSortedResultRequestDto
{
    public Guid ServiceOrderId { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)] public string? SearchText { get; set; }
    [EnumDataType(typeof(ServicePaymentStatus))] public ServicePaymentStatus? Status { get; set; }
}

public class AddServicePaymentDto
{
    [Required, Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? Amount { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    [EnumDataType(typeof(SalesPaymentMethod))] public SalesPaymentMethod PaymentMethod { get; set; }
    [Required, StringLength(ServiceConsts.MaxIdempotencyKeyLength)] public string IdempotencyKey { get; set; } = string.Empty;
    [StringLength(ServiceConsts.MaxReferenceNoLength)] public string? ReferenceNo { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
}

public class AddServiceRefundDto
{
    [Required, Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? Amount { get; set; }
    public DateTimeOffset RefundDate { get; set; }
    [EnumDataType(typeof(SalesPaymentMethod))] public SalesPaymentMethod Method { get; set; }
    [Required, StringLength(ServiceConsts.MaxIdempotencyKeyLength)] public string IdempotencyKey { get; set; } = string.Empty;
    [StringLength(ServiceConsts.MaxReferenceNoLength)] public string? ReferenceNo { get; set; }
    [Required, StringLength(ServiceConsts.MaxNoteLength)] public string Reason { get; set; } = string.Empty;
}

public class VoidServicePaymentDto
{
    public Guid PaymentId { get; set; }
    [Required, StringLength(ServiceConsts.MaxIdempotencyKeyLength)] public string IdempotencyKey { get; set; } = string.Empty;
    [Required, StringLength(ServiceConsts.MaxNoteLength)] public string Reason { get; set; } = string.Empty;
    [Required, StringLength(40)] public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class ServicePaymentDto : EntityDto<Guid>
{
    public Guid ServiceOrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public SalesPaymentMethod PaymentMethod { get; set; }
    public ServicePaymentStatus Status { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime? RecordedAt { get; set; }
    public Guid? RecordedBy { get; set; }
    public DateTime? VoidedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public string? VoidReason { get; set; }
    public bool IsLegacy { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class ServiceRefundDto : EntityDto<Guid>
{
    public Guid ServiceOrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime RefundDate { get; set; }
    public SalesPaymentMethod Method { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public Guid RecordedBy { get; set; }
}

public class ServiceMoneySummaryDto
{
    public Guid ServiceOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public decimal PlannedTotal { get; set; }
    public decimal? ActualTotal { get; set; }
    public decimal Revenue { get; set; }
    public decimal GrossPosted { get; set; }
    public decimal GrossRefunded { get; set; }
    public decimal NetPaid { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal PlannedRemaining { get; set; }
    public decimal AdvanceExcess { get; set; }
    public decimal Receivable { get; set; }
    public decimal CustomerCredit { get; set; }
    public decimal RefundDue { get; set; }
    public bool HasInconsistentLedger { get; set; }
}

public class ServiceCustomerMoneySummaryDto
{
    public decimal Receivable { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal CustomerCredit { get; set; }
    public decimal RefundDue { get; set; }
    public long InconsistentOrderCount { get; set; }
}
