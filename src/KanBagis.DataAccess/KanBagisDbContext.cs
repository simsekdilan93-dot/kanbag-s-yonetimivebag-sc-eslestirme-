using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanBagis.DataAccess;

public class KanBagisDbContext(DbContextOptions<KanBagisDbContext> options) : DbContext(options)
{
    public DbSet<KanGrubu> KanGruplari => Set<KanGrubu>();
    public DbSet<Bagisci> Bagiscilar => Set<Bagisci>();
    public DbSet<Hastane> Hastaneler => Set<Hastane>();
    public DbSet<KanStok> KanStoklari => Set<KanStok>();
    public DbSet<KanTalebi> KanTalepleri => Set<KanTalebi>();
    public DbSet<Bagis> Bagislar => Set<Bagis>();
    public DbSet<Bildirim> Bildirimler => Set<Bildirim>();
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurations klasöründeki tüm IEntityTypeConfiguration sınıflarını uygular.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KanBagisDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enum'lar veritabanında sayı yerine okunabilir metin olarak saklanır ("Acil", "Kabul"...).
        configurationBuilder.Properties<Cinsiyet>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<AboGrubu>().HaveConversion<string>().HaveMaxLength(2);
        configurationBuilder.Properties<Aciliyet>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<TalepDurumu>().HaveConversion<string>().HaveMaxLength(15);
        configurationBuilder.Properties<BildirimYaniti>().HaveConversion<string>().HaveMaxLength(15);
        configurationBuilder.Properties<KullaniciRolu>().HaveConversion<string>().HaveMaxLength(10);
    }
}
