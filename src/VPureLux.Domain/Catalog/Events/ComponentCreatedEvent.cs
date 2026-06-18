using System;

namespace VPureLux.Catalog.Events;

public sealed record ComponentCreatedEvent(Guid ComponentId, string Code, string Name);
