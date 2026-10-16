using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class HastaneConfiguration : IEntityTypeConfiguration<Hastane>
{
    public void Configure(EntityTypeBuilder<Hastane> builder)
    {
        builder.ToTable("Hastaneler");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Ad).IsRequired().HasMaxLength(100);
        builder.Property(h => h.Sehir).IsRequired().HasMaxLength(30);
        builder.Property(h => h.Telefon).IsRequired().HasMaxLength(15);

        builder.HasIndex(h => new { h.Ad, h.Sehir }).IsUnique();

        builder.HasData(OrnekVeri.Hastaneler);
    }
}
