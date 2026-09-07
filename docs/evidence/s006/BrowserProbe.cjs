const fs=require('fs');
const path=require('path');
const {chromium}=require(process.env.S006_NODE_MODULES+'/playwright');
const root=path.resolve(__dirname,'../../..');
const auth=JSON.parse(fs.readFileSync(path.join(root,'artifacts/s006/uat-auth.json'),'utf8'));
(async()=>{
 const browser=await chromium.launch({headless:true,channel:'chrome'});
 try{
  const context=await browser.newContext({viewport:{width:1440,height:960},locale:'vi-VN'});
  await context.route('**/*',route=>new URL(route.request().url()).hostname==='localhost'?route.continue():route.abort());
  const page=await context.newPage();
  await page.goto('http://localhost:5196/Account/Login',{waitUntil:'domcontentloaded'});
  await page.locator('[name="LoginInput.UserNameOrEmailAddress"]').fill(auth.UserName);
  await page.locator('[name="LoginInput.Password"]').fill(auth.Password);
  // The existing Development helper fills admin on first focus; replace after its focus listener.
  await page.locator('[name="LoginInput.UserNameOrEmailAddress"]').fill(auth.UserName);
  if(await page.locator('[name="LoginInput.UserNameOrEmailAddress"]').inputValue()!==auth.UserName)throw new Error('Login helper changed fixture username');
  await page.locator('button[type=submit]').click();
  try{await page.waitForURL(url=>!url.pathname.startsWith('/Account/Login'),{waitUntil:'domcontentloaded'});}
  catch(e){await page.screenshot({path:path.join(root,'artifacts/s006/browser-login-failure.png')});console.log((await page.locator('body').innerText()).slice(-1600));throw e;}
  page.on('request',async r=>{if(r.url().endsWith('/api/app/service-work')){const h=await r.allHeaders();console.log('Actual API header names',Object.keys(h),'Cookie names',(h.cookie||'').split(';').map(c=>c.split('=')[0].trim()));}});
  const result=await page.evaluate(async()=>{
   const r=await fetch('/api/app/service-work',{method:'POST',headers:{'Content-Type':'application/json','X-Requested-With':'XMLHttpRequest'},body:JSON.stringify({code:'UATSVC_20260907_ANTI_AFTER',name:'UATSVC_20260907 native browser antiforgery probe',unit:'Lan',defaultPrice:100000,status:1})});
   return {status:r.status,body:await r.text(),url:r.url};
  });
  fs.writeFileSync(path.join(root,'artifacts/s006/browser-antiforgery-after.json'),JSON.stringify(result,null,2));
  console.log(JSON.stringify({status:result.status,url:result.url,body:result.body.slice(0,200)}));
  if(result.status!==400&&!result.url.endsWith('/Error?httpStatusCode=400'))throw new Error('Missing antiforgery was not rejected');
  await context.storageState({path:path.join(root,'artifacts/s006/browser-auth.json')});
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
