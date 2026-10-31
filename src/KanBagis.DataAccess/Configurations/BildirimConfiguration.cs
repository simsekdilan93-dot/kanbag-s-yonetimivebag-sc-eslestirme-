using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class BildirimConfiguration : IEntityTypeConfiguration<Bildirim>
{
    public void Configure(EntityTypeBuilder<Bildirim> builder)
    {
        builder.ToTable("Bildirimler", t =>
        {
            t.HasCheckConstraint("CK_Bildirim_Yanit", "Yanit IN ('Beklemede', 'Kabul', 'Red')");
            // Yanıtlanmış bildirimin yanıt tarihi olmak zorunda; bekleyenin olmaz.
            t.HasCheckConstraint("CK_Bildirim_YanitTarihi",
                "(Yanit = 'Beklemede' AND YanitTarihi IS NULL) OR (Yanit <> 'Beklemede' AND YanitTarihi IS NOT NULL)");
        });

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Mesaj).IsRequired().HasMaxLength(500);

        // N-N: Bagisci <-> KanTalebi. Bildirim ara tablodur, iki tarafa da FK taşır.
        // Aynı talep aynı bağışçıya iki kez bildirilemez.
        builder.HasIndex(b => new { b.BagisciId, b.KanTalebiId }).IsUnique();

        // "Bağışçının açık bir talepte kabulü var mı?" kuralı için.
        builder.HasIndex(b => new { b.BagisciId, b.Yanit });

        // Bağışçı ya da talep silinirse ona ait bildirimlerin anlamı kalmaz: birlikte silinir.
        builder.HasOne(b => b.Bagisci)
               .WithMany(x => x.Bildirimler)
               .HasForeignKey(b => b.BagisciId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.KanTalebi)
               .WithMany(t => t.Bildirimler)
               .HasForeignKey(b => b.KanTalebiId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(OrnekVeri.Bildirimler);
    }
}
