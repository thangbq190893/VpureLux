# S-001 Service Foundation Verification

Date: 2026-09-07 (Asia/Saigon). Starting branch: `codex/warranty-release-review`.
Starting HEAD: `83fb3f50232113a03a4af2a8f85cc74a87b7cac3`.
Accepted Warranty source: `d1e8b5684d21eca3ee5586fe75913b60e24de190`.
Service source audit: `6ef1def` / `docs/SERVICE_INVENTORY_AUDIT.md`.
Decision: S-001 COMPLETE. Implementation commit: `babc96fc5ecba242e3f23d0612c3a46df3916dc3`.

## Scope And Runtime

- Only Work Catalog is executable: `Service -> Danh muc cong viec`, one server-paged DataTable and one ABP create/edit modal.
- `Service:IsEnabled` defaults to false. `Service__IsEnabled` is the corresponding environment override. Application APIs check the flag; pages and menu also respect it. No application JSON or environment on VPL/production was enabled by this task.
- Existing permission names `VPureLux.Service`, `.View`, `.ManageWorks` are retained. Root/View protect reads, ManageWorks additionally protects mutations. Admin seed declarations are prepared but no DbMigrator or external database seed was executed.
- Work fields: Code (required, trimmed/uppercase, immutable after creation), Name, Unit, DefaultPrice, nullable StandardCost, Status, Note, ABP audit fields and concurrency stamp. Stale edits are rejected.
- Codes are explicitly entered, as in the historical Work Catalog. No number generator or max-string scan is restored; future automatic numbering must use current BusinessCodeGenerator.
- Unit is required for new/explicitly edited work, but nullable in storage for existing records whose unit is unknown. No guessed unit/default is written into legacy rows. StandardCost null means unknown, zero means known zero; negative monetary values are rejected.
- Updating Work never queries/updates Service lines or other business modules. A persisted historical line fixture retains its code/name/unit/price/cost after a Work edit. No historical cost is inferred from current templates, including old non-null line cost fields.
- ServiceOrder, owned ServiceOrderLine, and ServicePayment are persistence-only shells with protected constructors and private setters. No order creation/confirm/start/complete/cancel, payment posting/void, Inventory ServiceIssue, reminder integration or reporting API exists in S-001. Inventory enum values 1..5 and Sales/Warranty behavior are untouched.

## Migration Provenance

Inspected `bcc1b36cfd4edb5f2e2ac2df24400048bdacbfac` and `220d41c3817f9bd2b5bc1d5c79805face82c1132`. The original migration pair is identical between these commits and was restored from the latter under its original identity:

`20260824113235_AddServiceModule`

Git blob identities after restoration match historical blobs exactly:

| File | Git blob |
|---|---|
| AddServiceModule.cs | `7fa98618d92dfc8e9c8bcd86bad23e0bcdd738e6` |
| AddServiceModule.Designer.cs | `0fb8667bb2eb2c3e2f38569aefb9201ffa93c454` |

Up, Down and target-model semantics are unchanged. The original four tables are AppServiceWorks, AppServiceOrders, AppServiceOrderLines and AppServicePayments.

Only the Service blocks were merged into the current snapshot as the recovered baseline. The entire historical snapshot/DbContext was not copied. EF then generated one forward migration:

`20260906174229_AddServiceWorkCatalogFields`

Its Up contains exactly two nullable AddColumn operations on AppServiceWorks: Unit nvarchar(32), StandardCost decimal(18,2). No default value, business DML, duplicate CreateTable, or core-table alteration. The timestamp is the EF-generated UTC timestamp; the local review date is 2026-09-07.

Model tests compare every pre-existing entity's full metadata in the Sales V1 target model against the forward target model, including Sales revisions/cancellation/refund/effective-line fields, CustomerCare, Warranty, Inventory and indexes. There are exactly four additional Service entity mappings. Offline `has-pending-model-changes` returns NONE.

## Offline SQL Evidence

All EF tooling used an explicit dummy connection override to `127.0.0.1,1`, database `S001_OFFLINE_ONLY`. No SQL Server connection was opened. No script was executed.

Local generated artifacts under `artifacts/s001-foundation/`:

| Script | Purpose | SHA-256 |
|---|---|---|
| empty.sql | 0 -> forward: each original Service table created exactly once | `1A7E76031AEC9D295CE52CF7DD4B9D9DCABCEF04E927CDC3EAB77669179FA001` |
| legacy.sql | original Service ID -> forward: zero Service table creates; includes intervening Sales V1 if not yet applied | `BD39BE77869B663EF9E821082ADAF3AC3005317043F67EC3F3C34CE46B9D66D1` |
| current-upgrade.sql | current Sales V1 ID -> forward: only the two new nullable Service columns | `9133A9ACC2425E333561AD488E52E3A7367E11B2E964064A75DDCFE711004417` |
| idempotent.sql | full chain: original Service table operations guarded by the original history ID | `B109B744486FFF28ED0BD355D511C2A0DFCCACC105CE3FB948BDFDD77704F24A` |

These scripts contain EF-generated migration-history inserts, not manual history repairs or business data writes. No change was made to a database's history. A legacy database already recording both the original Service migration and Sales V1 has only the forward Service migration pending in this source chain. A database without those history entries follows the restored chain normally.

Regenerate with `dotnet ef migrations script <from> 20260906174229_AddServiceWorkCatalogFields --project src/VPureLux.EntityFrameworkCore --startup-project src/VPureLux.DbMigrator --configuration Release --no-build --output <local-file>`. Add `--idempotent` for the full guarded chain. Set the dummy override first. Do not run database update or DbMigrator.

Offline inspection is not proof that every deployed schema matches its history entries. SQL Server empty/legacy rehearsal, rowversion/locking and actual legacy-row reconciliation remain S-006 work on separately authorized test targets. Never apply Down to a database containing Service history.

## Verification And Issues Found

- Release solution build and local Web publish passed with no errors. Existing Scriban NU1903 and incremental-build OpenIddict/test-entrypoint warnings remain unrelated; no dependency upgrade is included.
- Domain focused Service/Sales/Inventory: 38/38. Application Work DTO validation: 4/4. Service application execution tests live in the EF integration host, following the repository's pattern; they test runtime disablement and real intercepted permission deny/allow calls, not only attributes.
- Final post-correction results: full EF suite 215/215, including seven Service foundation/application/runtime/model/snapshot/harness tests; Service Web 5/5 (four HTTP workflow tests plus one source guard); Warranty Web 13/13. No failures or skips in these final runs. Final Release solution build: 0 errors, 2 pre-existing warnings (NU1903 and CS7022).
- Service repository tests cover persistence, null versus zero, count/search/status, supported code/name/unit/status sorting, stable tie breakers and page 2 beyond page size. Price/cost/note/action columns do not advertise unsupported sorting. No lookups or fixed-top-N lists were added.
- HTTP tests exposed and fixed two vi-VN defects before completion: RangeAttribute must parse decimal limits invariantly; HTML number values must use an invariant form binder or 123.50 becomes 12350. The new binder is local to Service Work forms, not registered globally. Valid create/edit and missing-token rejection are exercised through Razor HTTP requests.
- HTTP permission tests use a test-only request header bridge into the existing authorization matrix because TestServer does not forward the caller's AsyncLocal state. This bridge is not in application code.
- Two initial combined EF runs returned 172/173 with the same SQLite error in `Install_Vs_Open_Revision_Should_Allow_Exactly_One_Outcome`: registering SQL functions on a shared connection with an active statement. An isolated rerun passed, but that did not clear the group failure. The test module now retains an anchor to a uniquely named shared in-memory database and gives each DbContext its own native connection. A focused active-reader/context-initialization test proves the intended isolation. No Sales test assertions or business locking logic were altered. Keep the original `ef.trx` and `ef-r2.trx`; final broader regression is recorded separately.
- Full/combined Web suite is not claimed; focused groups remain bounded with a 1.5 GiB GC heap cap and 90-second hang timeout.

## Browser And Artifact Checks

Local browser review used only `VPureLux.Web.Tests.dll` with its SQLite in-memory EF module, in-memory cache/locks, disabled CustomerCare intake and explicit `VPURELUX_SERVICE_UI_REVIEW=1`. This is a test-only opt-in, not an application/VPL configuration change. The local listener on 127.0.0.1:5097 was stopped after review; all review records were disposable in-memory fixtures.

Playwright checked 1440x960 and 390x844, ABP modal open/save, a real 123.50 input, unknown-cost display versus zero, and DataTable page 2 with 24 records. Screenshots: `works-desktop.png`, `works-mobile.png`, `modal-desktop.png`, `modal-mobile.png` under the local artifact folder. Mobile modal bounds were x=8, y=8, width=374, height=826.375; horizontal scrolling is intentional for the operational table. App-level scripts and local icons loaded; external Google Fonts requests failed in the review environment and used fallback fonts. Duplicate navigation entries are inherited from the test host's existing contributor registration, not introduced into the production menu.

The local publish contains the Service script with identical source/publish SHA-256 `CEBC1CD02D6414E8F88C8026AC50DB62BD7F937429066D70DB1CAA6ACEF27955`, passes Node syntax checking, and includes Font Awesome regular/solid woff2 assets. This does not claim a production publish/deploy smoke test. Raw TRX, SQL, screenshots, browser script and publish output remain ignored/local.

## Boundaries And Next Step

No VPL/production database access, migration application, DbMigrator, production action, deployment, service restart, push or user-owned-file staging is authorized/performed. Tests create only disposable SQLite in-memory data. Production remains the accepted Sales V1 release.

S-001 is DONE and S-002 is READY after final validation. S-002 must implement operator workflow explicitly; passive mapped entities do not authorize posting or future snapshot refresh. S-003..S-006 remain HOLD, and S-002 is not claimed in this task.

## Source Manifest

The implementation commit contains exactly these 41 files; the large generated Designer files are required migration metadata, not copied application workflows:

- `src/VPureLux.Application.Contracts/Permissions/VPureLuxPermissionDefinitionProvider.cs`
- `src/VPureLux.Application.Contracts/Permissions/VPureLuxPermissions.cs`
- `src/VPureLux.Application.Contracts/Service/ServiceWorkContracts.cs`
- `src/VPureLux.Application/Permissions/VPureLuxPermissionDataSeedContributor.cs`
- `src/VPureLux.Application/Service/ServiceWorkAppService.cs`
- `src/VPureLux.Application/VPureLuxApplicationModule.cs`
- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- `src/VPureLux.Domain.Shared/Service/ServiceConsts.cs`
- `src/VPureLux.Domain.Shared/Service/ServiceEnums.cs`
- `src/VPureLux.Domain.Shared/Service/ServiceErrorCodes.cs`
- `src/VPureLux.Domain.Shared/Service/ServiceOptions.cs`
- `src/VPureLux.Domain/Service/IServiceWorkRepository.cs`
- `src/VPureLux.Domain/Service/ServiceOrder.cs`
- `src/VPureLux.Domain/Service/ServiceOrderLine.cs`
- `src/VPureLux.Domain/Service/ServicePayment.cs`
- `src/VPureLux.Domain/Service/ServiceWork.cs`
- `src/VPureLux.EntityFrameworkCore/EntityFrameworkCore/VPureLuxDbContext.cs`
- `src/VPureLux.EntityFrameworkCore/EntityFrameworkCore/VPureLuxEntityFrameworkCoreModule.cs`
- `src/VPureLux.EntityFrameworkCore/Migrations/20260824113235_AddServiceModule.cs`
- `src/VPureLux.EntityFrameworkCore/Migrations/20260824113235_AddServiceModule.Designer.cs`
- `src/VPureLux.EntityFrameworkCore/Migrations/20260906174229_AddServiceWorkCatalogFields.cs`
- `src/VPureLux.EntityFrameworkCore/Migrations/20260906174229_AddServiceWorkCatalogFields.Designer.cs`
- `src/VPureLux.EntityFrameworkCore/Migrations/VPureLuxDbContextModelSnapshot.cs`
- `src/VPureLux.EntityFrameworkCore/Service/EfCoreServiceWorkRepository.cs`
- `src/VPureLux.EntityFrameworkCore/Service/ServiceOrderConfiguration.cs`
- `src/VPureLux.EntityFrameworkCore/Service/ServicePaymentConfiguration.cs`
- `src/VPureLux.EntityFrameworkCore/Service/ServiceWorkConfiguration.cs`
- `src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs`
- `src/VPureLux.Web/Pages/Service/ServiceWorkForm.cs`
- `src/VPureLux.Web/Pages/Service/WorkModal.cshtml`
- `src/VPureLux.Web/Pages/Service/WorkModal.cshtml.cs`
- `src/VPureLux.Web/Pages/Service/Works.cshtml`
- `src/VPureLux.Web/Pages/Service/Works.cshtml.cs`
- `src/VPureLux.Web/Pages/Service/Works.js`
- `test/VPureLux.Application.Tests/Service/ServiceWorkContractTests.cs`
- `test/VPureLux.Domain.Tests/Service/ServiceWorkDomainTests.cs`
- `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Service/ServiceFoundationTests.cs`
- `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/VPureLuxEntityFrameworkCoreTestModule.cs`
- `test/VPureLux.Web.Tests/Pages/ServiceTestAuthorizationFilter.cs`
- `test/VPureLux.Web.Tests/Pages/ServiceWorkWebTests.cs`
- `test/VPureLux.Web.Tests/VPureLuxWebTestModule.cs`

Separate documentation commit: `AGENT_TASKS.md`, `docs/MODULE_MAP.md`, `docs/S001_SERVICE_FOUNDATION.md` only. Neither user-owned file is included.
