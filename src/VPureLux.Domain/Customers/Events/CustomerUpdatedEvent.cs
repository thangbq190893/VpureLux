using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerUpdatedEvent(Guid CustomerId, string Code);
