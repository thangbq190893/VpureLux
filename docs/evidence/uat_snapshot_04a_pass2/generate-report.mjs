/**
 * Regenerate clean UAT Pass 2 report from findings.json
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const findings = JSON.parse(fs.readFileSync(path.join(__dirname, 'findings.json'), 'utf8'));
const REPORT = path.join(__dirname, '..', '..', 'UAT_SNAPSHOT_04A_PASS2_FULL_E2E_TEST.md');

const latest = new Map();
for (const s of findings.scenarios) {
  if (s.id.endsWith('-ERR')) continue;
  const prev = latest.get(s.id);
  if (!prev || new Date(s.at) > new Date(prev.at)) latest.set(s.id, s);
}
const scenarios = [...latest.values()].sort((a, b) => a.id.localeCompare(b.id));

const counts = { PASS: 0, FAIL: 0, BLOCKED: 0, NOT_TESTED: 0 };
for (const s of scenarios) {
  if (counts[s.result] !== undefined) counts[s.result]++;
  else counts.NOT_TESTED++;
}

const moduleOrder = [
  'A.Global', 'B.Catalog Product', 'C.Catalog Vật tư', 'D.BOM', 'E.Pricing',
  'F.Inventory Warehouse', 'G.Inventory Receipt', 'H.Inventory Issue', 'I.Inventory Adjustment',
  'J.Sales Create', 'K.Sales Payment', 'L.Sales List', 'M.Customer History', 'N.Audit',
  'O.Validation', 'P.UI/UX',
];

const checkpoint = {};
for (const s of scenarios) {
  if (!checkpoint[s.module]) checkpoint[s.module] = { PASS: 0, FAIL: 0, BLOCKED: 0, NOT_TESTED: 0 };
  const k = counts[s.result] !== undefined ? s.result : 'NOT_TESTED';
  checkpoint[s.module][k]++;
}

const paymentBug = scenarios.find((s) => s.id === 'K2')?.result === 'FAIL';
const issues = findings.issues || [];

let coverage = '';
for (const s of scenarios) {
  const notes = (s.notes || '').replace(/\|/g, '/').replace(/\s+/g, ' ').slice(0, 120);
  coverage += `| ${s.id} | ${s.module} | ${s.scenario} | ${s.result} | ${notes} | ${s.evidence || ''} |\n`;
}

let cprows = '';
for (const mod of moduleOrder) {
  const c = checkpoint[mod] || { PASS: 0, FAIL: 0, BLOCKED: 0, NOT_TESTED: 0 };
  const total = c.PASS + c.FAIL + c.BLOCKED + c.NOT_TESTED;
  cprows += `| ${mod} | ${total ? 'done' : 'skipped'} | ${c.PASS} | ${c.FAIL} | ${c.BLOCKED} | ${c.NOT_TESTED} | |\n`;
}

let issueRows = '';
for (const i of issues) {
  issueRows += `| ${i.id} | ${i.severity} | ${i.module} | ${i.url} | ${(i.issue || '').replace(/\|/g, '/')} | ${(i.steps || '').replace(/\|/g, '/')} | ${(i.expected || '').replace(/\|/g, '/')} | ${(i.actual || '').slice(0, 100).replace(/\|/g, '/')} | ${i.evidence || ''} | ${i.suggestedFix || ''} |\n`;
}

const extraIssues = [
  { id: 'VAL-G-ZERO', severity: 'MEDIUM', module: 'G.Inventory Receipt', issue: 'Zero quantity receipt may not show friendly validation', suggestedFix: '04F validation' },
  { id: 'BOM-D5', severity: 'MEDIUM', module: 'D.BOM', issue: 'BOM publish button click did not confirm published state in UI', suggestedFix: '04E BOM publish UX' },
  { id: 'INV-G4', severity: 'LOW', module: 'G.Inventory Receipt', issue: 'Balance increase not verified numerically after receipt', suggestedFix: '04D inventory verification' },
  { id: 'ADJ-I3', severity: 'LOW', module: 'I.Inventory Adjustment', issue: 'Negative adjustment not executed (no stock context)', suggestedFix: '04D' },
  { id: 'LOC-LOGIN-001', severity: 'MEDIUM', module: 'A.Global', issue: 'Login form labels in English (from Pass 1, not re-failed)', suggestedFix: '04F localization' },
  { id: 'UX-INV-001', severity: 'LOW', module: 'G.Inventory Receipt', issue: 'Column label Mặt hàng tồn kho vs Vật tư', suggestedFix: '04G UI' },
];

const allIssues = [...issues, ...extraIssues.filter((e) => !issues.some((i) => i.id === e.id))];
const top10 = allIssues.slice(0, 10);

const td = findings.testData || {};

const report = `# UAT Snapshot 04A Pass 2 — Full E2E Functional Test

**Target:** http://180.93.99.150/  
**Run ID:** ${findings.runId || findings.prefix}  
**Date:** 2026-07-07  
**Mode:** Audit only — no application source fixes  
**Tester:** Playwright headless runner (\`uat-pass2-runner.mjs\`)

## Executive Summary

| Metric | Count |
|--------|-------|
| **PASS** | ${counts.PASS} |
| **FAIL** | ${counts.FAIL} |
| **BLOCKED** | ${counts.BLOCKED} |
| **NOT TESTED** | ${counts.NOT_TESTED} |

**Overall:** PARTIAL PASS — core workflows exercised end-to-end; **Sales payment recording FAILED (bug reproduced).**

**Sales payment recording worked?** **NO** — partial payment on confirmed order \`${td.orderNo || 'SO-202607-000003'}\` submitted but paid/remaining/status unchanged and no history row.

**Known payment-history bug reproduced?** **YES** — see K2, PAY-HIST-001, screenshot \`K2-partial-payment.png\`.

### Test data created (prefix \`${td.prefix || 'UAT04A2'}\`)
| Entity | Code/ID |
|--------|---------|
| Product | ${td.productCode} |
| Vật tư | ${td.materialCode} |
| Warehouse | ${td.warehouseCode} |
| Customer | ${td.customerCode} |
| Sales order | ${td.orderNo} (${td.orderId}) |
| Lot | ${td.lotNo} |

### Top 10 issues (by severity)
${top10.map((i) => `- **${i.id}** (${i.severity}): ${i.issue}`).join('\n')}

### Highest severity
**PAY-HIST-001 (HIGH)** — Ghi nhận thanh toán submits without persisting payment history or updating receivable summary.

### Fix batch recommendations
| Batch | Scope |
|-------|--------|
| **04B Critical functional** | None BLOCKER; payment is HIGH not BLOCKER for confirm/sales flow |
| **04C Sales payment/receivable** | PAY-HIST-001 — AddPayment persistence, history reload, list columns sync |
| **04D Inventory transaction** | Receipt balance verification, negative adjustment paths |
| **04E BOM/Pricing** | D5 publish confirmation UX |
| **04F Validation/localization** | G-val-zero, LOC-LOGIN-001 |
| **04G UI consistency** | UX-INV-001, dropdown clipping (A-dropdown NOT TESTED) |

---

## Coverage Table
| ID | Module | Scenario | Result | Notes | Evidence |
|----|--------|----------|--------|-------|----------|
${coverage}

## Checkpoint Table
| Module | Status | PASS | FAIL | BLOCKED | NOT TESTED | Notes |
|--------|--------|------|------|---------|------------|-------|
${cprows}

## Issue Table
| ID | Severity | Module | URL | Issue | Steps | Expected | Actual | Evidence | Suggested fix |
|----|----------|--------|-----|-------|-------|----------|--------|----------|---------------|
${issueRows || '| PAY-HIST-001 | HIGH | K.Sales Payment | see K2 | Payment not persisted | Partial 500000 on SO-202607-000003 | History + partial status | Paid 0, rows 0 | K2-partial-payment.png | 04C |\n'}

## Module notes

### K. Sales Payment (deep test) — CRITICAL
- **K1 PASS:** Add-payment form visible on confirmed order SO-202607-000003.
- **K2 FAIL (HIGH):** Entered amount 500000, date, method, ref \`UAT04A2_20260707_0233PAY1\`, note. After submit: **Đã thanh toán = 0 ₫**, **Còn nợ = 1.500.000 ₫**, **Trạng thái = Chưa thanh toán**, **Chưa có lịch sử thanh toán**.
- **K3 BLOCKED:** Could not complete remaining payment because K2 left order unpaid.
- **K4 PASS:** Overpayment 999999999 blocked (no new row).
- **K5 PASS:** Revenue/cost/profit unchanged; ledger stock movements not increased by payment.
- **K6 PASS (vacuous):** No rows to verify order.

### Workflows verified PASS
- Login, navigation (Catalog/BOM/Pricing/Inventory/Sales/Customer/Audit)
- Product create PROD-202607070002, search, details, edit code readonly
- Vật tư create MAT-202607070002, no Linh kiện
- BOM draft save with 2 lines, product context
- Product pricing 1.500.000 ₫
- Warehouse create (manual code)
- Receipt with auto LOT-202607070001, ledger in
- Issue, insufficient stock validation
- Positive adjustment, all-zero blocked
- Sales draft → edit → **confirm** SO-202607-000003, inventory issue, ledger Đơn bán hàng
- Sales list payment columns/filter
- Customer history receivable after customer select
- Audit list + export

### NOT TESTED / PARTIAL
- A-dropdown dropdown clipping interactive check
- D5 BOM publish (PARTIAL — publish click, status unclear)
- I3 negative adjustment, I6 mixed direction
- O2 BOM publish conflict
- G-val-zero zero qty validation (FAIL)
- G4 balance numeric verify (PARTIAL)

---

*Generated: ${new Date().toISOString()}*  
*Evidence folder: \`docs/evidence/uat_snapshot_04a_pass2/\`*  
*Findings JSON: \`docs/evidence/uat_snapshot_04a_pass2/findings.json\`*
`;

fs.writeFileSync(REPORT, report);
console.log('Report written:', REPORT);
console.log('Counts:', counts);
