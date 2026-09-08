# Sales Adjustment UAT 001

## Decision

**UAT PASS - READY FOR RELEASE REVIEW.** Four independent confirmed Sales orders were exercised through the authenticated Razor UI against test catalog `VPL`. UI outcomes and read-only Sales/Inventory reconciliation agree in all four cases.

## Runtime And Safety

- Trusted starting baseline: `8458af8`; adjustment foundation includes `0b20eeb`.
- Source finally tested: `f92899b` after the focused cross-handler validation repair.
- Runtime: local Web at `https://localhost:44326`, environment `UAT`, local Redis.
- Every mutating phase was preceded by read-only `DB_NAME()` proof returning exactly `VPL`.
- Fixture prefix: `UATADJ_20260908_141921`.
- Existing UAT-prefixed non-machine products, BOMs, warehouse, lots, and stock were reused. Orders were created/confirmed and adjusted only through normal browser/application flows.
- Production `VPureLux`, VPS, migration, DbMigrator, deployment, push, direct SQL DML, existing non-UAT business records, and GrossPosted/NetPaid were not touched.

## Focused Defect Found And Fixed

The first price-only submit exposed an unrelated ModelState error named `Reason`. Razor Pages flattened the required inherited `StartInput.Reason` validation entry even though the Confirm form does not post the Start form. This prevented `SubmitRevisionAsync` from being called and left all effective data unchanged.

`AdjustModel.OnPostConfirmAsync` now removes only the Start input keys, including the observed flattened `Reason`, before validating the posted adjustment lines. Start-handler validation remains intact. No application service, delta engine, database model, query, migration, or JavaScript changed. Focused `Sales_Adjust_Page` tests passed 4/4 after the final repair.

Local repair commits:

- `1bffafa` - initial handler-validation isolation attempt.
- `ad51e9a` - nullable binding diagnostic refinement.
- `f92899b` - final exact flattened-key repair and regression.

## Browser Cases

### Case 1 - Price Only: PASS

- Order: `SO-202609-000010` / `7b3540ad-73fe-9cac-6374-3a239223ce77`.
- Applied revision: `45186015-2e82-01aa-261f-3a2392252b52`.
- Quantity remained `1`; unit price and effective total changed from `100000` to `110000`.
- Revision status is Applied; Details shows revenue `110000`, cost `100000`, profit `10000`.
- No revision Inventory transaction exists. Original allocation `1` on lot `DC7C3DE7-A60A-542B-B94D-3A235A19BD1F` is unchanged.
- MAT_A balance and lot remained `186`; inventory value remained `18,600,000` across the adjustment.

### Case 2 - Quantity Increase: PASS

- Order: `SO-202609-000007` / `ea87c282-567c-33c4-5f26-3a23921603d7`.
- Applied revision: `0fcf9b53-6033-d1be-9b80-3a23922dc287`.
- Effective quantity changed from `1` to `3`; Details shows total/cost `300000`.
- Original issue/allocation stayed at `1`. New issue transaction `2f543952-f374-bb9b-c6c9-3a23922de364` contains exactly MAT_A quantity `2` and one new FIFO allocation quantity `2`.
- MAT_A balance and lot changed only from `186` to `184`.
- No duplicate revision issue was observed. Existing focused same-key replay regression remains PASS.

### Case 3 - Add Product: PASS

- Order: `SO-202609-000008` / `82fb183b-c999-f691-31d0-3a239216bebf`.
- Applied revision: `e07799f1-ac9b-93c2-f477-3a23922ec701`.
- Original Product A line stayed quantity `1`, price `100000`, with original issue/allocation unchanged.
- Product B was added at quantity `2`, price `150000`; Details shows both effective lines and total/cost `400000`.
- New issue transaction `b8a7ca7d-b5ae-8169-26ca-3a23922f2c55` contains exactly MAT_B quantity `2`.
- MAT_A remained `184`; MAT_B balance/lot changed only from `198` to `196`, and value from `29,700,000` to `29,400,000`.

### Case 4 - Decrease Waiting Warehouse: PASS

- Order: `SO-202609-000009` / `12f97c6a-f4d8-1255-3bd9-3a2392175069`.
- Pending revision: `c48c8e06-9d2e-2894-3254-3a2392302df9`.
- Submitted quantity changed from `3` to `2`; revision remains Draft/pending with `AppliedAt = NULL`.
- UI explicitly says one reduced/changed line requires Warehouse receipt confirmation before apply and labels the impact `Nhận lại` quantity `1`; no final-success wording appears.
- Effective Sales line remains quantity `3`, total/cost `300000`.
- No issue or reversal transaction exists. Original allocation remains quantity `3`; MAT_A balance/lot remains `184`, value `18,400,000`.
- Warehouse confirmation was deliberately not performed.

## UI, Logs, And Verification

- One obvious primary command is shown: `Xác nhận điều chỉnh`; no operator Save -> Apply workflow or technical Draft/Revision terminology is required.
- Current visible values were the values processed. Details immediately showed effective values for Cases 1-3; Case 4 remained on Adjust with clear Warehouse-pending semantics.
- Browser AX screenshots before/after each action are retained in the Codex task transcript. No relevant JavaScript console error occurred on the final runtime; the only historical console entries are SignalR disconnects caused by deliberate local app restarts.
- All adjustment POSTs on the final runtime returned HTTP 302; resulting Details/Adjust pages returned HTTP 200. No HTTP 500 or final-runtime adjustment validation failure occurred.
- Three terse `An error occurred using a transaction` log entries follow SignalR chat disconnects during page navigation; they did not belong to the adjustment POSTs, which completed 302, and reconciled business data is consistent.
- Final `dotnet build VPureLux.slnx -c Release --no-restore -m:2`: PASS, 0 errors, 4 pre-existing warnings (two nullable seed warnings, one Scriban advisory, one Microsoft.NET.Test.Sdk entry-point warning).
- Focused final Web PageModel tests: 4 passed, 0 failed.
- Prior accepted focused EF adjustment evidence: 8 passed, including stale-draft authority, positive delta/replay, add/remove isolation, factual reversal, Warehouse wait, and late Inventory/CustomerCare rollback.
- `git diff --check`: PASS. No JS changed; no migration/model change was introduced.

## Remaining Risk

Production has not been tested or changed. Release review must inspect the three local validation-fix commits and this VPL evidence before deciding whether to publish/deploy. The known combined Web testhost leak and GrossPosted/NetPaid issue remain separate tasks.
