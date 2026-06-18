using System;

namespace VPureLux.Catalog.Events;

public sealed record ProductActivatedEvent(Guid ProductId, string Code);
