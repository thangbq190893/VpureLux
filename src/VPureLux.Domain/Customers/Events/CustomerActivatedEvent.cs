using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerActivatedEvent(Guid CustomerId, string Code);
