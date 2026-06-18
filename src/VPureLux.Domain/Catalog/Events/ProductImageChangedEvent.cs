using System;

namespace VPureLux.Catalog.Events;

public sealed record ProductImageChangedEvent(
    Guid ProductId,
    string Code,
    string? PreviousHash,
    string NewHash,
    string MimeType,
    string FileName);
