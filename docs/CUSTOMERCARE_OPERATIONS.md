# CustomerCare Operations

## Runtime gates

The new CustomerCare workflow is controlled by the `CustomerCare` configuration section:

- `IsEnabled`: enables the CustomerCare module runtime.
- `IsSalesIntakeEnabled`: enables intake of confirmed Sales orders.
- `SalesIntakeGoLiveFrom`: required ISO-8601 boundary for Sales intake. Orders before this instant are outside automatic intake.

Both enablement flags default to `false`; intake also stays stopped while the go-live boundary is empty. Runtime environment variables may override JSON with
`CustomerCare__IsEnabled`, `CustomerCare__IsSalesIntakeEnabled`, and
`CustomerCare__SalesIntakeGoLiveFrom`.

## Current rollout behavior

Sales confirmation no longer creates Warranty assets or reminders synchronously. This protects Sales,
BOM snapshots, FIFO posting, and financial snapshots from CustomerCare failures.

The idempotent intake worker is delivered. It runs only when both gates are deliberately enabled
with an approved go-live boundary; this is not permission to backfill historical Sales orders.

## Completed workflow

1. Mark a Catalog Product as a machine and configure replacement policy only for tracked Components.
2. Confirm Sales normally. Sales does not create CustomerCare data in its transaction.
3. The gated background worker scans only confirmed machine lines on or after `SalesIntakeGoLiveFrom`, creating one PendingInstallation asset per integer unit and retaining the sold BOM snapshot.
4. A technician reviews actual positions and confirms installation. Only then are first reminders created from `InstalledAt`.
5. Operators may register machines bought elsewhere without a Product or Sales reference. Each real position can be mapped or unmapped and can have an explicit replacement baseline or no baseline.
6. Reminder timing is calculated at query time from `WarningDate` and `DueDate`. Complete, skip, reschedule, and suspend actions require a reason and append an idempotent maintenance event.
7. An existing reminder keeps its original cycle/warning snapshot. Completing it starts a new cycle from the current enabled Component policy. No successor is created when that policy is absent/disabled, the Component is inactive, or the actual position is inactive/unmapped. Re-enabling a policy does not backfill a missed successor.

Direct completion from the Warranty reminder is a transitional workflow while Service is disabled. Once Service is enabled, actual replacement must be completed from a Service order so inventory issue, labor revenue, payment, and schedule changes remain one business transaction.

## Test/UAT boundary (authoritative as of 2026-09-04)

`VPL` is the authorized test/UAT database. `VPureLux` is production. Older notes that call VPL production, require a new test VPS service, or request production write-UAT are superseded by this boundary.

- prove the configured SQL target and `DB_NAME() = VPL` before test writes;
- use a local Web runtime on a separate local port; no new VPS application is required;
- create clearly prefixed fixtures and preserve fingerprints of pre-existing business rows;
- `CustomerCare__IsEnabled=false` and `CustomerCare__IsSalesIntakeEnabled=false` during migration/smoke setup;
- an explicit approved test go-live instant before enabling intake;
- no automatic historical backfill; inspect eligible candidates before any worker run;
- isolate test cache/lock configuration from production, without changing production configuration.

Never reuse `/etc/vpurelux/vpurelux.env`, replace `/opt/vpurelux/app`, run production UAT mutations, or restart production for this verification task. VPL authorization is not production authorization. W-008 reuses accepted migration/rollout evidence; a new migration or deployment requires its own explicit scope.

Technical completion of W-008 does not close W-GATE. Record operator acceptance separately; Service stays on HOLD until that gate is accepted.

For zero-legacy-row-change rehearsals, create a new CustomerGroup as well as a new Customer, Catalog items, and warehouse. Creating a Customer can touch the referenced group's concurrency/audit metadata even without changing its business fields. Preserve the original baseline and report any mismatch; never reset metadata to hide it. The 2026-09-04 first run exposed this case; the isolated second run passed all 41 pre-run table fingerprints.

## Acceptance state

The user explicitly accepted Warranty/CustomerCare on 2026-09-07 (Asia/Saigon). W-008 and W-GATE are DONE. The future-cycle rule above is implemented without a migration, historical reminder rewrite, or backfill. The first 2026-09-04 fixture's CustomerGroup metadata touch remains an accepted test-harness exception; the original 40/41 result is preserved and isolated R2 41/41 is the official data-safety evidence.

The accepted W-008 application/test fixes remain local and uncommitted at the Service audit boundary; acceptance is not a deployment record. Production stays at the frozen Sales V1 source. `SERVICE_INVENTORY_AUDIT.md` records the completed source audit and makes S-001 READY without starting Service implementation.

For all later modules, configuration/templates suggest or initialize new business facts only. Existing facts retain their independent snapshots. An explicit authorized and audited edit may change intended fields; editing defaults must not automatically rewrite, recalculate, resync, or backfill prior records.

## Operational verification

- Confirm migration history contains `20260824050543_AddCustomerCareFoundation` on the test database only.
- Verify login, icons/webfonts, Sales confirmation, machine intake, installation, external-machine creation, reminder actions, and per-machine history.
- Check hourly logs for actionable identifiers and verify no Sales 500 was introduced.
- Keep both gates disabled before rollback. Application rollback may point the test symlink to the prior release; do not run migration `Down` on a database containing CustomerCare history.

## Schema migration safety

`AddCustomerCareFoundation` is schema-only. Its `Up` migration does not update, delete, insert, rename,
or backfill business rows. Existing Warranty assets keep their stored values; an existing Active asset
without `InstalledAt` is interpreted by the application as `PendingReview`.

The generated `Down` path removes CustomerCare tables/columns and is for disposable test databases
only. Do not run it against a database containing CustomerCare history.
