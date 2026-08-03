using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace VPureLux.Reports;

public class SalesReportsAppService : ApplicationService, ISalesReportsAppService
{
    private readonly ISalesReportReadRepository _reports;

    public SalesReportsAppService(ISalesReportReadRepository reports)
    {
        _reports = reports;
    }

    [Authorize(VPureLuxPermissions.Reports.Sales.View)]
    public async Task<SalesRevenueReportDto> GetSalesRevenueAsync(SalesRevenueReportInput input)
    {
        var toDateExclusive = Normalize(input);
        return await _reports.GetSalesRevenueAsync(input, toDateExclusive);
    }

    [Authorize(VPureLuxPermissions.Reports.Profit.View)]
    public async Task<SalesProfitReportDto> GetSalesProfitAsync(SalesProfitReportInput input)
    {
        var toDateExclusive = Normalize(input);
        return await _reports.GetSalesProfitAsync(input, toDateExclusive);
    }

    private DateTime Normalize(SalesRevenueReportInput input)
    {
        NormalizeDates(input.FromDate, input.ToDate, out var fromDate, out var toDate, out var toDateExclusive);
        input.FromDate = fromDate;
        input.ToDate = toDate;
        input.GroupBy = NormalizeGroupBy(input.GroupBy);
        input.MaxDetailRows = NormalizeMaxDetailRows(input.MaxDetailRows);
        return toDateExclusive;
    }

    private DateTime Normalize(SalesProfitReportInput input)
    {
        NormalizeDates(input.FromDate, input.ToDate, out var fromDate, out var toDate, out var toDateExclusive);
        input.FromDate = fromDate;
        input.ToDate = toDate;
        input.GroupBy = NormalizeGroupBy(input.GroupBy);
        input.MaxDetailRows = NormalizeMaxDetailRows(input.MaxDetailRows);
        return toDateExclusive;
    }

    private void NormalizeDates(DateTime? from, DateTime? to, out DateTime fromDate, out DateTime toDate, out DateTime toDateExclusive)
    {
        if (from.HasValue || to.HasValue)
        {
            fromDate = (from ?? to)!.Value.Date;
            toDate = (to ?? from)!.Value.Date;
        }
        else
        {
            var now = Clock.Now.Date;
            fromDate = new DateTime(now.Year, now.Month, 1);
            toDate = fromDate.AddMonths(1).AddDays(-1);
        }

        if (fromDate > toDate)
        {
            throw new UserFriendlyException(L["Reports:DateRangeInvalid"]);
        }

        toDateExclusive = toDate.AddDays(1);
    }

    private static ReportPeriodGroup NormalizeGroupBy(ReportPeriodGroup value) =>
        Enum.IsDefined(value) ? value : ReportPeriodGroup.Day;

    private static int NormalizeMaxDetailRows(int value) =>
        value <= 0 ? 500 : Math.Min(value, 5000);
}
