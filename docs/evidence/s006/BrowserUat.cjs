const fs=require('fs'),path=require('path');
const {chromium}=require(process.env.S006_NODE_MODULES+'/playwright');
const root=path.resolve(__dirname,'../../..'),dir=path.join(root,'artifacts/s006');
const fixtures=JSON.parse(fs.readFileSync(path.join(dir,'fixtures.json'),'utf8'));
const evidence={screens:[],posts:[],errors:[],checks:[]};
(async()=>{
 const browser=await chromium.launch({channel:'chrome',headless:true});
 try{
  const context=await browser.newContext({storageState:path.join(dir,'browser-auth.json'),viewport:{width:1440,height:960},locale:'vi-VN',timezoneId:'Asia/Ho_Chi_Minh'});
  const control=JSON.parse(fs.readFileSync(path.join(dir,'uat-auth.json'),'utf8')).Control;
  const enabled=await context.request.post('http://localhost:5196/s006-control/service/true',{headers:{'X-S006-Control':control}});
  if(!enabled.ok())throw new Error('Local S006 feature enable failed');
  await context.route('**/*',r=>new URL(r.request().url()).hostname==='localhost'?r.continue():r.abort());
  const page=await context.newPage();page.on('pageerror',e=>evidence.errors.push(e.message));
  page.on('dialog',async d=>{evidence.errors.push('Unexpected native dialog '+d.type());await d.dismiss()});
  async function go(url){
   const r=await page.goto('http://localhost:5196'+url,{waitUntil:'domcontentloaded'});if(r.status()!==200)throw new Error(url+' '+r.status());await page.waitForFunction(()=>window.abp&&window.jQuery);
   const accept=page.getByText('Accept',{exact:true});if(await accept.count()&&await accept.isVisible())await accept.click();
   await page.waitForFunction(()=>Array.from(document.querySelectorAll('#ServiceWorksTable,#ServiceOrdersTable,#ServicePaymentsTable,#ServiceRefundsTable,#BusinessReportTable')).every(t=>jQuery.fn.dataTable.isDataTable(t)&&jQuery(t).DataTable().settings()[0]._bInitComplete));
   if(await page.locator('[data-total=documentCount]').count())await page.waitForFunction(()=>document.querySelector('[data-total=documentCount]').textContent.trim()!=='-');
  }
  async function api(url){return await page.evaluate(async url=>{const r=await fetch(url,{headers:{'X-Requested-With':'XMLHttpRequest'}});if(!r.ok)throw new Error('GET '+url+' '+r.status);return r.json()},url);}
  async function shot(name){await page.evaluate(()=>Promise.all(document.getAnimations().filter(a=>a.effect?.getTiming().iterations!==Infinity).map(a=>a.finished.catch(()=>{}))));await page.screenshot({path:path.join(dir,name+'.png'),fullPage:true});const dimensions=await page.evaluate(()=>({width:innerWidth,documentWidth:document.documentElement.scrollWidth}));evidence.screens.push({name,...dimensions});if(dimensions.documentWidth>dimensions.width+2)throw new Error('Document overflow '+name);}
  async function submit(route){const result=page.waitForResponse(r=>r.url().includes(route)&&r.request().method()==='POST');await page.locator('.modal.show button[type=submit]').click();const r=await result;evidence.posts.push({route,status:r.status()});if(r.status()!==204){evidence.failureBody=await r.text();throw new Error('Modal submit '+r.status()+' '+route)}await page.locator('.modal.show').waitFor({state:'hidden'});}
  for(const size of [{width:1440,height:960},{width:390,height:844}]){
   await page.setViewportSize(size);const prefix=size.width===390?'mobile':'desktop';
   for(const [name,url] of [['works','/Service/Works'],['orders','/Service'],['create','/Service/Create'],['edit','/Service/Edit/'+fixtures.Orders.security.id],['details','/Service/Details/'+fixtures.Orders.core123.id],['service-report','/Reports/ServiceRevenue'],['all-report','/Reports/BusinessRevenue']]){
    await go(url);await shot(prefix+'-'+name);
   }
   await go('/Service/Details/'+fixtures.Orders.shortage.id);await page.locator('#CompleteServiceOrder').click();await page.locator('.modal.show').waitFor();await shot(prefix+'-complete-modal');await page.locator('.modal.show button[data-bs-dismiss=modal]').first().click();
  }
  await page.setViewportSize({width:1440,height:960});await go('/Service/Works');
  if(!(await api('/api/app/service-work?SearchText=UATSVC_20260907_UI&MaxResultCount=10')).items.some(x=>x.code==='UATSVC_20260907_UI')){
  await page.locator('#CreateServiceWork').click();await page.locator('.modal.show').waitFor();
  await page.locator('#Input_Code').fill('UATSVC_20260907_UI');
  await page.locator('#Input_Name').fill('UATSVC_20260907 <img src=x onerror=window.s006Xss=1>');
  await page.locator('#Input_Unit').fill('Lan');await page.locator('#Input_DefaultPrice').fill('1500000');await page.locator('#Input_StandardCost').fill('');
  await shot('desktop-work-modal');await submit('/Service/WorkModal');
  }
  await page.locator('#ServiceWorkSearch').fill('UATSVC_20260907_UI');await page.locator('#ServiceWorkFilters').evaluate(f=>f.requestSubmit());
  await page.getByText('UATSVC_20260907 <img src=x onerror=window.s006Xss=1>',{exact:true}).waitFor();
  if(await page.evaluate(()=>!!window.s006Xss))throw new Error('XSS executed');evidence.checks.push('Work modal create, nullable cost, literal encoded HTML');
  const order=fixtures.Orders.browser;
  async function money(amount,reference,refund=false){
   await go('/Service/Details/'+order.id);await page.locator(refund?'#AddServiceRefund':'#AddServicePayment').click();await page.locator('.modal.show').waitFor();
   await page.locator('#Input_Amount').fill(amount);await page.locator('#Input_ReferenceNo').fill(reference);await page.locator('#Input_Note').fill('UATSVC_20260907 actual UI event');
   await page.setViewportSize({width:390,height:844});await shot(refund?'mobile-refund-modal':'mobile-payment-'+reference);await submit('/Service/MoneyModal');
  }
  await go('/Service/Details/'+order.id);
  let payments=await api('/api/app/service-payment?ServiceOrderId='+order.id+'&MaxResultCount=20');
  if(!payments.items.some(x=>x.referenceNo==='UATSVC_20260907_UI_PAY1'))await money('1.500.000','UATSVC_20260907_UI_PAY1');
  await go('/Service/Details/'+order.id);
  payments=await api('/api/app/service-payment?ServiceOrderId='+order.id+'&MaxResultCount=20');
  const label=await page.evaluate(()=>abp.localization.getResource('VPureLux')('Service:VoidReceipt'));
  if(payments.items.find(x=>x.referenceNo==='UATSVC_20260907_UI_PAY1').status===1){
   const row=page.locator('#ServicePaymentsTable tbody tr').filter({hasText:'UATSVC_20260907_UI_PAY1'});await row.waitFor();
   if(await row.locator('.dropdown-toggle').count())await row.locator('.dropdown-toggle').click();
   await row.getByText(label,{exact:true}).click();await page.locator('.modal.show').waitFor();await page.locator('#Input_Reason').fill('UATSVC_20260907 wrong receipt');await shot('mobile-void-modal');await submit('/Service/VoidPaymentModal');
  }
  if(!payments.items.some(x=>x.referenceNo==='UATSVC_20260907_UI_PAY2'))await money('1.500.000','UATSVC_20260907_UI_PAY2');
  await go('/Service/Details/'+order.id);
  if((await api('/api/app/service-order/'+order.id)).status!==4){await page.locator('#CompleteServiceOrder').click();await page.locator('.modal.show').waitFor();await page.locator('[data-actual-quantity]').fill('1');await submit('/Service/CompleteModal');}
  if(!(await api('/api/app/service-payment/refund-list?ServiceOrderId='+order.id+'&MaxResultCount=20')).items.some(x=>x.referenceNo==='UATSVC_20260907_UI_REFUND'))await money('500.000','UATSVC_20260907_UI_REFUND',true);
  await page.setViewportSize({width:390,height:844});
  await go('/Service/Details/'+order.id);await page.locator('#ServicePaymentsTable').scrollIntoViewIfNeeded();await shot('mobile-money-settled');
  const activeRow=page.locator('#ServicePaymentsTable tbody tr').filter({hasText:'UATSVC_20260907_UI_PAY2'});
  await activeRow.getByText(label,{exact:true}).click();await page.locator('.modal.show').waitFor();await shot('mobile-void-modal');
  await page.locator('.modal.show button[data-bs-dismiss=modal]').first().click();
  await page.locator('#ServicePaymentsTable').evaluate(t=>jQuery(t).DataTable().page.len(1).page(1).draw(false));
  evidence.checks.push('Actual UI: vi-VN 1.500.000 payment, reasoned Void, second receipt, Complete actual1/2, factual refund500.000, payment page2');
  evidence.decision=evidence.errors.length?'FAIL':'PASS';
 }finally{fs.writeFileSync(path.join(dir,'browser-uat-r2.json'),JSON.stringify(evidence,null,2));await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
