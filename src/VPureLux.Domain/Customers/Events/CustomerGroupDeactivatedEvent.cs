using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerGroupDeactivatedEvent(Guid CustomerGroupId, string Code);
