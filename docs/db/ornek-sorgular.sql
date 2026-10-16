-- =====================================================================
-- Kan Bağışı Yönetim Sistemi — Elle yazılmış SQL örnekleri
-- Tam şema migration'dan üretilir: docs/db/schema.sql
-- Bu dosya ilişki/kısıt mantığını ve CRUD'un SQL karşılığını gösterir.
-- =====================================================================


-- ---------------------------------------------------------------------
-- 1) ELLE YAZILMIŞ İLİŞKİ VE KISIT ÖRNEĞİ (DDL)
--    Bildirim tablosu: Bagisci <-> KanTalebi N-N ilişkisinin ara tablosu
-- ---------------------------------------------------------------------
CREATE TABLE Bildirimler (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,      -- Birincil anahtar
    BagisciId      INTEGER NOT NULL,
    KanTalebiId    INTEGER NOT NULL,
    Mesaj          TEXT    NOT NULL,
    GonderimTarihi TEXT    NOT NULL,
    Okundu         INTEGER NOT NULL DEFAULT 0,
    Yanit          TEXT    NOT NULL DEFAULT 'Beklemede',

    -- Yabancı anahtar: olmayan bir bağışçıya bildirim yazılamaz.
    -- ON DELETE CASCADE: bağışçı silinirse bildirimleri de silinir.
    CONSTRAINT FK_Bildirimler_Bagiscilar
        FOREIGN KEY (BagisciId) REFERENCES Bagiscilar (Id) ON DELETE CASCADE,

    CONSTRAINT FK_Bildirimler_KanTalepleri
        FOREIGN KEY (KanTalebiId) REFERENCES KanTalepleri (Id) ON DELETE CASCADE,

    -- CHECK kısıtı: Yanit yalnızca bu üç değerden biri olabilir.
    CONSTRAINT CK_Bildirim_Yanit CHECK (Yanit IN ('Beklemede', 'Kabul', 'Red'))
);

-- UNIQUE indeks: aynı talep aynı bağışçıya iki kez bildirilemez.
-- Bu indeks N-N ilişkide "aynı çift tekrar edemez" kuralını sağlar.
CREATE UNIQUE INDEX IX_Bildirimler_BagisciId_KanTalebiId
    ON Bildirimler (BagisciId, KanTalebiId);


-- ---------------------------------------------------------------------
-- 2) CRUD İŞLEMLERİNİN SQL KARŞILIĞI (DML)
--    EfRepository<T> sınıfındaki metotlar bu sorguları üretir.
-- ---------------------------------------------------------------------

-- CREATE  → EkleAsync
INSERT INTO Bagiscilar (TcKimlikNo, Ad, Soyad, Telefon, DogumTarihi, Cinsiyet, Sehir, KanGrubuId)
VALUES ('99900000011', 'Deniz', 'Yıldız', '05550000011', '1996-05-20', 'Kadin', 'Bingöl', 1);

-- READ    → TumunuGetirAsync / IdIleGetirAsync
SELECT * FROM Bagiscilar;
SELECT * FROM Bagiscilar WHERE Id = 1;

-- UPDATE  → GuncelleAsync
UPDATE Bagiscilar SET Telefon = '05559999999' WHERE Id = 1;

-- DELETE  → SilAsync
DELETE FROM Bagiscilar WHERE Id = 11;


-- ---------------------------------------------------------------------
-- 3) PROJEYE ÖZGÜ SORGULAR
-- ---------------------------------------------------------------------

-- Eşleştirme: Bingöl'de yaşayan, 0 Rh- kan grubundan ve son 3 ayda bağış yapmamış bağışçılar.
-- IX_Bagiscilar_KanGrubuId_Sehir bileşik indeksini kullanır.
SELECT b.Ad, b.Soyad, b.Telefon
FROM Bagiscilar b
WHERE b.KanGrubuId = 8
  AND b.Sehir = 'Bingöl'
  AND NOT EXISTS (
        SELECT 1 FROM Bagislar g
        WHERE g.BagisciId = b.Id AND g.BagisTarihi > date('now', '-3 months'));

-- Kan grubuna göre stok durumu (JOIN)
SELECT k.Ad AS KanGrubu, s.UniteSayisi
FROM KanStoklari s
JOIN KanGruplari k ON k.Id = s.KanGrubuId
ORDER BY s.UniteSayisi;

-- Açık talepler ve kaç bağışçının kabul ettiği (JOIN + GROUP BY)
SELECT t.Id, h.Ad AS Hastane, k.Ad AS KanGrubu, t.UniteSayisi, t.Aciliyet,
       SUM(CASE WHEN bi.Yanit = 'Kabul' THEN 1 ELSE 0 END) AS KabulEden
FROM KanTalepleri t
JOIN Hastaneler  h ON h.Id = t.HastaneId
JOIN KanGruplari k ON k.Id = t.KanGrubuId
LEFT JOIN Bildirimler bi ON bi.KanTalebiId = t.Id
WHERE t.Durum = 'Acik'
GROUP BY t.Id;


-- ---------------------------------------------------------------------
-- 4) TRANSACTION ÖRNEĞİ
--    BagisRepository.BagisKaydetVeStokArtirAsync aynı mantıkla çalışır.
-- ---------------------------------------------------------------------
BEGIN TRANSACTION;
    INSERT INTO Bagislar (BagisciId, HastaneId, UniteSayisi, BagisTarihi)
    VALUES (10, 1, 1, datetime('now'));

    UPDATE KanStoklari SET UniteSayisi = UniteSayisi + 1, GuncellemeTarihi = datetime('now')
    WHERE KanGrubuId = 8;
COMMIT;   -- Hata olursa: ROLLBACK; (iki işlem de geri alınır)
