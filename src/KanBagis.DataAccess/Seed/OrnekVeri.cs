using KanBagis.Entities;

namespace KanBagis.DataAccess.Seed;

/// <summary>
/// Migration ile veritabanına yüklenen örnek veri.
/// KVKK: Tüm kişi verileri SENTETİKTİR. TC kimlik numaraları "999" ile başlar
/// (gerçek TC algoritmasına uymaz), e-postalar ornek.com alan adını kullanır,
/// hastaneler gerçek kurumları temsil etmez.
/// </summary>
public static class OrnekVeri
{
    public static readonly KanGrubu[] KanGruplari =
    [
        new() { Id = 1, Ad = "A Rh+", AboGrubu = AboGrubu.A, RhPozitif = true },
        new() { Id = 2, Ad = "A Rh-", AboGrubu = AboGrubu.A, RhPozitif = false },
        new() { Id = 3, Ad = "B Rh+", AboGrubu = AboGrubu.B, RhPozitif = true },
        new() { Id = 4, Ad = "B Rh-", AboGrubu = AboGrubu.B, RhPozitif = false },
        new() { Id = 5, Ad = "AB Rh+", AboGrubu = AboGrubu.AB, RhPozitif = true },
        new() { Id = 6, Ad = "AB Rh-", AboGrubu = AboGrubu.AB, RhPozitif = false },
        new() { Id = 7, Ad = "0 Rh+", AboGrubu = AboGrubu.O, RhPozitif = true },
        new() { Id = 8, Ad = "0 Rh-", AboGrubu = AboGrubu.O, RhPozitif = false },
    ];

    private static readonly DateTime StokTarihi = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    public static readonly KanStok[] KanStoklari =
    [
        new() { Id = 1, KanGrubuId = 1, UniteSayisi = 24, GuncellemeTarihi = StokTarihi },
        new() { Id = 2, KanGrubuId = 2, UniteSayisi = 6,  GuncellemeTarihi = StokTarihi },
        new() { Id = 3, KanGrubuId = 3, UniteSayisi = 15, GuncellemeTarihi = StokTarihi },
        new() { Id = 4, KanGrubuId = 4, UniteSayisi = 3,  GuncellemeTarihi = StokTarihi },
        new() { Id = 5, KanGrubuId = 5, UniteSayisi = 8,  GuncellemeTarihi = StokTarihi },
        new() { Id = 6, KanGrubuId = 6, UniteSayisi = 1,  GuncellemeTarihi = StokTarihi },
        new() { Id = 7, KanGrubuId = 7, UniteSayisi = 30, GuncellemeTarihi = StokTarihi },
        new() { Id = 8, KanGrubuId = 8, UniteSayisi = 2,  GuncellemeTarihi = StokTarihi },
    ];

    public static readonly Hastane[] Hastaneler =
    [
        new() { Id = 1, Ad = "Örnek Devlet Hastanesi",     Sehir = "Bingöl",     Telefon = "04260000001" },
        new() { Id = 2, Ad = "Örnek Şehir Hastanesi",      Sehir = "Elazığ",     Telefon = "04240000002" },
        new() { Id = 3, Ad = "Örnek Üniversite Hastanesi", Sehir = "Diyarbakır", Telefon = "04120000003" },
    ];

    public static readonly Bagisci[] Bagiscilar =
    [
        new() { Id = 1,  TcKimlikNo = "99900000001", Ad = "Ayşe",   Soyad = "Yılmaz", Telefon = "05550000001", Eposta = "ayse.yilmaz@ornek.com",  DogumTarihi = new(1995, 4, 12),  Cinsiyet = Cinsiyet.Kadin, Kilo = 62, Sehir = "Bingöl",     KanGrubuId = 1 },
        new() { Id = 2,  TcKimlikNo = "99900000002", Ad = "Mehmet", Soyad = "Kaya",   Telefon = "05550000002", Eposta = "mehmet.kaya@ornek.com",  DogumTarihi = new(1988, 9, 3),   Cinsiyet = Cinsiyet.Erkek, Kilo = 84, Sehir = "Bingöl",     KanGrubuId = 8 },
        new() { Id = 3,  TcKimlikNo = "99900000003", Ad = "Zeynep", Soyad = "Demir",  Telefon = "05550000003", Eposta = null,                     DogumTarihi = new(2000, 1, 25),  Cinsiyet = Cinsiyet.Kadin, Kilo = 55, Sehir = "Elazığ",     KanGrubuId = 3 },
        new() { Id = 4,  TcKimlikNo = "99900000004", Ad = "Ali",    Soyad = "Çelik",  Telefon = "05550000004", Eposta = "ali.celik@ornek.com",    DogumTarihi = new(1992, 6, 30),  Cinsiyet = Cinsiyet.Erkek, Kilo = 78, Sehir = "Bingöl",     KanGrubuId = 1 },
        new() { Id = 5,  TcKimlikNo = "99900000005", Ad = "Elif",   Soyad = "Şahin",  Telefon = "05550000005", Eposta = "elif.sahin@ornek.com",   DogumTarihi = new(1998, 11, 8),  Cinsiyet = Cinsiyet.Kadin, Kilo = 58, Sehir = "Elazığ",     KanGrubuId = 8 },
        new() { Id = 6,  TcKimlikNo = "99900000006", Ad = "Mustafa",Soyad = "Arslan", Telefon = "05550000006", Eposta = null,                     DogumTarihi = new(1985, 2, 14),  Cinsiyet = Cinsiyet.Erkek, Kilo = 91, Sehir = "Diyarbakır", KanGrubuId = 5 },
        new() { Id = 7,  TcKimlikNo = "99900000007", Ad = "Fatma",  Soyad = "Doğan",  Telefon = "05550000007", Eposta = "fatma.dogan@ornek.com",  DogumTarihi = new(1990, 7, 19),  Cinsiyet = Cinsiyet.Kadin, Kilo = 67, Sehir = "Bingöl",     KanGrubuId = 7 },
        new() { Id = 8,  TcKimlikNo = "99900000008", Ad = "Emre",   Soyad = "Koç",    Telefon = "05550000008", Eposta = "emre.koc@ornek.com",     DogumTarihi = new(1997, 3, 5),   Cinsiyet = Cinsiyet.Erkek, Kilo = 73, Sehir = "Diyarbakır", KanGrubuId = 2 },
        new() { Id = 9,  TcKimlikNo = "99900000009", Ad = "Merve",  Soyad = "Aydın",  Telefon = "05550000009", Eposta = null,                     DogumTarihi = new(2001, 12, 1),  Cinsiyet = Cinsiyet.Kadin, Kilo = 52, Sehir = "Bingöl",     KanGrubuId = 4 },
        new() { Id = 10, TcKimlikNo = "99900000010", Ad = "Burak",  Soyad = "Öztürk", Telefon = "05550000010", Eposta = "burak.ozturk@ornek.com", DogumTarihi = new(1993, 8, 22),  Cinsiyet = Cinsiyet.Erkek, Kilo = 88, Sehir = "Bingöl",     KanGrubuId = 8 },
    ];

    public static readonly KanTalebi[] KanTalepleri =
    [
        new() { Id = 1, HastaneId = 1, KanGrubuId = 8, UniteSayisi = 4, Aciliyet = Aciliyet.Acil,   Durum = TalepDurumu.Acik,       OlusturmaTarihi = new(2026, 10, 10, 8, 30, 0, DateTimeKind.Utc) },
        new() { Id = 2, HastaneId = 2, KanGrubuId = 1, UniteSayisi = 2, Aciliyet = Aciliyet.Normal, Durum = TalepDurumu.Acik,       OlusturmaTarihi = new(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc) },
        new() { Id = 3, HastaneId = 1, KanGrubuId = 3, UniteSayisi = 1, Aciliyet = Aciliyet.Normal, Durum = TalepDurumu.Karsilandi, OlusturmaTarihi = new(2026, 9, 20, 11, 15, 0, DateTimeKind.Utc), KapanisTarihi = new(2026, 9, 22, 16, 0, 0, DateTimeKind.Utc) },
    ];

    public static readonly Bagis[] Bagislar =
    [
        new() { Id = 1, BagisciId = 2, HastaneId = 1, UniteSayisi = 1, BagisTarihi = new(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc) },
        new() { Id = 2, BagisciId = 3, HastaneId = 2, UniteSayisi = 1, BagisTarihi = new(2026, 5, 2, 9, 30, 0, DateTimeKind.Utc) },
        new() { Id = 3, BagisciId = 1, HastaneId = 1, UniteSayisi = 1, BagisTarihi = new(2026, 6, 15, 13, 0, 0, DateTimeKind.Utc) },
        new() { Id = 4, BagisciId = 2, HastaneId = 1, UniteSayisi = 1, BagisTarihi = new(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc) },
        new() { Id = 5, BagisciId = 7, HastaneId = 1, UniteSayisi = 1, BagisTarihi = new(2026, 8, 20, 15, 45, 0, DateTimeKind.Utc) },
    ];

    public static readonly Bildirim[] Bildirimler =
    [
        new() { Id = 1, KanTalebiId = 1, BagisciId = 2,  Mesaj = "Örnek Devlet Hastanesi'nde acil 0 Rh- kan ihtiyacı var.", GonderimTarihi = new(2026, 10, 10, 8, 35, 0, DateTimeKind.Utc), Okundu = true,  Yanit = BildirimYaniti.Kabul, YanitTarihi = new(2026, 10, 10, 9, 10, 0, DateTimeKind.Utc) },
        new() { Id = 2, KanTalebiId = 1, BagisciId = 5,  Mesaj = "Örnek Devlet Hastanesi'nde acil 0 Rh- kan ihtiyacı var.", GonderimTarihi = new(2026, 10, 10, 8, 35, 0, DateTimeKind.Utc), Okundu = true,  Yanit = BildirimYaniti.Red, YanitTarihi = new(2026, 10, 10, 11, 40, 0, DateTimeKind.Utc) },
        new() { Id = 3, KanTalebiId = 1, BagisciId = 10, Mesaj = "Örnek Devlet Hastanesi'nde acil 0 Rh- kan ihtiyacı var.", GonderimTarihi = new(2026, 10, 10, 8, 35, 0, DateTimeKind.Utc), Okundu = false, Yanit = BildirimYaniti.Beklemede },
        new() { Id = 4, KanTalebiId = 2, BagisciId = 1,  Mesaj = "Örnek Şehir Hastanesi'nde A Rh+ kan ihtiyacı var.",       GonderimTarihi = new(2026, 10, 12, 14, 5, 0, DateTimeKind.Utc), Okundu = false, Yanit = BildirimYaniti.Beklemede },
        new() { Id = 5, KanTalebiId = 2, BagisciId = 4,  Mesaj = "Örnek Şehir Hastanesi'nde A Rh+ kan ihtiyacı var.",       GonderimTarihi = new(2026, 10, 12, 14, 5, 0, DateTimeKind.Utc), Okundu = false, Yanit = BildirimYaniti.Beklemede },
    ];
}
