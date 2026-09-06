# S-003 Atomic Service Completion

Baseline: `bd8be2a533bb69bc7973497c7cca07bf244314a0`, branch `codex/warranty-release-review`.
Decision: S-003 COMPLETE. Implementation: `18e9f02a279709ca01018f06933aec12de64cf12` (`feat(service): add atomic service completion`), local only.
Scope: local implementation and isolated tests only. No VPL/production connection, migration execution, deployment, push or S-004.

## Business Facts

- Only InProgress completes. Draft/Confirmed/Cancelled reject. Completed allows only exact canonical replay.
- The command includes all order line IDs exactly once, integer actual quantities from zero through planned quantity, a required completion instant, expected concurrency stamp and idempotency key. Missing, duplicate, foreign, negative or excessive lines reject.
- Zero is unperformed: no inventory allocation, replacement event or line revenue; nullable actual line cost stays null. A machine-level ServiceCompleted event still records the completed visit, including labor-only/all-zero/unpositioned visits.
- Planning code/name/unit/price/quantity and optional labor standard-cost snapshots are never refreshed from Catalog or Work. Deactivating a Component after planning does not erase the actual work or prevent consuming its existing inventory-enabled StockItem; it prevents a new replacement cycle.
- Performed Material cost is the sum of actual FIFO allocation quantity times original lot unit cost. InventoryTransactionLineId links each material fact to its allocations.
- Performed Labor uses its planning StandardCostSnapshot times actual quantity. Null stays unknown; zero stays known zero. ActualCostAmount/ActualProfitAmount on the order are nullable and authoritative for completeness. Existing numeric TotalCostAmount/TotalProfitAmount contain the known subtotal/provisional margin when labor cost is missing; future S-005 must not present them as final profit without checking the nullable facts.
- A positioned replacement must refer to this asset. One performed replacement line per position per command avoids two competing successor cycles; multiple units belong on that line. Repeated Components on separate positions or unpositioned lines remain supported.
- A replacement must not move an existing factual baseline backwards. An earlier CompletedAt rejects the whole completion rather than overwriting newer machine history.

## Transaction And Coordination

`ServiceOrderOperationCoordinator` is the common boundary for Update, Confirm, Start, Cancel and Complete. Creation is the initial unique insert, with no existing order to coordinate. Future S-004 mutations must reuse this boundary.

Completion order:

1. Acquire `VPureLux:ServiceOrder:{id:N}`.
2. Resolve immutable CustomerAssetId in a short read-only UoW, which ends before the business transaction.
3. Acquire `VPureLux:CustomerAsset:{id:N}`.
4. Open a new transactional UoW; reload the order, validate version/state/command and current asset/position identity.
5. Batch-read stock, lots and balances. Reuse InventoryManager.AllocateFifo. Flush stock writes inside this transaction.
6. Call the CustomerCare-owned integration; write actual events, close relevant old reminders, create eligible successors and update factual baselines.
7. Record Service actual facts and Completed state. Save and complete the UoW. Release locks only after commit or rollback/disposal.

`CustomerAssetOperationCoordinator` also coordinates installation, external-asset edits, reminder Complete/Skip/Reschedule, and asset suspension. Installation follows existing SalesOrder -> CustomerAsset ordering. No CustomerAsset path acquires a ServiceOrder lock; no Service completion acquires a SalesOrder lock. Existing Warranty automatic UoWs retain their asset lease through disposal, including error paths.

Inventory keeps current lot/balance concurrency tokens. No replacement FIFO implementation and no Sales business-rule changes. Optimistic conflicts surface as SERVICE_019; an operator reloads/retries with fresh state. Real SQL Server/Redis isolation/deadlock/transient-error behavior remains an explicit S-006 rehearsal requirement, not something SQLite proves.

## Idempotency

`ServiceCompletionCommand` hashes a versioned canonical string with SHA-256:

- Order ID in N format;
- UTC ticks of CompletedAt (equivalent offsets represent the same instant);
- all LineId/ActualQuantity pairs sorted by LineId with invariant numeric formatting.

The expected version is an optimistic precondition, not a business fact in the replay hash. The key is the replay identity, separate from the hash.

Same key plus same hash returns the persisted completion result, including timestamp, stock transaction ID, financial facts and ordered actual quantities, without new business writes. Same key plus changed facts returns SERVICE_023. Different key on a completed order rejects. Keys also retain the existing database unique constraint; cross-order collisions are translated. No hashes are inferred for historical orders with null hashes.

## FIFO And Queries

- `InventoryTransactionType.ServiceIssue = 6`; values 1 through 5 are unchanged. ReferenceType is ServiceOrder and ReferenceId is the order ID; the Inventory ledger labels and links the source.
- Component IDs, StockItems, available lots and balances are loaded in bounded set queries. Lots are grouped in memory by StockItem and share tracked quantity mutations across repeated lines.
- FIFO order remains ReceivedAt -> CreationTime -> Id, skipping depleted lots. There are no repository reads inside the Service material-allocation loop or the CustomerCare replacement loop.
- Asset/position validation reads and CustomerCare reads are bounded, not N+1. Existing Catalog/Work snapshots are not queried to rebuild historical values. Batch balance loading extends the current repository without changing existing Sales callers.
- A shortage reports order ID/number, line, material code, warehouse, requested and available quantity. Available is bounded by both lot availability and balance quantity. No partial Service completion is allowed.

## CustomerCare Ownership

`ICustomerCareServiceCompletion` accepts bounded facts: ServiceOrderId, AssetId, CompletedAt, operator ID and the explicitly performed positioned materials. Service only reads asset/position identity for validation; it does not mutate Warranty repositories.

CustomerCare owns all maintenance events, baseline changes and reminders:

- One ServiceCompleted event per order, even with no positioned replacement.
- One Replacement event per performed positioned Material. SourceId is the Service order; ServiceOrderLineId is the exact line. No fabricated replacement for labor or quantity zero.
- Close only pending reminders matching this machine, this position and the replaced Component. CompletionEventId links old reminders to the actual event. Unrelated positions/machines/Components are not updated, including their modification metadata.
- Never map, remap or reactivate a position from the old Service line. Inactive/unmapped/remapped positions retain their authoritative state and do not create successors.
- Active mapped MissingBaseline is the explicit exception: an actual replacement establishes the first factual baseline. This is not configuration-driven backfill.
- A new successor requires the matching active/missing-baseline position, current enabled policy and active Component. It snapshots current cycle/warning days and starts at actual CompletedAt. Old policy snapshots remain unchanged. Absent/disabled policy or inactive Component still permits historical completion, but no successor. Enabling later does not backfill.

When Service.IsEnabled is true, direct Warranty CompleteReminder rejects with SERVICE_025 on the server. With Service disabled, W-008's accepted manual replacement behavior remains available. Skip, reschedule and suspension are not indiscriminately blocked. Payment remains a separate future S-004 workflow, not part of this completion transaction.

## UI And Permissions

- Service.Default and Service.Complete protect completion. Feature-disabled requests reject even with permission.
- InProgress Details opens an ABP ModalManager form with planned snapshots, Material/Labor, position, actual quantity, completion time and a computed actual-price summary.
- POST uses antiforgery. Quantity is integer. The HTML datetime-local value is parsed with an exact invariant format and explicit UTC+07 offset; API input is DateTimeOffset. Invalid values return validation/business errors, not a generic 500.
- The modal uses a compact stacked layout on phones and tabular layout on desktop. Completed Details shows actual quantities/amounts instead of labeling planning totals as actual work.

## Schema And Legacy Safety

Migration: `20260906200825_AddServiceCompletionFacts`.

Seven nullable additions only: order CompletionCommandHash/ActualCostAmount/ActualProfitAmount; line ActualCostAmount/InventoryTransactionLineId; reminder CompletionEventId; event ServiceOrderLineId. No Sales schema changes, table recreation, data SQL, defaults fabricating old facts or backfill. Existing rows remain null/unknown. The migration was generated, reviewed and not applied. Offline model-drift check passed against a deliberately unusable loopback connection.

## Verification

- Domain Service/Inventory: 33/33; Application Service/CustomerCare: 6/6.
- EF combined Service/Inventory/Warranty/CustomerCare/Sales: 203/203. Service completion tests explicitly restore the real UnitOfWorkManager because the shared legacy EF test module otherwise disables every transaction.
- Web Service: 24/24; focused Warranty Web: 13/13. No claim of a combined/full Web-suite pass.
- Real isolated SQLite rollback covers shortage after an earlier allocation and an injected failure after stock and CustomerCare writes, preserving InProgress, lots/balances, old reminders and absence of new issue/allocations/events/successors.
- Barrier-based tests cover same-key replay, different keys, Complete versus Cancel, two services on the same asset/position, and the final available stock unit. A stale writer on the shared Inventory lot repository cannot double-allocate after Service completes.
- SQL Server/Redis multi-process concurrency, especially different assets contending through Service versus full Sales/Inventory posting, remains S-006. SQLite stale-writer/barrier coverage is narrower than that live proof.
- Browser review uses a local proxy over the existing SQLite/in-memory Web test fixture, not the normal configured Web runtime. No external database or cache is used. Local artifacts are under `artifacts/s003-completion-browser/` and are not committed. Final responsive review and final post-review verification are recorded in AGENT_TASKS.md.
- Final desktop/mobile review passed at 1440x960 and 390x844 after replacing horizontal modal scrolling on phones with stacked rows. No page overflow or JavaScript errors. Actual summary changed from 300000 to 150000, browser submit returned 204, and the reloaded Completed Details displayed actual quantities/amounts with all mutation actions removed. The temporary test host was stopped.
- Final Release solution build: 0 errors; pre-existing Scriban NU1903 and test-entrypoint CS7022 warnings. Offline model drift NONE, JavaScript syntax and staged diff checks PASS.

## Next Boundary

Do not deploy or enable Service based on this source commit alone. S-004 remains separate; S-005 must preserve unknown-cost semantics; S-006 owns live schema/race/reconciliation/rollout gates. Production remains the frozen Sales V1 release.
