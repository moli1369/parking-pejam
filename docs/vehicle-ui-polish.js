(() => {
  'use strict';
  const d = document.getElementById('vehicleDialog');
  if (!d) return;

  const style = document.createElement('style');
  style.textContent = `
    .vehicle-modal{width:min(94vw,980px);max-height:88vh;overflow:auto;padding:0!important;border-radius:24px;background:#081523!important}
    .vehicle-modal-top{padding:22px 24px 14px;background:linear-gradient(135deg,#0d2238,#091522);border-bottom:1px solid #183149}
    .vehicle-modal-top .eyebrow{color:#5da7e8}
    .vehicle-modal-top h2{font-size:1.8rem!important;margin:4px 0!important}
    .vehicle-hero{margin:0!important;border:0!important;border-radius:0!important;padding:18px 24px!important;background:#0b1929!important}
    .vehicle-avatar{width:58px!important;height:58px!important;border-radius:15px!important;font-size:1.8rem!important}
    .vehicle-grid{display:grid!important;grid-template-columns:repeat(4,1fr)!important;gap:9px!important;padding:0 24px 18px}
    .detail-card{padding:12px!important;background:#091625!important}
    .detail-card strong{font-size:.82rem!important}
    #vehicleExtraSections{display:grid;grid-template-columns:1.08fr .92fr;gap:12px;padding:0 24px 18px}
    #vehicleExtraSections .vehicle-visual{display:contents}
    #vehicleExtraSections .vehicle-photo{grid-column:1;grid-row:1;min-height:205px!important}
    #vehicleExtraSections .recognition-panel{grid-column:2;grid-row:1}
    #vehicleExtraSections .vehicle-section{grid-column:1 / -1}
    #vehicleExtraSections .vehicle-section .vehicle-section-title{margin-bottom:8px}
    #vehicleExtraSections .vehicle-section:nth-of-type(2){display:none}
    .vehicle-section{margin-top:0!important;padding:14px 24px!important;border-top:1px solid #173049!important}
    .vehicle-section-title{font-size:.62rem!important;color:#6f94b6!important}
    .profile-grid{grid-template-columns:repeat(6,1fr)!important;gap:7px!important}
    .profile-grid div{padding:9px!important}
    .profile-grid strong{font-size:.73rem!important}
    .telemetry{grid-template-columns:repeat(3,1fr)!important;gap:7px!important}
    .timeline{max-height:230px;overflow:auto;padding-right:3px}
    .timeline-item{padding:7px 0!important}
    .vehicle-footer{margin:0!important;padding:12px 24px!important;background:#07121f;border-top:1px solid #173049}
    .vehicle-footer span{font-size:.62rem!important}
    @media(max-width:820px){
      .vehicle-modal-top{padding:18px 17px 12px}
      .vehicle-hero{padding:15px 17px!important}
      .vehicle-grid{grid-template-columns:repeat(2,1fr)!important;padding:0 17px 15px}
      #vehicleExtraSections{grid-template-columns:1fr;padding:0 17px 15px}
      #vehicleExtraSections .vehicle-photo,#vehicleExtraSections .recognition-panel{grid-column:auto;grid-row:auto}
      .vehicle-section{padding:13px 17px!important}
      .profile-grid,.telemetry{grid-template-columns:repeat(2,1fr)!important}
    }
    @media(max-width:520px){
      .vehicle-grid{grid-template-columns:1fr 1fr!important}
      .profile-grid,.telemetry{grid-template-columns:1fr 1fr!important}
      .vehicle-footer{padding:12px 17px!important}
    }
  `;
  document.head.appendChild(style);
})();
