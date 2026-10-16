CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Hastaneler" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Hastaneler" PRIMARY KEY AUTOINCREMENT,
    "Ad" TEXT NOT NULL,
    "Sehir" TEXT NOT NULL,
    "Telefon" TEXT NOT NULL
);

CREATE TABLE "KanGruplari" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_KanGruplari" PRIMARY KEY AUTOINCREMENT,
    "Ad" TEXT NOT NULL
);

CREATE TABLE "Bagiscilar" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Bagiscilar" PRIMARY KEY AUTOINCREMENT,
    "TcKimlikNo" TEXT NOT NULL,
    "Ad" TEXT NOT NULL,
    "Soyad" TEXT NOT NULL,
    "Telefon" TEXT NOT NULL,
    "Eposta" TEXT NULL,
    "DogumTarihi" TEXT NOT NULL,
    "Cinsiyet" TEXT NOT NULL,
    "Sehir" TEXT NOT NULL,
    "KanGrubuId" INTEGER NOT NULL,
    CONSTRAINT "CK_Bagisci_TcKimlikNo_11Hane" CHECK (length(TcKimlikNo) = 11),
    CONSTRAINT "FK_Bagiscilar_KanGruplari_KanGrubuId" FOREIGN KEY ("KanGrubuId") REFERENCES "KanGruplari" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "KanStoklari" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_KanStoklari" PRIMARY KEY AUTOINCREMENT,
    "UniteSayisi" INTEGER NOT NULL,
    "GuncellemeTarihi" TEXT NOT NULL,
    "KanGrubuId" INTEGER NOT NULL,
    CONSTRAINT "CK_KanStok_UniteSayisi_Negatif_Olamaz" CHECK (UniteSayisi >= 0),
    CONSTRAINT "FK_KanStoklari_KanGruplari_KanGrubuId" FOREIGN KEY ("KanGrubuId") REFERENCES "KanGruplari" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "KanTalepleri" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_KanTalepleri" PRIMARY KEY AUTOINCREMENT,
    "UniteSayisi" INTEGER NOT NULL,
    "Aciliyet" TEXT NOT NULL,
    "Durum" TEXT NOT NULL,
    "OlusturmaTarihi" TEXT NOT NULL,
    "HastaneId" INTEGER NOT NULL,
    "KanGrubuId" INTEGER NOT NULL,
    CONSTRAINT "CK_KanTalebi_Aciliyet" CHECK (Aciliyet IN ('Normal', 'Acil')),
    CONSTRAINT "CK_KanTalebi_Durum" CHECK (Durum IN ('Acik', 'Karsilandi', 'Iptal')),
    CONSTRAINT "CK_KanTalebi_UniteSayisi_Pozitif" CHECK (UniteSayisi > 0),
    CONSTRAINT "FK_KanTalepleri_Hastaneler_HastaneId" FOREIGN KEY ("HastaneId") REFERENCES "Hastaneler" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_KanTalepleri_KanGruplari_KanGrubuId" FOREIGN KEY ("KanGrubuId") REFERENCES "KanGruplari" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Bagislar" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Bagislar" PRIMARY KEY AUTOINCREMENT,
    "BagisTarihi" TEXT NOT NULL,
    "UniteSayisi" INTEGER NOT NULL,
    "BagisciId" INTEGER NOT NULL,
    "HastaneId" INTEGER NOT NULL,
    CONSTRAINT "CK_Bagis_UniteSayisi_Pozitif" CHECK (UniteSayisi > 0),
    CONSTRAINT "FK_Bagislar_Bagiscilar_BagisciId" FOREIGN KEY ("BagisciId") REFERENCES "Bagiscilar" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Bagislar_Hastaneler_HastaneId" FOREIGN KEY ("HastaneId") REFERENCES "Hastaneler" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Bildirimler" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Bildirimler" PRIMARY KEY AUTOINCREMENT,
    "Mesaj" TEXT NOT NULL,
    "GonderimTarihi" TEXT NOT NULL,
    "Okundu" INTEGER NOT NULL,
    "Yanit" TEXT NOT NULL,
    "BagisciId" INTEGER NOT NULL,
    "KanTalebiId" INTEGER NOT NULL,
    CONSTRAINT "CK_Bildirim_Yanit" CHECK (Yanit IN ('Beklemede', 'Kabul', 'Red')),
    CONSTRAINT "FK_Bildirimler_Bagiscilar_BagisciId" FOREIGN KEY ("BagisciId") REFERENCES "Bagiscilar" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Bildirimler_KanTalepleri_KanTalebiId" FOREIGN KEY ("KanTalebiId") REFERENCES "KanTalepleri" ("Id") ON DELETE CASCADE
);

INSERT INTO "Hastaneler" ("Id", "Ad", "Sehir", "Telefon")
VALUES (1, 'Örnek Devlet Hastanesi', 'Bingöl', '04260000001');
SELECT changes();

INSERT INTO "Hastaneler" ("Id", "Ad", "Sehir", "Telefon")
VALUES (2, 'Örnek Şehir Hastanesi', 'Elazığ', '04240000002');
SELECT changes();

INSERT INTO "Hastaneler" ("Id", "Ad", "Sehir", "Telefon")
VALUES (3, 'Örnek Üniversite Hastanesi', 'Diyarbakır', '04120000003');
SELECT changes();


INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (1, 'A Rh+');
SELECT changes();

INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (2, 'A Rh-');
SELECT changes();

INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (3, 'B Rh+');
SELECT changes();

INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (4, 'B Rh-');
SELECT changes();

INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (5, 'AB Rh+');
SELECT changes();

INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (6, 'AB Rh-');
SELECT changes();

INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (7, '0 Rh+');
SELECT changes();

INSERT INTO "KanGruplari" ("Id", "Ad")
VALUES (8, '0 Rh-');
SELECT changes();


INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (1, 'Ayşe', 'Kadin', '1995-04-12', 'ayse.yilmaz@ornek.com', 1, 'Bingöl', 'Yılmaz', '99900000001', '05550000001');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (2, 'Mehmet', 'Erkek', '1988-09-03', 'mehmet.kaya@ornek.com', 8, 'Bingöl', 'Kaya', '99900000002', '05550000002');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (3, 'Zeynep', 'Kadin', '2000-01-25', NULL, 3, 'Elazığ', 'Demir', '99900000003', '05550000003');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (4, 'Ali', 'Erkek', '1992-06-30', 'ali.celik@ornek.com', 1, 'Bingöl', 'Çelik', '99900000004', '05550000004');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (5, 'Elif', 'Kadin', '1998-11-08', 'elif.sahin@ornek.com', 8, 'Elazığ', 'Şahin', '99900000005', '05550000005');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (6, 'Mustafa', 'Erkek', '1985-02-14', NULL, 5, 'Diyarbakır', 'Arslan', '99900000006', '05550000006');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (7, 'Fatma', 'Kadin', '1990-07-19', 'fatma.dogan@ornek.com', 7, 'Bingöl', 'Doğan', '99900000007', '05550000007');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (8, 'Emre', 'Erkek', '1997-03-05', 'emre.koc@ornek.com', 2, 'Diyarbakır', 'Koç', '99900000008', '05550000008');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (9, 'Merve', 'Kadin', '2001-12-01', NULL, 4, 'Bingöl', 'Aydın', '99900000009', '05550000009');
SELECT changes();

INSERT INTO "Bagiscilar" ("Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon")
VALUES (10, 'Burak', 'Erkek', '1993-08-22', 'burak.ozturk@ornek.com', 8, 'Bingöl', 'Öztürk', '99900000010', '05550000010');
SELECT changes();


INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (1, '2026-10-01 09:00:00', 1, 24);
SELECT changes();

INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (2, '2026-10-01 09:00:00', 2, 6);
SELECT changes();

INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (3, '2026-10-01 09:00:00', 3, 15);
SELECT changes();

INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (4, '2026-10-01 09:00:00', 4, 3);
SELECT changes();

INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (5, '2026-10-01 09:00:00', 5, 8);
SELECT changes();

INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (6, '2026-10-01 09:00:00', 6, 1);
SELECT changes();

INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (7, '2026-10-01 09:00:00', 7, 30);
SELECT changes();

INSERT INTO "KanStoklari" ("Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi")
VALUES (8, '2026-10-01 09:00:00', 8, 2);
SELECT changes();


INSERT INTO "KanTalepleri" ("Id", "Aciliyet", "Durum", "HastaneId", "KanGrubuId", "OlusturmaTarihi", "UniteSayisi")
VALUES (1, 'Acil', 'Acik', 1, 8, '2026-10-10 08:30:00', 4);
SELECT changes();

INSERT INTO "KanTalepleri" ("Id", "Aciliyet", "Durum", "HastaneId", "KanGrubuId", "OlusturmaTarihi", "UniteSayisi")
VALUES (2, 'Normal', 'Acik', 2, 1, '2026-10-12 14:00:00', 2);
SELECT changes();

INSERT INTO "KanTalepleri" ("Id", "Aciliyet", "Durum", "HastaneId", "KanGrubuId", "OlusturmaTarihi", "UniteSayisi")
VALUES (3, 'Normal', 'Karsilandi', 1, 3, '2026-09-20 11:15:00', 1);
SELECT changes();


INSERT INTO "Bagislar" ("Id", "BagisTarihi", "BagisciId", "HastaneId", "UniteSayisi")
VALUES (1, '2026-03-10 10:00:00', 2, 1, 1);
SELECT changes();

INSERT INTO "Bagislar" ("Id", "BagisTarihi", "BagisciId", "HastaneId", "UniteSayisi")
VALUES (2, '2026-05-02 09:30:00', 3, 2, 1);
SELECT changes();

INSERT INTO "Bagislar" ("Id", "BagisTarihi", "BagisciId", "HastaneId", "UniteSayisi")
VALUES (3, '2026-06-15 13:00:00', 1, 1, 1);
SELECT changes();

INSERT INTO "Bagislar" ("Id", "BagisTarihi", "BagisciId", "HastaneId", "UniteSayisi")
VALUES (4, '2026-07-01 11:00:00', 2, 1, 1);
SELECT changes();

INSERT INTO "Bagislar" ("Id", "BagisTarihi", "BagisciId", "HastaneId", "UniteSayisi")
VALUES (5, '2026-08-20 15:45:00', 7, 1, 1);
SELECT changes();


INSERT INTO "Bildirimler" ("Id", "BagisciId", "GonderimTarihi", "KanTalebiId", "Mesaj", "Okundu", "Yanit")
VALUES (1, 2, '2026-10-10 08:35:00', 1, 'Örnek Devlet Hastanesi''nde acil 0 Rh- kan ihtiyacı var.', 1, 'Kabul');
SELECT changes();

INSERT INTO "Bildirimler" ("Id", "BagisciId", "GonderimTarihi", "KanTalebiId", "Mesaj", "Okundu", "Yanit")
VALUES (2, 5, '2026-10-10 08:35:00', 1, 'Örnek Devlet Hastanesi''nde acil 0 Rh- kan ihtiyacı var.', 1, 'Red');
SELECT changes();

INSERT INTO "Bildirimler" ("Id", "BagisciId", "GonderimTarihi", "KanTalebiId", "Mesaj", "Okundu", "Yanit")
VALUES (3, 10, '2026-10-10 08:35:00', 1, 'Örnek Devlet Hastanesi''nde acil 0 Rh- kan ihtiyacı var.', 0, 'Beklemede');
SELECT changes();

INSERT INTO "Bildirimler" ("Id", "BagisciId", "GonderimTarihi", "KanTalebiId", "Mesaj", "Okundu", "Yanit")
VALUES (4, 1, '2026-10-12 14:05:00', 2, 'Örnek Şehir Hastanesi''nde A Rh+ kan ihtiyacı var.', 0, 'Beklemede');
SELECT changes();

INSERT INTO "Bildirimler" ("Id", "BagisciId", "GonderimTarihi", "KanTalebiId", "Mesaj", "Okundu", "Yanit")
VALUES (5, 4, '2026-10-12 14:05:00', 2, 'Örnek Şehir Hastanesi''nde A Rh+ kan ihtiyacı var.', 0, 'Beklemede');
SELECT changes();


CREATE INDEX "IX_Bagiscilar_KanGrubuId_Sehir" ON "Bagiscilar" ("KanGrubuId", "Sehir");

CREATE UNIQUE INDEX "IX_Bagiscilar_TcKimlikNo" ON "Bagiscilar" ("TcKimlikNo");

CREATE INDEX "IX_Bagislar_BagisciId_BagisTarihi" ON "Bagislar" ("BagisciId", "BagisTarihi");

CREATE INDEX "IX_Bagislar_HastaneId" ON "Bagislar" ("HastaneId");

CREATE UNIQUE INDEX "IX_Bildirimler_BagisciId_KanTalebiId" ON "Bildirimler" ("BagisciId", "KanTalebiId");

CREATE INDEX "IX_Bildirimler_KanTalebiId" ON "Bildirimler" ("KanTalebiId");

CREATE UNIQUE INDEX "IX_Hastaneler_Ad_Sehir" ON "Hastaneler" ("Ad", "Sehir");

CREATE UNIQUE INDEX "IX_KanGruplari_Ad" ON "KanGruplari" ("Ad");

CREATE UNIQUE INDEX "IX_KanStoklari_KanGrubuId" ON "KanStoklari" ("KanGrubuId");

CREATE INDEX "IX_KanTalepleri_Durum_KanGrubuId" ON "KanTalepleri" ("Durum", "KanGrubuId");

CREATE INDEX "IX_KanTalepleri_HastaneId" ON "KanTalepleri" ("HastaneId");

CREATE INDEX "IX_KanTalepleri_KanGrubuId" ON "KanTalepleri" ("KanGrubuId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261016063707_IlkSema', '10.0.13');

COMMIT;

