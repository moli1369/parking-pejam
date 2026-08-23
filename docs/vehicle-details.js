(() => {
  'use strict';
  const dialog = document.getElementById('vehicleDialog');
  if (!dialog) return;

  const lang = () => { try { return localStorage.getItem('parking-pejam-lang') || 'en'; } catch { return 'en'; } };
  const t = (key) => ({
    en: { subtitle:'Vehicle & occupancy intelligence', profile:'VEHICLE PROFILE', timeline:'VEHICLE TIMELINE', recognition:'PLATE RECOGNITION', telemetry:'SENSOR TELEMETRY', owner:'OWNER / DRIVER', entry:'ENTRY TIME', vin:'VIN / CHASSIS', condition:'CONDITION', make:'Make', model:'Model', year:'Year', color:'Color', plate:'License plate', zone:'Parking zone', sensorId:'Sensor ID', reading:'Last reading', battery:'Battery', duration:'Duration', checked:'Checked in by', sensorOnline:'Sensor online', sensorOffline:'Sensor offline', free:'Free / ready', occupied:'Occupied', reserved:'Reserved', out:'Out of service', new:'NEW', used:'USED', conditionNew:'New vehicle', conditionUsed:'Pre-owned vehicle', demo:'Demo data — production version reads these fields from the sensor/API layer.', close:'Close', entryEvent:'Vehicle entered', plateEvent:'Plate recognized', sensorEvent:'Sensor updated', operatorEvent:'Entry registered by operator', scanConfidence:'Recognition confidence', camera:'Camera', lastSeen:'Last seen', currentStay:'Current stay', noExit:'Still inside', imageAlt:'Vehicle profile', verified:'Verified', unknown:'—' },
    de: { subtitle:'Fahrzeug- & Belegungsinformationen', profile:'FAHRZEUGPROFIL', timeline:'FAHRZEUG-TIMELINE', recognition:'KENNZEICHENERKENNUNG', telemetry:'SENSOR-TELEMETRIE', owner:'HALTER / FAHRER', entry:'EINFAHRT', vin:'FIN / FAHRGESTELLNUMMER', condition:'ZUSTAND', make:'Marke', model:'Modell', year:'Baujahr', color:'Farbe', plate:'Kennzeichen', zone:'Parkzone', sensorId:'Sensor-ID', reading:'Letzte Messung', battery:'Batterie', duration:'Dauer', checked:'Eingecheckt von', sensorOnline:'Sensor online', sensorOffline:'Sensor offline', free:'Frei / bereit', occupied:'Belegt', reserved:'Reserviert', out:'Außer Betrieb', new:'NEUWAGEN', used:'GEBRAUCHT', conditionNew:'Neufahrzeug', conditionUsed:'Gebrauchtfahrzeug', demo:'Demodaten — die Produktivversion liest diese Felder aus Sensor/API.', close:'Schließen', entryEvent:'Fahrzeug eingefahren', plateEvent:'Kennzeichen erkannt', sensorEvent:'Sensor aktualisiert', operatorEvent:'Eintrag vom Bediener registriert', scanConfidence:'Erkennungsgenauigkeit', camera:'Kamera', lastSeen:'Zuletzt gesehen', currentStay:'Aktueller Aufenthalt', noExit:'Noch im Parkhaus', imageAlt:'Fahrzeugprofil', verified:'Verifiziert', unknown:'—' },
    fa: { subtitle:'هوش خودرو و وضعیت جای پارک', profile:'مشخصات خودرو', timeline:'تایم‌لاین خودرو', recognition:'پلاک‌خوانی', telemetry:'اطلاعات سنسور', owner:'مالک / راننده', entry:'زمان ورود', vin:'شماره شاسی / VIN', condition:'وضعیت خودرو', make:'برند', model:'مدل', year:'سال ساخت', color:'رنگ', plate:'پلاک', zone:'زون پارکینگ', sensorId:'شناسه سنسور', reading:'آخرین قرائت', battery:'باتری', duration:'مدت حضور', checked:'ثبت ورود توسط', sensorOnline:'سنسور آنلاین', sensorOffline:'سنسور آفلاین', free:'خالی / آماده', occupied:'اشغال', reserved:'رزرو', out:'خارج از سرویس', new:'نو', used:'استوک / کارکرده', conditionNew:'خودروی نو', conditionUsed:'خودروی کارکرده', demo:'داده‌ها نمایشی هستند — نسخه اصلی این اطلاعات را از سنسور و API دریافت می‌کند.', close:'بستن', entryEvent:'ورود خودرو', plateEvent:'تشخیص پلاک', sensorEvent:'به‌روزرسانی سنسور', operatorEvent:'ثبت ورود توسط اپراتور', scanConfidence:'اطمینان پلاک‌خوانی', camera:'دوربین', lastSeen:'آخرین مشاهده', currentStay:'حضور فعلی', noExit:'هنوز داخل پارکینگ است', imageAlt:'پروفایل خودرو', verified:'تأیید شد', unknown:'—' }
  }[lang()] || {});
  const q = id => document.getElementById(id);
  const set = (id, value) => { const n = q(id); if (n) n.textContent = value; };
  const hash = s => [...s].reduce((a,c)=>((a*31)+c.charCodeAt(0))>>>0,7);
  const fmt = d => new Date(d).toLocaleString(lang()==='fa'?'fa-IR':lang()==='de'?'de-DE':'en-GB',{dateStyle:'medium',timeStyle:'short'});
  const elapsed = d => { const mins=Math.max(1,Math.floor((Date.now()-new Date(d).getTime())/60000)); const h=Math.floor(mins/60),m=mins%60; return h?`${h}h ${m}m`:`${mins} min`; };
  const cars=[['Mercedes-Benz','C-Class','Obsidian Black',2024],['BMW','320i','Alpine White',2023],['Audi','A4','Daytona Grey',2022],['Volkswagen','Passat','Deep Blue',2021],['Porsche','Macan','Carrara White',2024],['Toyota','Corolla','Silver Metallic',2023]];
  const drivers=['Operator 03','Reception Desk','Service Team','Fleet Office','Guest Services','Logistics Team'];
  const colors=['Obsidian Black','Alpine White','Daytona Grey','Deep Blue','Carrara White','Silver Metallic'];
  const statusLabel = b => b.classList.contains('out')?t('out'):b.classList.contains('reserved')?t('reserved'):b.classList.contains('occupied')?t('occupied'):t('free');
  const vin = n => { const chars='ABCDEFGHJKLMNPRSTUVWXYZ0123456789'; let x=hash(n),o='W0L'; for(let i=0;i<14;i++){x=(x*1664525+1013904223)>>>0;o+=chars[x%chars.length]} return o.slice(0,17); };
  const plate = n => ['N-PE','N-PP','ER-PP','FÜ-PP'][hash(n)%4]+' '+String(10+hash(n)%89).padStart(2,'0')+' '+String.fromCharCode(65+hash(n)%26)+String.fromCharCode(65+(hash(n)>>4)%26);

  function injectStyles(){
    if (document.getElementById('vehicle-intelligence-style')) return;
    const s=document.createElement('style'); s.id='vehicle-intelligence-style';
    s.textContent=`
      .vehicle-visual{display:grid;grid-template-columns:1.25fr 1fr;gap:16px;margin-top:16px}
      .vehicle-photo{min-height:190px;border:1px solid #203b57;border-radius:18px;overflow:hidden;position:relative;background:radial-gradient(circle at 50% 35%,#234a6d,#0a1727 64%)}
      .vehicle-photo:before{content:"";position:absolute;inset:0;background:linear-gradient(145deg,#ffffff08 1px,transparent 1px);background-size:18px 18px;opacity:.35}
      .car-stage{position:absolute;inset:0;display:grid;place-items:center}
      .car-svg{width:min(90%,430px);filter:drop-shadow(0 18px 25px #0008)}
      .photo-label{position:absolute;left:12px;bottom:12px;border:1px solid #ffffff16;background:#071321cc;border-radius:9px;padding:6px 9px;color:#d8e5f1;font-size:.68rem;font-weight:900}
      .recognition-panel{border:1px solid #203b57;border-radius:18px;padding:17px;background:#091625;display:flex;flex-direction:column;justify-content:space-between}
      .recognition-top{display:flex;justify-content:space-between;gap:10px}.confidence{color:#62d49a;font-weight:950;font-size:.78rem}.plate-scan{margin:18px 0 8px;padding:18px;border-radius:14px;background:#edf2f5;color:#111b27;font-weight:1000;letter-spacing:.14em;text-align:center;font-size:1.05rem;box-shadow:inset 0 0 0 2px #d4dbe1}
      .recognition-meta{display:grid;grid-template-columns:1fr 1fr;gap:8px;color:#8297ac;font-size:.67rem}.recognition-meta strong{display:block;margin-top:4px;color:#e8f1f8;font-size:.76rem}
      .timeline{display:grid;gap:10px}.timeline-item{display:grid;grid-template-columns:18px 1fr auto;gap:10px;align-items:start;padding:9px 0}.timeline-dot{width:10px;height:10px;border-radius:50%;margin-top:5px;background:#4abf90;box-shadow:0 0 0 4px #19422f}.timeline-dot.blue{background:#5da7e8;box-shadow:0 0 0 4px #193a56}.timeline-title{font-size:.77rem;font-weight:900;color:#e5eef6}.timeline-meta{font-size:.66rem;color:#7489a0;margin-top:3px}.timeline-time{font-size:.65rem;color:#738ba2;white-space:nowrap}
      @media(max-width:820px){.vehicle-visual{grid-template-columns:1fr}.vehicle-photo{min-height:170px}.timeline-item{grid-template-columns:16px 1fr}.timeline-time{grid-column:2}}
    `;
    document.head.appendChild(s);
  }

  function carSvg(color, accent){
    return `<svg class="car-svg" viewBox="0 0 700 280" xmlns="http://www.w3.org/2000/svg" aria-label="${t('imageAlt')}">
      <defs><linearGradient id="body" x1="0" x2="1"><stop offset="0" stop-color="${accent}"/><stop offset=".55" stop-color="${color}"/><stop offset="1" stop-color="#0e1d2e"/></linearGradient></defs>
      <ellipse cx="350" cy="230" rx="270" ry="22" fill="#0008"/>
      <path d="M90 195 L120 147 Q145 130 210 122 L270 78 Q290 64 340 64 L455 69 Q488 73 520 113 L577 132 Q598 138 610 167 L625 195 Z" fill="url(#body)" stroke="#b7d7ed44" stroke-width="4"/>
      <path d="M250 87 L303 87 L337 120 L214 120 Z M350 87 L447 91 L486 119 L352 119 Z" fill="#9fc7e52b" stroke="#c9e8ff33" stroke-width="3"/>
      <circle cx="185" cy="195" r="38" fill="#0b1119" stroke="#93aec533" stroke-width="8"/><circle cx="185" cy="195" r="16" fill="#7b92a8"/>
      <circle cx="514" cy="195" r="38" fill="#0b1119" stroke="#93aec533" stroke-width="8"/><circle cx="514" cy="195" r="16" fill="#7b92a8"/>
      <rect x="560" y="159" width="44" height="18" rx="6" fill="#6ed39d" opacity=".75"/><rect x="126" y="158" width="36" height="13" rx="6" fill="#e06b75" opacity=".75"/>
    </svg>`;
  }

  function ensureExtraSections(){
    const modal=document.querySelector('.vehicle-modal'); if(!modal||document.getElementById('vehicleExtraSections')) return;
    const extra=document.createElement('div'); extra.id='vehicleExtraSections'; extra.innerHTML=`
      <div class="vehicle-visual">
        <div class="vehicle-photo"><div id="vehicleCarStage" class="car-stage"></div><div class="photo-label">● LIVE VEHICLE PROFILE</div></div>
        <div class="recognition-panel"><div class="recognition-top"><div class="vehicle-section-title">${t('recognition')}</div><div id="plateConfidence" class="confidence">98.7% · ${t('verified')}</div></div><div id="plateScan" class="plate-scan">N-PE 00 AA</div><div class="recognition-meta"><div>${t('camera')}<strong id="plateCamera">Gate A · CAM-02</strong></div><div>${t('lastSeen')}<strong id="plateSeen">—</strong></div></div></div>
      </div>
      <div class="vehicle-section"><div class="vehicle-section-title">${t('timeline')}</div><div id="vehicleTimeline" class="timeline"></div></div>
    `;
    modal.querySelector('.vehicle-footer').before(extra);
  }

  function openForButton(button){
    ensureExtraSections();
    const id=button.dataset.id||'A-01', h=hash(id), car=cars[h%cars.length], occupied=button.classList.contains('occupied'), reserved=button.classList.contains('reserved'), out=button.classList.contains('out');
    const status=out?t('out'):reserved?t('reserved'):occupied?t('occupied'):t('free');
    const now=Date.now(), entry=new Date(now-((h%210)+18)*60000), plateNo=plate(id), owner=drivers[h%drivers.length], v=vin(id), battery=72+h%27;
    const condition=h%3===0?'new':'used';
    set('vehicleTitle',id);set('vehicleSubtitle',t('subtitle'));set('vehicleStatus','● '+status);set('vehicleModel',car[0]+' '+car[1]);set('vehiclePlate',plateNo);set('vehicleDriver',owner);set('vehicleOperator',t('checked')+' '+owner);set('vehicleEntry',fmt(entry));set('vehicleDuration',t('duration')+' '+elapsed(entry));set('vehicleVin',v);set('vehicleCondition',condition==='new'?t('new'):t('used'));set('vehicleConditionNote',condition==='new'?t('conditionNew'):t('conditionUsed'));set('vehicleMake',car[0]);set('vehicleModel2',car[1]);set('vehicleYear',car[3]);set('vehicleColor',car[2]);set('vehiclePlate2',plateNo);set('vehicleZone',id.split('-')[0]);set('vehicleSensorId','PEJAM-'+id);set('vehicleReading',fmt(now));set('vehicleBattery',battery+'%');set('vehicleSensor',out?t('sensorOffline'):t('sensorOnline'));
    const statusNode=q('vehicleStatus'); if(statusNode) statusNode.classList.toggle('occupied',occupied);
    const carColor=['#6e7a8d','#dfe6ec','#5b6677','#315f83','#d8d1bf','#9aa1a8'][h%6],accent=['#54739b','#ffffff','#7b8798','#4a89b8','#efe8dc','#bfc7cf'][h%6];
    q('vehicleCarStage').innerHTML=carSvg(carColor,accent);q('plateScan').textContent=plateNo;q('plateSeen').textContent=fmt(new Date(now-1000*22));q('plateConfidence').textContent=(96+h%4+0.4).toFixed(1)+'% · '+t('verified');
    const events=[
      {dot:'',title:t('entryEvent'),meta:owner,time:fmt(entry)},
      {dot:'blue',title:t('plateEvent'),meta:`${t('camera')} · CAM-02`,time:fmt(new Date(entry.getTime()+1800))},
      {dot:'',title:t('operatorEvent'),meta:owner,time:fmt(new Date(entry.getTime()+4300))},
      {dot:'blue',title:t('sensorEvent'),meta:`${t('sensorId')} · PEJAM-${id}`,time:fmt(new Date(now-60000*2))},
      {dot:'',title:occupied?t('sensorOnline'):t('lastSeen'),meta:`${t('battery')} ${battery}%`,time:fmt(new Date(now))}
    ];
    q('vehicleTimeline').innerHTML=events.map(e=>`<div class="timeline-item"><span class="timeline-dot ${e.dot}"></span><div><div class="timeline-title">${e.title}</div><div class="timeline-meta">${e.meta}</div></div><div class="timeline-time">${e.time}</div></div>`).join('');
    const foot=dialog.querySelector('.vehicle-footer span');if(foot)foot.textContent=t('demo');const close2=q('closeVehicle2');if(close2)close2.textContent=t('close');dialog.showModal();
  }
  function close(){dialog.close()}
  injectStyles();
  ensureExtraSections();
  document.addEventListener('click',e=>{const b=e.target.closest?.('.spot');if(b){e.preventDefault();openForButton(b);}});
  q('closeVehicle')?.addEventListener('click',close);q('closeVehicle2')?.addEventListener('click',close);
  dialog.addEventListener('click',e=>{if(e.target===dialog)close()});
})();
