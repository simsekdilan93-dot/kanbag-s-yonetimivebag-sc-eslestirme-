using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class BagisciKiloVeKanGrubuAboRh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboGrubu",
                table: "KanGruplari",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RhPozitif",
                table: "KanGruplari",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Kilo",
                table: "Bagiscilar",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 1,
                column: "Kilo",
                value: 62);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 2,
                column: "Kilo",
                value: 84);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 3,
                column: "Kilo",
                value: 55);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 4,
                column: "Kilo",
                value: 78);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 5,
                column: "Kilo",
                value: 58);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 6,
                column: "Kilo",
                value: 91);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 7,
                column: "Kilo",
                value: 67);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 8,
                column: "Kilo",
                value: 73);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 9,
                column: "Kilo",
                value: 52);

            migrationBuilder.UpdateData(
                table: "Bagiscilar",
                keyColumn: "Id",
                keyValue: 10,
                column: "Kilo",
                value: 88);

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "A", true });

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "A", false });

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "B", true });

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "B", false });

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "AB", true });

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "AB", false });

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "O", true });

            migrationBuilder.UpdateData(
                table: "KanGruplari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "AboGrubu", "RhPozitif" },
                values: new object[] { "O", false });

            migrationBuilder.CreateIndex(
                name: "IX_KanGruplari_AboGrubu_RhPozitif",
                table: "KanGruplari",
                columns: new[] { "AboGrubu", "RhPozitif" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bagisci_Kilo",
                table: "Bagiscilar",
                sql: "Kilo BETWEEN 30 AND 250");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KanGruplari_AboGrubu_RhPozitif",
                table: "KanGruplari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bagisci_Kilo",
                table: "Bagiscilar");

            migrationBuilder.DropColumn(
                name: "AboGrubu",
                table: "KanGruplari");

            migrationBuilder.DropColumn(
                name: "RhPozitif",
                table: "KanGruplari");

            migrationBuilder.DropColumn(
                name: "Kilo",
                table: "Bagiscilar");
        }
    }
}
