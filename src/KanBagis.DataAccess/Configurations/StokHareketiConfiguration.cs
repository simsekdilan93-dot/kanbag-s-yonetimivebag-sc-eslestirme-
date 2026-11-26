using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class StokHareketiConfiguration : IEntityTypeConfiguration<StokHareketi>
{
    public void Configure(EntityTypeBuilder<StokHareketi> builder)
    {
        // İş kuralları veritabanında da korunur: uygulamada bir hata olsa bile tutarsız hareket yazılamaz.
        builder.ToTable("StokHareketleri", t =>
        {
            t.HasCheckConstraint("CK_StokHareketi_Miktar_Pozitif", "Miktar > 0");
            t.HasCheckConstraint("CK_StokHareketi_Stok_Negatif_Olamaz", "OncekiStok >= 0 AND SonrakiStok >= 0");
            // Sonraki stok = önceki ± miktar (türe göre).
            t.HasCheckConstraint("CK_StokHareketi_Hesap",
                "(Tur = 'Giris' AND SonrakiStok = OncekiStok + Miktar) OR (Tur = 'Cikis' AND SonrakiStok = OncekiStok - Miktar)");
            // Neden türle uyumlu olmalı.
            t.HasCheckConstraint("CK_StokHareketi_Neden_Tur",
                "(Tur = 'Giris' AND Neden IN ('Bagis','TransferGiris','SayimFazlasi')) OR " +
                "(Tur = 'Cikis' AND Neden IN ('Transfuzyon','TransferCikis','Imha','SayimEksigi'))");
            // Bağış hareketi bir bağışa bağlıdır; diğerleri bağlı olamaz.
            t.HasCheckConstraint("CK_StokHareketi_Bagis",
                "(Neden = 'Bagis' AND BagisId IS NOT NULL) OR (Neden <> 'Bagis' AND BagisId IS NULL)");
        });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Aciklama).HasMaxLength(250);

        builder.HasIndex(h => new { h.KanGrubuId, h.Tarih });
        builder.HasIndex(h => h.BagisId).IsUnique().HasFilter("BagisId IS NOT NULL");   // bir bağış = bir giriş

        // Hareketler denetim kaydıdır: bağlı oldukları kayıtların silinmesini engeller.
        builder.HasOne(h => h.KanGrubu).WithMany().HasForeignKey(h => h.KanGrubuId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.Hastane).WithMany().HasForeignKey(h => h.HastaneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.Kullanici).WithMany().HasForeignKey(h => h.KullaniciId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(h => h.Bagis).WithMany().HasForeignKey(h => h.BagisId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StokUyarisiConfiguration : IEntityTypeConfiguration<StokUyarisi>
{
    public void Configure(EntityTypeBuilder<StokUyarisi> builder)
    {
        builder.ToTable("StokUyarilari", t =>
            t.HasCheckConstraint("CK_StokUyarisi_Seviye", "Seviye IN ('Dusuk','Kritik')"));

        builder.HasKey(u => u.Id);
        builder.Ignore(u => u.Acik);

        // Bir kan grubunun aynı anda yalnızca bir AÇIK uyarısı olabilir.
        builder.HasIndex(u => u.KanGrubuId).IsUnique().HasFilter("KapanisTarihi IS NULL");

        builder.HasOne(u => u.KanGrubu).WithMany().HasForeignKey(u => u.KanGrubuId).OnDelete(DeleteBehavior.Restrict);
        // Başlangıç uyarıları sabit veriyle değil, migration'da o anki stoktan SQL ile üretilir
        // (var olan bir veritabanında stok örnek veriden farklı olabilir).
    }
}
