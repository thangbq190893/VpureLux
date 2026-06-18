using System;

namespace VPureLux.Catalog.Events;

public sealed record ComponentImageRemovedEvent(Guid ComponentId, string Code, string PreviousHash);
