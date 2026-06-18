using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerGroupChangedEvent(
    Guid CustomerId,
    Guid PreviousCustomerGroupId,
    Guid NewCustomerGroupId);
