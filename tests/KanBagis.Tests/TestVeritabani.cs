using KanBagis.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.Tests;

/// <summary>
/// Her test için bellekte (RAM) sıfırdan bir SQLite veritabanı açar ve gerçek
/// migration'ları uygular. Testler birbirini etkilemez, diskte dosya oluşmaz.
/// </summary>
public sealed class TestVeritabani : IDisposable
{
    private readonly SqliteConnection _baglanti;
    public KanBagisDbContext Context { get; }

    public TestVeritabani()
    {
        // "Foreign Keys=True": SQLite'ta yabancı anahtar denetimini açar.
        _baglanti = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        _baglanti.Open();

        var options = new DbContextOptionsBuilder<KanBagisDbContext>()
            .UseSqlite(_baglanti)
            .Options;

        Context = new KanBagisDbContext(options);
        Context.Database.Migrate();
    }

    /// <summary>Takip edilen nesneleri bırakır; sonraki okuma doğrudan veritabanından yapılır.</summary>
    public void TakibiTemizle() => Context.ChangeTracker.Clear();

    public void Dispose()
    {
        Context.Dispose();
        _baglanti.Dispose();
    }
}
