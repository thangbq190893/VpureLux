using System;

namespace VPureLux.Sales.Events;

public sealed record SalesOrderCreatedEvent(Guid SalesOrderId, string OrderNo, Guid CustomerId);
public sealed record SalesOrderConfirmedEvent(Guid SalesOrderId, string OrderNo, Guid CustomerId, decimal Revenue, decimal Profit);
public sealed record SalesOrderCancelledEvent(Guid SalesOrderId, string OrderNo, Guid CustomerId);
