using System;

namespace VPureLux.Catalog.Events;

public sealed record ComponentUpdatedEvent(Guid ComponentId, string Code, string Name);
