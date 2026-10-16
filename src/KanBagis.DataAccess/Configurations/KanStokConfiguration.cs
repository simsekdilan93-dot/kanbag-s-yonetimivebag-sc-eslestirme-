using KanBagis.DataAccess.Seed;
using KanBagis.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KanBagis.DataAccess.Configurations;

public class KanStokConfiguration : IEntityTypeConfiguration<KanStok>
{
    public void Configure(EntityTypeBuilder<KanStok> builder)
    {
        builder.ToTable("KanStoklari", t =>
            t.HasCheckConstraint("CK_KanStok_UniteSayisi_Negatif_Olamaz", "UniteSayisi >= 0"));

        builder.HasKey(s => s.Id);

        // 1-1: Her kan grubunun tek bir stok kaydı vardır.
        // KanGrubuId üzerindeki benzersiz indeks ilişkiyi 1-1 yapan şeydir.
        builder.HasOne(s => s.KanGrubu)
               .WithOne(k => k.Stok)
               .HasForeignKey<KanStok>(s => s.KanGrubuId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.KanGrubuId).IsUnique();

        builder.HasData(OrnekVeri.KanStoklari);
    }
}
