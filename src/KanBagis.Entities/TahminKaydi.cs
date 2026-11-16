namespace KanBagis.Entities;

public enum TahminKaynagi
{
    Istatistik,            // yapay zekâ kapalı ya da çıktısı kullanılamadı
    YapayZeka,             // yapay zekâ çıktısı doğrulamadan olduğu gibi geçti
    YapayZekaDuzeltildi,   // yapay zekâ çıktısı kullanıldı, ama bazı değerler doğrulamada düzeltildi
}

/// <summary>
/// Her tahmin çalıştırmasının kaydı (AI kullanım günlüğü). İstem, modelin ham çıktısı ve doğrulama
/// notları saklanır; böylece hangi tahminin nereden geldiği sonradan denetlenebilir (şeffaflık).
/// KVKK: İstemde kişisel veri yoktur, yalnızca kan grubu bazında toplu sayılar vardır.
/// </summary>
public class TahminKaydi : IEntity
{
    public int Id { get; set; }
    public DateTime Tarih { get; set; }
    public TahminKaynagi Kaynak { get; set; }
    public string? Model { get; set; }

    /// <summary>Modele gönderilen (yapay zekâ kapalıysa gönderilecek olan) istem: sistem + kullanıcı.</summary>
    public string? Istem { get; set; }

    /// <summary>Modelin döndürdüğü ham metin (doğrulamadan önce).</summary>
    public string? HamCikti { get; set; }

    /// <summary>Doğrulamada yapılan düzeltmeler ve hatalar (her satır bir not).</summary>
    public string? DogrulamaNotlari { get; set; }

    /// <summary>Ekranda gösterilen son sonuç (JSON).</summary>
    public string SonucJson { get; set; } = "{}";

    public int SureMilisaniye { get; set; }
}
