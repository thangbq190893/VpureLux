# Catalog Image Extension Implementation Plan vFinal

## 1. Purpose

This document defines the final implementation plan for adding Product and
Component images to the certified Catalog module.

It reconciles:

- The existing certified Catalog implementation.
- The approved Catalog Image Extension decisions.
- Future Sales item-selection requirements.
- The approved Audit Architecture Review vFinal.

This is an implementation plan only. It does not authorize production-code
changes.

## 2. Sequencing Decision

### Recommendation

Complete and recertify the Catalog Image Extension before implementing the
Audit module.

### Rationale

1. Image upload, replacement, and removal introduce new business-audit events.
   Defining these events before Audit implementation avoids immediately
   reopening and recertifying Audit.
2. Audit records must never contain Base64 image content. Implementing the
   image event contracts first allows Audit handlers to consume safe,
   lightweight metadata from their first release.
3. Future Sales item-selection UI requires Catalog image thumbnails. Completing
   the Catalog capability first removes a known dependency without making Sales
   own or snapshot images.
4. The extension preserves existing Catalog aggregate boundaries and can be
   delivered as a controlled extension to the certified module.

### Scope Boundary

The Catalog Image Extension sprint must not redesign Sales item selection.
Catalog will expose reusable image metadata and thumbnail endpoints. A future
Sales UI enhancement may consume them after Catalog recertification.

## 3. Approved Decisions

- Product supports one optional image.
- Component supports one optional image.
- Image content is stored as Base64 in SQL Server during phase 1.
- Maximum decoded image size is 2 MB.
- Supported formats are JPG, JPEG, PNG, and WEBP.
- Product and Component list pages display thumbnails.
- Catalog Create/Edit UI supports image upload, preview, replacement, and
  removal.
- Sales item-selection UI may display Catalog images.
- Sales does not own or snapshot Catalog images.
- Base64 image content must never be included in domain events, business-audit
  payloads, or ABP audit payloads.
- SHA256 `ImageHash` is stored for change detection and duplicate-upload
  prevention.
- Phase 2 may migrate content storage to S3, MinIO, or Azure Blob.

## 4. Current Catalog Impact Assessment

### Existing Certified Design

- `Product` and `Component` are independent aggregate roots.
- Both aggregates currently contain catalog identity, descriptive data, and
  active-state behavior.
- Catalog application services use repositories and do not directly access
  `DbContext`.
- Existing DTOs, EF mappings, API controllers, and Razor Pages do not contain
  image fields or image workflows.
- Existing Catalog permissions provide View, Create, and Edit capabilities.

### Required Catalog Changes

Catalog production source changes and a database migration are required.
Catalog must be recertified after the extension.

The extension does not change:

- Product or Component aggregate boundaries.
- Existing Catalog ownership rules.
- BOM references to Product and Component.
- Pricing ownership.
- Inventory StockItem ownership.
- Sales snapshot ownership.

### Other Certified Modules

| Module | Required Source Change During Image Sprint |
|---|---|
| BOM | No |
| Customer | No |
| Pricing | No |
| Inventory | No |
| Sales | No; future selector enhancement will consume Catalog endpoints |
| Audit | Not yet implemented; design handlers after image events exist |

## 5. Final Domain Design

### ImageData Value Object

Introduce an immutable `ImageData` value object owned by Product or Component.

Fields:

- `ImageBase64`
- `MimeType`
- `FileName`
- `ImageHash`

An aggregate may have no image. A partially populated `ImageData` value object
is invalid.

### Aggregate Behaviors

Add the following behavior to both Product and Component:

- `SetImage(ImageData image)`
- `RemoveImage()`

Rules:

- Existing Product and Component identity and code immutability remain
  unchanged.
- Replacement occurs only through aggregate behavior.
- Removal occurs only through aggregate behavior.
- Uploading the same calculated hash to the same aggregate is idempotent and
  must not create a duplicate change event.
- The hash is not globally unique. The same image may legitimately be used by
  different Catalog items.

### Domain Validation

The domain model must enforce stable structural invariants:

- Required image fields are present together.
- MIME type belongs to the supported allowlist.
- File name length and MIME type length are within documented limits.
- `ImageHash` is a valid SHA256 hexadecimal value.
- Base64 content is not empty when an image exists.

Decoded size, file signature, and server-calculated hash validation belong to
the application image-processing boundary before aggregate behavior is called.

### Domain Events

Introduce explicit lightweight events:

- `ProductImageChangedEvent`
- `ProductImageRemovedEvent`
- `ComponentImageChangedEvent`
- `ComponentImageRemovedEvent`

Allowed event data:

- Aggregate identifier.
- Catalog code where useful for audit display.
- Previous image hash.
- New image hash.
- MIME type.
- File name.
- Actor/correlation metadata supplied through the audit pipeline where
  applicable.

Prohibited event data:

- `ImageBase64`.
- Decoded image bytes.
- Large object graphs.

## 6. Application Architecture

### Image Processing Boundary

Introduce a dedicated application abstraction, such as
`ICatalogImageProcessor`, responsible for:

- Decoding Base64 safely.
- Validating decoded size.
- Validating supported MIME type and extension.
- Inspecting file signature/magic bytes.
- Rejecting mismatched MIME type, extension, and actual content.
- Calculating SHA256 server-side.
- Producing validated `ImageData`.
- Producing bounded thumbnails for read operations.

Clients must not be trusted to supply the authoritative hash or MIME type.

This abstraction isolates content processing and reduces the future migration
cost to external object storage.

### Size Validation

The 2 MB limit applies to decoded image bytes, not the Base64 string length.
Base64 storage adds approximately 33 percent overhead.

Image processing must also guard against malformed images and decompression
bombs by using a proven image-processing library with bounded decode settings.

### Duplicate Upload Prevention

Duplicate prevention is scoped to the same Product or Component:

1. Process the submitted image.
2. Calculate SHA256 server-side.
3. Compare it with the aggregate's current `ImageHash`.
4. If hashes match, return the current image metadata without persistence or a
   new domain event.

No global unique hash constraint will be created.

## 7. Application Contracts

### General Catalog DTOs

General Product and Component list/detail DTOs must never include Base64
content.

Add lightweight image metadata:

- `HasImage`
- `ImageHash`

The UI can construct dedicated image and thumbnail URLs using the item ID.

### Dedicated Image Contracts

Introduce dedicated contracts for:

- Image upload request: Base64 content, declared MIME type, and file name.
- Image metadata response: hash, MIME type, file name, and last-change
  metadata where available.
- Image removal.
- Binary image and thumbnail retrieval.

Dedicated contracts keep large content out of routine Catalog queries and make
future storage replacement possible without changing general DTOs.

### Permissions

Use existing Catalog permissions:

- Product image read: Products View permission.
- Product image upload/remove: Products Edit permission.
- Component image read: Components View permission.
- Component image upload/remove: Components Edit permission.

No new image-specific permissions are required unless a later business rule
requires separate image administration.

## 8. Database Design

### Mapping Strategy

Map nullable owned `ImageData` columns directly on `AppProducts` and
`AppComponents`.

Columns for each table:

| Column | SQL Type | Nullability |
|---|---|---|
| `ImageBase64` | `NVARCHAR(MAX)` | NULL |
| `ImageMimeType` | `NVARCHAR(50)` | NULL |
| `ImageFileName` | `NVARCHAR(255)` | NULL |
| `ImageHash` | `CHAR(64)` or `NVARCHAR(64)` | NULL |

The final hash column type must be consistent across Product and Component.

### Constraints and Indexes

- All image columns must be NULL when no image exists.
- Application and domain validation prevent partial image data.
- Add a non-unique index on `ImageHash` only if measured query or operational
  needs justify it.
- Do not create a globally unique `ImageHash` constraint.

### Migration Requirement

A Catalog image migration is required to:

- Add nullable image columns to `AppProducts`.
- Add nullable image columns to `AppComponents`.
- Update the EF Core model snapshot.

Existing records remain valid with NULL image columns. No data backfill is
required.

### Storage and Operational Impact

- A 2 MB decoded image may require roughly 2.8 MB of Base64 text storage.
- Database size, backup time, restore time, and replication traffic will grow.
- Routine list and lookup queries must avoid selecting `ImageBase64`.
- Image content must only be loaded through dedicated image operations.

## 9. HTTP API Design

### Product Routes

- `GET /api/catalog/products/{id}/image`
- `GET /api/catalog/products/{id}/thumbnail`
- `PUT /api/catalog/products/{id}/image`
- `DELETE /api/catalog/products/{id}/image`

### Component Routes

- `GET /api/catalog/components/{id}/image`
- `GET /api/catalog/components/{id}/thumbnail`
- `PUT /api/catalog/components/{id}/image`
- `DELETE /api/catalog/components/{id}/image`

### Response Behavior

- GET image endpoints return decoded binary content, not Base64 JSON.
- Response `Content-Type` is based on validated stored metadata.
- `ETag` uses `ImageHash`.
- Cache headers allow hash-based browser caching.
- Missing images return the documented not-found behavior.
- Upload and delete controllers delegate only to application services.

### Thumbnail Strategy

Store only the original image in phase 1.

The thumbnail endpoint generates a bounded thumbnail, recommended maximum
96x96 pixels, through the image processor and caches it by `ImageHash`.
Thumbnail generation must use a proven image library. It must not modify the
Catalog aggregate or persist a second domain-owned image.

## 10. Catalog UI Design

### Product and Component Lists

- Display a bounded thumbnail using the dedicated thumbnail endpoint.
- Display a consistent placeholder when no image exists.
- Use lazy loading.
- Never embed Base64 content in list-page HTML.
- Ensure list queries do not load image content.

### Create and Edit Pages

- Provide image file selection and preview.
- Validate extension and approximate size client-side for usability.
- Treat server-side validation as authoritative.
- Allow replacement and removal.
- Render upload/remove actions only for users with the relevant Edit
  permission.

### Sales Compatibility

Future Sales item-selection UI will:

- Read Product and Component metadata from Catalog.
- Display images through Catalog thumbnail endpoints.
- Use placeholders when images are absent.
- Never copy image Base64 or image hashes into SalesOrder snapshots.

Sales rich item selection remains a separate future enhancement and is not part
of the Catalog Image Extension sprint.

## 11. Audit Alignment

### Business Audit Requirements

Image upload, replacement, and removal are business-auditable actions.

Business audit records may contain:

- Item type and identifier.
- Catalog code.
- Previous and new SHA256 hashes.
- MIME type.
- File name.
- User identifier.
- Correlation identifier.
- Timestamp.

Business audit records must never contain:

- Base64 content.
- Decoded bytes.
- Thumbnail bytes.

### Audit Payload Limits

Image audit event metadata must remain below the approved 32 KB limits for
`OldValueJson`, `NewValueJson`, and `MetadataJson`.

The future Audit implementation must also include the separately approved:

- `AUDIT_EXPORT_REQUESTED`
- `AUDIT_EXPORT_COMPLETED`
- `AUDIT_001 PayloadTooLarge`
- Severity values: Informational = 0, Important = 1, Critical = 2

These Audit decisions do not require changes during the Catalog image sprint,
but the image events must be compatible with them.

## 12. Error and Validation Design

The implementation documentation alignment step must define Catalog image
error codes for at least:

- Unsupported format.
- Decoded image exceeds 2 MB.
- Invalid Base64.
- File signature does not match declared type.
- Invalid or unsafe image content.
- Image not found where required.

Duplicate same-hash upload is idempotent and does not require an error.

All client-facing failures must use documented business or validation
exceptions. Raw image-library, SQL, or decoding exceptions must not be exposed.

## 13. Test Strategy

### Domain Tests

- `ImageData` immutability.
- Required-field consistency.
- Supported MIME types.
- SHA256 format validation.
- Product image set, replace, same-hash no-op, and remove.
- Component image set, replace, same-hash no-op, and remove.
- Domain events contain metadata only and never Base64.

### Application Tests

- Server-side SHA256 calculation.
- Decoded 2 MB boundary validation.
- Invalid Base64 rejection.
- MIME/extension/signature mismatch rejection.
- Unsupported-format rejection.
- Same-hash idempotency.
- Permission enforcement.
- General DTOs do not expose Base64.
- List queries do not load image content.

### Repository and EF Tests

- Nullable owned-value persistence.
- Product image persistence and removal.
- Component image persistence and removal.
- Existing rows without images remain valid.
- Model snapshot and migration consistency.
- No global unique hash restriction.

### API Tests

- Upload, retrieve, replace, and remove Product image.
- Upload, retrieve, replace, and remove Component image.
- Correct binary content type.
- ETag and cache behavior.
- Thumbnail behavior.
- Authorization and validation failures.

### Razor Page Tests

- Product and Component list thumbnails.
- Placeholder behavior.
- Upload, preview, replace, and remove flows.
- Permission-aware image actions.
- No Base64 content embedded in rendered list pages.

### Audit Integration Tests

- Image events contain hashes and metadata only.
- Base64 never enters ABP audit or business-audit payloads.
- Serialized image event metadata remains below 32 KB.

### Regression Tests

- Existing Catalog tests remain passing.
- BOM Catalog references remain unaffected.
- Pricing Catalog validation remains unaffected.
- Inventory Catalog synchronization remains unaffected.
- Sales persistence and snapshots remain unaffected.

## 14. Implementation Sequence

### STEP 01 - Documentation Alignment

- Update Catalog specification, API contracts, validation rules, error codes,
  domain-event catalog, database schema specification, and test specification.
- Record the sequencing decision that Catalog Image Extension precedes Audit.

### STEP 02 - Domain Layer

- Add `ImageData`.
- Extend Product and Component aggregate behaviors.
- Add lightweight image domain events.

### STEP 03 - Application Contracts

- Add lightweight image metadata to Catalog DTOs.
- Add dedicated image upload and metadata contracts.
- Add application-service image operations.

### STEP 04 - Application Layer

- Implement image processor and validation orchestration.
- Calculate SHA256 server-side.
- Implement same-hash idempotency.
- Ensure general queries do not load Base64.

### STEP 05 - Infrastructure

- Add owned-value EF mappings.
- Add repository projections/content operations required to avoid loading
  Base64 in routine queries.
- Generate and validate migration and model snapshot.

### STEP 06 - HTTP API and Catalog UI

- Implement dedicated binary image and thumbnail endpoints.
- Add Product and Component upload/remove UI.
- Add lazy-loaded list thumbnails and placeholders.

### STEP 07 - Testing

- Implement the domain, application, EF, API, Razor Page, audit-safety, and
  regression test scopes defined above.

### STEP 08 - Catalog Recertification

- Verify DDD, ABP, security, migration, performance, audit-safety, and
  documentation compliance.
- Confirm that Catalog remains compatible with BOM, Pricing, Inventory, and
  Sales.

### Next Phase

After Catalog recertification, proceed with Audit implementation. Sales
item-selection image integration may be implemented as a separate approved
Sales UI enhancement.

## 15. Files Expected to Change During Implementation

Exact file names must follow existing repository conventions. Expected areas:

- Catalog Domain.Shared constants, errors, and event contracts.
- Product and Component domain aggregates.
- Catalog application contracts and DTOs.
- Product and Component application services.
- Catalog image-processing abstraction and implementation.
- Product and Component EF configurations.
- Catalog migration and model snapshot.
- Product and Component HTTP API controllers.
- Product and Component Razor Pages and localization resources.
- Catalog domain, application, EF, API, and Web tests.
- Catalog-related architecture and specification documents.

No BOM, Customer, Pricing, Inventory, Sales, or Audit production files should
be modified during this sprint unless a compile-time documented dependency is
discovered and separately approved.

## 16. Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Database growth from Base64 storage | Enforce decoded 2 MB limit; monitor DB and backup growth |
| Large Catalog list payloads | Never include or project Base64 in general DTOs |
| N+1 thumbnail requests | Lazy loading, browser caching, ETag, and server cache by hash |
| MIME spoofing or malicious files | Validate extension, MIME, magic bytes, and bounded decode |
| Decompression bombs | Use a proven image library with resource limits |
| Audit or log leakage | Explicit safe events; prohibit Base64 serialization and logging |
| Stale thumbnails | Cache using immutable SHA256 hash keys |
| Future external-storage migration | Keep content operations behind dedicated contracts and processor abstraction |
| Certified-module regression | Run full Catalog regression plus boundary tests |

## 17. Completion and Certification Criteria

The Catalog Image Extension is complete only when:

- Product and Component images can be uploaded, retrieved, replaced, and
  removed.
- Decoded content is limited to 2 MB.
- Only JPG, JPEG, PNG, and WEBP content is accepted.
- SHA256 is calculated server-side and same-hash uploads are idempotent.
- Product and Component lists display cached thumbnails without loading Base64
  in list queries.
- Dedicated image endpoints return binary content.
- Sales can later consume image metadata and thumbnails without owning images.
- No Base64 content appears in events, logs, audit payloads, or general DTOs.
- Migration and model snapshot are valid.
- All Catalog image tests and existing Catalog regression tests pass.
- Catalog is recertified.

## 18. Final Recommendation

Implement the Catalog Image Extension before the Audit module.

This sequencing establishes safe image-change events before Audit handlers are
built, satisfies the known future Sales thumbnail dependency, and avoids
reopening a newly certified Audit module. After Catalog Image Extension
recertification, proceed with Audit implementation. Do not include the future
Sales rich item selector in the Catalog image sprint.
