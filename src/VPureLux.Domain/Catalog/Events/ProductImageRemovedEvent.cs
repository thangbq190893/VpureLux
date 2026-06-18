using System;

namespace VPureLux.Catalog.Events;

public sealed record ProductImageRemovedEvent(Guid ProductId, string Code, string PreviousHash);
