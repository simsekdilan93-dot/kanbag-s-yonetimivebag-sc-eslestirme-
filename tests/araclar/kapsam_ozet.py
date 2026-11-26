"""Cobertura XML'den katman ve dosya bazında satır/dal kapsamı özeti.
Kullanım: python kapsam_ozet.py <coverage.cobertura.xml> [--dosya]"""
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

agac = ET.parse(sys.argv[1]).getroot()
dosya_modu = "--dosya" in sys.argv

satirlar = defaultdict(lambda: [0, 0])      # paket -> [kapsanan, toplam]
dallar = defaultdict(lambda: [0, 0])
dosyalar = defaultdict(lambda: [0, 0])
gorulen = set()

for paket in agac.iter("package"):
    ad = paket.get("name")
    for sinif in paket.iter("class"):
        dosya = sinif.get("filename").replace("\\", "/")
        if "/obj/" in dosya or "Migrations/" in dosya or dosya.endswith(".cshtml"):
            continue
        for s in sinif.iter("line"):
            anahtar = (dosya, s.get("number"))
            if anahtar in gorulen:
                continue
            gorulen.add(anahtar)
            kapsandi = int(s.get("hits")) > 0
            satirlar[ad][1] += 1
            satirlar[ad][0] += kapsandi
            dosyalar[dosya][1] += 1
            dosyalar[dosya][0] += kapsandi
            kosul = s.get("condition-coverage")
            if kosul:
                a, b = kosul.split("(")[1].rstrip(")").split("/")
                dallar[ad][0] += int(a)
                dallar[ad][1] += int(b)

def yuzde(a, b):
    return f"%{100 * a / b:5.1f}" if b else "   -  "

print(f"{'Proje':28} {'Satır':>18} {'Dal':>18}")
ts, tt, ds, dt = 0, 0, 0, 0
for ad in sorted(satirlar):
    s, t = satirlar[ad]
    a, b = dallar[ad]
    ts += s; tt += t; ds += a; dt += b
    print(f"{ad:28} {yuzde(s, t)} ({s:4}/{t:4}) {yuzde(a, b)} ({a:4}/{b:4})")
print(f"{'TOPLAM':28} {yuzde(ts, tt)} ({ts:4}/{tt:4}) {yuzde(ds, dt)} ({ds:4}/{dt:4})")

if dosya_modu:
    print()
    for d, (s, t) in sorted(dosyalar.items(), key=lambda x: x[1][0] / max(1, x[1][1])):
        kisa = d.split("/src/")[-1] if "/src/" in d else d
        print(f"{yuzde(s, t)} ({s:4}/{t:4})  {kisa}")
