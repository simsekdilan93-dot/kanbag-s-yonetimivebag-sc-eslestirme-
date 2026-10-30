using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class KanGrubuConfiguration : IEntityTypeConfiguration<KanGrubu>
{
    public void Configure(EntityTypeBuilder<KanGrubu> builder)
    {
        builder.ToTable("KanGruplari");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Ad).IsRequired().HasMaxLength(10);

        // Aynı kan grubu iki kez eklenemez.
        builder.HasIndex(k => k.Ad).IsUnique();
        builder.HasIndex(k => new { k.AboGrubu, k.RhPozitif }).IsUnique();

        builder.HasData(OrnekVeri.KanGruplari);
    }
}
