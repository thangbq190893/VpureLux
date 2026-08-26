using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Sales;

[Route("api/sales")]
public class SalesPostConfirmationController : AbpControllerBase
{
    private readonly ISalesPostConfirmationAppService _appService;

    public SalesPostConfirmationController(ISalesPostConfirmationAppService appService) =>
        _appService = appService;

    [HttpPost("orders/{salesOrderId:guid}/revisions")]
    public Task<SalesOrderRevisionDto> OpenRevisionAsync(
        Guid salesOrderId,
        [FromBody] OpenSalesOrderRevisionDto input) =>
        _appService.OpenRevisionAsync(salesOrderId, input);

    [HttpGet("revisions/{revisionId:guid}")]
    public Task<SalesOrderRevisionDto> GetRevisionAsync(Guid revisionId) =>
        _appService.GetRevisionAsync(revisionId);

    [HttpPut("revisions/{revisionId:guid}")]
    public Task<SalesOrderRevisionDto> UpdateRevisionAsync(
        Guid revisionId,
        [FromBody] UpdateSalesOrderRevisionDto input) =>
        _appService.UpdateRevisionAsync(revisionId, input);

    [HttpPost("revisions/{revisionId:guid}/returned-goods")]
    public Task ConfirmRevisionReturnedGoodsAsync(
        Guid revisionId,
        [FromBody] ConfirmRevisionReturnedGoodsDto input) =>
        _appService.ConfirmRevisionReturnedGoodsAsync(revisionId, input);

    [HttpPost("revisions/{revisionId:guid}/apply")]
    public Task<SalesOrderRevisionDto> ApplyRevisionAsync(
        Guid revisionId,
        [FromBody] ApplySalesOrderRevisionDto input) =>
        _appService.ApplyRevisionAsync(revisionId, input);

    [HttpPost("revisions/{revisionId:guid}/cancel")]
    public Task CancelRevisionAsync(Guid revisionId, [FromBody] ReasonDto input) =>
        _appService.CancelRevisionAsync(revisionId, input);

    [HttpPost("orders/{salesOrderId:guid}/cancel-confirmed")]
    public Task<SalesOrderCancellationDto> CancelConfirmedAsync(
        Guid salesOrderId,
        [FromBody] CancelConfirmedSalesOrderDto input) =>
        _appService.CancelConfirmedAsync(salesOrderId, input);

    [HttpGet("cancellations/{cancellationId:guid}")]
    public Task<SalesOrderCancellationDto> GetCancellationAsync(Guid cancellationId) =>
        _appService.GetCancellationAsync(cancellationId);

    [HttpPost("cancellations/{cancellationId:guid}/returned-goods")]
    public Task<SalesOrderCancellationDto> ConfirmReturnedGoodsAsync(
        Guid cancellationId,
        [FromBody] ConfirmCancellationReturnedGoodsDto input) =>
        _appService.ConfirmReturnedGoodsAsync(cancellationId, input);

    [HttpPost("cancellations/{cancellationId:guid}/refunds")]
    public Task<SalesOrderRefundDto> RecordCancellationRefundAsync(
        Guid cancellationId,
        [FromBody] RecordSalesOrderRefundDto input) =>
        _appService.RecordCancellationRefundAsync(cancellationId, input);

    [HttpPost("revisions/{revisionId:guid}/refunds")]
    public Task<SalesOrderRefundDto> RecordRevisionRefundAsync(
        Guid revisionId,
        [FromBody] RecordSalesOrderRefundDto input) =>
        _appService.RecordRevisionRefundAsync(revisionId, input);

    [HttpPost("payments/{paymentId:guid}/void")]
    public Task VoidPaymentAsync(Guid paymentId, [FromBody] ReasonDto input) =>
        _appService.VoidPaymentAsync(paymentId, input);
}
