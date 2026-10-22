using KanBagis.Business.Guvenlik;

namespace KanBagis.Tests;

public class ParolaHasherTestleri
{
    // Testler hızlı çalışsın diye düşük iterasyon; uygulama 600.000 kullanır.
    private readonly Pbkdf2ParolaHasher _hasher = new(iterasyon: 1_000);

    [Fact]
    public void Hash_parolanin_kendisini_icermez()
    {
        var hash = _hasher.Hashle("Gizli1234");

        Assert.DoesNotContain("Gizli1234", hash);
        Assert.StartsWith("PBKDF2-SHA256$1000$", hash);
        Assert.Equal(4, hash.Split('$').Length);
    }

    [Fact]
    public void Ayni_parola_her_seferinde_farkli_hash_uretir_TUZ()
    {
        var hash1 = _hasher.Hashle("Gizli1234");
        var hash2 = _hasher.Hashle("Gizli1234");

        Assert.NotEqual(hash1, hash2);               // tuzlar farklı → hash'ler farklı
        Assert.True(_hasher.Dogrula("Gizli1234", hash1));
        Assert.True(_hasher.Dogrula("Gizli1234", hash2));
    }

    [Theory]
    [InlineData("gizli1234")]   // büyük/küçük harf farkı
    [InlineData("Gizli12345")]
    [InlineData("")]
    public void Yanlis_parola_dogrulanmaz(string yanlis)
    {
        var hash = _hasher.Hashle("Gizli1234");

        Assert.False(_hasher.Dogrula(yanlis, hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Gizli1234")]                       // düz metin saklanmış gibi
    [InlineData("PBKDF2-SHA256$abc$xx$yy")]
    [InlineData("MD5$1$AAAA$BBBB")]
    public void Bozuk_hash_kabul_edilmez(string bozuk)
    {
        Assert.False(_hasher.Dogrula("Gizli1234", bozuk));
    }

    [Fact]
    public void Uygulamanin_varsayilani_OWASP_onerisini_karsilar()
    {
        var hash = new Pbkdf2ParolaHasher().Hashle("Gizli1234");

        Assert.Contains("$600000$", hash);
    }
}
