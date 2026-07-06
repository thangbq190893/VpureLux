# UAT Fix 03K.2 - Shared Business Code Generator Infrastructure

## Reason

03K.1 found that Product, Vật tư/Component, Customer, Warehouse, and Inventory lot numbers are still entered manually, while Sales/BOM/Pricing already have separate generation behavior. This batch adds a shared generator infrastructure first so later batches can apply auto-generated codes consistently without changing entity create behavior yet.

## Scope

- Added `IBusinessCodeGenerator` and `BusinessCodeGenerationContext`.
- Added an Application-layer generator implementation.
- Added focused generator tests.
- Added localized business errors for lock acquisition and retry exhaustion.
- Did not wire the generator into Product, Vật tư/Component, Customer, Warehouse, Receipt, Adjustment, Sales, BOM, Pricing, FIFO, posting, or costing flows.

## Format

Generated candidates use:

```text
{PREFIX}-{yyyyMMdd}{sequence:D4}
```

Example:

```text
PROD-202607060001
```

`PaddingLength` is configurable through the context and defaults to `4`.

## Cache Strategy

The generator stores the latest allocated integer sequence in distributed cache for approximately seven days.

Logical cache key:

```text
Sequence:{SequenceName}:{yyyyMMdd}
```

The key intentionally omits a hard-coded `VPureLux:` prefix because ABP distributed cache is already configured with `AbpDistributedCacheOptions.KeyPrefix = "VPureLux:"` in the Web host. This avoids a double `VPureLux:VPureLux:` prefix.

## Lock Strategy

The generator acquires an ABP distributed lock before reading, seeding, incrementing, checking collisions, and writing the cache value.

Lock key:

```text
VPureLux:SequenceLock:{SequenceName}:{yyyyMMdd}
```

The Web host already backs ABP distributed locking with Redis. Tests use an in-memory lock.

## Seed Strategy

If the cache value is missing or invalid, the caller-provided `SeedMaxAsync` callback supplies the current DB maximum numeric suffix for that sequence/date/prefix.

The generator deliberately expects a MAX suffix seed, not COUNT. Later entity batches should query matching codes/lots by prefix/date, parse the numeric suffix, and pass the maximum existing suffix to the generator.

## Retry And Collision Behavior

For each candidate, the generator calls the caller-provided `ExistsAsync` duplicate check. If a candidate already exists, the generator increments and retries up to `RetryLimit` times, defaulting to `20`.

If the lock cannot be acquired or all retry attempts collide, the generator throws a friendly `BusinessException`. Existing domain/repository duplicate checks and DB unique indexes must remain the final safety layer in later entity batches.

## Date Handling

The generator uses `BusinessCodeGenerationContext.Date` when supplied. Otherwise, it uses ABP `IClock.Now`.

The sequence resets by `SequenceName` and `yyyyMMdd`.

## Not Applied In This Batch

- Product code generation.
- Vật tư/Component code generation.
- Customer code generation.
- Warehouse code generation.
- Receipt lot number generation.
- Adjustment positive-delta lot number generation.
- Sales `OrderNo`.
- BOM `VersionNo`.
- Pricing `VersionNo`.
- Any UI changes or manual-field removal.
- Any Domain rule, DB schema, migration, FIFO, posting, or costing changes.

## Tests Run

Validation completed:

```text
dotnet build VPureLux.slnx --no-restore -m:2 -> passed
dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~BusinessCode" -m:1 -> passed, 10 tests
git diff --check -> passed
git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md -> no matches
```

Manual browser smoke was not run because this batch does not expose the generator in UI flows.

## Deferred

- Decide which entities get generated codes first.
- Decide whether manual override is allowed and who can use it.
- Decide whether supplier/manufacturer lot number needs a separate field before auto-generating internal `LotNo`.
- Add entity-specific seed queries in later batches.
- Keep Sales/BOM/Pricing generation unchanged unless a separate business decision approves convergence.
