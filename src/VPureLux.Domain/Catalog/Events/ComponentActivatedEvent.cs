using System;

namespace VPureLux.Catalog.Events;

public sealed record ComponentActivatedEvent(Guid ComponentId, string Code);
