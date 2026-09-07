using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Reports;

public enum BusinessRevenueSource : byte { Sales = 1, Service = 2 }

public class GetBusinessRevenueListInput : PagedAndSortedResultRequestDto
{
    [StringLength(128)] public string? SearchText { get; set; }
    public BusinessRevenueSource? Source { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class BusinessRevenueRowDto
{
    public BusinessRevenueSource Source { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid? CustomerAssetId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal? MaterialCost { get; set; }
    public bool? LaborCostKnown { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? TotalKnownCost { get; set; }
    public bool? CostIncomplete { get; set; }
    public decimal? Profit { get; set; }
    public decimal GrossPosted { get; set; }
    public decimal GrossRefunded { get; set; }
    public decimal NetPaid { get; set; }
    public decimal Receivable { get; set; }
    public decimal CustomerCredit { get; set; }
    public decimal RefundDue { get; set; }
    public bool HasInconsistentLedger { get; set; }
}

public class BusinessRevenueSummaryDto
{
    public long DocumentCount { get; set; }
    public decimal SalesRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal TotalRevenue => SalesRevenue + ServiceRevenue;
    public decimal? KnownMaterialCost { get; set; }
    public decimal? KnownLaborCost { get; set; }
    public decimal? TotalKnownCost { get; set; }
    public long? CostIncompleteCount { get; set; }
    public decimal? Profit { get; set; }
}

public interface IBusinessRevenueAppService : IApplicationService
{
    Task<PagedResultDto<BusinessRevenueRowDto>> GetServiceListAsync(GetBusinessRevenueListInput input);
    Task<BusinessRevenueSummaryDto> GetServiceSummaryAsync(GetBusinessRevenueListInput input);
    Task<PagedResultDto<BusinessRevenueRowDto>> GetConsolidatedListAsync(GetBusinessRevenueListInput input);
    Task<BusinessRevenueSummaryDto> GetConsolidatedSummaryAsync(GetBusinessRevenueListInput input);
}

// Infrastructure follows the existing Sales report read-contract convention; never exposed as an HTTP service.
public interface IBusinessRevenueReadRepository
{
    Task<long> GetCountAsync(GetBusinessRevenueListInput input, DateTime toExclusive, CancellationToken cancellationToken = default);
    Task<List<BusinessRevenueRowDto>> GetListAsync(GetBusinessRevenueListInput input, DateTime toExclusive, CancellationToken cancellationToken = default);
    Task<BusinessRevenueSummaryDto> GetSummaryAsync(GetBusinessRevenueListInput input, DateTime toExclusive, CancellationToken cancellationToken = default);
}
