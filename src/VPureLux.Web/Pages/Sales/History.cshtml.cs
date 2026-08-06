using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class HistoryModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    private readonly ISalesOrderAppService _service;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    public bool CanViewProfit { get; private set; }

    public HistoryModel(
        ISalesOrderAppService service,
        IAuthorizationService authorizationService,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _service = service;
        _authorizationService = authorizationService;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        CanViewProfit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.ViewProfit)).Succeeded;
    }

    public async Task<JsonResult> OnGetListAsync(GetSalesOrderListInput input)
    {
        var result = await _service.GetListAsync(new GetSalesOrderListInput
        {
            CustomerId = input.CustomerId,
            PaymentStatus = input.PaymentStatus,
            Status = SalesOrderStatus.Confirmed,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        });

        return new JsonResult(new PagedResultDto<SalesHistoryRow>(
            result.TotalCount,
            result.Items.Select(ToRow).ToList()));
    }

    private SalesHistoryRow ToRow(SalesOrderDto order) => new(
        order.Id,
        order.OrderNo,
        order.OrderDate.ToString("dd/MM/yyyy", Vi),
        string.IsNullOrEmpty(order.CustomerNameSnapshot)
            ? _localizer["Sales:CustomerContextUnavailable"].Value
            : $"{order.CustomerCodeSnapshot} - {order.CustomerNameSnapshot}",
        FormatMoney(order.TotalRevenueAmount),
        FormatOptionalMoney(order.TotalProfitAmount));

    private static string FormatMoney(decimal value)
    {
        var amount = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        return amount.ToString("#,0", Vi) + " ₫";
    }

    private static string FormatOptionalMoney(decimal? value) => value.HasValue ? FormatMoney(value.Value) : "—";

    public sealed record SalesHistoryRow(
        Guid Id,
        string OrderNo,
        string OrderDate,
        string CustomerName,
        string TotalRevenueAmount,
        string TotalProfitAmount);
}
