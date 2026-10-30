namespace KanBagis.Tests;

public static class TestVerisi
{
    /// <summary>
    /// Algoritmaya uygun TC kimlik numarası üretir (yalnızca testlerde, bellek içi veritabanında kullanılır).
    /// İlk 9 hane "1" + sıra numarasıdır; 10. ve 11. haneler algoritmayla hesaplanır.
    /// </summary>
    public static string GecerliTc(int sira)
    {
        var ilk9 = "1" + sira.ToString("D8");
        var d = ilk9.Select(c => c - '0').ToArray();
        var onuncu = (((d[0] + d[2] + d[4] + d[6] + d[8]) * 7 - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        var onbirinci = (d.Sum() + onuncu) % 10;
        return $"{ilk9}{onuncu}{onbirinci}";
    }
}
