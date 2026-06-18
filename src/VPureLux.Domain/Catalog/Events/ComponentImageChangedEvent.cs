using System;

namespace VPureLux.Catalog.Events;

public sealed record ComponentImageChangedEvent(
    Guid ComponentId,
    string Code,
    string? PreviousHash,
    string NewHash,
    string MimeType,
    string FileName);
