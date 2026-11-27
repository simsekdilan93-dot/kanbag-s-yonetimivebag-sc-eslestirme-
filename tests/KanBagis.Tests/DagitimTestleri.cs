using System.Net;
using System.Text.RegularExpressions;

namespace KanBagis.Tests;

/// <summary>
/// Hafta 14 — Dağıtım hazırlığı: depoda gizli anahtar yok, örnek yapılandırma dosyaları gizli değer içermiyor,
/// gerekli belgeler mevcut. Depo kök klasörü testin çalıştığı klasörden yukarı doğru aranır.
/// </summary>
public class DagitimTestleri
{
    private static readonly string Kok = DepoKokunuBul();

    private static string DepoKokunuBul()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "KanBagis.slnx"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Depo kökü bulunamadı.");
    }

    /// <summary>Depoya giren metin dosyaları (derleme çıktıları, veritabanı ve git klasörü hariç).</summary>
    private static IEnumerable<string> DepoDosyalari() =>
        Directory.EnumerateFiles(Kok, "*", SearchOption.AllDirectories)
            .Where(f => !Regex.IsMatch(f, @"[\\/](bin|obj|\.git|\.vs|node_modules|TestResults)[\\/]"))
            .Where(f => Regex.IsMatch(f, @"\.(cs|cshtml|json|md|js|css|props|slnx|csproj|py|runsettings|example|txt|yml|yaml|ps1|sh|sql)$|\.env", RegexOptions.IgnoreCase));

    // Desenler parçalı yazılır ki bu dosya kendini "anahtar içeriyor" diye yakalamasın.
    public static TheoryData<string, string> GizliDesenler => new()
    {
        { "Anthropic API anahtarı", "sk-" + @"ant-[A-Za-z0-9_\-]{20,}" },
        { "Twilio hesap kimliği", @"\bA" + @"C[0-9a-f]{32}\b" },
        { "Özel anahtar", "-----BEGIN [A-Z ]*PRIV" + "ATE KEY-----" },
        { "Dolu AuthToken", "\"Auth" + "Token\"\\s*:\\s*\"[^\"]+\"" },
        { "Dolu ApiAnahtari", "\"Api" + "Anahtari\"\\s*:\\s*\"[^\"]+\"" },
        { "Bağlantı dizesinde parola", "(?i)pass" + "word\\s*=\\s*[^;\"\\s]{3,}" },
    };

    [Theory]
    [MemberData(nameof(GizliDesenler))]
    public void Depoda_gizli_anahtar_yok(string tur, string desen)
    {
        var bulunan = DepoDosyalari()
            .Where(f => Regex.IsMatch(File.ReadAllText(f), desen))
            .Select(f => Path.GetRelativePath(Kok, f))
            .ToList();

        Assert.True(bulunan.Count == 0, $"{tur} bulundu: {string.Join(", ", bulunan)}");
    }

    [Fact]
    public void Ornek_ortam_degiskeni_dosyasi_var_ve_degerleri_bos()
    {
        var yol = Path.Combine(Kok, ".env.example");
        Assert.True(File.Exists(yol), ".env.example yok");

        var satirlar = File.ReadAllLines(yol).Where(s => !string.IsNullOrWhiteSpace(s) && !s.TrimStart().StartsWith('#')).ToList();
        Assert.NotEmpty(satirlar);
        // Gizli değerler (anahtar, parola, token) örnek dosyada BOŞ olmalı.
        foreach (var s in satirlar.Where(s => Regex.IsMatch(s, "^[^=]*(API_KEY|TOKEN|SID|PAROLA|PASSWORD|APIANAHTARI|GONDEREN)[^=]*=", RegexOptions.IgnoreCase)))
            Assert.Matches(@"^[A-Za-z0-9_]+=$", s);
    }

    [Fact]
    public void Gercek_ortam_dosyasi_ve_veritabani_gitignore_ile_disarida()
    {
        var gitignore = File.ReadAllText(Path.Combine(Kok, ".gitignore"));
        Assert.Matches(@"(?m)^\.env
?$", gitignore);
        Assert.Matches(@"(?m)^\*\.db
?$", gitignore);
        Assert.Matches(@"(?m)^anahtarlar/
?$", gitignore);
    }

    [Theory]
    [InlineData("README.md", "## Kurulum")]
    [InlineData("README.md", "## Yapılandırma")]
    [InlineData("docs/Dagitim.md", "Ortam değişkenleri")]
    [InlineData("docs/Uc-Noktalar.md", "/saglik")]
    [InlineData("docs/Mimari.md", "Stok")]
    [InlineData("docs/Erisilebilirlik.md", "axe")]
    public void Belgeler_mevcut_ve_guncel(string dosya, string icermeli)
    {
        var yol = Path.Combine(Kok, dosya);
        Assert.True(File.Exists(yol), $"{dosya} yok");
        Assert.Contains(icermeli, File.ReadAllText(yol));
    }

    [Fact]
    public void Readmedeki_ortam_degiskenleri_kodda_kullanilanlarla_ayni()
    {
        // Belge eskimesin: kodda okunan her yapılandırma bölümü README/Dagitim belgesinde geçmeli.
        var belge = File.ReadAllText(Path.Combine(Kok, "docs/Dagitim.md"));
        var kodda = Directory.EnumerateFiles(Path.Combine(Kok, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), "Bolum = \"([A-Za-z:]+)\"").Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();
        Assert.NotEmpty(kodda);

        foreach (var bolum in kodda)
            Assert.Contains(bolum.Replace(":", "__"), belge);
    }
}
