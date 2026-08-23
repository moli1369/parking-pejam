(() => {
  const labels={en:'Vehicle exit',de:'Fahrzeugausgang',fa:'خروج خودرو'};
  function add(){
    if(document.getElementById('dispatchOpenBtn'))return;
    const host=document.querySelector('.hero-actions')||document.querySelector('.strip-actions');
    if(!host)return;
    const b=document.createElement('button');b.id='dispatchOpenBtn';b.className='ghost';b.type='button';
    const lang=()=>localStorage.getItem('parking-pejam-lang')||'en';
    b.textContent='↗ '+(labels[lang()]||labels.en);
    b.onclick=()=>window.ParkingPejamDispatch?.open();
    host.appendChild(b);
    document.querySelectorAll('.lang').forEach(x=>x.addEventListener('click',()=>setTimeout(()=>{b.textContent='↗ '+(labels[lang()]||labels.en)},40)));
  }
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',add);else add();
})();
