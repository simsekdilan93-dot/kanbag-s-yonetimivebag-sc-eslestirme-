using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ParolaSifirlamaKodlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParolaSifirlamaKodlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KodHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SonKullanmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DenemeSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    KapanisTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    KullaniciId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParolaSifirlamaKodlari", x => x.Id);
                    table.CheckConstraint("CK_ParolaSifirlama_Deneme", "DenemeSayisi >= 0");
                    table.CheckConstraint("CK_ParolaSifirlama_Sure", "SonKullanmaTarihi > OlusturmaTarihi");
                    table.ForeignKey(
                        name: "FK_ParolaSifirlamaKodlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParolaSifirlamaKodlari_KullaniciId",
                table: "ParolaSifirlamaKodlari",
                column: "KullaniciId",
                unique: true,
                filter: "KapanisTarihi IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParolaSifirlamaKodlari");
        }
    }
}
