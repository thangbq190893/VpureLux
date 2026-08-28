using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.ManageRefunds)]
public class RefundsModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly ISalesPostConfirmationAppService _postConfirmation;

    public RefundsModel(ISalesPostConfirmationAppService postConfirmation) =>
        _postConfirmation = postConfirmation;

    public void OnGet() { }

    public async Task<JsonResult> OnGetListAsync(GetSalesRefundTasksInput input)
    {
        var result = await _postConfirmation.GetRefundTasksAsync(input);
        return new JsonResult(new PagedResultDto<RefundTaskRow>(result.TotalCount, result.Items.Select(x => new RefundTaskRow(
            x.TaskType,
            x.OperationId,
            x.SalesOrderId,
            x.OrderNo,
            $"{x.CustomerCode} - {x.CustomerName}".Trim(' ', '-'),
            x.RemainingAmount,
            FormatMoney(x.RemainingAmount),
            x.CreatedAt.ToString("dd/MM/yyyy HH:mm", Vi))).ToList()));
    }

    private static string FormatMoney(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero).ToString("#,0", Vi) + " VND";

    public sealed record RefundTaskRow(
        SalesRefundTaskType TaskType,
        System.Guid OperationId,
        System.Guid SalesOrderId,
        string OrderNo,
        string Customer,
        decimal RemainingAmountValue,
        string RemainingAmount,
        string CreatedAt);
}
