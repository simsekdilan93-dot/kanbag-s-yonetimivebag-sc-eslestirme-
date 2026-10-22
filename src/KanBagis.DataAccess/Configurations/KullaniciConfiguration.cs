using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class KullaniciConfiguration : IEntityTypeConfiguration<Kullanici>
{
    public void Configure(EntityTypeBuilder<Kullanici> builder)
    {
        builder.ToTable("Kullanicilar", t =>
        {
            // Rol ile bağlı kayıt tutarlı olmalı: bağışçı hesabı hastaneye, kurum hesabı bağışçıya bağlanamaz.
            t.HasCheckConstraint("CK_Kullanici_Rol_Baglantisi",
                "(Rol = 'Bagisci' AND BagisciId IS NOT NULL AND HastaneId IS NULL) OR " +
                "(Rol = 'Kurum' AND HastaneId IS NOT NULL AND BagisciId IS NULL)");
            t.HasCheckConstraint("CK_Kullanici_BasarisizGiris", "BasarisizGirisSayisi >= 0");
        });

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Eposta).IsRequired().HasMaxLength(100);
        builder.Property(k => k.ParolaHash).IsRequired().HasMaxLength(200);

        // Giriş e-posta ile yapılır: hem benzersiz olmalı hem de hızlı bulunmalı.
        builder.HasIndex(k => k.Eposta).IsUnique();

        // 1-1 (isteğe bağlı): bir bağışçının en fazla bir hesabı olur.
        builder.HasOne(k => k.Bagisci)
               .WithOne()
               .HasForeignKey<Kullanici>(k => k.BagisciId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(k => k.BagisciId).IsUnique();

        // 1-N: bir hastanenin birden çok personel hesabı olabilir.
        builder.HasOne(k => k.Hastane)
               .WithMany()
               .HasForeignKey(k => k.HastaneId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
