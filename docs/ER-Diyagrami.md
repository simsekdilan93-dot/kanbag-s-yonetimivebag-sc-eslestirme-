# ER Diyagramı

```mermaid
erDiagram
    KAN_GRUBU ||--o{ BAGISCI : "sahiptir"
    KAN_GRUBU ||--|| KAN_STOK : "stoğu tutulur"
    KAN_GRUBU ||--o{ KAN_TALEBI : "talep edilir"
    HASTANE ||--o{ KAN_TALEBI : "açar"
    HASTANE ||--o{ BAGIS : "kabul eder"
    BAGISCI ||--o{ BAGIS : "yapar"
    BAGISCI ||--o{ BILDIRIM : "alır"
    KAN_TALEBI ||--o{ BILDIRIM : "gönderilir"
    BAGISCI |o--o| KULLANICI : "hesabı"
    HASTANE ||--o{ KULLANICI : "personel hesabı"

    KAN_GRUBU {
        int Id PK
        string Ad UK "A Rh+, 0 Rh- ..."
        string AboGrubu "A / B / AB / O"
        bool RhPozitif "(AboGrubu, RhPozitif) UK"
    }

    BAGISCI {
        int Id PK
        string TcKimlikNo UK "11 hane (CHECK)"
        string Ad
        string Soyad
        string Telefon
        string Eposta "boş olabilir"
        date DogumTarihi
        string Cinsiyet
        int Kilo "30-250 (CHECK)"
        string Sehir "81 il listesinden"
        int KanGrubuId FK
    }

    HASTANE {
        int Id PK
        string Ad
        string Sehir
        string Telefon
    }

    KAN_STOK {
        int Id PK
        int KanGrubuId FK,UK "1-1"
        int UniteSayisi ">= 0 (CHECK)"
        datetime GuncellemeTarihi
    }

    KAN_TALEBI {
        int Id PK
        int HastaneId FK
        int KanGrubuId FK
        int UniteSayisi "> 0 (CHECK)"
        string Aciliyet "Normal / Acil"
        string Durum "Acik / Karsilandi / Iptal"
        datetime OlusturmaTarihi
    }

    BAGIS {
        int Id PK
        int BagisciId FK
        int HastaneId FK
        datetime BagisTarihi
        int UniteSayisi "> 0 (CHECK)"
    }

    BILDIRIM {
        int Id PK
        int BagisciId FK
        int KanTalebiId FK
        string Mesaj
        datetime GonderimTarihi
        bool Okundu
        string Yanit "Beklemede / Kabul / Red"
    }

    KULLANICI {
        int Id PK
        string Eposta UK
        string ParolaHash "PBKDF2 + tuz"
        string Rol "Bagisci / Kurum"
        int BasarisizGirisSayisi
        datetime KilitBitisTarihi "boş olabilir"
        datetime OlusturmaTarihi
        datetime SonGirisTarihi "boş olabilir"
        int BagisciId FK,UK "Rol=Bagisci ise dolu"
        int HastaneId FK "Rol=Kurum ise dolu"
    }
```

> **PK** birincil anahtar · **FK** yabancı anahtar · **UK** benzersiz (unique)

## Varlıklar

| Varlık | Açıklama |
|---|---|
| **KanGrubu** | 8 kan grubunun tutulduğu sabit tablo. ABO grubu ve Rh faktörü ayrı alanlarda tutulur; uyumluluk bu ikisinden hesaplanır (Hafta 6). |
| **Bagisci** | Sisteme kayıtlı gönüllü bağışçı. |
| **Hastane** | Kan talebi açan ve bağış kabul eden kurum. |
| **KanStok** | Her kan grubu için eldeki ünite sayısı. |
| **KanTalebi** | Hastanenin belirli bir kan grubu için açtığı talep. |
| **Bagis** | Bağışçının yaptığı her bağışın kaydı (bağış geçmişi). |
| **Bildirim** | Talebin uygun bağışçıya iletilmesi ve bağışçının yanıtı. |
| **Kullanici** | Sisteme giriş yapan hesap (Hafta 5). Rolüne göre bir bağışçıya ya da hastaneye bağlıdır. |

## İlişkiler

| İlişki | Tür | Silme davranışı | Açıklama |
|---|---|---|---|
| KanGrubu – KanStok | **1-1** | Restrict | `KanStok.KanGrubuId` benzersiz olduğu için her kan grubunun tek bir stok kaydı vardır. |
| KanGrubu – Bagisci | **1-N** | Restrict | Bağışçısı olan kan grubu silinemez. |
| KanGrubu – KanTalebi | **1-N** | Restrict | |
| Hastane – KanTalebi | **1-N** | Restrict | |
| Hastane – Bagis | **1-N** | Restrict | |
| Bagisci – Bagis | **1-N** | Restrict | Bağış geçmişi kayıt niteliğindedir, silinmez. |
| Bagisci – Kullanici | **1-1** (isteğe bağlı) | Cascade | Her bağışçının en fazla bir hesabı olur (`BagisciId` benzersiz). Örnek veri bağışçılarının hesabı yoktur. |
| Hastane – Kullanici | **1-N** | Restrict | Bir hastanenin birden çok personel hesabı olabilir. |
| Bagisci – KanTalebi | **N-N** | Cascade | **Bildirim** ara tablosuyla çözülür. `(BagisciId, KanTalebiId)` benzersizdir. |

## Hafta 4'te yapılan model düzeltmeleri

| Değişiklik | Neden |
|---|---|
| `Hastane` tablosu eklendi; `KanTalebi`'nden `HastaneAdi` ve `Sehir` çıkarıldı | **3NF ihlali vardı:** Şehir bilgisi talebe değil hastaneye bağlıydı (geçişli bağımlılık), aynı hastane bilgisi her talepte tekrar ediyordu. |
| `Bagis` tablosu eklendi; `Bagisci.SonBagisTarihi` kaldırıldı | Son bağış tarihi bağış kayıtlarından hesaplanabilir. Ayrıca saklamak **gereksiz tekrar (denormalizasyon)** olur ve iki değer zamanla tutarsızlaşabilir. Bağış geçmişi de kayda geçmiş olur. |

## Anahtarlar ve normalizasyon

- Her tablonun **birincil anahtarı (PK)** `Id`'dir. TC kimlik no gibi doğal anahtarlar PK yapılmaz, **benzersiz (unique)** kısıtla korunur. Böyle anahtarlar değişebilir ve gizli kalması gereken bilgi içerir.
- İlişkiler **yabancı anahtarlarla (FK)** kurulur. Yabancı anahtar, olmayan bir kayda bağlantı yapılmasını engeller (**referans bütünlüğü**).
- **1NF:** Her alan tek bir değer tutar.
- **2NF:** Tekli PK kullanıldığı için kısmi bağımlılık oluşmaz.
- **3NF:** Anahtar olmayan alanlar başka bir anahtar olmayan alana bağlı değildir. Kan grubu ve hastane bilgisi ayrı tablolarda tutulur, diğer tablolarda yalnızca Id'leri bulunur.
