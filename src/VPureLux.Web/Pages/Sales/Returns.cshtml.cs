using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.ConfirmReturnedGoods)]
public class ReturnsModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly ISalesPostConfirmationAppService _postConfirmation;

    public ReturnsModel(ISalesPostConfirmationAppService postConfirmation) =>
        _postConfirmation = postConfirmation;

    public void OnGet() { }

    public async Task<JsonResult> OnGetListAsync(GetSalesReturnTasksInput input)
    {
        var result = await _postConfirmation.GetReturnTasksAsync(input);
        return new JsonResult(new PagedResultDto<ReturnTaskRow>(result.TotalCount, result.Items.Select(x => new ReturnTaskRow(
            x.TaskType,
            x.OperationId,
            x.RevisionLineId,
            x.SalesOrderId,
            x.OrderNo,
            $"{x.CustomerCode} - {x.CustomerName}".Trim(' ', '-'),
            x.ItemName,
            FormatQuantity(x.Quantity),
            x.Reason,
            x.IsException,
            x.CreatedAt.ToString("dd/MM/yyyy HH:mm", Vi))).ToList()));
    }

    private static string FormatQuantity(decimal value) =>
        value == decimal.Truncate(value) ? value.ToString("0", Vi) : value.ToString("0.####", Vi);

    public sealed record ReturnTaskRow(
        SalesReturnTaskType TaskType,
        System.Guid OperationId,
        System.Guid? RevisionLineId,
        System.Guid SalesOrderId,
        string OrderNo,
        string Customer,
        string ItemName,
        string Quantity,
        string Reason,
        bool IsException,
        string CreatedAt);
}
