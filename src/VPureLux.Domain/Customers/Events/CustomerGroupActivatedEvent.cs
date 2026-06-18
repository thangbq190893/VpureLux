using System;

namespace VPureLux.Customers.Events;

public sealed record CustomerGroupActivatedEvent(Guid CustomerGroupId, string Code);
