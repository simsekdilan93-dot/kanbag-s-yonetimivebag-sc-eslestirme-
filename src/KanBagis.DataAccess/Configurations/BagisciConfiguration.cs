using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class BagisciConfiguration : IEntityTypeConfiguration<Bagisci>
{
    public void Configure(EntityTypeBuilder<Bagisci> builder)
    {
        builder.ToTable("Bagiscilar", t =>
        {
            t.HasCheckConstraint("CK_Bagisci_TcKimlikNo_11Hane", "length(TcKimlikNo) = 11");
            t.HasCheckConstraint("CK_Bagisci_Kilo", "Kilo BETWEEN 30 AND 250");
        });

        builder.HasKey(b => b.Id);
        builder.Property(b => b.TcKimlikNo).IsRequired().HasMaxLength(11).IsFixedLength();
        builder.Property(b => b.Ad).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Soyad).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Telefon).IsRequired().HasMaxLength(15);
        builder.Property(b => b.Eposta).HasMaxLength(100);
        builder.Property(b => b.Sehir).IsRequired().HasMaxLength(30);

        // TC kimlik no benzersiz: aynı kişi iki kez kayıt olamaz.
        builder.HasIndex(b => b.TcKimlikNo).IsUnique();

        // Eşleştirme sorgusu "bu kan grubundan, bu şehirdeki bağışçılar" diye arar;
        // bileşik indeks bu sorguyu tüm tabloyu taramadan yanıtlar.
        builder.HasIndex(b => new { b.KanGrubuId, b.Sehir });

        // 1-N: Bir kan grubunun çok bağışçısı olur. Bağışçısı olan kan grubu silinemez.
        builder.HasOne(b => b.KanGrubu)
               .WithMany(k => k.Bagiscilar)
               .HasForeignKey(b => b.KanGrubuId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(OrnekVeri.Bagiscilar);
    }
}
