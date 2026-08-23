(() => {
  'use strict';
  const q = id => document.getElementById(id);
  const storeKey = 'parking-pejam-arrivals-v2';
  const apiBase = window.PARKING_API_BASE || '';
  const get = () => { try { return JSON.parse(localStorage.getItem(storeKey) || '{"batches":[],"vehicles":[]}'); } catch { return {batches:[],vehicles:[]}; } };
  const save = data => { try { localStorage.setItem(storeKey, JSON.stringify(data)); } catch {} };
  const data = get();
  const uid = (p='ID') => `${p}-${Date.now()}-${Math.random().toString(36).slice(2,8).toUpperCase()}`;
  const esc = s => String(s ?? '').replace(/[&<>\"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;',"'":'&#39;'}[c]));
  const now = () => new Date().toISOString();
  const i18n = {
    en:{title:'Vehicle Arrival & Tally',sub:'Register vehicles as they leave the vessel and enter the importer yard.',batch:'Import batch',vessel:'Vessel',voyage:'Voyage / Trip',port:'Arrival port',declared:'Declared vehicles',received:'Received',remaining:'Remaining',discrepancy:'Discrepancy',sequence:'Tally / sequence no.',vin:'VIN / chassis',make:'Make',model:'Model',year:'Year',condition:'Condition',new:'New',used:'Used',color:'Color',origin:'Country of origin',engine:'Engine no.',plate:'Temporary / plate',damage:'Initial damage / notes',customs:'Customs status',customsPending:'Pending',customsCleared:'Cleared',customsHold:'On hold',operator:'Recorded by',save:'Register vehicle',close:'Close',newBatch:'New batch',batchName:'Batch reference',shipment:'Shipment / BL reference',batchInfo:'Create or select an import batch before registering vehicles.',saved:'Vehicle registered successfully',apiSaved:'Saved to the server',localSaved:'Saved in demo mode'},
    de:{title:'Fahrzeugeingang & Zählung',sub:'Fahrzeuge beim Verlassen des Schiffes und beim Eingang im Importlager erfassen.',batch:'Importcharge',vessel:'Schiff',voyage:'Reise / Fahrt',port:'Ankunftshafen',declared:'Angemeldet',received:'Erhalten',remaining:'Restmenge',discrepancy:'Abweichung',sequence:'Zähl-/Sequenznummer',vin:'FIN / Fahrgestellnummer',make:'Marke',model:'Modell',year:'Baujahr',condition:'Zustand',new:'Neu',used:'Gebraucht',color:'Farbe',origin:'Herkunftsland',engine:'Motornummer',plate:'Vorläufiges Kennzeichen',damage:'Erstschaden / Hinweise',customs:'Zollstatus',customsPending:'Ausstehend',customsCleared:'Freigegeben',customsHold:'Gesperrt',operator:'Erfasst von',save:'Fahrzeug erfassen',close:'Schließen',newBatch:'Neue Charge',batchName:'Chargenreferenz',shipment:'Sendung / B/L Referenz',batchInfo:'Vor der Fahrzeugerfassung eine Importcharge auswählen oder anlegen.',saved:'Fahrzeug erfolgreich erfasst',apiSaved:'Auf dem Server gespeichert',localSaved:'Im Demo-Modus gespeichert'},
    fa:{title:'ثبت ورود و بارشمار خودرو',sub:'ثبت خودروها هنگام تخلیه از کشتی و ورود به محوطه شرکت واردکننده.',batch:'محموله وارداتی',vessel:'نام کشتی',voyage:'شماره سفر / Voyage',port:'بندر ورود',declared:'تعداد اعلامی',received:'دریافت‌شده',remaining:'باقی‌مانده',discrepancy:'اختلاف',sequence:'شماره بارشمار / ردیف',vin:'شماره شاسی / VIN',make:'برند',model:'مدل',year:'سال ساخت',condition:'وضعیت',new:'نو',used:'استوک / کارکرده',color:'رنگ',origin:'کشور مبدأ',engine:'شماره موتور',plate:'پلاک موقت',damage:'آسیب اولیه / توضیحات',customs:'وضعیت گمرک',customsPending:'در انتظار',customsCleared:'ترخیص‌شده',customsHold:'توقیف / بررسی',operator:'ثبت‌شده توسط',save:'ثبت خودرو',close:'بستن',newBatch:'محموله جدید',batchName:'شناسه محموله',shipment:'شماره بارنامه / B/L',batchInfo:'قبل از ثبت خودرو یک محموله وارداتی را انتخاب یا ایجاد کنید.',saved:'خودرو با موفقیت ثبت شد',apiSaved:'در سرور ذخیره شد',localSaved:'در حالت دمو ذخیره شد'}
  };
  const lang = () => localStorage.getItem('parking-pejam-lang') || 'en';
  const t = k => (i18n[lang()] || i18n.en)[k] || i18n.en[k] || k;
  const canUseApi = () => location.protocol.startsWith('http') && !/github\.io$/i.test(location.hostname);
  const json = async (path, options={}) => { const res = await fetch(apiBase + path, {credentials:'include',headers:{'Content-Type':'application/json',...(options.headers||{})},...options}); if(!res.ok) throw new Error((await res.text()) || res.statusText); return res.status===204?null:res.json(); };

  function inject(){
    if(q('arrivalDialog')) return;
    const d=document.createElement('dialog'); d.id='arrivalDialog';
    d.innerHTML=`<div class="arrival-modal"><div class="arrival-head"><div><div class="eyebrow">IMPORT INTAKE</div><h2>${t('title')}</h2><p>${t('sub')}</p></div><button class="modal-close" id="arrivalClose">×</button></div>
      <div class="batch-banner"><div><span class="eyebrow">${t('batch')}</span><strong id="activeBatchLabel">—</strong></div><div class="tally"><div><small>${t('declared')}</small><strong id="tDeclared">0</strong></div><div><small>${t('received')}</small><strong id="tReceived">0</strong></div><div><small>${t('remaining')}</small><strong id="tRemaining">0</strong></div><div><small>${t('discrepancy')}</small><strong id="tDiscrepancy">0</strong></div></div></div>
      <div class="batch-tools"><button class="ghost small" id="newBatchBtn">＋ ${t('newBatch')}</button><select id="batchSelect"></select></div>
      <form id="arrivalForm" class="arrival-form"><div class="form-section"><div class="section-title">1 · ${t('batch')}</div><div class="form-grid"><label><span>${t('vessel')}</span><input name="vessel" required></label><label><span>${t('voyage')}</span><input name="voyage"></label><label><span>${t('port')}</span><input name="port"></label><label><span>${t('batchName')}</span><input name="batchName" required></label><label><span>${t('shipment')}</span><input name="shipment"></label><label><span>${t('declared')}</span><input name="declared" type="number" min="0" required></label></div></div>
      <div class="form-section"><div class="section-title">2 · ${t('sequence')} / ${t('vin')}</div><div class="form-grid"><label><span>${t('sequence')}</span><input name="sequence" type="number" min="1" required></label><label class="wide"><span>${t('vin')}</span><input name="vin" minlength="11" maxlength="17" required></label></div></div>
      <div class="form-section"><div class="section-title">3 · Vehicle</div><div class="form-grid"><label><span>${t('make')}</span><input name="make" required></label><label><span>${t('model')}</span><input name="model" required></label><label><span>${t('year')}</span><input name="year" type="number" min="1950" max="2100"></label><label><span>${t('color')}</span><input name="color"></label><label><span>${t('origin')}</span><input name="origin"></label><label><span>${t('condition')}</span><select name="condition"><option value="New">${t('new')}</option><option value="Used">${t('used')}</option></select></label><label><span>${t('engine')}</span><input name="engine"></label><label><span>${t('plate')}</span><input name="plate"></label></div></div>
      <div class="form-section"><div class="section-title">4 · Customs & handover</div><div class="form-grid"><label><span>${t('customs')}</span><select name="customs"><option>${t('customsPending')}</option><option>${t('customsCleared')}</option><option>${t('customsHold')}</option></select></label><label><span>${t('operator')}</span><input name="operator" value="Current operator"></label><label class="full"><span>${t('damage')}</span><textarea name="damage" rows="3"></textarea></label></div></div>
      <div class="arrival-actions"><button type="button" class="ghost" id="arrivalClose2">${t('close')}</button><button class="primary" type="submit">${t('save')}</button></div></form>
    </div>`;
    document.body.appendChild(d);
    q('arrivalClose').onclick=()=>d.close(); q('arrivalClose2').onclick=()=>d.close(); d.addEventListener('click',e=>{if(e.target===d)d.close()});
    q('newBatchBtn').onclick=newBatch; q('arrivalForm').onsubmit=submit; refreshBatches();
  }

  async function newBatch(){
    const vessel=prompt(t('vessel'))||''; const voyage=prompt(t('voyage'))||''; const name=prompt(t('batchName')); if(!name)return; const declared=Number(prompt(t('declared'),'100')||0); const port=prompt(t('port'))||''; const shipment=prompt(t('shipment'))||'';
    if(canUseApi()){
      try{ const b=await json('/api/import/shipments',{method:'POST',body:JSON.stringify({vesselName:vessel,voyageNumber:voyage,portOfEntry:port,shipmentReference:name,billOfLadingNumber:shipment,declaredVehicleCount:declared})}); data.batches.unshift({id:b.id,name:b.shipmentReference,vessel:b.vesselName,voyage:b.voyageNumber,port:b.portOfEntry,shipment:b.billOfLadingNumber,declared:b.declaredVehicleCount,server:true}); save(data); refreshBatches(b.id); return; }
    const b={id:uid('BATCH'),name,vessel,voyage,port,shipment,declared,createdAt:now(),server:false}; data.batches.unshift(b); save(data); refreshBatches(b.id);
  }

  async function refreshBatches(selectId){
    if(canUseApi()){
      try{
        const remote=await json('/api/import/shipments');
        data.batches=remote.map(b=>({id:b.id,name:b.shipmentReference,vessel:b.vesselName,voyage:b.voyageNumber,port:b.portOfEntry,shipment:b.billOfLadingNumber,declared:b.declaredVehicleCount,received:b.receivedVehicleCount,server:true})); save(data);
      }catch{}
    }
    const s=q('batchSelect'); if(!s)return; s.innerHTML=data.batches.map(b=>`<option value="${esc(b.id)}">${esc(b.name)} · ${esc(b.vessel||'—')}</option>`).join('') || `<option value="">${t('newBatch')}</option>`;
    if(selectId)s.value=selectId;
    s.onchange=updateBatchBanner; updateBatchBanner();
  }
  function activeBatch(){ return data.batches.find(b=>String(b.id)===String(q('batchSelect')?.value)) || data.batches[0]; }
  async function updateBatchBanner(){
    const b=activeBatch(); if(!b){q('activeBatchLabel').textContent='—'; return;}
    if(canUseApi() && b.server){ try{const remote=await json('/api/import/shipments/'+b.id); b.declared=remote.declaredVehicleCount;b.received=remote.receivedVehicleCount;b.remaining=remote.remainingVehicleCount;}catch{} }
    const count=b.server?(b.received||0):data.vehicles.filter(v=>String(v.batchId)===String(b.id)).length; const discrepancy=count-(b.declared||0);
    q('activeBatchLabel').textContent=`${b.name} · ${b.vessel||'—'}`;q('tDeclared').textContent=b.declared||0;q('tReceived').textContent=count;q('tRemaining').textContent=Math.max(0,(b.declared||0)-count);q('tDiscrepancy').textContent=discrepancy;
  }

  async function submit(e){
    e.preventDefault(); const f=new FormData(e.target); const b=activeBatch(); if(!b){alert(t('batchInfo'));return;}
    const payload={Vin:String(f.get('vin')).trim().toUpperCase(),Make:String(f.get('make')).trim(),Model:String(f.get('model')).trim(),ModelYear:f.get('year')?Number(f.get('year')):null,Color:String(f.get('color')||'').trim()||null,Condition:f.get('condition')==='Used'?1:0,OriginCountry:String(f.get('origin')||'').trim()||null,EngineNumber:String(f.get('engine')||'').trim()||null,TemporaryPlate:String(f.get('plate')||'').trim()||null,CustomsStatus:String(f.get('customs')||'').trim()||null,DamageNotes:String(f.get('damage')||'').trim()||null,Notes:`${t('operator')}: ${String(f.get('operator')||'').trim()}`};
    if(canUseApi() && b.server){
      try{
        const result=await json('/api/import/shipments/'+b.id+'/vehicles',{method:'POST',body:JSON.stringify(payload)});
        b.received=(b.received||0)+1; save(data); await updateBatchBanner(); e.target.reset(); showToast('✓ '+t('saved')+' · '+t('apiSaved')); window.dispatchEvent(new CustomEvent('vehicle-registered',{detail:result})); return;
      }catch(error){ showToast('⚠ '+error.message); return; }
    }
    const vehicle={id:uid('VEH'),batchId:b.id,sequence:f.get('sequence'),vin:payload.Vin,make:payload.Make,model:payload.Model,year:payload.ModelYear,condition:f.get('condition'),color:payload.Color,origin:payload.OriginCountry,engine:payload.EngineNumber,plate:payload.TemporaryPlate,customs:payload.CustomsStatus,damage:payload.DamageNotes,operator:f.get('operator'),vessel:b.vessel,voyage:b.voyage,port:b.port,shipment:b.shipment,receivedAt:now(),yardSlot:null,status:'Received'};
    data.vehicles.unshift(vehicle); save(data); await updateBatchBanner(); e.target.reset(); window.dispatchEvent(new CustomEvent('vehicle-registered',{detail:vehicle})); showToast('✓ '+t('saved')+' · '+t('localSaved'));
  }
  function showToast(text){const notice=document.createElement('div');notice.className='arrival-toast';notice.textContent=text;document.body.appendChild(notice);setTimeout(()=>notice.remove(),2600)}

  window.ParkingPejamArrival={open:()=>{inject();if(!q('arrivalDialog').open)q('arrivalDialog').showModal()},getData:()=>data,refresh:refreshBatches};
})();
