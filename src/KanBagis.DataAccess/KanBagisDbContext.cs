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
    public DbSet<TahminKaydi> TahminKayitlari => Set<TahminKaydi>();
    public DbSet<StokHareketi> StokHareketleri => Set<StokHareketi>();
    public DbSet<StokUyarisi> StokUyarilari => Set<StokUyarisi>();
    public DbSet<ParolaSifirlamaKodu> ParolaSifirlamaKodlari => Set<ParolaSifirlamaKodu>();

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
        configurationBuilder.Properties<DisKanalDurumu>().HaveConversion<string>().HaveMaxLength(12);
        configurationBuilder.Properties<HareketTuru>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<HareketNedeni>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<StokDurumu>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<TahminKaynagi>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<KullaniciRolu>().HaveConversion<string>().HaveMaxLength(10);
    }
}
