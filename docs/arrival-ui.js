(() => {
  'use strict';
  const t = k => {
    const lang = (()=>{try{return localStorage.getItem('parking-pejam-lang')||'en'}catch{return'en'}})();
    const d={en:{open:'Register arrival / tally',helper:'New vehicles received from vessel'},de:{open:'Fahrzeugzugang erfassen',helper:'Neue Fahrzeuge vom Schiff erfassen'},fa:{open:'ثبت ورود / بارشمار',helper:'ثبت خودروهای تازه تخلیه‌شده از کشتی'}};
    return (d[lang]||d.en)[k]||k;
  };
  function boot(){
    if(!window.ParkingPejamArrival)return setTimeout(boot,250);
    if(document.getElementById('arrivalQuickAction'))return;
    const style=document.createElement('style');style.textContent='.arrival-quick{margin-left:auto;display:flex;flex-direction:column;gap:2px}.arrival-quick button{border:1px solid #3b9b7b;border-radius:12px;background:linear-gradient(135deg,#1e7c5a,#246c98);color:#fff;padding:10px 14px;font-weight:900;cursor:pointer;box-shadow:0 10px 25px #0004}.arrival-quick small{color:#7890a7;font-size:.61rem;padding-left:4px}.toolbar .arrival-quick{order:4}@media(max-width:680px){.toolbar .arrival-quick{width:100%}.arrival-quick button{width:100%}}';document.head.appendChild(style);
    const host=document.querySelector('.toolbar');if(!host)return;
    const wrap=document.createElement('div');wrap.className='arrival-quick';wrap.id='arrivalQuickAction';wrap.innerHTML=`<button type="button">＋ ${t('open')}</button><small>${t('helper')}</small>`;wrap.querySelector('button').onclick=()=>window.ParkingPejamArrival.open();host.appendChild(wrap);
  }
  boot();
})();