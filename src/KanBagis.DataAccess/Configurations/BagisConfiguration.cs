using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class BagisConfiguration : IEntityTypeConfiguration<Bagis>
{
    public void Configure(EntityTypeBuilder<Bagis> builder)
    {
        builder.ToTable("Bagislar", t =>
            t.HasCheckConstraint("CK_Bagis_UniteSayisi_Pozitif", "UniteSayisi > 0"));

        builder.HasKey(b => b.Id);

        // "Bağışçının son bağış tarihi" sorgusu için (3 ay kuralı).
        builder.HasIndex(b => new { b.BagisciId, b.BagisTarihi });

        // Bağış geçmişi kayıt niteliğinde: bağışı olan bağışçı/hastane silinemez.
        builder.HasOne(b => b.Bagisci)
               .WithMany(x => x.Bagislar)
               .HasForeignKey(b => b.BagisciId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Hastane)
               .WithMany(h => h.Bagislar)
               .HasForeignKey(b => b.HastaneId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(OrnekVeri.Bagislar);
    }
}
