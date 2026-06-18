using System;

namespace VPureLux.Bom.Events;

public sealed record BomVersionClonedEvent(
    Guid SourceBomVersionId,
    Guid NewBomVersionId,
    Guid ProductId,
    int VersionNo);
