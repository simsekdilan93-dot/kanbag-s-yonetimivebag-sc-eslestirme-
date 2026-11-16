using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TelefonBildirimKanallari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisKanalDeneme",
                table: "Bildirimler",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DisKanalDurumu",
                table: "Bildirimler",
                type: "TEXT",
                maxLength: 12,
                nullable: false,
                defaultValue: "Yok");

            migrationBuilder.AddColumn<string>(
                name: "DisKanalHata",
                table: "Bildirimler",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisKanalTarihi",
                table: "Bildirimler",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnlikBildirimKanali",
                table: "Bagiscilar",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SmsIzni",
                table: "Bagiscilar",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "AnlikBildirimKanali", "SmsIzni" },
                values: new object[] { null, false });

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DisKanalDeneme", "DisKanalHata", "DisKanalTarihi" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DisKanalDeneme", "DisKanalHata", "DisKanalTarihi" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DisKanalDeneme", "DisKanalHata", "DisKanalTarihi" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DisKanalDeneme", "DisKanalHata", "DisKanalTarihi" },
                values: new object[] { 0, null, null });

            migrationBuilder.UpdateData(
                table: "Bildirimler",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "DisKanalDeneme", "DisKanalHata", "DisKanalTarihi" },
                values: new object[] { 0, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_DisKanalDurumu",
                table: "Bildirimler",
                column: "DisKanalDurumu");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bildirim_DisKanalDeneme",
                table: "Bildirimler",
                sql: "DisKanalDeneme >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bildirim_DisKanalDurumu",
                table: "Bildirimler",
                sql: "DisKanalDurumu IN ('Yok', 'Kuyrukta', 'Gonderildi', 'Basarisiz')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bildirimler_DisKanalDurumu",
                table: "Bildirimler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bildirim_DisKanalDeneme",
                table: "Bildirimler");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bildirim_DisKanalDurumu",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "DisKanalDeneme",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "DisKanalDurumu",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "DisKanalHata",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "DisKanalTarihi",
                table: "Bildirimler");

            migrationBuilder.DropColumn(
                name: "AnlikBildirimKanali",
                table: "Bagiscilar");

            migrationBuilder.DropColumn(
                name: "SmsIzni",
                table: "Bagiscilar");
        }
    }
}
