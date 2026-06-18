# Catalog Image Extension Implementation Specification

## Status

Approved for implementation. This specification supplements the certified
Catalog module and `CATALOG_IMAGE_EXTENSION_IMPLEMENTATION_PLAN_VFINAL.md`.

## Domain

- Product and Component each own one optional immutable `ImageData` value
  object.
- `ImageData` contains `ImageBase64`, `MimeType`, `FileName`, and `ImageHash`.
- Supported MIME types are `image/jpeg`, `image/png`, and `image/webp`.
- Aggregate image changes use `SetImage` and `RemoveImage`.
- Same-hash uploads to the same aggregate are idempotent.
- Image domain events contain identifiers and metadata only, never image
  content.

## Validation

- Maximum decoded image size: 2 MB.
- The application layer validates Base64, extension, MIME type, file
  signature, decoded size, and image safety before invoking aggregate behavior.
- SHA256 is calculated server-side.

## Persistence

- Original images are stored as Base64 in nullable owned columns on
  `AppProducts` and `AppComponents`.
- Thumbnails are generated dynamically and are not persisted.

## API And UI

- Dedicated Product and Component image and thumbnail endpoints return binary
  content with `ETag` set to the image hash.
- General Catalog DTOs never expose image content.
- Catalog list pages use lazy-loaded thumbnails and placeholders.
- Catalog Create/Edit pages support upload; Edit pages support replacement and
  removal.

## Error Codes

- `CATALOG_005` Unsupported image format.
- `CATALOG_006` Invalid Base64 image content.
- `CATALOG_007` Image exceeds the 2 MB decoded-size limit.
- `CATALOG_008` Invalid image signature.
- `CATALOG_009` Unsafe or unreadable image content.
- `CATALOG_010` Image not found.

## Audit Safety

Image content must not be written to domain events, logs, general DTOs, or
audit payloads. Dedicated image upload and retrieval application methods disable
ABP parameter/result auditing. Business audit metadata is limited to item ID,
code, hashes, MIME type, and file name.
