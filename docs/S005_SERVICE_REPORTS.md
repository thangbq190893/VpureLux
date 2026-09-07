# S-005 Service And Consolidated Reports

Date: 2026-09-07 (Asia/Saigon)
Baseline: `codex/warranty-release-review` / `3725ec7cd616ccb08503a307502161f24f1ac0eb`
Implementation source: `e40aed2e9b8e1072e3958d9a47ae52d780fd0592` (`feat(service): add service and consolidated reports`, 25 files).
Decision: S-005 COMPLETE locally. S-006 READY, unclaimed.
Scope: local reporting implementation on accepted Sales V1 and S-001 through S-004. No external database, migration application, deployment or push.

## Recognition And Historical Facts

- Service recognition rows require `Completed` and a persisted `CompletedAt`. Draft, Confirmed, InProgress and Cancelled contribute no recognized revenue.
- Revenue is persisted `ServiceOrder.TotalRevenueAmount`, not planned charges, receipts, refunds or current catalog prices.
- Only performed lines (`ActualQuantity > 0`) contribute costs. Material uses actual FIFO `ActualCostAmount`. For pre-S003 completions without `CompletionCommandHash`, the historical material `CostAmountSnapshot` is retained as the factual FIFO fallback. No historical labor zero is inferred from that fallback.
- Labor `ActualCostAmount = null` is unknown; zero is known zero. A missing performed material cost also marks cost incomplete.
- `TotalKnownCost = known material cost + known labor cost`. `LaborCost` is null when any performed labor cost is unknown. `LaborCostKnown` states that distinction explicitly.
- `CostIncomplete = true` whenever a performed cost is unknown. In that case `Profit = null`; the UI says "Chua xac dinh" (localized with Vietnamese accents), never a definitive inflated profit. With complete costs, `Profit = Revenue - MaterialCost - LaborCost`.
- Summary profit is also null if any filtered document has incomplete costs. The summary exposes known material/labor/total costs and the number of incomplete documents. It does not sum the displayed page or pretend that known costs are complete costs.
- Customer, machine, order and cost/revenue facts come from persisted snapshots. There are no joins to current Work, Component, BOM, price or replacement-policy configuration to recalculate history.
- New S003 completion instants are UTC and become UTC+07 business wall times for display/filtering. Pre-S003 completion wall times stay unshifted. `DocumentDate` is already the recognition wall time, so JavaScript does not shift it again.

## Settlement Is Not Revenue

The report reuses `ServiceMoneySummary.Projection` through the existing S004 SQL summary query; only that query's visibility changed from private to internal.

```text
GrossPosted   = SUM(ServicePayment.Amount WHERE Status = Posted)
GrossRefunded = SUM(ServiceRefund.Amount)
NetPaid       = max(GrossPosted - GrossRefunded, 0)
Receivable    = max(Completed ActualTotal - NetPaid, 0)
CustomerCredit = RefundDue = max(NetPaid - Completed ActualTotal, 0)
```

`HasInconsistentLedger` identifies a legacy ledger whose refunds exceed posted receipts. The UI flags the net settlement instead of presenting it as healthy. No record is repaired or backfilled.

These columns are **current settlement of documents recognized in the selected period**, not receipts/refunds that occurred during that period. A later partial refund changes settlement, not historical recognized revenue or cost. Payments never create extra recognition rows.

Cancelled Service orders are absent from recognition rows. Their outstanding refund obligations remain available through S004 order/customer money summaries and Details; no money is lost from those projections.

## Sales V1 Compatibility And Explicit Discrepancy

- Sales recognition requires Confirmed status and ConfirmedAt, and aggregates effective Product lines only. Superseded and cancelled lines/orders do not inflate totals.
- Revenue, cost and profit use current persisted effective-line amounts. Existing Sales recognition-date convention is retained: filter raw ConfirmedAt, as the accepted Sales reports do. S005 does not silently re-time historical Sales.
- Existing Sales reports, stored procedures, payment APIs and state transitions are unchanged.
- Source inspection found that `GetPostedPaidAmountsAsync` aggregates posted payments without subtracting factual refunds; some current Sales API fields name that value `NetPaid`. Blind reuse would reproduce the historical gross-versus-net defect identified by the Service audit.
- The new read model therefore reads the existing posted-payment and factual Sales-refund tables in separate grouped joins. It returns explicit GrossPosted, GrossRefunded and factual net/receivable/credit. It does not change any Sales write or refund behavior. Fixing the misleading existing API/projection is a separate Sales task.
- Automated reconciliation uses real confirmed, revised, returned, cancelled and refunded Sales through existing application workflows, plus completed/refunded Service. Accepted Sales revenue 10,000 + Service revenue 3,000 = consolidated 13,000; receipts and refunds create no extra revenue.
- The user's separate Sales post-confirm adjustment defect remains uninvestigated and unfixed in S005. This report regression is not a claim that that separate defect has been resolved.

## Architecture And Query Plan

- Contracts: `Application.Contracts/Reports/BusinessRevenueDtos.cs`, historical Source values Sales=1 and Service=2.
- Application: `Reports/BusinessRevenueAppService`, four read endpoints (Service/consolidated list and summary), authorization, date/source validation and response masking.
- EF: `Reports/EfCoreBusinessRevenueReadRepository`, normalized SQL projections with `Concat`/UNION ALL for mixed sources. Sales/Service domain models remain separate.
- Count, source/date/search filtering, whitelisted sorting, Skip/Take and summary aggregation all execute in the database. No load-all-and-merge, per-row lookup or top-N substitute.
- Whitelisted sort fields: documentDate, documentNo, customerName, revenue, totalKnownCost and profit; ascending/descending. Stable ties use recognition date, source and document ID. Unsupported sorts fall back to date descending.
- Dates use `[FromDate, ToDate + 1 day)`, with invalid/reversed/extreme ranges rejected. Default is current UTC+07 month through today. Search is bounded to 128 characters and includes snapshot document/customer/asset identifiers and names.
- SQL Server provider translation tests exercise all three source scopes, mixed UNION ALL, grouped totals and OFFSET/FETCH without opening a connection. Live execution plans/performance remain S006 work, not proven by SQLite or ToQueryString.
- No migration, index, stored procedure, schema, DML, historical backfill or denormalized balance was added.

## Permission Boundary

Recovered historical permission names, without renaming persisted definitions:

| Surface | Required access |
| --- | --- |
| Service report | Reports.Service.View and enabled Service feature |
| Consolidated report | Reports.Consolidated.View plus selected underlying source report permissions |
| Mixed/All sources | Both Reports.Sales.View and Reports.Service.View; Service enabled |
| Sales-only consolidated | Reports.Sales.View; allowed even when Service disabled |
| Service costs | Service.ViewCost |
| Service profit | Service.ViewCost and Service.ViewProfit |
| Sales costs | Sales.ViewCost and Reports.Profit.View |
| Sales profit | Above cost access and Sales.ViewProfit |

Denied cost values/flags are null in the server response. Denied profit is null. Mixed summaries require the corresponding permission for every selected source; they never expose a misleading partial cost/profit total. Sorting by denied cost/profit is disabled server-side to avoid a comparison side channel. Direct document links additionally require the operational source View permission.

Cost visibility plus revenue naturally allows arithmetic inference of profit; the existing separate permissions govern direct field exposure, not an invented confidentiality model that could prevent arithmetic. No new role architecture is introduced.

The common authorization **test fake** was corrected to deny the matching `PermissionRequirement`, instead of denying every MVC policy whenever any field-level permission was denied. This enables a realistic HTTP 200 report with masked fields. Production authorization code is unchanged; actual deny/allow Web and EF tests cover the boundaries, with focused Warranty regression on the shared fake.

Permission seed declarations include the restored permissions, but no seed or DbMigrator was executed. External permission rollout/role acceptance belongs to an explicitly authorized S006 run. Report menu entries are gated with Service; a directly authorized Sales-only consolidated request is still supported when Service is disabled.

## Operator UI

- Reports / Service revenue and Reports / Consolidated revenue are additive. Existing Sales revenue/profit reports remain independent.
- Thin Razor PageModels call the report AppService. Shared page partial and JavaScript implement date/search filters, source selection and ABP server-side DataTables.
- The Service summary displays revenue once, document count, known material/labor/total costs, incomplete-cost count and profit. Consolidated summary displays separate Sales/Service revenue and their total.
- Unsupported sorts are not advertised. Text renderers encode customer/machine/document facts; document IDs are URL-encoded. Currency uses vi-VN formatting. Unknown cost is not formatted as zero.
- LeptonX owns the source dropdown. Browser verification interacts with its visible combobox/options, not the hidden native select. No native prompt/alert/confirm or new mutation form was added.
- Preserves the historical minifier correction `const summary = () => ...`; asynchronous summary responses are versioned to prevent an older request overwriting the latest filter totals.
- No export was added: a new bounded, permission-parity export is deferred rather than expanding this task.

## Verification Evidence

Isolated SQLite/in-memory fixtures only; offline dummy SQL Server connection is `127.0.0.1,1 / S005_OFFLINE_ONLY`. Normal Web configuration was never started against VPL or production.

| Gate | Result |
| --- | --- |
| Release solution build, --no-restore -m:2 | PASS, 0 errors; existing Scriban NU1903 and Web test entry-point CS7022 warnings |
| Domain Service | 39/39 PASS |
| Application Service/Reports | 14/14 PASS |
| EF Service/Reports/Sales/Inventory/Warranty/CustomerCare | 236/236 PASS |
| Web S005 plus S004 money (final post-polish) | 6/6 PASS: 3 new report cases and 3 existing money HTTP cases |
| Web existing Sales ReportsPagesTests | 21/21 PASS |
| Web Warranty | 13/13 PASS |
| EF pending model changes (offline) | NONE |
| node --check / actual NUglify | PASS |
| git diff --check | PASS |

TRX evidence is local/ignored under `artifacts/s005/`; tests are source-controlled. Domain/EF fixtures cover planned-vs-actual/advance/refund boundaries, historical config immutability, labor null/zero/known cost, unperformed cost exclusion, legacy FIFO fallback, UTC+07 midnight and last tick, real page 2, source/search/sort, permission masking and mixed Sales reconciliation. There are 10 new EF S005 cases, including three SQL-provider translation cases.

Initial failed runs are retained, not counted as pass: EF fixture omitted a catalog concurrency stamp; Application validation used a different object's ValidationContext; Web field-deny exposed the overbroad test fake. Those test-only issues were corrected and the above runs are green. Browser first attempt targeted the hidden native select, then passed using the actual LeptonX dropdown.

Browser fixture: temporary TestServer-to-loopback proxy, seeded 13 completed Service orders with alternating known/unknown labor cost and hostile machine-name text. Chrome/Playwright at 1440x960 and 390x844 verifies both pages, actual page 2 (3 rows), search and source changes, safe text/no XSS, vi-VN currency, and no document horizontal overflow. Local screenshots/scripts are under `artifacts/s005-browser/`. Duplicate navigation entries in this testhost are pre-existing fixture behavior, not a new production menu change.

Final post-polish browser rerun: PASS on both viewports and both routes, including access to the financial columns through horizontal table scrolling. Screenshots of the summary, rows and settlement columns were inspected. The 6/6 report/money rerun passed after the final summary-label edit. Total focused regression: 329 passed, 0 failed (39 Domain + 14 Application + 236 EF + 40 Web); earlier reruns are not added twice. The temporary proxy was stopped. The isolated browser harness build also reported existing transitive package advisories; no package upgrade was made in this report-only task.

## Reproduction And Handoff

```powershell
dotnet build VPureLux.slnx -c Release --no-restore -m:2
dotnet test test/VPureLux.Domain.Tests -c Release --no-build --filter FullyQualifiedName~Service -m:1
dotnet test test/VPureLux.Application.Tests -c Release --no-build --filter "FullyQualifiedName~Service|FullyQualifiedName~Reports" -m:1
dotnet test test/VPureLux.EntityFrameworkCore.Tests -c Release --no-build --filter "FullyQualifiedName~Service|FullyQualifiedName~Reports|FullyQualifiedName~Sales|FullyQualifiedName~Inventory|FullyQualifiedName~Warranty|FullyQualifiedName~CustomerCare" -m:1
$env:DOTNET_GCHeapHardLimit='0x60000000'
dotnet test test/VPureLux.Web.Tests -c Release --no-build --filter "FullyQualifiedName~S005|FullyQualifiedName~ServiceOrderWebTests.Money_" -m:1 --blame-hang-timeout 90s
dotnet test test/VPureLux.Web.Tests -c Release --no-build --filter FullyQualifiedName~ReportsPagesTests -m:1 --blame-hang-timeout 90s
dotnet test test/VPureLux.Web.Tests -c Release --no-build --filter FullyQualifiedName~Warranty -m:1 --blame-hang-timeout 90s
```

Run EF drift from `src/VPureLux.EntityFrameworkCore` with an explicitly offline dummy connection, Release and --no-build. Do not run DbMigrator. Split Web groups intentionally avoid the known combined testhost leak; no combined full-Web pass is claimed.

S006 owns authorized real SQL Server/Redis rehearsal, legacy-schema/fingerprint reconciliation, performance/permissions/operator acceptance and eventual rollout. S005 does not authorize those actions. Cash-period reporting, accounting ledger, revenue reversals/credit notes, export and the separate Sales defects remain out of scope. Two user-owned documents (`docs/html.txt` and the Sales review DOCX) must stay unstaged.
