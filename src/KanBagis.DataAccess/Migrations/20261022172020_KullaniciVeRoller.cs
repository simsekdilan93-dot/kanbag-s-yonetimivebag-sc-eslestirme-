using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class KullaniciVeRoller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Eposta = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ParolaHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Rol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BasarisizGirisSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    KilitBitisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SonGirisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BagisciId = table.Column<int>(type: "INTEGER", nullable: true),
                    HastaneId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                    table.CheckConstraint("CK_Kullanici_BasarisizGiris", "BasarisizGirisSayisi >= 0");
                    table.CheckConstraint("CK_Kullanici_Rol_Baglantisi", "(Rol = 'Bagisci' AND BagisciId IS NOT NULL AND HastaneId IS NULL) OR (Rol = 'Kurum' AND HastaneId IS NOT NULL AND BagisciId IS NULL)");
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Bagiscilar_BagisciId",
                        column: x => x.BagisciId,
                        principalTable: "Bagiscilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Hastaneler_HastaneId",
                        column: x => x.HastaneId,
                        principalTable: "Hastaneler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_BagisciId",
                table: "Kullanicilar",
                column: "BagisciId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Eposta",
                table: "Kullanicilar",
                column: "Eposta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_HastaneId",
                table: "Kullanicilar",
                column: "HastaneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
