using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class IndexModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    private readonly ISalesOrderAppService _service;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;
    [BindProperty(SupportsGet = true)] public Guid? CustomerId { get; set; }
    [BindProperty(SupportsGet = true)] public SalesOrderStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public SalesOrderReceivableStatus? PaymentStatus { get; set; }
    public bool CanCreate { get; private set; }
    public bool CanViewHistory { get; private set; }
    public bool CanCancel { get; private set; }

    public IndexModel(
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
        await SetPermissionsAsync();
    }

    public async Task<JsonResult> OnGetListAsync(GetSalesOrderListInput input)
    {
        var canCancel = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Cancel)).Succeeded;
        var result = await _service.GetListAsync(new GetSalesOrderListInput
        {
            CustomerId = input.CustomerId,
            Status = input.Status,
            PaymentStatus = input.PaymentStatus,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        });

        return new JsonResult(new PagedResultDto<SalesOrderRow>(
            result.TotalCount,
            result.Items.Select(order => ToRow(order, canCancel)).ToList()));
    }

    public async Task<JsonResult> OnPostCancelAsync(Guid id)
    {
        try
        {
            await _service.CancelAsync(id);
            return new JsonResult(new { success = true });
        }
        catch (BusinessException exception) when (IsKnownCancelException(exception))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return new JsonResult(new
            {
                error = new
                {
                    message = SalesUiFormatter.GetFriendlyErrorMessage(_localizer, exception)
                }
            });
        }
    }

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Create)).Succeeded;
        CanViewHistory = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.ViewCustomerHistory)).Succeeded;
        CanCancel = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Cancel)).Succeeded;
    }

    private SalesOrderRow ToRow(SalesOrderDto order, bool canCancel)
    {
        var payment = order.PaymentSummary;
        var rowCanCancel = canCancel &&
            (order.Status == SalesOrderStatus.Draft ||
             (order.Status == SalesOrderStatus.Confirmed &&
              payment.PaymentStatus == SalesOrderReceivableStatus.Unpaid &&
              payment.PaidAmount == 0));
        var cancelConfirmationMessage = order.Status == SalesOrderStatus.Confirmed
            ? _localizer["Sales:CancelConfirmedUnpaidOrderMessage"].Value
            : _localizer["Sales:CancelOrderMessage"].Value;
        return new SalesOrderRow(
            order.Id,
            order.OrderNo,
            FormatDate(order.OrderDate),
            string.IsNullOrEmpty(order.CustomerNameSnapshot)
                ? _localizer["Sales:CustomerContextUnavailable"].Value
                : order.CustomerNameSnapshot,
            _localizer[$"Sales:Status:{order.Status}"].Value,
            FormatMoney(order.TotalRevenueAmount),
            FormatPaymentAmount(payment.PaymentStatus, payment.TotalAmount),
            FormatPaymentAmount(payment.PaymentStatus, payment.PaidAmount),
            FormatPaymentAmount(payment.PaymentStatus, payment.RemainingAmount),
            _localizer[$"Sales:PaymentStatus:{payment.PaymentStatus}"].Value,
            GetPaymentStatusBadgeClass(payment.PaymentStatus),
            rowCanCancel,
            cancelConfirmationMessage);
    }

    private static string FormatMoney(decimal value)
    {
        var amount = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        return amount.ToString("#,0", Vi) + " ₫";
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("dd/MM/yyyy", Vi);
    }

    private static string FormatPaymentAmount(SalesOrderReceivableStatus status, decimal amount)
    {
        return status == SalesOrderReceivableStatus.NotApplicable ? "—" : FormatMoney(amount);
    }

    private static string GetPaymentStatusBadgeClass(SalesOrderReceivableStatus status) => status switch
    {
        SalesOrderReceivableStatus.Unpaid => "text-bg-danger",
        SalesOrderReceivableStatus.PartiallyPaid => "text-bg-warning text-dark",
        SalesOrderReceivableStatus.Paid => "text-bg-success",
        SalesOrderReceivableStatus.Overpaid => "text-bg-info text-dark",
        _ => "text-bg-secondary"
    };

    private static bool IsKnownCancelException(BusinessException exception) =>
        exception.Code is
            VPureLuxDomainErrorCodes.SalesOrderAlreadyConfirmed or
            VPureLuxDomainErrorCodes.SalesOrderAlreadyCancelled or
            VPureLuxDomainErrorCodes.SalesConfirmedOrderCancelRequiresUnpaid or
            VPureLuxDomainErrorCodes.SalesInventoryValidationFailed or
            VPureLuxDomainErrorCodes.SalesOrderCannotBeModified or
            VPureLuxDomainErrorCodes.InventoryTransactionNotFound or
            VPureLuxDomainErrorCodes.InventoryIdempotencyConflict or
            VPureLuxDomainErrorCodes.SalesOrderNotFound or
            VPureLuxDomainErrorCodes.SalesConcurrentModification or
            VPureLuxDomainErrorCodes.AccessDenied or
            VPureLuxDomainErrorCodes.ValidationFailed;

    public sealed record SalesOrderRow(
        Guid Id,
        string OrderNo,
        string OrderDate,
        string CustomerName,
        string StatusLabel,
        string TotalRevenueAmount,
        string PaymentTotalAmount,
        string PaymentPaidAmount,
        string PaymentRemainingAmount,
        string PaymentStatusLabel,
        string PaymentStatusBadgeClass,
        bool CanCancel,
        string CancelConfirmationMessage);
}
