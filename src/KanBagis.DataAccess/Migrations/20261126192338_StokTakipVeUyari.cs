using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class StokTakipVeUyari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KritikEsik",
                table: "KanStoklari",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UyariEsigi",
                table: "KanStoklari",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StokHareketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tur = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Neden = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Miktar = table.Column<int>(type: "INTEGER", nullable: false),
                    OncekiStok = table.Column<int>(type: "INTEGER", nullable: false),
                    SonrakiStok = table.Column<int>(type: "INTEGER", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    KanGrubuId = table.Column<int>(type: "INTEGER", nullable: false),
                    HastaneId = table.Column<int>(type: "INTEGER", nullable: true),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: true),
                    BagisId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokHareketleri", x => x.Id);
                    table.CheckConstraint("CK_StokHareketi_Bagis", "(Neden = 'Bagis' AND BagisId IS NOT NULL) OR (Neden <> 'Bagis' AND BagisId IS NULL)");
                    table.CheckConstraint("CK_StokHareketi_Hesap", "(Tur = 'Giris' AND SonrakiStok = OncekiStok + Miktar) OR (Tur = 'Cikis' AND SonrakiStok = OncekiStok - Miktar)");
                    table.CheckConstraint("CK_StokHareketi_Miktar_Pozitif", "Miktar > 0");
                    table.CheckConstraint("CK_StokHareketi_Neden_Tur", "(Tur = 'Giris' AND Neden IN ('Bagis','TransferGiris','SayimFazlasi')) OR (Tur = 'Cikis' AND Neden IN ('Transfuzyon','TransferCikis','Imha','SayimEksigi'))");
                    table.CheckConstraint("CK_StokHareketi_Stok_Negatif_Olamaz", "OncekiStok >= 0 AND SonrakiStok >= 0");
                    table.ForeignKey(
                        name: "FK_StokHareketleri_Bagislar_BagisId",
                        column: x => x.BagisId,
                        principalTable: "Bagislar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_Hastaneler_HastaneId",
                        column: x => x.HastaneId,
                        principalTable: "Hastaneler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_KanGruplari_KanGrubuId",
                        column: x => x.KanGrubuId,
                        principalTable: "KanGruplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokHareketleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokUyarilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Seviye = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    StokDegeri = table.Column<int>(type: "INTEGER", nullable: false),
                    Esik = table.Column<int>(type: "INTEGER", nullable: false),
                    AcilisTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KapanisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    KanGrubuId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokUyarilari", x => x.Id);
                    table.CheckConstraint("CK_StokUyarisi_Seviye", "Seviye IN ('Dusuk','Kritik')");
                    table.ForeignKey(
                        name: "FK_StokUyarilari_KanGruplari_KanGrubuId",
                        column: x => x.KanGrubuId,
                        principalTable: "KanGruplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 8, 15 });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 3, 6 });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 6, 12 });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 2, 4 });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 3, 6 });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 1, 2 });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 10, 20 });

            migrationBuilder.UpdateData(
                table: "KanStoklari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "KritikEsik", "UyariEsigi" },
                values: new object[] { 4, 8 });

            migrationBuilder.AddCheckConstraint(
                name: "CK_KanStok_Esikler",
                table: "KanStoklari",
                sql: "KritikEsik >= 0 AND UyariEsigi >= KritikEsik");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_BagisId",
                table: "StokHareketleri",
                column: "BagisId",
                unique: true,
                filter: "BagisId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_HastaneId",
                table: "StokHareketleri",
                column: "HastaneId");

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_KanGrubuId_Tarih",
                table: "StokHareketleri",
                columns: new[] { "KanGrubuId", "Tarih" });

            migrationBuilder.CreateIndex(
                name: "IX_StokHareketleri_KullaniciId",
                table: "StokHareketleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_StokUyarilari_KanGrubuId",
                table: "StokUyarilari",
                column: "KanGrubuId",
                unique: true,
                filter: "KapanisTarihi IS NULL");

            // Başlangıç uyarıları: eşiğin altındaki stoklar için, veritabanındaki GERÇEK stok değerinden açılır.
            migrationBuilder.Sql("""
                INSERT INTO StokUyarilari (KanGrubuId, Seviye, StokDegeri, Esik, AcilisTarihi, GuncellemeTarihi, KapanisTarihi)
                SELECT KanGrubuId,
                       CASE WHEN UniteSayisi <= KritikEsik THEN 'Kritik' ELSE 'Dusuk' END,
                       UniteSayisi,
                       CASE WHEN UniteSayisi <= KritikEsik THEN KritikEsik ELSE UyariEsigi END,
                       GuncellemeTarihi, GuncellemeTarihi, NULL
                FROM KanStoklari
                WHERE UniteSayisi <= UyariEsigi;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StokHareketleri");

            migrationBuilder.DropTable(
                name: "StokUyarilari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KanStok_Esikler",
                table: "KanStoklari");

            migrationBuilder.DropColumn(
                name: "KritikEsik",
                table: "KanStoklari");

            migrationBuilder.DropColumn(
                name: "UyariEsigi",
                table: "KanStoklari");
        }
    }
}
