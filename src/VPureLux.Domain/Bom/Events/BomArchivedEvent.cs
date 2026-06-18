using System;

namespace VPureLux.Bom.Events;

public sealed record BomArchivedEvent(Guid BomVersionId, Guid ProductId, int VersionNo);
