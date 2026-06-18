using System;

namespace VPureLux.Bom.Events;

public sealed record BomVersionCreatedEvent(Guid BomVersionId, Guid ProductId, int VersionNo);
