using KanBagis.DataAccess.Repositories;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.Tests;

public class CrudTestleri : IDisposable
{
    private readonly TestVeritabani _db = new();
    private readonly BagisciRepository _bagiscilar;

    public CrudTestleri() => _bagiscilar = new BagisciRepository(_db.Context);

    public void Dispose() => _db.Dispose();

    private static Bagisci YeniBagisci(string tc = "99900000099") => new()
    {
        TcKimlikNo = tc,
        Ad = "Deneme",
        Soyad = "Bağışçı",
        Telefon = "05550000099",
        DogumTarihi = new DateOnly(1999, 1, 1),
        Cinsiyet = Cinsiyet.Kadin,
        Kilo = 60,
        Sehir = "Bingöl",
        KanGrubuId = 1,
    };

    [Fact]
    public async Task Ornek_veri_yuklenir()
    {
        Assert.Equal(8, await _db.Context.KanGruplari.CountAsync());
        Assert.Equal(8, await _db.Context.KanStoklari.CountAsync());
        Assert.Equal(10, await _db.Context.Bagiscilar.CountAsync());
        Assert.Equal(3, await _db.Context.KanTalepleri.CountAsync());
    }

    [Fact]
    public async Task Create_ve_Read()
    {
        var eklenen = await _bagiscilar.EkleAsync(YeniBagisci());
        _db.TakibiTemizle();

        var okunan = await _bagiscilar.IdIleGetirAsync(eklenen.Id);

        Assert.NotNull(okunan);
        Assert.Equal("99900000099", okunan.TcKimlikNo);
        Assert.Equal(11, (await _bagiscilar.TumunuGetirAsync()).Count);
    }

    [Fact]
    public async Task Update()
    {
        var bagisci = (await _bagiscilar.IdIleGetirAsync(1))!;
        bagisci.Telefon = "05559999999";

        await _bagiscilar.GuncelleAsync(bagisci);
        _db.TakibiTemizle();

        Assert.Equal("05559999999", (await _bagiscilar.IdIleGetirAsync(1))!.Telefon);
    }

    [Fact]
    public async Task Delete()
    {
        var eklenen = await _bagiscilar.EkleAsync(YeniBagisci());

        Assert.True(await _bagiscilar.SilAsync(eklenen.Id));
        Assert.Null(await _bagiscilar.IdIleGetirAsync(eklenen.Id));
        Assert.False(await _bagiscilar.SilAsync(eklenen.Id));
    }

    [Fact]
    public async Task Tc_ile_bagisci_bulunur_ve_kan_grubu_gelir()
    {
        var bagisci = await _bagiscilar.TcIleGetirAsync("99900000002");

        Assert.NotNull(bagisci);
        Assert.Equal("0 Rh-", bagisci.KanGrubu.Ad);
    }

    [Fact]
    public async Task Aday_sorgusu_kan_grubu_ve_ile_gore_suzer_ve_son_bagisi_getirir()
    {
        // Bingöl'deki 0 Rh- (Id 8) bağışçılar: Burak (hiç bağış yok) ve Mehmet (son bağış 01.07.2026).
        var adaylar = await _bagiscilar.AdaylariGetirAsync([8], ["Bingöl"]);

        Assert.Equal(["Burak", "Mehmet"], adaylar.Select(a => a.Bagisci.Ad));
        Assert.Null(adaylar[0].SonBagisTarihi);
        Assert.Equal(new DateTime(2026, 7, 1, 11, 0, 0), adaylar[1].SonBagisTarihi);

        // Birden çok kan grubu: 0 Rh- (8) ve A Rh+ (1) → Ayşe ve Ali de gelir.
        var coklu = await _bagiscilar.AdaylariGetirAsync([1, 8], ["Bingöl"]);
        Assert.Equal(4, coklu.Count);

        // Birden çok il: Bingöl + Elazığ'daki 0 Rh- → Burak, Elif, Mehmet.
        Assert.Equal(3, (await _bagiscilar.AdaylariGetirAsync([8], ["Bingöl", "Elazığ"])).Count);

        // Başka il: Elazığ'daki 0 Rh- yalnızca Elif.
        Assert.Equal(["Elif"], (await _bagiscilar.AdaylariGetirAsync([8], ["Elazığ"])).Select(a => a.Bagisci.Ad));
    }

    [Fact]
    public async Task Son_bagis_tarihi_bagis_tablosundan_hesaplanir()
    {
        Assert.Equal(new DateTime(2026, 7, 1, 11, 0, 0), await _bagiscilar.SonBagisTarihiAsync(2));
        Assert.Null(await _bagiscilar.SonBagisTarihiAsync(10));
    }
}
