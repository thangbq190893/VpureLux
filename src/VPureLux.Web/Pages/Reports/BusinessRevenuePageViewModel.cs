namespace VPureLux.Web.Pages.Reports;

public class BusinessRevenuePageViewModel
{
    public string Mode { get; set; } = "consolidated";
    public string TitleKey { get; set; } = "Reports:BusinessRevenue";
    public string FromDate { get; set; } = string.Empty;
    public string ToDate { get; set; } = string.Empty;
}
