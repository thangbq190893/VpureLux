using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace VPureLux.Sales;

public interface ISalesPostConfirmationAppService : IApplicationService
{
    Task<SalesOrderRevisionDto> OpenRevisionAsync(Guid salesOrderId, OpenSalesOrderRevisionDto input);
    Task<SalesOrderRevisionDto> GetRevisionAsync(Guid revisionId);
    Task<SalesOrderRevisionDto> UpdateRevisionAsync(Guid revisionId, UpdateSalesOrderRevisionDto input);
    Task ConfirmRevisionReturnedGoodsAsync(Guid revisionId, ConfirmRevisionReturnedGoodsDto input);
    Task<SalesOrderRevisionDto> ApplyRevisionAsync(Guid revisionId, ApplySalesOrderRevisionDto input);
    Task CancelRevisionAsync(Guid revisionId, ReasonDto input);
    Task<SalesOrderCancellationDto> CancelConfirmedAsync(Guid salesOrderId, CancelConfirmedSalesOrderDto input);
    Task<SalesOrderCancellationDto> GetCancellationAsync(Guid cancellationId);
    Task<SalesOrderCancellationDto> ConfirmReturnedGoodsAsync(Guid cancellationId, ConfirmCancellationReturnedGoodsDto input);
    Task<SalesOrderRefundDto> RecordCancellationRefundAsync(Guid cancellationId, RecordSalesOrderRefundDto input);
    Task<SalesOrderRefundDto> RecordRevisionRefundAsync(Guid revisionId, RecordSalesOrderRefundDto input);
    Task VoidPaymentAsync(Guid paymentId, ReasonDto input);
}
