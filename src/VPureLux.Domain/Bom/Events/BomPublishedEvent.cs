using System;

namespace VPureLux.Bom.Events;

public sealed record BomPublishedEvent(Guid BomVersionId, Guid ProductId, int VersionNo);
