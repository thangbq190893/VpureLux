using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerGroupUpdatedEvent(Guid CustomerGroupId, string Code, string Name);
