using System;

namespace VPureLux.Sales.Events;

public sealed record SalesOrderCreatedEvent(Guid SalesOrderId, string OrderNo, Guid CustomerId);
public sealed record SalesOrderConfirmedEvent(Guid SalesOrderId, string OrderNo, Guid CustomerId, decimal Revenue, decimal Profit);
public sealed record SalesOrderCancelledEvent(Guid SalesOrderId, string OrderNo, Guid CustomerId);
public sealed record SalesOrderRevisionOpenedEvent(Guid RevisionId, Guid SalesOrderId, int RevisionNo, string Reason);
public sealed record SalesOrderRevisionAppliedEvent(Guid RevisionId, Guid SalesOrderId, int RevisionNo, string Reason, decimal AppliedTotal, decimal RefundDue);
public sealed record SalesOrderRevisionCancelledEvent(Guid RevisionId, Guid SalesOrderId, int RevisionNo, string Reason);
public sealed record SalesOrderCancellationApprovedEvent(Guid CancellationId, Guid SalesOrderId, string ReasonGroup, string Reason, decimal RefundDue);
public sealed record SalesOrderRefundRecordedEvent(Guid RefundId, Guid SalesOrderId, Guid? RevisionId, Guid? CancellationId, decimal Amount, string Reason);
public sealed record SalesOrderPaymentVoidedEvent(Guid PaymentId, Guid SalesOrderId, string Reason);
