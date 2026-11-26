using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TekAcikTalepKurali : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KanTalepleri_HastaneId_KanGrubuId_Durum",
                table: "KanTalepleri");

            // Var olan veritabanında eski hata yüzünden mükerrer açık talep kalmışsa indeks oluşturulamaz.
            // Her hastane + kan grubu için EN ESKİ açık talep korunur; diğerleri silinmez, not düşülerek iptal edilir
            // (bildirim ve bağış kayıtları bağlı olabilir, silmek ilişkili kayıtları yetim bırakırdı).
            migrationBuilder.Sql("""
                UPDATE KanTalepleri
                SET Durum = 'Iptal',
                    KapanisTarihi = strftime('%Y-%m-%d %H:%M:%S', 'now'),
                    Aciklama = substr(COALESCE(Aciklama || ' ', '') || '[Mükerrer açık talep: otomatik iptal]', 1, 300)
                WHERE Durum = 'Acik'
                  AND EXISTS (SELECT 1 FROM KanTalepleri d
                              WHERE d.Durum = 'Acik' AND d.HastaneId = KanTalepleri.HastaneId
                                AND d.KanGrubuId = KanTalepleri.KanGrubuId AND d.Id < KanTalepleri.Id);
                """);

            migrationBuilder.CreateIndex(
                name: "UX_KanTalebi_HastaneBasinaTekAcikTalep",
                table: "KanTalepleri",
                columns: new[] { "HastaneId", "KanGrubuId" },
                unique: true,
                filter: "Durum = 'Acik'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_KanTalebi_HastaneBasinaTekAcikTalep",
                table: "KanTalepleri");

            migrationBuilder.CreateIndex(
                name: "IX_KanTalepleri_HastaneId_KanGrubuId_Durum",
                table: "KanTalepleri",
                columns: new[] { "HastaneId", "KanGrubuId", "Durum" });
        }
    }
}
