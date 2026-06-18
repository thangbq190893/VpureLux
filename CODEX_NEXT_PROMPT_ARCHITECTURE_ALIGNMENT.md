# Codex Next Prompt: V2 Architecture Alignment Audit

Use this prompt after placing the V2 documentation files in the repository root.

```text
Stop feature implementation. Perform a V2 architecture alignment audit only.

Read first:
- CODEX_README_VPURELUX_V2.md

Then read the remaining V2 documents in the order defined there.

V2 docs are the current source of truth. Older specs are historical references only when they conflict with V2.

Do not implement code. Do not modify production code, tests, migrations, DTOs, AppServices, permissions, EF mappings, domain rules, or business logic.

Create only:
- V2_ARCHITECTURE_ALIGNMENT_REPORT.md

Audit source against V2 decisions:
- Product/SKU sales model.
- Direct Component sales support.
- ComponentPurchasePriceVersion old concept.
- Component Suggested Selling Price missing.
- Product Suggested Selling Price behavior.
- Giá cấu thành linh kiện missing.
- BOM required for every sellable Product/SKU.
- Product with BOM one component for loose component sales.
- Inventory component-only stock in phase 1.
- Raw GUID UI blockers.
- Missing Activate actions.

Report:
1. V2 docs read.
2. Conflicts found.
3. Impact matrix: Domain, Contracts, Application, EF/migrations, Web/Razor, Tests, Audit.
4. Proposed implementation phases.
5. Data/migration risk.
6. Recommended first implementation batch.
7. Questions requiring approval.

Final response: summary, report file created, top conflicts, recommended first batch, destructive migration/data reset risk, forbidden areas changed yes/no.
```
