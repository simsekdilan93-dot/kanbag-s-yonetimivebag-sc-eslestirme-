using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class BagisTalepBaglantisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KanTalebiId",
                table: "Bagislar",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Bagislar",
                keyColumn: "Id",
                keyValue: 1,
                column: "KanTalebiId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Bagislar",
                keyColumn: "Id",
                keyValue: 2,
                column: "KanTalebiId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Bagislar",
                keyColumn: "Id",
                keyValue: 3,
                column: "KanTalebiId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Bagislar",
                keyColumn: "Id",
                keyValue: 4,
                column: "KanTalebiId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Bagislar",
                keyColumn: "Id",
                keyValue: 5,
                column: "KanTalebiId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Bagislar_BagisciId_KanTalebiId",
                table: "Bagislar",
                columns: new[] { "BagisciId", "KanTalebiId" },
                unique: true,
                filter: "KanTalebiId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bagislar_KanTalebiId",
                table: "Bagislar",
                column: "KanTalebiId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bagislar_KanTalepleri_KanTalebiId",
                table: "Bagislar",
                column: "KanTalebiId",
                principalTable: "KanTalepleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bagislar_KanTalepleri_KanTalebiId",
                table: "Bagislar");

            migrationBuilder.DropIndex(
                name: "IX_Bagislar_BagisciId_KanTalebiId",
                table: "Bagislar");

            migrationBuilder.DropIndex(
                name: "IX_Bagislar_KanTalebiId",
                table: "Bagislar");

            migrationBuilder.DropColumn(
                name: "KanTalebiId",
                table: "Bagislar");
        }
    }
}
