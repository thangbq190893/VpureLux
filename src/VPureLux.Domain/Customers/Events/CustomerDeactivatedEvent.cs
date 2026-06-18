using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerDeactivatedEvent(Guid CustomerId, string Code);
