using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KanBagis.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hastaneler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Sehir = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hastaneler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KanGruplari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanGruplari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bagiscilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TcKimlikNo = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 11, nullable: false),
                    Ad = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Soyad = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    Eposta = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DogumTarihi = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Cinsiyet = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Sehir = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    KanGrubuId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bagiscilar", x => x.Id);
                    table.CheckConstraint("CK_Bagisci_TcKimlikNo_11Hane", "length(TcKimlikNo) = 11");
                    table.ForeignKey(
                        name: "FK_Bagiscilar_KanGruplari_KanGrubuId",
                        column: x => x.KanGrubuId,
                        principalTable: "KanGruplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KanStoklari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UniteSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KanGrubuId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanStoklari", x => x.Id);
                    table.CheckConstraint("CK_KanStok_UniteSayisi_Negatif_Olamaz", "UniteSayisi >= 0");
                    table.ForeignKey(
                        name: "FK_KanStoklari_KanGruplari_KanGrubuId",
                        column: x => x.KanGrubuId,
                        principalTable: "KanGruplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KanTalepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UniteSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    Aciliyet = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Durum = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HastaneId = table.Column<int>(type: "INTEGER", nullable: false),
                    KanGrubuId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanTalepleri", x => x.Id);
                    table.CheckConstraint("CK_KanTalebi_Aciliyet", "Aciliyet IN ('Normal', 'Acil')");
                    table.CheckConstraint("CK_KanTalebi_Durum", "Durum IN ('Acik', 'Karsilandi', 'Iptal')");
                    table.CheckConstraint("CK_KanTalebi_UniteSayisi_Pozitif", "UniteSayisi > 0");
                    table.ForeignKey(
                        name: "FK_KanTalepleri_Hastaneler_HastaneId",
                        column: x => x.HastaneId,
                        principalTable: "Hastaneler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KanTalepleri_KanGruplari_KanGrubuId",
                        column: x => x.KanGrubuId,
                        principalTable: "KanGruplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bagislar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BagisTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UniteSayisi = table.Column<int>(type: "INTEGER", nullable: false),
                    BagisciId = table.Column<int>(type: "INTEGER", nullable: false),
                    HastaneId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bagislar", x => x.Id);
                    table.CheckConstraint("CK_Bagis_UniteSayisi_Pozitif", "UniteSayisi > 0");
                    table.ForeignKey(
                        name: "FK_Bagislar_Bagiscilar_BagisciId",
                        column: x => x.BagisciId,
                        principalTable: "Bagiscilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bagislar_Hastaneler_HastaneId",
                        column: x => x.HastaneId,
                        principalTable: "Hastaneler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mesaj = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    GonderimTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Okundu = table.Column<bool>(type: "INTEGER", nullable: false),
                    Yanit = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    BagisciId = table.Column<int>(type: "INTEGER", nullable: false),
                    KanTalebiId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.Id);
                    table.CheckConstraint("CK_Bildirim_Yanit", "Yanit IN ('Beklemede', 'Kabul', 'Red')");
                    table.ForeignKey(
                        name: "FK_Bildirimler_Bagiscilar_BagisciId",
                        column: x => x.BagisciId,
                        principalTable: "Bagiscilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bildirimler_KanTalepleri_KanTalebiId",
                        column: x => x.KanTalebiId,
                        principalTable: "KanTalepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Hastaneler",
                columns: new[] { "Id", "Ad", "Sehir", "Telefon" },
                values: new object[,]
                {
                    { 1, "Örnek Devlet Hastanesi", "Bingöl", "04260000001" },
                    { 2, "Örnek Şehir Hastanesi", "Elazığ", "04240000002" },
                    { 3, "Örnek Üniversite Hastanesi", "Diyarbakır", "04120000003" }
                });

            migrationBuilder.InsertData(
                table: "KanGruplari",
                columns: new[] { "Id", "Ad" },
                values: new object[,]
                {
                    { 1, "A Rh+" },
                    { 2, "A Rh-" },
                    { 3, "B Rh+" },
                    { 4, "B Rh-" },
                    { 5, "AB Rh+" },
                    { 6, "AB Rh-" },
                    { 7, "0 Rh+" },
                    { 8, "0 Rh-" }
                });

            migrationBuilder.InsertData(
                table: "Bagiscilar",
                columns: new[] { "Id", "Ad", "Cinsiyet", "DogumTarihi", "Eposta", "KanGrubuId", "Sehir", "Soyad", "TcKimlikNo", "Telefon" },
                values: new object[,]
                {
                    { 1, "Ayşe", "Kadin", new DateOnly(1995, 4, 12), "ayse.yilmaz@ornek.com", 1, "Bingöl", "Yılmaz", "99900000001", "05550000001" },
                    { 2, "Mehmet", "Erkek", new DateOnly(1988, 9, 3), "mehmet.kaya@ornek.com", 8, "Bingöl", "Kaya", "99900000002", "05550000002" },
                    { 3, "Zeynep", "Kadin", new DateOnly(2000, 1, 25), null, 3, "Elazığ", "Demir", "99900000003", "05550000003" },
                    { 4, "Ali", "Erkek", new DateOnly(1992, 6, 30), "ali.celik@ornek.com", 1, "Bingöl", "Çelik", "99900000004", "05550000004" },
                    { 5, "Elif", "Kadin", new DateOnly(1998, 11, 8), "elif.sahin@ornek.com", 8, "Elazığ", "Şahin", "99900000005", "05550000005" },
                    { 6, "Mustafa", "Erkek", new DateOnly(1985, 2, 14), null, 5, "Diyarbakır", "Arslan", "99900000006", "05550000006" },
                    { 7, "Fatma", "Kadin", new DateOnly(1990, 7, 19), "fatma.dogan@ornek.com", 7, "Bingöl", "Doğan", "99900000007", "05550000007" },
                    { 8, "Emre", "Erkek", new DateOnly(1997, 3, 5), "emre.koc@ornek.com", 2, "Diyarbakır", "Koç", "99900000008", "05550000008" },
                    { 9, "Merve", "Kadin", new DateOnly(2001, 12, 1), null, 4, "Bingöl", "Aydın", "99900000009", "05550000009" },
                    { 10, "Burak", "Erkek", new DateOnly(1993, 8, 22), "burak.ozturk@ornek.com", 8, "Bingöl", "Öztürk", "99900000010", "05550000010" }
                });

            migrationBuilder.InsertData(
                table: "KanStoklari",
                columns: new[] { "Id", "GuncellemeTarihi", "KanGrubuId", "UniteSayisi" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 1, 24 },
                    { 2, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 2, 6 },
                    { 3, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 3, 15 },
                    { 4, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 4, 3 },
                    { 5, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 5, 8 },
                    { 6, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 6, 1 },
                    { 7, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 7, 30 },
                    { 8, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), 8, 2 }
                });

            migrationBuilder.InsertData(
                table: "KanTalepleri",
                columns: new[] { "Id", "Aciliyet", "Durum", "HastaneId", "KanGrubuId", "OlusturmaTarihi", "UniteSayisi" },
                values: new object[,]
                {
                    { 1, "Acil", "Acik", 1, 8, new DateTime(2026, 10, 10, 8, 30, 0, 0, DateTimeKind.Utc), 4 },
                    { 2, "Normal", "Acik", 2, 1, new DateTime(2026, 10, 12, 14, 0, 0, 0, DateTimeKind.Utc), 2 },
                    { 3, "Normal", "Karsilandi", 1, 3, new DateTime(2026, 9, 20, 11, 15, 0, 0, DateTimeKind.Utc), 1 }
                });

            migrationBuilder.InsertData(
                table: "Bagislar",
                columns: new[] { "Id", "BagisTarihi", "BagisciId", "HastaneId", "UniteSayisi" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 10, 10, 0, 0, 0, DateTimeKind.Utc), 2, 1, 1 },
                    { 2, new DateTime(2026, 5, 2, 9, 30, 0, 0, DateTimeKind.Utc), 3, 2, 1 },
                    { 3, new DateTime(2026, 6, 15, 13, 0, 0, 0, DateTimeKind.Utc), 1, 1, 1 },
                    { 4, new DateTime(2026, 7, 1, 11, 0, 0, 0, DateTimeKind.Utc), 2, 1, 1 },
                    { 5, new DateTime(2026, 8, 20, 15, 45, 0, 0, DateTimeKind.Utc), 7, 1, 1 }
                });

            migrationBuilder.InsertData(
                table: "Bildirimler",
                columns: new[] { "Id", "BagisciId", "GonderimTarihi", "KanTalebiId", "Mesaj", "Okundu", "Yanit" },
                values: new object[,]
                {
                    { 1, 2, new DateTime(2026, 10, 10, 8, 35, 0, 0, DateTimeKind.Utc), 1, "Örnek Devlet Hastanesi'nde acil 0 Rh- kan ihtiyacı var.", true, "Kabul" },
                    { 2, 5, new DateTime(2026, 10, 10, 8, 35, 0, 0, DateTimeKind.Utc), 1, "Örnek Devlet Hastanesi'nde acil 0 Rh- kan ihtiyacı var.", true, "Red" },
                    { 3, 10, new DateTime(2026, 10, 10, 8, 35, 0, 0, DateTimeKind.Utc), 1, "Örnek Devlet Hastanesi'nde acil 0 Rh- kan ihtiyacı var.", false, "Beklemede" },
                    { 4, 1, new DateTime(2026, 10, 12, 14, 5, 0, 0, DateTimeKind.Utc), 2, "Örnek Şehir Hastanesi'nde A Rh+ kan ihtiyacı var.", false, "Beklemede" },
                    { 5, 4, new DateTime(2026, 10, 12, 14, 5, 0, 0, DateTimeKind.Utc), 2, "Örnek Şehir Hastanesi'nde A Rh+ kan ihtiyacı var.", false, "Beklemede" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bagiscilar_KanGrubuId_Sehir",
                table: "Bagiscilar",
                columns: new[] { "KanGrubuId", "Sehir" });

            migrationBuilder.CreateIndex(
                name: "IX_Bagiscilar_TcKimlikNo",
                table: "Bagiscilar",
                column: "TcKimlikNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bagislar_BagisciId_BagisTarihi",
                table: "Bagislar",
                columns: new[] { "BagisciId", "BagisTarihi" });

            migrationBuilder.CreateIndex(
                name: "IX_Bagislar_HastaneId",
                table: "Bagislar",
                column: "HastaneId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_BagisciId_KanTalebiId",
                table: "Bildirimler",
                columns: new[] { "BagisciId", "KanTalebiId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KanTalebiId",
                table: "Bildirimler",
                column: "KanTalebiId");

            migrationBuilder.CreateIndex(
                name: "IX_Hastaneler_Ad_Sehir",
                table: "Hastaneler",
                columns: new[] { "Ad", "Sehir" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanGruplari_Ad",
                table: "KanGruplari",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanStoklari_KanGrubuId",
                table: "KanStoklari",
                column: "KanGrubuId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanTalepleri_Durum_KanGrubuId",
                table: "KanTalepleri",
                columns: new[] { "Durum", "KanGrubuId" });

            migrationBuilder.CreateIndex(
                name: "IX_KanTalepleri_HastaneId",
                table: "KanTalepleri",
                column: "HastaneId");

            migrationBuilder.CreateIndex(
                name: "IX_KanTalepleri_KanGrubuId",
                table: "KanTalepleri",
                column: "KanGrubuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bagislar");

            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "KanStoklari");

            migrationBuilder.DropTable(
                name: "Bagiscilar");

            migrationBuilder.DropTable(
                name: "KanTalepleri");

            migrationBuilder.DropTable(
                name: "Hastaneler");

            migrationBuilder.DropTable(
                name: "KanGruplari");
        }
    }
}
