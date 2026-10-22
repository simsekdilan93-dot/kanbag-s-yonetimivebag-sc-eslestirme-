using KanBagis.Entities;

namespace KanBagis.Business.Kimlik;

public record BagisciKayitIstegi(
    string Eposta,
    string Parola,
    string TcKimlikNo,
    string Ad,
    string Soyad,
    string Telefon,
    DateOnly DogumTarihi,
    Cinsiyet Cinsiyet,
    string Sehir,
    int KanGrubuId);

public record KurumKayitIstegi(
    string Eposta,
    string Parola,
    string HastaneAdi,
    string Sehir,
    string Telefon);

public record KayitSonucu(bool Basarili, string? Hata, Kullanici? Kullanici)
{
    public static KayitSonucu Tamam(Kullanici kullanici) => new(true, null, kullanici);
    public static KayitSonucu Hatali(string hata) => new(false, hata, null);
}

public enum GirisDurumu { Basarili, HataliBilgi, HesapKilitli }

public record GirisSonucu(GirisDurumu Durum, Kullanici? Kullanici = null, DateTime? KilitBitisTarihi = null);
