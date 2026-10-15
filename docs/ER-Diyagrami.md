# ER Diyagramı

```mermaid
erDiagram
    KAN_GRUBU ||--o{ BAGISCI : "sahiptir"
    KAN_GRUBU ||--|| KAN_STOK : "stoğu tutulur"
    KAN_GRUBU ||--o{ KAN_TALEBI : "talep edilir"
    BAGISCI ||--o{ BILDIRIM : "alır"
    KAN_TALEBI ||--o{ BILDIRIM : "gönderilir"

    KAN_GRUBU {
        int Id PK
        string Ad "A Rh+, 0 Rh- ... (benzersiz)"
    }

    BAGISCI {
        int Id PK
        string TcKimlikNo "benzersiz"
        string Ad
        string Soyad
        string Telefon
        string Eposta
        date DogumTarihi
        string Cinsiyet
        string Sehir
        date SonBagisTarihi "boş olabilir"
        int KanGrubuId FK
    }

    KAN_STOK {
        int Id PK
        int KanGrubuId FK "benzersiz (1-1)"
        int UniteSayisi
        datetime GuncellemeTarihi
    }

    KAN_TALEBI {
        int Id PK
        int KanGrubuId FK
        string HastaneAdi
        string Sehir
        int UniteSayisi
        string Aciliyet "Normal / Acil"
        string Durum "Açık / Karşılandı / İptal"
        datetime OlusturmaTarihi
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
```

## Varlıklar

| Varlık | Açıklama |
|---|---|
| **KanGrubu** | 8 kan grubunun tutulduğu sabit tablo (A Rh+, A Rh-, B Rh+, B Rh-, AB Rh+, AB Rh-, 0 Rh+, 0 Rh-). |
| **Bagisci** | Sisteme kayıtlı gönüllü bağışçı. |
| **KanStok** | Her kan grubu için eldeki ünite sayısı. |
| **KanTalebi** | Hastanenin belirli bir kan grubu için açtığı talep. |
| **Bildirim** | Bir talebin uygun bağışçıya iletilmesi ve bağışçının yanıtı. |

## İlişkiler

| İlişki | Tür | Açıklama |
|---|---|---|
| KanGrubu – KanStok | **1-1** | Her kan grubunun tek bir stok kaydı vardır (`KanStok.KanGrubuId` benzersiz). |
| KanGrubu – Bagisci | **1-N** | Bir kan grubunda çok bağışçı olabilir, her bağışçının tek bir kan grubu vardır. |
| KanGrubu – KanTalebi | **1-N** | Bir kan grubu için çok talep açılabilir. |
| Bagisci – KanTalebi | **N-N** | Bir talep çok bağışçıya, bir bağışçıya çok talep bildirilebilir. Bu ilişki **Bildirim** ara tablosuyla çözülür. |

## Anahtarlar ve normalizasyon

- Her tablonun kendi **birincil anahtarı (PK)** `Id` vardır. TC kimlik no gibi doğal anahtarlar değişebilir ya da gizli kalması gerekebilir, bu yüzden tanımlayıcı olarak kullanılmaz. Bunun yerine **benzersiz (unique)** kısıtla korunur.
- İlişkiler **yabancı anahtarlarla (FK)** kurulur: `KanGrubuId`, `BagisciId`, `KanTalebiId`.
- **1NF:** Her alan tek bir değer tutar. Örneğin bir bağışçının birden fazla telefonu ayrı sütunlara bölünmez.
- **2NF:** Tekli PK kullanıldığı için kısmi bağımlılık oluşmaz.
- **3NF:** Kan grubu adı bağışçı, stok ve talep tablolarında tekrar edilmez, yalnızca `KanGrubuId` tutulur. Kan grubu adı değişirse tek bir yerden güncellenir.
