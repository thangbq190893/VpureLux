using System;

namespace VPureLux.Catalog.Events;

public sealed record ComponentDeactivatedEvent(Guid ComponentId, string Code);
