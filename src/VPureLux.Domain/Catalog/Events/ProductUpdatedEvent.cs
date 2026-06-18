using System;

namespace VPureLux.Catalog.Events;

public sealed record ProductUpdatedEvent(Guid ProductId, string Code, string Name);
