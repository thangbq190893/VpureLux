using System;

namespace VPureLux.Catalog.Events;

public sealed record ProductDeactivatedEvent(Guid ProductId, string Code);
