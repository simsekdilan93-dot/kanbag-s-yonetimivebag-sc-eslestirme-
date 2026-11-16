using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class TahminKaydiConfiguration : IEntityTypeConfiguration<TahminKaydi>
{
    public void Configure(EntityTypeBuilder<TahminKaydi> builder)
    {
        builder.ToTable("TahminKayitlari", t =>
            t.HasCheckConstraint("CK_TahminKaydi_Sure", "SureMilisaniye >= 0"));

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Model).HasMaxLength(50);
        builder.Property(k => k.SonucJson).IsRequired();

        // "En son tahmin" sorgusu için.
        builder.HasIndex(k => k.Tarih);
    }
}
