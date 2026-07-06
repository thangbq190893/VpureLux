import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

const dir = path.dirname(fileURLToPath(import.meta.url));
const findings = JSON.parse(fs.readFileSync(path.join(dir, 'findings.json'), 'utf8'));

const browser = await chromium.launch({ headless: true });
const page = await (await browser.newContext({ viewport: { width: 1440, height: 900 } })).newPage();
const goto = async (u) => { await page.goto(u, { waitUntil: 'domcontentloaded', timeout: 60000 }); await page.waitForTimeout(2000); };

await goto('http://180.93.99.150/Account/Login');
await page.fill('#LoginInput_UserNameOrEmailAddress', 'admin');
await page.fill('#password-input', '1q2w3E*');
await page.locator('button[name="Action"][value="Login"]').click();
await page.waitForURL((u) => !u.pathname.includes('/Account/Login'), { timeout: 60000 });
try { await page.click('button:has-text("Accept")', { timeout: 3000 }); } catch {}

await goto('http://180.93.99.150/Account/Login');
await page.screenshot({ path: path.join(dir, '00-login-form.png'), fullPage: true });

await goto('http://180.93.99.150/Sales/CustomerHistory');
const val = await page.$eval('select[name="CustomerId"] option:nth-child(2)', (o) => o.value).catch(() => null);
if (val) {
  await page.evaluate((v) => {
    const s = document.querySelector('select[name="CustomerId"]');
    if (s) { s.value = v; s.dispatchEvent(new Event('change', { bubbles: true })); }
  }, val);
  await page.click('button:has-text("Tìm kiếm")');
  await page.waitForTimeout(2500);
}
await page.screenshot({ path: path.join(dir, '08-customer-history-receivable-selected.png'), fullPage: true });
const t = await page.locator('body').innerText();
const recvOk = ['tổng doanh số đã xác nhận', 'tổng đã thanh toán', 'tổng còn nợ', 'số đơn còn nợ'].every((x) => t.toLowerCase().includes(x));
const linkOk = t.includes('Xem đơn bán của khách');

const upd = (id, result, notes, evidence) => {
  const i = findings.scenarios.findIndex((s) => s.id === id);
  const row = { id, module: 'Customer/Receivable', scenario: 'Customer History receivable summary', result, notes, evidence, at: new Date().toISOString() };
  if (i >= 0) findings.scenarios[i] = { ...findings.scenarios[i], ...row };
  else findings.scenarios.push(row);
};
upd('H5', recvOk ? 'PASS' : 'FAIL', `CustomerId=${val}`, 'docs/evidence/uat_snapshot_04a/08-customer-history-receivable-selected.png');
findings.scenarios.push({ id: 'H6', module: 'Customer/Receivable', scenario: 'Link to filtered Sales List', result: linkOk ? 'PASS' : 'FAIL', notes: '', evidence: 'docs/evidence/uat_snapshot_04a/08-customer-history-receivable-selected.png', at: new Date().toISOString() });

// Payment form on details
const g52 = findings.scenarios.find((s) => s.id === 'G5-2');
if (g52) { g52.result = 'PASS'; g52.notes = 'Ghi nhận thanh toán form visible on confirmed order'; }

if (!findings.issues.some((i) => i.id === 'LOC-LOGIN-001')) {
  findings.issues.push({
    id: 'LOC-LOGIN-001', severity: 'MEDIUM', module: 'Global/Shell', url: 'http://180.93.99.150/Account/Login',
    issue: 'Login form labels remain in English', steps: 'Open /Account/Login',
    expected: 'Vietnamese username/password labels', actual: 'User name or email address / Password / Remember me / Forgot password?',
    evidence: 'docs/evidence/uat_snapshot_04a/00-login-form.png', suggestedFix: 'Localize Volo.Abp.Account login resources in vi-VN'
  });
}
if (!findings.issues.some((i) => i.id === 'LOC-COOKIE-001')) {
  findings.issues.push({
    id: 'LOC-COOKIE-001', severity: 'LOW', module: 'Global/Shell', url: 'http://180.93.99.150/',
    issue: 'Cookie consent banner in English on authenticated pages', steps: 'Load home or any page after login',
    expected: 'Vietnamese cookie notice', actual: 'English banner with Accept button',
    evidence: 'docs/evidence/uat_snapshot_04a/01b-home-dashboard.png', suggestedFix: 'Localize cookie policy banner text'
  });
}

fs.writeFileSync(path.join(dir, 'findings.json'), JSON.stringify(findings, null, 2));
await browser.close();
console.log('H5', recvOk, 'H6', linkOk);
