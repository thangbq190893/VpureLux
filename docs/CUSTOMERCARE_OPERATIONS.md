# CustomerCare Operations

## Runtime gates

The new CustomerCare workflow is controlled by the `CustomerCare` configuration section:

- `IsEnabled`: enables the CustomerCare module runtime.
- `IsSalesIntakeEnabled`: enables intake of confirmed Sales orders.
- `SalesIntakeGoLiveFrom`: required ISO-8601 boundary for Sales intake. Orders before this instant are outside automatic intake.

Both enablement flags default to `false`; intake also stays stopped while the go-live boundary is empty. Runtime environment variables may override JSON with
`CustomerCare__IsEnabled`, `CustomerCare__IsSalesIntakeEnabled`, and
`CustomerCare__SalesIntakeGoLiveFrom`.

## W-001 rollout behavior

Sales confirmation no longer creates Warranty assets or reminders synchronously. This protects Sales,
BOM snapshots, FIFO posting, and financial snapshots from CustomerCare failures.

W-001 does not run a database migration, update existing Warranty rows, or backfill historical Sales
orders. Automatic intake remains unavailable until the idempotent worker is delivered and both gates
are deliberately enabled with an approved go-live boundary.

## Completed workflow

1. Mark a Catalog Product as a machine and configure replacement policy only for tracked Components.
2. Confirm Sales normally. Sales does not create CustomerCare data in its transaction.
3. The gated background worker scans only confirmed machine lines on or after `SalesIntakeGoLiveFrom`, creating one PendingInstallation asset per integer unit and retaining the sold BOM snapshot.
4. A technician reviews actual positions and confirms installation. Only then are first reminders created from `InstalledAt`.
5. Operators may register machines bought elsewhere without a Product or Sales reference. Each real position can be mapped or unmapped and can have an explicit replacement baseline or no baseline.
6. Reminder timing is calculated at query time from `WarningDate` and `DueDate`. Complete, skip, reschedule, and suspend actions require a reason and append an idempotent maintenance event.

Direct completion from the Warranty reminder is a transitional workflow while Service is disabled. Once Service is enabled, actual replacement must be completed from a Service order so inventory issue, labor revenue, payment, and schedule changes remain one business transaction.

## Test deployment boundary

Do not point a Warranty development build at the active `VPL` database. A test rollout requires all of the following to be proven before running DbMigrator or starting Web:

- a separate SQL database name and connection string, such as `VPureLux_Test`;
- a separate systemd service/environment file and non-production HTTP port;
- `CustomerCare__IsEnabled=false` and `CustomerCare__IsSalesIntakeEnabled=false` during migration/smoke setup;
- an explicit approved test go-live instant before enabling intake;
- no automatic historical backfill.

Never reuse `/etc/vpurelux/vpurelux.env` or replace `/opt/vpurelux/app` for a test deployment unless those targets are first proven to be non-production. Prefer `/etc/vpurelux/vpurelux-test.env`, `/opt/vpurelux-test/releases/...`, and `vpurelux-web-test.service`.

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
