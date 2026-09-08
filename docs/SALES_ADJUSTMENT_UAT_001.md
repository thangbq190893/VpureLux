# Sales Adjustment UAT 001

## Checkpoint

- TRUSTED_BASELINE: `8458af8`
- CURRENT_HEAD: `8458af8` at task claim
- OBJECTIVE: Autonomously complete four-case browser UAT against VPL for confirmed Sales adjustments.
- STATUS: `IN_PROGRESS`
- Runtime URL: `https://localhost:44326`
- Fixture prefix: `UATADJ_20260908_141921`
- Database guard: read-only `SELECT DB_NAME()` returned exactly `VPL` before fixture work.

## Invariants

- Current form values are authoritative; stale Draft values cannot silently win.
- Price-only adjustment does not change Inventory.
- Quantity increase issues only the positive delta.
- Quantity decrease waits for Warehouse and does not change effective Sales or Inventory beforehand.
- The existing SalesOrder coordinator transaction remains authoritative for Sales, Inventory/FIFO, and CustomerCare.
- Production database, VPS, migration, DbMigrator, deployment, push, existing non-UAT business records, and GrossPosted/NetPaid are out of scope.

## Completed

- Trusted commits `eeba88a`, `0b20eeb`, `ec5ef4e`, and `8458af8` verified locally.
- VPureLux engineering preflight passed at `8458af8`; only the two protected user files are untracked.
- Adjustment Razor/PageModel/JS, AppService/coordinator, Inventory/FIFO, and focused tests inspected.
- VPL identity guard passed.

## Remaining

- Start the trusted Web source locally against the guarded VPL connection.
- Select bounded existing UAT-prefixed customer, warehouse, products, BOM, and stock facts.
- Create four independent confirmed orders through authenticated application flows.
- Execute and reconcile Cases 1-4 through the real Razor UI.
- Add focused fixes/tests only if an in-scope defect is proven.
- Record fixture IDs, screenshots, logs, database reconciliation, final validation, and close the handoff.

## Files

- `AGENT_TASKS.md`
- `docs/SALES_ADJUSTMENT_UAT_001.md`

## Risks And Blockers

- None at claim. The earlier order `SO-202609-000005` is contaminated by a Draft revision and will not be reused.

## Next Action

Launch the local Web runtime with an explicit VPL connection, authenticate as the authorized admin account, then create fresh isolated fixtures through normal business flows.
