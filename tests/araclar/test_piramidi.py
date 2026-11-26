"""Test listesini (dotnet test --list-tests) katmanlara ayırır.
Sınıflandırma testin GÖVDESİNE bakılarak yapılır:
  Uçtan uca  : gerçek HTTP isteği (WebApplicationFactory: GirisYapmisIstemciAsync / YeniIstemci / _uygulama)
  Entegrasyon: gerçek SQLite veritabanı (TestVeritabani, _db, db.Context, Kur(, Servis(db), Baglam())
               ya da sınıfın alan/kurucusunda veritabanı açılıyorsa
  Birim      : ikisi de yok (saf fonksiyon / sahte nesne)
Kullanım: python test_piramidi.py <test_listesi.txt> <tests klasörü>"""
import os
import re
import sys
from collections import Counter, defaultdict

liste, klasor = sys.argv[1], sys.argv[2]

UCTAN = re.compile(r"GirisYapmisIstemciAsync|YeniIstemci\(|_uygulama\.")
ENTEG = re.compile(r"TestVeritabani|_db\.|db\.Context|Servis\(db\)|Baglam\(|BagisRepository\(")
# "Kur(" yardımcısı bazı sınıflarda veritabanı açar (YapayZeka), bazılarında yalnızca sahte HTTP kurar (OSRM).
KUR_DB = re.compile(r"Kur\([^)]*\)[^\n]*\n\s*\{[\s\S]{0,200}?TestVeritabani")
kur_db = set()
SINIF = re.compile(r"public (?:sealed )?class (\w+)")
TEST = re.compile(r"\n\s*\[(?:Fact|Theory)\]")
METOD = re.compile(r"public (?:async Task|void) (\w+)")

govde = {}
sinif_db = {}
for ad in os.listdir(klasor):
    if not ad.endswith(".cs"):
        continue
    metin = open(os.path.join(klasor, ad), encoding="utf-8-sig").read()
    # Bir dosyada birden çok test sınıfı olabilir: sınıf bildirimlerinin konumlarından böl.
    konumlar = [m.start() for m in SINIF.finditer(metin)] + [len(metin)]
    for i in range(len(konumlar) - 1):
        blok = metin[konumlar[i]:konumlar[i + 1]]
        sinif = SINIF.match(blok).group(1)
        parcalar = TEST.split(blok)
        # Testlerin öncesindeki kısım (alanlar, kurucu) veritabanı açıyorsa sınıfın testleri entegrasyondur.
        sinif_db[sinif] = "TestVeritabani" in parcalar[0]
        if KUR_DB.search(blok):
            kur_db.add(sinif)
        for p in parcalar[1:]:
            m = METOD.search(p)
            if m:
                govde[(sinif, m.group(1))] = p

say = Counter()
sinif_say = defaultdict(Counter)
for satir in open(liste, encoding="utf-8"):
    ad = satir.strip()
    if not ad:
        continue
    parca = ad.split("(")[0].split(".")
    sinif, metod = parca[-2], parca[-1]
    g = govde.get((sinif, metod), "")
    if UCTAN.search(g):
        tur = "Uçtan uca (HTTP)"
    elif ENTEG.search(g) or sinif_db.get(sinif) or (sinif in kur_db and "Kur(" in g):
        tur = "Entegrasyon (veritabanı)"
    else:
        tur = "Birim"
    say[tur] += 1
    sinif_say[sinif][tur] += 1

toplam = sum(say.values())
for tur in ["Birim", "Entegrasyon (veritabanı)", "Uçtan uca (HTTP)"]:
    print(f"{tur:26} {say[tur]:4}  (%{100 * say[tur] / toplam:4.1f})")
print(f"{'TOPLAM':26} {toplam:4}")
print()
for sinif in sorted(sinif_say):
    c = sinif_say[sinif]
    print(f"{sinif:28} birim {c['Birim']:3}  entegrasyon {c['Entegrasyon (veritabanı)']:3}  uçtan uca {c['Uçtan uca (HTTP)']:3}  = {sum(c.values()):3}")
