(() => {
  'use strict';
  const lang = () => { try { return localStorage.getItem('parking-pejam-lang') || 'en'; } catch { return 'en'; } };
  const q = id => document.getElementById(id);
  const dialog = q('vehicleDialog');
  if (!dialog) return;

  const copy = {
    en:{subtitle:'Vehicle & occupancy intelligence',owner:'OWNER / DRIVER',entry:'ENTRY TIME',vin:'VIN / CHASSIS',condition:'CONDITION',profile:'VEHICLE PROFILE',telemetry:'SENSOR TELEMETRY',make:'Make',model:'Model',year:'Year',color:'Color',plate:'License plate',zone:'Parking zone',sensorId:'Sensor ID',reading:'Last reading',battery:'Battery',new:'NEW',used:'USED',conditionNoteNew:'New vehicle',conditionNoteUsed:'Pre-owned vehicle',duration:'Duration',checked:'Checked in by',sensorOnline:'Sensor online',sensorOffline:'Sensor offline',free:'Free / ready',occupied:'Occupied',reserved:'Reserved',out:'Out of service',demo:'Demo data — production version reads these fields from the sensor/API layer.',close:'Close',unknown:'—'},
    de:{subtitle:'Fahrzeug- & Belegungsinformationen',owner:'HALTER / FAHRER',entry:'EINFAHRT',vin:'FIN / FAHRGESTELLNUMMER',condition:'ZUSTAND',profile:'FAHRZEUGPROFIL',telemetry:'SENSOR-TELEMETRIE',make:'Marke',model:'Modell',year:'Baujahr',color:'Farbe',plate:'Kennzeichen',zone:'Parkzone',sensorId:'Sensor-ID',reading:'Letzte Messung',battery:'Batterie',new:'NEUWAGEN',used:'GEBRAUCHT',conditionNoteNew:'Neufahrzeug',conditionNoteUsed:'Gebrauchtfahrzeug',duration:'Dauer',checked:'Eingecheckt von',sensorOnline:'Sensor online',sensorOffline:'Sensor offline',free:'Frei / bereit',occupied:'Belegt',reserved:'Reserviert',out:'Außer Betrieb',demo:'Demodaten — die Produktivversion liest diese Felder aus Sensor/API.',close:'Schließen',unknown:'—'},
    fa:{subtitle:'اطلاعات خودرو و وضعیت اشغال جای پارک',owner:'مالک / راننده',entry:'زمان ورود',vin:'شماره شاسی / VIN',condition:'وضعیت خودرو',profile:'مشخصات خودرو',telemetry:'اطلاعات سنسور',make:'برند',model:'مدل',year:'سال ساخت',color:'رنگ',plate:'پلاک',zone:'زون پارکینگ',sensorId:'شناسه سنسور',reading:'آخرین قرائت',battery:'باتری',new:'نو',used:'استوک / کارکرده',conditionNoteNew:'خودروی نو',conditionNoteUsed:'خودروی کارکرده',duration:'مدت حضور',checked:'ثبت ورود توسط',sensorOnline:'سنسور آنلاین',sensorOffline:'سنسور آفلاین',free:'خالی / آماده',occupied:'اشغال',reserved:'رزرو',out:'خارج از سرویس',demo:'داده‌ها نمایشی هستند — نسخه اصلی این اطلاعات را از سنسور و API دریافت می‌کند.',close:'بستن',unknown:'—'}
  };
  const t = key => (copy[lang()] || copy.en)[key] || copy.en[key] || key;
  const brands = [
    ['Mercedes-Benz','C-Class','Obsidian Black',2024],
    ['BMW','320i','Alpine White',2023],
    ['Audi','A4','Daytona Grey',2022],
    ['Volkswagen','Passat','Deep Blue',2021],
    ['Porsche','Macan','Carrara White',2024],
    ['Toyota','Corolla','Silver Metallic',2023]
  ];
  const drivers = ['Operator 03','Reception Desk','Service Team','Fleet Office','Guest Services','Logistics Team'];
  const colors = ['Obsidian Black','Alpine White','Daytona Grey','Deep Blue','Carrara White','Silver Metallic'];
  const hash = s => [...s].reduce((a,c)=>((a*31)+c.charCodeAt(0))>>>0,7);
  const fmt = (date) => new Date(date).toLocaleString(lang()==='fa'?'fa-IR':lang()==='de'?'de-DE':'en-GB',{dateStyle:'medium',timeStyle:'short'});
  const elapsed = date => { const mins=Math.max(1,Math.floor((Date.now()-new Date(date).getTime())/60000)); if(mins<60) return `${mins} min`; const h=Math.floor(mins/60),m=mins%60; return `${h}h ${m}m`; };
  const vin = n => { const chars='ABCDEFGHJKLMNPRSTUVWXYZ0123456789'; let x=hash(n), out='W0L'; for(let i=0;i<14;i++){x=(x*1664525+1013904223)>>>0;out+=chars[x%chars.length]} return out.slice(0,17); };
  const plate = n => ['N-PE','N-PP','ER-PP','FÜ-PP'][hash(n)%4] + ' ' + String(10+(hash(n)%89)).padStart(2,'0') + ' ' + String.fromCharCode(65+(hash(n)%26)) + String.fromCharCode(65+((hash(n)>>4)%26));
  const set = (id,value) => { const node=q(id); if(node) node.textContent=value; };

  function injectCss(){
    if(q('vehicle-details-style')) return;
    const style=document.createElement('style'); style.id='vehicle-details-style';
    style.textContent=`
      .vehicle-modal{width:min(94vw,940px);max-height:88vh;overflow:auto;border:1px solid #2b4967;background:linear-gradient(180deg,#0d1b2e,#071320);color:#edf5ff;border-radius:24px;padding:25px;box-shadow:0 40px 120px #000b}
      .vehicle-modal-top{display:flex;justify-content:space-between;align-items:flex-start;gap:16px}.vehicle-modal h2{margin:5px 0 2px;font-size:2rem;letter-spacing:-.045em}.vehicle-subtitle{color:#8096ad;font-size:.82rem}.modal-close{border:1px solid #27425e;background:#0a1728;color:#d7e3ef;width:40px;height:40px;border-radius:12px;font-size:1.55rem;line-height:1;cursor:pointer}
      .vehicle-hero{display:grid;grid-template-columns:auto 1fr auto;gap:18px;align-items:center;margin:22px 0 18px;padding:18px;border:1px solid #1c344e;border-radius:18px;background:radial-gradient(circle at 0 0,#193653 0,transparent 45%),#0a1727}.vehicle-avatar{width:72px;height:72px;border-radius:18px;display:grid;place-items:center;font-size:2.2rem;background:#132c45;border:1px solid #2b4a66}.vehicle-status{display:inline-flex;align-items:center;gap:7px;font-size:.7rem;font-weight:950;color:#59d398;text-transform:uppercase;letter-spacing:.08em}.vehicle-status.occupied{color:#ef7c87}.vehicle-model{font-size:1.4rem;font-weight:950;margin-top:4px}.vehicle-plate{display:inline-block;margin-top:8px;padding:5px 11px;border-radius:7px;background:#edf2f5;color:#111b27;font-weight:950;letter-spacing:.12em;font-size:.78rem}.vehicle-live{font-size:.73rem;color:#8da3b9;white-space:nowrap}.vehicle-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:10px}.detail-card{padding:15px;border:1px solid #1a3047;border-radius:15px;background:#0a1626}.detail-card span,.profile-grid span,.telemetry span{display:block;color:#6f849c;font-size:.61rem;letter-spacing:.09em;font-weight:900}.detail-card strong{display:block;margin-top:7px;font-size:.88rem;word-break:break-word}.detail-card small{display:block;margin-top:5px;color:#8498ae;font-size:.66rem}.vehicle-section{margin-top:16px;padding-top:16px;border-top:1px solid #172c43}.vehicle-section-title{font-size:.64rem;letter-spacing:.14em;font-weight:950;color:#7690aa;margin-bottom:12px}.profile-grid{display:grid;grid-template-columns:repeat(6,1fr);gap:10px}.profile-grid div,.telemetry div{padding:11px;border-radius:12px;background:#091625;border:1px solid #162b42}.profile-grid strong,.telemetry strong{display:block;margin-top:5px;font-size:.77rem;color:#dfeaf5}.telemetry{display:grid;grid-template-columns:1.4fr 1fr 1fr;gap:10px}.vehicle-footer{display:flex;justify-content:space-between;align-items:center;gap:14px;margin-top:18px;padding-top:15px;border-top:1px solid #172c43;color:#72879e;font-size:.67rem}.vehicle-footer .ghost{white-space:nowrap}
      @media(max-width:900px){.vehicle-grid{grid-template-columns:repeat(2,1fr)}.profile-grid{grid-template-columns:repeat(3,1fr)}}@media(max-width:620px){.vehicle-modal{padding:17px;border-radius:18px}.vehicle-hero{grid-template-columns:1fr;text-align:center}.vehicle-avatar{margin:auto}.vehicle-live{justify-self:center}.vehicle-grid,.profile-grid,.telemetry{grid-template-columns:1fr 1fr}.vehicle-footer{flex-direction:column;align-items:stretch}.vehicle-footer .ghost{width:100%}}
    `; document.head.appendChild(style);
  }

  function openForButton(button){
    const id=button.dataset.id || 'A-1';
    const spot=id;
    const h=hash(id);
    const car=brands[h%brands.length];
    const isOccupied=button.classList.contains('occupied');
    const isReserved=button.classList.contains('reserved');
    const isOut=button.classList.contains('out');
    const status=isOut?t('out'):isReserved?t('reserved'):isOccupied?t('occupied'):t('free');
    const now=new Date();
    const entry=new Date(now.getTime()-((h%210)+18)*60000);
    const condition=(h%3===0)?'new':'used';
    const owner=drivers[h%drivers.length];
    const v=vin(id);
    const p=plate(id);
    const sensor=`PEJAM-${id}`;
    const battery=72+(h%27);
    set('vehicleTitle',spot);set('vehicleSubtitle',t('subtitle'));set('vehicleStatus','● '+status);
    const statusNode=q('vehicleStatus'); if(statusNode) statusNode.classList.toggle('occupied',isOccupied);
    set('vehicleModel',car[0]+' '+car[1]);set('vehiclePlate',p);set('vehicleDriver',owner);set('vehicleOperator',t('checked')+' '+owner);set('vehicleEntry',fmt(entry));set('vehicleDuration',t('duration')+' '+elapsed(entry));set('vehicleVin',v);
    set('vehicleCondition',condition==='new'?t('new'):t('used'));set('vehicleConditionNote',condition==='new'?t('conditionNoteNew'):t('conditionNoteUsed'));set('vehicleMake',car[0]);set('vehicleModel2',car[1]);set('vehicleYear',String(car[3]));set('vehicleColor',colors[h%colors.length]);set('vehiclePlate2',p);set('vehicleZone',spot.split('-')[0]);set('vehicleSensorId',sensor);set('vehicleReading',fmt(now));set('vehicleBattery',battery+'%');set('vehicleSensor',button.classList.contains('out')?t('sensorOffline'):t('sensorOnline'));
    const titleNode=q('vehicleSubtitle'); if(titleNode) titleNode.textContent=t('subtitle');
    const foot=dialog.querySelector('.vehicle-footer span'); if(foot) foot.textContent=t('demo'); const close=q('closeVehicle2'); if(close) close.textContent=t('close');
    if(typeof dialog.showModal==='function') dialog.showModal(); else dialog.setAttribute('open','');
  }
  function close(){if(typeof dialog.close==='function')dialog.close();else dialog.removeAttribute('open')}
  injectCss();
  document.addEventListener('click',e=>{const b=e.target.closest && e.target.closest('.spot');if(b){e.preventDefault();openForButton(b)}});
  q('closeVehicle')?.addEventListener('click',close);q('closeVehicle2')?.addEventListener('click',close);
  dialog.addEventListener('click',e=>{if(e.target===dialog)close()});
})();
