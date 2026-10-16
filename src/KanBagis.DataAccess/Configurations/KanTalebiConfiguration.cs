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
        });

        builder.HasKey(t => t.Id);

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
