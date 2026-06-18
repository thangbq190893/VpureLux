using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerGroupCreatedEvent(Guid CustomerGroupId, string Code, string Name);
