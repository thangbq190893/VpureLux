using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Service;

public class ServiceOrderLineInput
{
    public ServiceOrderLineType LineType { get; set; }
    public Guid CatalogItemId { get; set; }
    public Guid? CustomerAssetComponentId { get; set; }
    [Range(1, 100000)]
    public int Quantity { get; set; } = 1;
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class CreateServiceOrderDto
{
    public Guid CustomerAssetId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? TechnicianUserId { get; set; }
    [StringLength(ServiceConsts.MaxAddressLength)]
    public string? ServiceAddress { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)]
    public string? Note { get; set; }
    [Required, MinLength(1)]
    public List<ServiceOrderLineInput> Lines { get; set; } = [];
}

public class UpdateServiceOrderDto
{
    public DateTime? ScheduledAt { get; set; }
    public Guid? TechnicianUserId { get; set; }
    [StringLength(ServiceConsts.MaxAddressLength)]
    public string? ServiceAddress { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)]
    public string? Note { get; set; }
    [Required, MinLength(1)]
    public List<ServiceOrderLineInput> Lines { get; set; } = [];
}

public class CompleteServiceOrderLineDto
{
    public Guid LineId { get; set; }
    [Range(0, 100000)]
    public int ActualQuantity { get; set; }
}

public class CompleteServiceOrderDto
{
    public DateTime? CompletedAt { get; set; }
    [Required, StringLength(ServiceConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
    [Required, MinLength(1)]
    public List<CompleteServiceOrderLineDto> Lines { get; set; } = [];
}

public class CreateServicePaymentDto
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public VPureLux.Sales.SalesPaymentMethod PaymentMethod { get; set; }
    [StringLength(ServiceConsts.MaxReferenceNoLength)]
    public string? ReferenceNo { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)]
    public string? Note { get; set; }
    [Required, StringLength(ServiceConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class GetServiceOrderListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CustomerAssetId { get; set; }
    public ServiceOrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ServiceLookupInput
{
    public Guid? CustomerId { get; set; }
    public string? SearchText { get; set; }
    [Range(1, 100)]
    public int MaxResultCount { get; set; } = 30;
}

public class GetServiceWorkListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public ServiceWorkStatus? Status { get; set; }
}

public class CreateUpdateServiceWorkDto
{
    [Required, StringLength(ServiceConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;
    [Required, StringLength(ServiceConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal DefaultPrice { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)]
    public string? Note { get; set; }
}
