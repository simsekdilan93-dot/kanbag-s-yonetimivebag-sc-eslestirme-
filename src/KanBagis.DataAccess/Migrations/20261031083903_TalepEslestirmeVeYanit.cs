using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TalepEslestirmeVeYanit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KanTalepleri_HastaneId",
                table: "KanTalepleri");

            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "KanTalepleri",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KapanisTarihi",
                table: "KanTalepleri",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "YanitTarihi",
                table: "Bildirimler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "YanitTarihi",
                value: new DateTime(2026, 10, 10, 9, 10, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "YanitTarihi",
                value: new DateTime(2026, 10, 10, 11, 40, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "YanitTarihi",
                value: null);

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "YanitTarihi",
                value: null);

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "YanitTarihi",
                value: null);

            migrationBuilder.UpdateData(
                table: "KanTalepleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Aciklama", "KapanisTarihi" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "KanTalepleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Aciklama", "KapanisTarihi" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "KanTalepleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Aciklama", "KapanisTarihi" },
                values: new object[] { null, new DateTime(2026, 9, 22, 16, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_KanTalepleri_HastaneId_KanGrubuId_Durum",
                table: "KanTalepleri",
                columns: new[] { "HastaneId", "KanGrubuId", "Durum" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_KanTalebi_KapanisTarihi",
                table: "KanTalepleri",
                sql: "(Durum = 'Acik' AND KapanisTarihi IS NULL) OR (Durum <> 'Acik' AND KapanisTarihi IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_BagisciId_Yanit",
                table: "Bildirimler",
                columns: new[] { "BagisciId", "Yanit" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bildirim_YanitTarihi",
                table: "Bildirimler",
                sql: "(Yanit = 'Beklemede' AND YanitTarihi IS NULL) OR (Yanit <> 'Beklemede' AND YanitTarihi IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KanTalepleri_HastaneId_KanGrubuId_Durum",
                table: "KanTalepleri");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KanTalebi_KapanisTarihi",
                table: "KanTalepleri");

            migrationBuilder.DropIndex(
                name: "IX_Bildirimler_BagisciId_Yanit",
                table: "Bildirimler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bildirim_YanitTarihi",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "KanTalepleri");

            migrationBuilder.DropColumn(
                name: "KapanisTarihi",
                table: "KanTalepleri");

            migrationBuilder.DropColumn(
                name: "YanitTarihi",
                table: "Bildirimler");

            migrationBuilder.CreateIndex(
                name: "IX_KanTalepleri_HastaneId",
                table: "KanTalepleri",
                column: "HastaneId");
        }
    }
}
