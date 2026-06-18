using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Sales;

public class CreateSalesOrderLineDto
{
    public Guid ProductId { get; set; }
    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? ActualSellingPrice { get; set; }
    [StringLength(SalesConsts.MaxOverrideReasonLength)]
    public string? OverrideReason { get; set; }
}

public class CreateSalesOrderDto
{
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime? OrderDate { get; set; }
    [Required, MinLength(1)]
    public List<CreateSalesOrderLineDto> Lines { get; set; } = new();
}

public class UpdateSalesOrderLineDto
{
    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ParseLimitsInInvariantCulture = true)]
    public decimal Quantity { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal ActualSellingPrice { get; set; }
    [StringLength(SalesConsts.MaxOverrideReasonLength)]
    public string? OverrideReason { get; set; }
}

public class ConfirmSalesOrderDto
{
    [Required, StringLength(SalesConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class GetSalesOrderListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public SalesOrderStatus? Status { get; set; }
}
