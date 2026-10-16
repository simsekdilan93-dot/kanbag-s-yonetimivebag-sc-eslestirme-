# Veritabanı

| Dosya | İçerik |
|---|---|
| [schema.sql](schema.sql) | Migration'dan üretilmiş tam şema: tablolar, ilişkiler, kısıtlar, indeksler ve örnek veri. |
| [ornek-sorgular.sql](ornek-sorgular.sql) | Elle yazılmış ilişki/kısıt örneği, CRUD'un SQL karşılığı, JOIN sorguları ve transaction örneği. |

## Migration

Şema kodda (Entity Framework Core, `src/KanBagis.DataAccess/Configurations`) tanımlanır. Her değişiklik bir **migration** dosyasına dönüşür.

```bash
# Yeni migration oluştur
dotnet ef migrations add <Ad> -p src/KanBagis.DataAccess -s src/KanBagis.Web -o Migrations

# Veritabanına uygula (uygulama açılırken de otomatik uygulanır)
dotnet ef database update -p src/KanBagis.DataAccess -s src/KanBagis.Web

# SQL şemasını yeniden üret
dotnet ef migrations script -p src/KanBagis.DataAccess -s src/KanBagis.Web -o docs/db/schema.sql
```

Neden migration kullanılır?
- Şemanın her sürümü depoda kayıtlıdır, sürüm kontrolüyle birlikte ilerler.
- Ekipteki herkes aynı komutla aynı veritabanını kurar.
- Değişiklik geri alınabilir (`dotnet ef migrations remove`, `database update <önceki>`).

## İndeksler

| İndeks | Tür | Neden |
|---|---|---|
| `Bagiscilar(TcKimlikNo)` | UNIQUE | Aynı kişi iki kez kayıt olamaz. TC ile arama da hızlanır. |
| `Bagiscilar(KanGrubuId, Sehir)` | Bileşik | Eşleştirme sorgusu "bu kan grubu + bu şehir" diye arar. |
| `Bagislar(BagisciId, BagisTarihi)` | Bileşik | "Son bağış tarihi" (3 ay kuralı) sorgusu. |
| `KanTalepleri(Durum, KanGrubuId)` | Bileşik | "Açık talepler" listesi. |
| `Bildirimler(BagisciId, KanTalebiId)` | UNIQUE | Aynı talep aynı kişiye iki kez bildirilmez. |
| `KanStoklari(KanGrubuId)` | UNIQUE | 1-1 ilişkiyi sağlar. |
| `KanGruplari(Ad)`, `Hastaneler(Ad, Sehir)` | UNIQUE | Tekrarlı kayıt engellenir. |

**İndeks eklenmeyen alanlar:** `Telefon`, `Eposta`, `Mesaj` gibi alanlarda arama yapılmıyor. İndeks her INSERT ve UPDATE işleminde ayrıca güncellenir. Bu yüzden sorgulanmayan alana indeks eklemek yazma işlemlerini boşuna yavaşlatır.

## Örnek veri (KVKK)

Tüm kişi verileri **sentetiktir** (`src/KanBagis.DataAccess/Seed/OrnekVeri.cs`):
- TC kimlik numaraları `999` ile başlar ve gerçek TC algoritmasına uymaz.
- E-postalar `ornek.com` alan adındadır, telefonlar `0555 000 00 xx` biçimindedir.
- Hastaneler gerçek kurumları temsil etmez.
