using System.Reflection;

namespace KanBagis.Web;

/// <summary>
/// Uygulamanın sürümü. Directory.Build.props içindeki &lt;Version&gt; değerinden gelir
/// (semantic versioning: MAJOR.MINOR.PATCH). .NET derlerken sonuna "+commit" ekleyebilir; o kısım gösterilmez.
/// </summary>
public static class SurumBilgisi
{
    public static string Surum { get; } =
        (typeof(SurumBilgisi).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
         ?? "0.0.0").Split('+')[0];
}
