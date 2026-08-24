using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Reports;

public enum BusinessRevenueSource : byte
{
    Sales = 1,
    Service = 2
}

public class GetBusinessRevenueListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
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
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get; set; }
}

public class BusinessRevenueSummaryDto
{
    public decimal SalesRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal TotalRevenue => SalesRevenue + ServiceRevenue;
    public decimal TotalCost { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public long DocumentCount { get; set; }
}

public interface IBusinessRevenueAppService : IApplicationService
{
    Task<PagedResultDto<BusinessRevenueRowDto>> GetServiceListAsync(GetBusinessRevenueListInput input);
    Task<BusinessRevenueSummaryDto> GetServiceSummaryAsync(GetBusinessRevenueListInput input);
    Task<PagedResultDto<BusinessRevenueRowDto>> GetConsolidatedListAsync(GetBusinessRevenueListInput input);
    Task<BusinessRevenueSummaryDto> GetConsolidatedSummaryAsync(GetBusinessRevenueListInput input);
}

public interface IBusinessRevenueReadRepository
{
    Task<long> GetCountAsync(GetBusinessRevenueListInput input, DateTime? toDateExclusive, CancellationToken cancellationToken = default);
    Task<List<BusinessRevenueRowDto>> GetListAsync(GetBusinessRevenueListInput input, DateTime? toDateExclusive, CancellationToken cancellationToken = default);
    Task<BusinessRevenueSummaryDto> GetSummaryAsync(GetBusinessRevenueListInput input, DateTime? toDateExclusive, CancellationToken cancellationToken = default);
}
