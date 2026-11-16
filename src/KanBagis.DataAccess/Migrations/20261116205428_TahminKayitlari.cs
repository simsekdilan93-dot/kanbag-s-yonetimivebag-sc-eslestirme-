using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TahminKayitlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TahminKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tarih = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kaynak = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Istem = table.Column<string>(type: "TEXT", nullable: true),
                    HamCikti = table.Column<string>(type: "TEXT", nullable: true),
                    DogrulamaNotlari = table.Column<string>(type: "TEXT", nullable: true),
                    SonucJson = table.Column<string>(type: "TEXT", nullable: false),
                    SureMilisaniye = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TahminKayitlari", x => x.Id);
                    table.CheckConstraint("CK_TahminKaydi_Sure", "SureMilisaniye >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TahminKayitlari_Tarih",
                table: "TahminKayitlari",
                column: "Tarih");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TahminKayitlari");
        }
    }
}
