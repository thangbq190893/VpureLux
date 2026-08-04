using System;
using System.Globalization;
using System.Linq;
using System.Text;
using VPureLux.Reports;
using VPureLux.Sales;

namespace VPureLux.Web.Pages.Reports;

public static class ReportCsv
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

    public static byte[] BuildSalesRevenue(SalesRevenueReportDto report, Func<SalesOrderReceivableStatus, string> paymentStatusLabel)
    {
        var csv = new StringBuilder();
        AppendSection(csv, "Tổng quan");
        AppendRow(csv, "Tổng doanh số", "Số đơn đã xác nhận", "Số lượng sản phẩm bán", "Giá trị đơn trung bình", "Đã thanh toán", "Còn nợ", "Chưa thanh toán", "Thanh toán một phần", "Đã thanh toán", "Trả dư");
        AppendRow(csv,
            Number(report.Summary.TotalRevenue),
            report.Summary.ConfirmedOrderCount.ToString(Invariant),
            Number(report.Summary.TotalQuantity),
            Number(report.Summary.AverageOrderValue),
            Number(report.Summary.PaidAmount),
            Number(report.Summary.RemainingAmount),
            report.Summary.UnpaidOrderCount.ToString(Invariant),
            report.Summary.PartiallyPaidOrderCount.ToString(Invariant),
            report.Summary.PaidOrderCount.ToString(Invariant),
            report.Summary.OverpaidOrderCount.ToString(Invariant));

        AppendSection(csv, "Doanh số theo thời gian");
        AppendRow(csv, "Thời gian", "Số đơn", "Số lượng", "Doanh số", "Đã thanh toán", "Còn nợ");
        foreach (var row in report.ByPeriod)
        {
            AppendRow(csv, Label(row.PeriodLabel, row.PeriodKey), row.OrderCount.ToString(Invariant),
                Number(row.Quantity), Number(row.Revenue), Number(row.PaidAmount), Number(row.RemainingAmount));
        }

        AppendSection(csv, "Top sản phẩm theo doanh số");
        AppendRow(csv, "Mã sản phẩm", "Tên sản phẩm", "Số lượng bán", "Số đơn", "Doanh số", "Tỷ trọng");
        foreach (var row in report.ByProduct)
        {
            AppendRow(csv, row.ProductCode, row.ProductName, Number(row.Quantity), row.OrderCount.ToString(Invariant),
                Number(row.Revenue), Number(row.RevenueSharePercent));
        }

        AppendSection(csv, "Doanh số theo khách hàng");
        AppendRow(csv, "Mã khách hàng", "Khách hàng", "Số đơn", "Doanh số", "Đã thanh toán", "Còn nợ");
        foreach (var row in report.ByCustomer)
        {
            AppendRow(csv, row.CustomerCode, row.CustomerName, row.OrderCount.ToString(Invariant),
                Number(row.Revenue), Number(row.PaidAmount), Number(row.RemainingAmount));
        }

        AppendSection(csv, "Danh sách đơn hàng");
        AppendRow(csv, "Mã đơn", "Ngày xác nhận", "Khách hàng", "Kho", "Tổng đơn", "Đã thanh toán", "Còn nợ", "Trạng thái thanh toán");
        foreach (var row in report.Orders)
        {
            AppendRow(csv, row.OrderNo, Date(row.ConfirmationTime), row.CustomerName, row.WarehouseName,
                Number(row.TotalAmount), Number(row.PaidAmount), Number(row.RemainingAmount), paymentStatusLabel(row.PaymentStatus));
        }

        return WithPreamble(csv);
    }

    public static byte[] BuildSalesProfit(SalesProfitReportDto report)
    {
        var csv = new StringBuilder();
        AppendSection(csv, "Tổng quan");
        AppendRow(csv, "Doanh số", "Giá vốn", "Lợi nhuận", "Tỷ suất LN", "Số đơn đã xác nhận", "Dòng chưa có giá vốn");
        AppendRow(csv, Number(report.Summary.Revenue), Number(report.Summary.CostAmount),
            Number(report.Summary.ProfitAmount), Number(report.Summary.ProfitMarginPercent),
            report.Summary.ConfirmedOrderCount.ToString(Invariant), report.Summary.MissingCostLineCount.ToString(Invariant));

        AppendSection(csv, "Lợi nhuận theo thời gian");
        AppendRow(csv, "Thời gian", "Số đơn", "Doanh số", "Giá vốn", "Lợi nhuận", "Tỷ suất LN");
        foreach (var row in report.ByPeriod)
        {
            AppendRow(csv, Label(row.PeriodLabel, row.PeriodKey), row.OrderCount.ToString(Invariant),
                Number(row.Revenue), Number(row.CostAmount), Number(row.ProfitAmount), Number(row.ProfitMarginPercent));
        }

        AppendSection(csv, "Lợi nhuận theo sản phẩm");
        AppendRow(csv, "Mã sản phẩm", "Tên sản phẩm", "Số lượng bán", "Doanh số", "Giá vốn", "Lợi nhuận", "Tỷ suất LN");
        foreach (var row in report.ByProduct)
        {
            AppendRow(csv, row.ProductCode, row.ProductName, Number(row.Quantity), Number(row.Revenue),
                Number(row.CostAmount), Number(row.ProfitAmount), Number(row.ProfitMarginPercent));
        }

        AppendSection(csv, "Lợi nhuận theo khách hàng");
        AppendRow(csv, "Mã khách hàng", "Khách hàng", "Số đơn", "Doanh số", "Giá vốn", "Lợi nhuận", "Tỷ suất LN", "Còn nợ");
        foreach (var row in report.ByCustomer)
        {
            AppendRow(csv, row.CustomerCode, row.CustomerName, row.OrderCount.ToString(Invariant),
                Number(row.Revenue), Number(row.CostAmount), Number(row.ProfitAmount),
                Number(row.ProfitMarginPercent), Number(row.RemainingAmount));
        }

        AppendSection(csv, "Chi tiết dòng bán hàng");
        AppendRow(csv, "Mã đơn", "Ngày xác nhận", "Khách hàng", "Mã sản phẩm", "Sản phẩm", "Số lượng", "Đơn giá", "Doanh số", "Giá vốn", "Lợi nhuận", "Tỷ suất LN", "Ghi chú");
        foreach (var row in report.Lines)
        {
            AppendRow(csv, row.OrderNo, Date(row.ConfirmationTime), row.CustomerName, row.ProductCode, row.ProductName,
                Number(row.Quantity), Number(row.UnitPrice), Number(row.Revenue),
                row.MissingCost ? "Chưa có giá vốn" : Number(row.CostAmount),
                row.MissingCost ? string.Empty : Number(row.ProfitAmount),
                row.MissingCost ? string.Empty : Number(row.ProfitMarginPercent),
                row.MissingCost ? "Thiếu giá vốn" : string.Empty);
        }

        return WithPreamble(csv);
    }

    private static void AppendSection(StringBuilder csv, string title)
    {
        if (csv.Length > 0)
        {
            csv.AppendLine();
        }
        AppendRow(csv, title);
    }

    private static void AppendRow(StringBuilder csv, params string[] columns)
    {
        csv.AppendLine(string.Join(",", columns.Select(Escape)));
    }

    private static string Escape(string value)
    {
        value ??= string.Empty;
        return value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    private static string Number(decimal value) => value.ToString("0.####", Invariant);
    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", Invariant);
    private static string Label(string label, string key) => string.IsNullOrWhiteSpace(label) ? key : label;

    private static byte[] WithPreamble(StringBuilder csv)
    {
        var preamble = Utf8Bom.GetPreamble();
        var content = Utf8Bom.GetBytes(csv.ToString());
        return [.. preamble, .. content];
    }
}
