using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Reports;

[Authorize(VPureLuxPermissions.Reports.Sales.View)]
public class SalesRevenueModel : VPureLuxPageModel
{
    private readonly ISalesReportsAppService _reports;
    private readonly IProductAppService _products;
    private readonly ICustomerAppService _customers;
    private readonly IWarehouseAppService _warehouses;

    [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public ReportPeriodGroup GroupBy { get; set; } = ReportPeriodGroup.Day;
    [BindProperty(SupportsGet = true)] public Guid? ProductId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? CustomerId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? WarehouseId { get; set; }
    [BindProperty(SupportsGet = true)] public SalesOrderReceivableStatus? PaymentStatus { get; set; }

    public SalesRevenueReportDto Report { get; private set; } = new();
    public List<SelectListItem> ProductOptions { get; private set; } = new();
    public List<SelectListItem> CustomerOptions { get; private set; } = new();
    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public IReadOnlyList<ReportPeriodGroup> GroupByOptions { get; } = Enum.GetValues<ReportPeriodGroup>()
        .Where(x => x != 0)
        .OrderBy(x => (byte)x)
        .ToList();
    public IReadOnlyList<SalesOrderReceivableStatus> PaymentStatusOptions { get; } =
    [
        SalesOrderReceivableStatus.Unpaid,
        SalesOrderReceivableStatus.PartiallyPaid,
        SalesOrderReceivableStatus.Paid,
        SalesOrderReceivableStatus.Overpaid
    ];

    public bool HasData =>
        Report.Summary.ConfirmedOrderCount > 0 ||
        Report.ByPeriod.Count > 0 ||
        Report.ByProduct.Count > 0 ||
        Report.ByCustomer.Count > 0 ||
        Report.Orders.Count > 0;

    public SalesRevenueModel(
        ISalesReportsAppService reports,
        IProductAppService products,
        ICustomerAppService customers,
        IWarehouseAppService warehouses)
    {
        _reports = reports;
        _products = products;
        _customers = customers;
        _warehouses = warehouses;
    }

    public async Task OnGetAsync()
    {
        NormalizeDefaults();
        await LoadFilterOptionsAsync();

        if (FromDate > ToDate)
        {
            ModelState.AddModelError(string.Empty, L["Reports:DateRangeInvalid"]);
            return;
        }

        try
        {
            Report = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
            {
                FromDate = FromDate,
                ToDate = ToDate,
                GroupBy = GroupBy,
                ProductId = ProductId,
                CustomerId = CustomerId,
                WarehouseId = WarehouseId,
                PaymentStatus = PaymentStatus,
                MaxDetailRows = 500
            });
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, L["Reports:LoadFailed"]);
            Report = new SalesRevenueReportDto();
        }
    }

    private void NormalizeDefaults()
    {
        if (!Enum.IsDefined(GroupBy) || GroupBy == 0)
        {
            GroupBy = ReportPeriodGroup.Day;
        }

        if (FromDate.HasValue || ToDate.HasValue)
        {
            FromDate = (FromDate ?? ToDate)!.Value.Date;
            ToDate = (ToDate ?? FromDate)!.Value.Date;
            return;
        }

        var today = Clock.Now.Date;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = FromDate.Value.AddMonths(1).AddDays(-1);
    }

    private async Task LoadFilterOptionsAsync()
    {
        ProductOptions = (await _products.GetListAsync(new GetProductListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();

        CustomerOptions = (await _customers.GetListAsync(new GetCustomerListInput
            {
                Status = CustomerStatus.Active,
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();

        WarehouseOptions = (await _warehouses.GetListAsync(new GetInventoryListInput
            {
                Status = InventoryEntityStatus.Active,
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }
}
