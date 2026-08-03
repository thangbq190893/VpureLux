using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using VPureLux.Sales;
using Volo.Abp.Application.Services;

namespace VPureLux.Reports;

public enum ReportPeriodGroup : byte
{
    Day = 1,
    Week = 2,
    Month = 3,
    Quarter = 4,
    Year = 5
}

public class SalesRevenueReportInput
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public ReportPeriodGroup GroupBy { get; set; } = ReportPeriodGroup.Day;
    public Guid? ProductId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? WarehouseId { get; set; }
    public SalesOrderReceivableStatus? PaymentStatus { get; set; }

    [Range(1, 5000)]
    public int MaxDetailRows { get; set; } = 500;
}

public class SalesProfitReportInput
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public ReportPeriodGroup GroupBy { get; set; } = ReportPeriodGroup.Day;
    public Guid? ProductId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? WarehouseId { get; set; }
    public bool LossOnly { get; set; }
    public bool MissingCostOnly { get; set; }

    [Range(1, 5000)]
    public int MaxDetailRows { get; set; } = 500;
}

public class SalesRevenueReportDto
{
    public SalesRevenueSummaryDto Summary { get; set; } = new();
    public List<SalesRevenuePeriodRowDto> ByPeriod { get; set; } = new();
    public List<SalesRevenueProductRowDto> ByProduct { get; set; } = new();
    public List<SalesRevenueCustomerRowDto> ByCustomer { get; set; } = new();
    public List<SalesRevenueOrderRowDto> Orders { get; set; } = new();
}

public class SalesRevenueSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public int ConfirmedOrderCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int UnpaidOrderCount { get; set; }
    public int PartiallyPaidOrderCount { get; set; }
    public int PaidOrderCount { get; set; }
    public int OverpaidOrderCount { get; set; }
}

public class SalesRevenuePeriodRowDto
{
    public string PeriodKey { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class SalesRevenueProductRowDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal RevenueSharePercent { get; set; }
}

public class SalesRevenueCustomerRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class SalesRevenueOrderRowDto
{
    public Guid SalesOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime ConfirmationTime { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public SalesOrderReceivableStatus PaymentStatus { get; set; }
}

public class SalesProfitReportDto
{
    public SalesProfitSummaryDto Summary { get; set; } = new();
    public List<SalesProfitPeriodRowDto> ByPeriod { get; set; } = new();
    public List<SalesProfitProductRowDto> ByProduct { get; set; } = new();
    public List<SalesProfitCustomerRowDto> ByCustomer { get; set; } = new();
    public List<SalesProfitLineRowDto> Lines { get; set; } = new();
}

public class SalesProfitSummaryDto
{
    public decimal Revenue { get; set; }
    public decimal CostAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    public int ConfirmedOrderCount { get; set; }
    public int LossOrderCount { get; set; }
    public int MissingCostLineCount { get; set; }
}

public class SalesProfitPeriodRowDto
{
    public string PeriodKey { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal CostAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    public int OrderCount { get; set; }
}

public class SalesProfitProductRowDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitMarginPercent { get; set; }
}

public class SalesProfitCustomerRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class SalesProfitLineRowDto
{
    public Guid SalesOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime ConfirmationTime { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Revenue { get; set; }
    public decimal CostAmount { get; set; }
    public decimal ProfitAmount { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    public bool MissingCost { get; set; }
}

public interface ISalesReportsAppService : IApplicationService
{
    Task<SalesRevenueReportDto> GetSalesRevenueAsync(SalesRevenueReportInput input);
    Task<SalesProfitReportDto> GetSalesProfitAsync(SalesProfitReportInput input);
}

public interface ISalesReportReadRepository
{
    Task<SalesRevenueReportDto> GetSalesRevenueAsync(
        SalesRevenueReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken = default);

    Task<SalesProfitReportDto> GetSalesProfitAsync(
        SalesProfitReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken = default);
}
