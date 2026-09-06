# W-008 Warranty Verification - 2026-09-04

## Boundary and baseline

- Status: W-008 DONE on 2026-09-06; user explicitly ACCEPTED the workflow on 2026-09-07 (Asia/Saigon), closing W-GATE. Technical evidence below is preserved as executed, not rerun for acceptance.
- Branch: `codex/warranty-release-review`; starting HEAD `5c5d1de`.
- Frozen production source: `a4717aa361e931aaa2bb09fd55d20d0efd9599c2`, annotated tag `release-2026-09-03-sales-v1`.
- Test writes: new prefixed fixtures in `VPL` only. Production `VPureLux`: no access or mutation, deployment, migration, or restart.
- W-GATE is DONE by the 2026-09-07 user decision. SERVICE-INVENTORY-AUDIT is complete; S-001 READY, not started. No Sales business expansion. Accepted W-008 fixes remain local/uncommitted at this boundary; no new deployment is implied.

## Inherited evidence (not a new execution)

- Accepted 2026-09-03 rehearsal: 14/14 legacy fingerprints matched; migration schema/procedure changes preserved business rows and report outputs. See `SALES_PRE_INSTALLATION_V1_TECHNICAL_IMPLEMENTATION.md`.
- Accepted production rollout: release `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa`, rollback `/opt/vpurelux/releases/web-20260825-111401`; authenticated read-only Sales/static smoke passed. No rerun authorized here.
- Earlier Warranty notification test/UI evidence exists in AGENT_TASKS (2026-08-24). It does not prove all current workflows or replace operator acceptance.
- Combined Web testhost has a known resource leak. Use bounded focused processes; never claim a full-suite pass from partial execution.

## Evidence audit and execution

| Area | Inherited evidence | Gap / rerun reason | Current execution |
| --- | --- | --- | --- |
| Product IsMachine / policy | Catalog forms and enabled-policy SQL tests | Current release regression | Web/EF PASS; VPL configured fixture machine + 3-month/15-day policy; default-unconfigured material does not schedule |
| Sales A: increase | Phase 2 pending reconciliation test | Replay assertion insufficient | Strengthened test PASS; VPL 1 -> 3 assets, apply twice, exactly 3 |
| Sales B: decrease | Pending-only reconciliation test | Sales V1 interaction | EF PASS; VPL 3 -> 1 pending, two Cancelled, three historical asset rows retained |
| Sales C/D: replace/remove | Dedicated Phase 2 tests | Current baseline confirmation | Both EF integration cases PASS; not repeated as live VPL product-swap/remove UAT |
| Sales E: price-only | Inventory/payment regression | Machine detail preservation | VPL machine detail/positions JSON unchanged after price-only revision |
| Sales F: cancellation | Installed-lock/cancellation tests | Intake retry after cancel | New EF test PASS; VPL cancelled revision-order retains exactly 3 Cancelled assets after multiple worker cycles, no reminder |
| Sales G: races | Existing concurrent tests | Cancel race omitted IsMachine; install could fail for unrelated validation | Added valid machine setting + shared start barrier; EF races PASS. Concurrent VPL HTTP Install/Cancel and Install/OpenRevision each returned one 200 and one 403, no 500 |
| Installed boundary | Sales installation lock tests | SQL/Redis runtime proof | VPL installed order rejects subsequent revision and cancellation with SALES_018; stored installation unchanged |
| Intake / pending / installation | Earlier Warranty workflow tests | Current runtime and snapshots | VPL four confirmed orders have zero assets while flags disabled, then four pending assets after controlled enablement; no pre-install reminder; installation/replay creates one reminder |
| Installation identity/positions | EF workflow | Real persisted operator facts | VPL serial, address, date, technician ID, and actual position stored; see fixture state and SQL inspection |
| Policy snapshots | Existing reminder snapshots remain immutable | Successor previously copied the old policy | PASS: old reminder remains 3/15; Complete on 2026-09-04 with current policy 6/21 creates successor due 2027-03-04 with warning date 2027-02-11. Disabled/deleted policy and inactive/unmapped position complete without successor; replay and re-enable do not duplicate/backfill |
| External machines / positions | Nine-position EF test | Mandatory real VPL workflow | PASS twice: no Sales source, nine positions retained, mapped/unmapped and missing baseline preserved; only 3 eligible positions scheduled; partial Core1..3 edit preserves Core4..9; duplicate-serial warning returned |
| External stock safety | Domain separation | Real counts/fingerprints | First external-only checkpoint: Inventory transactions 16, lines 17, allocations 10, lots 3, balances 3 unchanged; no stock issue from mapping |
| Reminder lifecycle / history | Broad EF external workflow test | Live actions + retry | VPL Complete/Reschedule/Skip each called twice: exactly 3 new events, old events unchanged; Suspend replay leaves zero pending reminders. This pre-fix live evidence is preserved; the next-cycle defect was subsequently resolved by the 2026-09-06 in-memory tests above |
| Warning / due / overdue | Notification aggregate test | Non-empty VPL/UI proof | Due-today is included in Warning, overdue separate; 3 initial reminders have WarningDate = DueDate - 15; first fixture bell total 3 = Warning 2 + Overdue 1 |
| Notification / UI | 2026-08-24 browser smoke | Current non-empty data | Desktop icons render; both timing links select expected filters and rows; mobile 390x844 ABP reschedule modal renders and was dismissed without saving; observed console warn/error list empty |
| Paging / modal / antiforgery | SQL/permission and Web source tests | Fresh baseline | PASS: server-side asset/reminder filter, sort, count, Skip/Take and page 2 with more rows than page size. Four authenticated Razor mutation POSTs without antiforgery token were rejected through the application's `/Error?httpStatusCode=400` path |
| Permissions | Definition and Authorize-attribute tests | Dedicated deny/allow execution required | PASS: all seven Warranty permissions deny through the server method interceptor and `IAuthorizationService`, then allow the corresponding proxy action/query. Mutation methods remain protected by server-side Authorize attributes |
| Flags / go-live / failure isolation | Domain flags + EF invalid-intake test | Pre-go-live and selected-before-Sales-change races | PASS: pre-go-live remains excluded. Intake now acquires the shared Sales order lock, reloads authoritative state in the transaction, and skips stale candidates. Deterministic select/pause/cancel and select/pause/product-replace tests create no stale asset; unchanged/replay creates exactly once |
| Sales/Inventory/BOM/reports | Accepted Sales V1 rehearsal | Focused current regression | Domain/Application/EF/Web PASS; VPL reports 200000 revenue, 20000 cost, 180000 profit; two cancelled orders excluded |
| Migration / production rollout | Accepted 2026-09-03 rehearsal/rollout | Application fix only; no schema change | Reused, not rerun. No migration created; final EF model check reports no changes. No deployment or production access |

## Findings and decisions

### W008-D01: RESOLVED - future cycles use the current policy

`WarrantyAppService.CompleteReminderAsync` now leaves the completed reminder snapshot unchanged and resolves the current policy only for the successor. With an active mapped position, active Component, and enabled 6-month/21-day policy, Complete on 2026-09-04 creates a successor due 2027-03-04 and warning on 2027-02-11. If policy is absent/disabled or the position is inactive/unmapped, Complete and its maintenance event still succeed but `NextReminderId` stays null. Re-enabling later does not backfill. Replay creates neither a second event nor a second successor.

The earlier live VPL result that used 3/15 remains preserved as evidence of the pre-fix defect; it was not rewritten. The implementation and acceptance tests use SQLite in-memory only in the 2026-09-06 close-out, so no VPL or production reminder was mutated.

### W008-D02: First fixture reused a legacy group and changed its metadata

The first fixture customer referenced existing test group `bdfdbe4c-97ca-21b3-9bc7-3a235a199ca8` (`UATSALE2_20260828_GRP`). Its full-row fingerprint changed; modification time is 2026-09-04 18:26:06, matching customer creation. The new isolated in-memory diagnostic reproduces a changed group concurrency stamp and modification time while Code/Name/Description/Status/SortOrder and creation facts stay unchanged.

Important limitation: the first baseline stored full-row hashes, not original per-column values. Therefore the in-memory diagnosis is evidence of the mechanism, not proof that every original VPL column was unchanged. The original run remains a 40/41-table full-row match with this one changed legacy row; it is NOT relabelled PASS and no metadata was restored.

The second run uses a new group and customer. All 41 pre-R2 tables retain all pre-R2 row hashes, including the previously touched group. This proves isolation for R2, not retroactive success for the first run. The fixture harness now creates its own group and defaults API calls to read-only.

### Residual non-blocking UX issue

In the earlier live race, expected SALES_015/SALES_016 arrived as HTTP 403 with a generic API message. State remained authoritative, no 500 or corruption occurred, and reload showed the committed result. Friendly localization is deferred as a separate UX improvement and is not a W-008 blocker.

## Data and reproducibility

- Original prefix: `W008_20260904`; second isolated prefix: `W008_20260904R2`.
- Database target proved from configuration and `DB_NAME()`: VPL on the configured SQL host. No production database connection was opened.
- Local runtime only: `http://localhost:5198`, Redis `127.0.0.1:6379,defaultDatabase=14`. No VPS test app/service was created. Intake was initially disabled, then enabled only after checking candidates against the new customer and the R2 start/go-live timestamp. Local host and browser tab were stopped/closed after UAT.
- R2 orders: `SO-202609-000001` (quantity/price revisions then cancelled), `000002` (installed), `000003` (cancel won race), `000004` (open revision won race, remains pending). Cancelled fixtures intentionally retain unresolved warehouse-return obligations; do not clean up or reinterpret these as production orders.
- Aggregate additions across both runs: 1 group, 2 customers, 6 Components, 2 Products/BOMs, 2 warehouses, 4 Sales orders, 9 assets, 25 positions, 8 reminders, 10 maintenance events, 1 inventory receipt lot, and 7 inventory transactions total. No new payment/refund or Service row.
- R2 inventory: receipt 100 at cost 10000; four initial issues + increase 2 - returned 2 = on-hand 96, value 960000. Cancellation itself does not invent physical returns. Original lots/balances/transactions are unchanged.
- Checked-in compact evidence: `docs/evidence/w008/original-run-reconciliation.json`, `isolated-r2-reconciliation.json`, `local-evidence-manifest.json`.
- Raw local evidence: `artifacts/w008-verification/` and `artifacts/w008-isolated-r2/` (immutable before hashes, after hashes, fixture IDs/state, reports, local Web log, TRX). Manifest records SHA-256 for key files; do not overwrite the original baseline.
- Harnesses under `docs/evidence/w008/` are test-only. API helper requires `VPL_UAT_PASSWORD` from the caller environment and explicit `-AllowFixtureWrites` for mutations. Do not rerun creation scripts blindly: use saved state and a fresh explicit prefix/baseline when another run is authorized.
- The 2026-09-06 close-out created no VPL fixture and performed no VPL write. All new D01, race, permission, paging, and antiforgery verification used isolated SQLite in-memory test hosts.

## Automated verification

| Command/group | Result |
| --- | --- |
| `dotnet build VPureLux.slnx -c Release --no-restore -m:2` | PASS, 0 errors; one existing Scriban NU1903 advisory |
| Domain suite | 102/102 |
| Application suite | 30/30 |
| EF Sales/Inventory/BOM/Warranty/Reports suite | 208/208 |
| Final W-008 focused EF matrix | 20/20, included in the 208 total |
| Warranty Web source + HTTP antiforgery | 13/13 |
| Sales/Inventory API Web regression | 6/6 |
| EF pending model changes | None; design-time check used unreachable local dummy connection, no DB operation |
| Test harness syntax | Five PowerShell scripts parse successfully |

Web groups used a 1.5 GiB managed-heap cap and 90-second hang timeout; both finished. No combined/full-Web-suite pass is claimed.

Final repository checks: `git diff --check` passes (only repository LF/CRLF conversion warnings). Branch/HEAD remain `codex/warranty-release-review` / `5c5d1de66d44a2968dff9677f9e325aed9b88b41`. Changes remain unstaged and uncommitted because push/commit was not requested. The two original user-owned untracked files are untouched. No persistent local Web process was started in this close-out.

## Execution log

- Read-only preflight completed; only two pre-existing user-owned untracked files, excluded from this task.
- Corrected stale UAT database instructions before runtime/testing; claimed W-008 only.
- Preserved the original D02 evidence, including the honest 40/41 first-run result and isolated R2 41/41 result.
- Implemented the approved D01 rule and authoritative intake revalidation under the Sales order lock; added permission, paging, and antiforgery evidence. No migration, commit, push, deployment, restart, VPL write, or production access was performed on 2026-09-06.

## Gate decision

Technical decision on 2026-09-06: W-008 DONE, READY FOR USER ACCEPTANCE = YES. W008-D01 resolved; W008-D02 accepted test-harness exception without data repair; intake race and security/UI evidence pass, with no known Severity 1/2 Warranty defect.

User decision on 2026-09-07: Warranty/CustomerCare ACCEPTED; W-GATE DONE. Service source audit completed in `SERVICE_INVENTORY_AUDIT.md`; S-001 READY but no Service implementation began. No new test run, database access, deployment, or historical-data mutation accompanied this acceptance. The frozen Sales production release/tag remains unchanged; accepted W-008 application/test changes have not been committed or deployed by the audit task.
