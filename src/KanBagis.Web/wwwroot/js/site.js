// Kan Bağışı — ortak arayüz davranışları (Hafta 14: kullanılabilirlik).
// Sayfa JavaScript olmadan da çalışır; bunlar yalnızca kolaylıktır, asıl kurallar sunucudadır.

// 1) Geri alınamayan işlemlerde onay: <form data-onay="Emin misiniz?">
//    Yakalama aşamasında çalışır: onay, aşağıdaki "tek gönderim" kilidinden önce sorulur.
document.addEventListener("submit", (e) => {
  const mesaj = e.target.dataset?.onay;
  if (mesaj && !window.confirm(mesaj)) e.preventDefault();
}, true);

// 2) Çift gönderimi önleme: gönderilen formun düğmesi kilitlenir ve "İşleniyor…" yazar.
//    Yavaş işlemlerde (yapay zekâ tahmini, harici servisler) iki kez tıklanınca aynı kaydın iki kez
//    oluşması engellenir (sunucu tarafındaki asıl koruma: Hata H13-04). Doğrulama hatası varsa
//    (jQuery doğrulaması gönderimi iptal eder → defaultPrevented) düğme kilitlenmez.
document.addEventListener("submit", (e) => {
  if (e.defaultPrevented) return;
  const dugme = e.submitter ?? e.target.querySelector("button[type=submit]");
  if (!dugme || dugme.dataset.kilitli) return;
  // Devre dışı düğmenin name/value değeri (ör. yanit=Kabul) gönderilmez; gizli alana kopyalanır.
  if (dugme.name) {
    const gizli = document.createElement("input");
    gizli.type = "hidden";
    gizli.name = dugme.name;
    gizli.value = dugme.value;
    e.target.appendChild(gizli);
  }
  dugme.dataset.kilitli = "1";
  dugme.dataset.eskiMetin = dugme.innerHTML;
  dugme.setAttribute("aria-busy", "true");
  setTimeout(() => { dugme.disabled = true; dugme.textContent = "İşleniyor…"; }, 0);
});

// Tarayıcının "geri" düğmesiyle dönülünce (sayfa önbellekten gelir) kilitli düğmeler yeniden açılır.
window.addEventListener("pageshow", () => {
  document.querySelectorAll("button[data-kilitli]").forEach((d) => {
    d.disabled = false;
    d.innerHTML = d.dataset.eskiMetin;
    delete d.dataset.kilitli;
    d.removeAttribute("aria-busy");
  });
});
