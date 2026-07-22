# UAT Fix 04G.1 - LeptonX Select2 Duplicate Controls

## Issue

Initial server-rendered line-editor selects could be enhanced by both LeptonX custom select handling and Select2. When LeptonX wrapped a `select.form-select` and page JavaScript later initialized Select2 on the same native select, the user could see duplicate controls for one product/material field.

## UAT Evidence

`docs/UAT_SNAPSHOT_04A_PASS2_FULL_E2E_TEST.md` lists UI consistency/dropdown coverage under 04G and marks `A-dropdown` as not tested in that pass. The attached 04G task adds later browser DOM evidence showing Select2 targets inside `.custom-select-wrapper[data-lpx-bound="true"]`, with LeptonX display/options nodes rendered before Select2 initialization.

## Root Cause

Line-editor Select2 targets were rendered with Bootstrap `form-select` classes. LeptonX treats those as native select enhancement candidates. The shared dynamic-row helper also initialized selects by looking for `select.form-select` and re-added `form-select` before Select2, so the two UI systems were not mutually exclusive.

During follow-up browser use, BOM multi-line selection exposed a second Select2 cloning issue: new BOM rows copied option HTML from the first live Select2-enhanced row. Those options could carry Select2 metadata such as `data-select2-id`, which made later row display/value state unreliable.

## Why Row Dau Duplicated But Dynamic Row Did Not

The first row is server-rendered and present during LeptonX page-load binding, so LeptonX can wrap it before our row scripts run. Dynamically added rows are cloned and prepared later by page JavaScript; prior cleanup avoided some cloned Select2 artifacts, which made dynamic rows less likely to show the same duplicate. The missing piece was defensive cleanup of LeptonX wrapper artifacts for already-rendered rows.

## Fix Strategy

- Mark Select2 targets explicitly with `data-use-select2="true"` and `js-select2`.
- Remove `form-select`, `form-select-sm`, and `form-select-lg` from Sales/BOM/Inventory line-editor Select2 target markup.
- Add `stripLeptonXSelectEnhancements` to the shared dynamic-row helper.
- Strip Select2 metadata from every descendant carrying `data-select2-id`, including cloned `<option>` nodes.
- For BOM rows created from the HTML `<template>`, use the template's clean options instead of copying mutated option HTML from the first live row.
- Before Select2 initialization, remove LeptonX data attributes and unwrap `.custom-select-wrapper[data-lpx-bound]` only when it contains an explicit Select2 target.
- Initialize Select2 from explicit markers/selectors instead of broad `select.form-select` discovery.
- Keep width/min-height through neutral CSS and Select2 `width: '100%'`.

## Affected Pages

- Sales Create line product select.
- Sales Edit add-line product select.
- BOM Create line Vật tư/component select.
- BOM Edit line Vật tư/component select.
- Inventory Receipt stock item select.
- Inventory Issue stock item select.
- Inventory Adjustment count stock item select.
- Shared dynamic row Select2 cleanup/initialization helpers.

## Intentionally Not Changed

- Domain, Application, EF, database schema, and migrations.
- Sales, BOM, Inventory posting, pricing, FIFO, costing, and versioning/business rules.
- Normal native selects such as customer, warehouse, filters, adjustment reason, and payment status.
- Row add/remove/reindex behavior except for running the same Select2/LeptonX cleanup during preparation/removal.

## Validation Behavior

Expected runtime probe after page init for every Select2 target:

```javascript
[...document.querySelectorAll('select.sales-line-product, select.js-select2, select[data-use-select2="true"]')].map(s => ({
    id: s.id,
    name: s.name,
    className: s.className,
    inLeptonXWrapper: !!s.closest('.custom-select-wrapper[data-lpx-bound]'),
    hasSelect2: !!window.jQuery && !!jQuery(s).data('select2'),
    nextIsSelect2: !!s.nextElementSibling?.classList?.contains('select2')
}))
```

Expected:

- `inLeptonXWrapper: false`.
- `hasSelect2: true` after Select2 initialization.
- Native select class does not include `form-select`, `form-select-sm`, or `form-select-lg`.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed, 2 warnings, 0 errors.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom" -m:1` - passed, 28 total.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 77 total.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 63 total.
- `git diff --check` - passed; Git reported CRLF normalization warnings only.
- Terminology grep requested by task - returned existing audit/evidence/prior fix-doc references only; this fix did not introduce user-facing legacy material wording.

## Manual Smoke Checklist

Deferred unless a browser session is run against the app:

- Sales Create: first product dropdown has one visible Select2 control; add/remove/reindex line still works.
- Sales Edit: add-line product dropdown has one visible Select2 control.
- BOM Create/Edit: first Vật tư dropdown and dynamically added dropdown each show one visible Select2 control; selecting row 2 and row 3 should update visible labels and submit all selected rows.
- Inventory Receipt/Issue/Adjustment: first stock item dropdown and dynamically added dropdown each show one visible Select2 control.
- Browser probe returns no Select2 target inside LeptonX wrapper.

## Residual Risk

Runtime LeptonX timing is best verified in a browser because server-rendered HTML tests can prove the markup contract but cannot execute the LeptonX page-load enhancer. The shared helper is defensive and runs before Select2 init, so any already-created LeptonX wrapper around explicit Select2 targets should be unwrapped at page initialization.
