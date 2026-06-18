using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerCreatedEvent(Guid CustomerId, string Code, Guid CustomerGroupId);
