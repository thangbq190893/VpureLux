# Audit Module Implementation Specification

## Ownership

- ABP Audit Logging remains the technical audit system.
- `BusinessAuditLog` is the append-only business-audit source of truth.
- Business audit records have no update, delete, or soft-delete lifecycle.
- Retention is forever.

## Data Rules

- CorrelationId and CausationId are required.
- User, System, and Integration actors are supported.
- OldValueJson, NewValueJson, and MetadataJson are selective safe payloads,
  each limited to 32 KB.
- Full aggregate snapshots, secrets, and Base64 image content are prohibited.
- Severity values are Informational = 0, Important = 1, Critical = 2.
- Export requested and completed actions create business audit records.

## Public Surface

- Audit search, detail, reports, and export are available.
- `Audit.View` protects read operations.
- `Audit.Export` protects export operations.
- No public create, update, or delete operations exist.
