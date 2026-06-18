using System;

namespace VPureLux.Catalog.Events;

public sealed record ProductCreatedEvent(Guid ProductId, string Code, string Name);
