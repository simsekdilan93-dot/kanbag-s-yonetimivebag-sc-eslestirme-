# Haftalık İlerleme

Bilgisayar Uygulamaları I — proje tabanlı ders paketi, P08 (Kan Bağışı Yönetimi ve Bağışçı Eşleştirme Sistemi).
Her haftanın kodu git geçmişinde, sürümler etiket (tag) olarak durur: [Sürüm notları](../CHANGELOG.md).

- [x] **Hafta 3:** Kurulum, katmanlı proje iskeleti, ER diyagramı, GitHub deposu
- [x] **Hafta 4:** Veritabanı şeması (migration), ilişkiler/kısıtlar/indeksler, sentetik örnek veri, CRUD veri erişim katmanı, testler
- [x] **Hafta 5:** Kayıt/giriş, PBKDF2 parola hash'i + tuz, hesap kilitleme, çerez oturumu, rol tabanlı erişim (Bağışçı/Kurum), güvenlik notu
- [x] **Hafta 6:** Bağışçı kaydı ve kan grubu yönetimi: ABO/Rh modeli ve uyumluluk, bağış uygunluk kuralı, TC algoritması, 81 il listesi, profil düzenleme (sahiplik kontrolü), kan grubu kilidi
- [x] **Hafta 7:** Talep ve grup/konum eşleştirme: talep açma, uyumlu + uygun bağışçıları mesafeye göre sıralama (Haversine), aciliyete göre yarıçap ve bildirim kotası, Kabul/Red yanıtı, kurumun kabul edenleri görmesi
- [x] **Hafta 8 (VİZE):** Uçtan uca entegrasyon (bağış kaydı → stok → talep kapanışı), bilinen hataların giderilmesi, entegrasyon testleri, `v0.1.0`, demo senaryosu
- [x] **Hafta 9:** Harici API entegrasyonu: OSRM ile yol mesafesi ve yol tarifi, ntfy ile anlık bildirim, Twilio ile SMS, arka planda gönderim kuyruğu, zaman aşımı / yeniden deneme / hız sınırı, anahtarların ortam değişkeninde saklanması (`v0.2.0`, [Harici-API.md](Harici-API.md))
- [x] **Hafta 10:** Yapay zekâ bileşeni: istatistiksel tahmin + Claude ile talep/stok tahmini, istem tasarımı, çıktı doğrulama, AI kullanım günlüğü, şeffaflık notu (`v0.3.0`, [Yapay-Zeka-Bileseni.md](Yapay-Zeka-Bileseni.md))
- [x] **Hafta 11:** Stok Takip ve Uyarı Modülü: kritik/düşük eşik, giriş/çıkış kaydı (atomik, silinemez), kritik eşik uyarısı, bağış kaydıyla entegrasyon (`v0.4.0`, [moduller/Stok-Takip-ve-Uyari-Modulu.md](moduller/Stok-Takip-ve-Uyari-Modulu.md))
- [x] **Hafta 12:** İstatistik Panosu ve Bildirimler: toplulaştırma sorguları, SVG grafikler, bildirim özeti, CSV dışa aktarma (`v0.5.0`, [moduller/Istatistik-Panosu.md](moduller/Istatistik-Panosu.md))
- [x] **Hafta 13:** Test ve hata ayıklama: sınır ve eşzamanlılık testleri, 6 hatanın ayıklanıp düzeltilmesi, test piramidi ve kapsam raporu (`v0.6.0`, `v0.6.1`, [test/Test-Raporu.md](test/Test-Raporu.md), [test/Hata-Kayitlari.md](test/Hata-Kayitlari.md))
- [x] **Hafta 14:** Dokümantasyon, arayüz iyileştirme ve dağıtım hazırlığı: README ve kurulum yönergesi (temiz ortamda doğrulandı), teknik dokümantasyon (mimari, modüller, uç noktalar), erişilebilirlik denetimi ve düzeltmeleri, sağlık kontrolü, ortam değişkenleri, yayın betikleri (`v0.7.0`, [Dagitim.md](Dagitim.md), [Erisilebilirlik.md](Erisilebilirlik.md))
