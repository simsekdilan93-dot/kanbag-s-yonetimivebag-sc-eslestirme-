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
    public async Task Eslestirme_sorgusu_son_bagis_tarihini_dikkate_alir()
    {
        // Bingöl'deki 0 Rh- bağışçılar: Mehmet (son bağışı 01.07.2026) ve Burak (hiç bağış yok).
        var hepsi = await _bagiscilar.UygunBagiscilariGetirAsync(8, "Bingöl", new DateTime(2026, 7, 16));
        Assert.Equal(["Burak", "Mehmet"], hepsi.Select(b => b.Ad));

        // Sınır 01.06.2026 olursa Mehmet o tarihten sonra bağış yaptığı için elenir.
        var uygunlar = await _bagiscilar.UygunBagiscilariGetirAsync(8, "Bingöl", new DateTime(2026, 6, 1));
        Assert.Equal(["Burak"], uygunlar.Select(b => b.Ad));
    }

    [Fact]
    public async Task Son_bagis_tarihi_bagis_tablosundan_hesaplanir()
    {
        Assert.Equal(new DateTime(2026, 7, 1, 11, 0, 0), await _bagiscilar.SonBagisTarihiAsync(2));
        Assert.Null(await _bagiscilar.SonBagisTarihiAsync(10));
    }
}
