using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class ParolaSifirlamaKoduConfiguration : IEntityTypeConfiguration<ParolaSifirlamaKodu>
{
    public void Configure(EntityTypeBuilder<ParolaSifirlamaKodu> builder)
    {
        builder.ToTable("ParolaSifirlamaKodlari", t =>
        {
            t.HasCheckConstraint("CK_ParolaSifirlama_Deneme", "DenemeSayisi >= 0");
            t.HasCheckConstraint("CK_ParolaSifirlama_Sure", "SonKullanmaTarihi > OlusturmaTarihi");
        });

        builder.HasKey(k => k.Id);
        builder.Property(k => k.KodHash).IsRequired().HasMaxLength(200);

        // Bir kullanıcının aynı anda yalnızca BİR açık kodu olur (yeni kod isteyince eskisi kapatılır).
        builder.HasIndex(k => k.KullaniciId).IsUnique().HasFilter("KapanisTarihi IS NULL");

        // Hesap silinirse kodları da silinir (kodun hesaptan bağımsız anlamı yok).
        builder.HasOne(k => k.Kullanici).WithMany().HasForeignKey(k => k.KullaniciId).OnDelete(DeleteBehavior.Cascade);
    }
}
