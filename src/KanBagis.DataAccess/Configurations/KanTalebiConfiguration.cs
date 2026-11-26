using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class KanTalebiConfiguration : IEntityTypeConfiguration<KanTalebi>
{
    public void Configure(EntityTypeBuilder<KanTalebi> builder)
    {
        builder.ToTable("KanTalepleri", t =>
        {
            t.HasCheckConstraint("CK_KanTalebi_UniteSayisi_Pozitif", "UniteSayisi > 0");
            t.HasCheckConstraint("CK_KanTalebi_Durum", "Durum IN ('Acik', 'Karsilandi', 'Iptal')");
            t.HasCheckConstraint("CK_KanTalebi_Aciliyet", "Aciliyet IN ('Normal', 'Acil')");
            // Açık talebin kapanış tarihi olmaz; kapanan talebin mutlaka olur.
            t.HasCheckConstraint("CK_KanTalebi_KapanisTarihi",
                "(Durum = 'Acik' AND KapanisTarihi IS NULL) OR (Durum <> 'Acik' AND KapanisTarihi IS NOT NULL)");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Aciklama).HasMaxLength(300);

        // Mükerrer talep kuralı VERİTABANINDA: bir hastanenin bir kan grubu için en fazla BİR açık talebi olur.
        // Yalnızca serviste "önce kontrol et, sonra ekle" yapılınca aynı anda gelen istekler ikisi de kontrolü geçip
        // iki açık talep oluşturabiliyordu (H13-04). Filtreli benzersiz indeks bunu kesin olarak engeller;
        // aynı zamanda "açık talep var mı?" sorgusunun indeksidir.
        builder.HasIndex(t => new { t.HastaneId, t.KanGrubuId })
               .IsUnique()
               .HasFilter("Durum = 'Acik'")
               .HasDatabaseName("UX_KanTalebi_HastaneBasinaTekAcikTalep");

        // "Açık talepleri kan grubuna göre listele" sorgusu için.
        builder.HasIndex(t => new { t.Durum, t.KanGrubuId });

        builder.HasOne(t => t.Hastane)
               .WithMany(h => h.Talepler)
               .HasForeignKey(t => t.HastaneId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.KanGrubu)
               .WithMany(k => k.Talepler)
               .HasForeignKey(t => t.KanGrubuId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(OrnekVeri.KanTalepleri);
    }
}
