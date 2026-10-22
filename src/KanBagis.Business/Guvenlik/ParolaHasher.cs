using System.Security.Cryptography;

namespace KanBagis.Business.Guvenlik;

public interface IParolaHasher
{
    string Hashle(string parola);
    bool Dogrula(string parola, string kayitliHash);
}

/// <summary>
/// PBKDF2-HMAC-SHA256 ile tuzlu parola hash'i.
///
/// Neden düz SHA-256 değil? SHA-256 çok hızlıdır; saldırgan saniyede milyarlarca tahmin dener.
/// PBKDF2 hesabı bilerek <see cref="VarsayilanIterasyon"/> kez tekrarlar ve her tahmini yavaşlatır.
///
/// Tuz (salt): Her parola için rastgele üretilen 16 bayt. Aynı parolayı kullanan iki kişinin
/// hash'leri farklı çıkar; önceden hesaplanmış hash tabloları (rainbow table) işe yaramaz.
/// Tuz gizli değildir, hash ile birlikte saklanır.
///
/// Saklanan biçim:  PBKDF2-SHA256$iterasyon$tuz(Base64)$hash(Base64)
/// </summary>
public sealed class Pbkdf2ParolaHasher(int iterasyon = Pbkdf2ParolaHasher.VarsayilanIterasyon) : IParolaHasher
{
    /// <summary>OWASP'ın PBKDF2-HMAC-SHA256 için önerdiği değer.</summary>
    public const int VarsayilanIterasyon = 600_000;

    private const string Onek = "PBKDF2-SHA256";
    private const int TuzBoyutu = 16;   // 128 bit
    private const int HashBoyutu = 32;  // 256 bit
    private static readonly HashAlgorithmName Algoritma = HashAlgorithmName.SHA256;

    public string Hashle(string parola)
    {
        var tuz = RandomNumberGenerator.GetBytes(TuzBoyutu);   // kriptografik olarak güvenli rastgele
        var hash = Rfc2898DeriveBytes.Pbkdf2(parola, tuz, iterasyon, Algoritma, HashBoyutu);

        return $"{Onek}${iterasyon}${Convert.ToBase64String(tuz)}${Convert.ToBase64String(hash)}";
    }

    public bool Dogrula(string parola, string kayitliHash)
    {
        var parcalar = kayitliHash.Split('$');
        if (parcalar.Length != 4 || parcalar[0] != Onek || !int.TryParse(parcalar[1], out var kayitliIterasyon))
            return false;

        byte[] tuz, beklenen;
        try
        {
            tuz = Convert.FromBase64String(parcalar[2]);
            beklenen = Convert.FromBase64String(parcalar[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        // Girilen parola, KAYITLI tuz ve iterasyonla yeniden hash'lenir ve karşılaştırılır.
        var hesaplanan = Rfc2898DeriveBytes.Pbkdf2(parola, tuz, kayitliIterasyon, Algoritma, beklenen.Length);

        // Sabit süreli karşılaştırma: ilk farklı baytta erken dönmez, böylece süre ölçerek
        // hash'i tahmin etmeye çalışan (timing attack) saldırgana ipucu vermez.
        return CryptographicOperations.FixedTimeEquals(hesaplanan, beklenen);
    }
}
