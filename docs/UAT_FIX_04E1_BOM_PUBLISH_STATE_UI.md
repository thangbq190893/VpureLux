# UAT Fix 04E.1 - BOM Publish State UI

## Issue

UAT Snapshot 04A Pass 2 reported `BOM-D5` as PARTIAL/MEDIUM. The publish button was clicked, but the UI did not make the published result clear enough for the tester to verify whether the BOM version was actually published and whether the current version changed.

## UAT Evidence

- Module: BOM
- Scenario: publish BOM
- Expected: after publish, the user clearly sees publish success, version status `Đã công bố`, current version update, and friendly conflict/error handling.
- Actual: publish state/result was unclear in the UI.
- Reference: `docs/UAT_SNAPSHOT_04A_PASS2_FULL_E2E_TEST.md`

## Root Cause

The publish PageModel already called the correct application service and redirected to the product history page after success. Business-rule errors were also localized. The gap was presentation: the reloaded page relied on plain text status and a JavaScript notification hook, with no visible success alert, no explicit current-version marker in the version history table, and no strong published-state marker on Details or the BOM landing current-version cell.

## Fix

- Added a visible success alert on the BOM product history page after publish/archive TempData messages.
- Added status badges for Draft/Published/Archived in BOM product history.
- Added an explicit `Phiên bản hiện tại` badge for the current published version in product history.
- Added a published/current badge on BOM Details.
- Added a published badge beside the BOM landing page current-version link.
- Kept the existing publish action, redirect/reload behavior, and app-service business rules unchanged.

## Success-State Behavior

After successful publish:

- the user sees `Đã công bố phiên bản định mức.`
- the page redirects/reloads with fresh state
- the published row shows `Đã công bố`
- the current published row shows `Phiên bản hiện tại`
- Details shows `Đã công bố`
- the BOM landing page current-version link points to the newly published version and shows it is published
- the publish button is no longer shown for the published version

## Conflict/Error Behavior

If publishing violates an existing BOM rule, such as trying to publish a second active/current BOM for the same product:

- the page stays on the product history page
- the localized friendly business message is shown
- raw `BusinessException` text and raw error codes are not shown
- the draft remains draft

## Intentionally Not Changed

- No BOM domain-rule changes.
- No database schema or migration changes.
- No BOM versioning semantic changes.
- No change to Sales, Inventory, or Pricing behavior.
- No change to create/edit/details/history mechanics beyond publish-state display.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed with existing warnings.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom" -m:1` - passed, 28 tests.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Bom" -m:1` - passed, 25 tests.
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Bom" -m:1` - no matching tests.
- `git diff --check` - passed, with line-ending normalization warnings only.
- `git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md` - returned existing audit/evidence references and prior fix-doc terminology-check text only; this fix did not introduce user-facing `Linh kiện` wording.

## Manual Smoke Checklist

Deferred/not run in this code pass:

- Open BOM product history for a product with a draft BOM.
- Click publish and confirm.
- Verify visible success alert.
- Verify history row shows `Đã công bố` and `Phiên bản hiện tại`.
- Verify Details shows `Đã công bố`.
- Verify BOM landing page current version points to the published version.
- Try publishing a second draft and confirm friendly conflict message.
