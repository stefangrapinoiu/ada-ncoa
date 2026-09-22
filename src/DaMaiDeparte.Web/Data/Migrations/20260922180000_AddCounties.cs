using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaMaiDeparte.Web.Data.Migrations
{
    /// <summary>
    /// Inserts a county ("Județ") level into the location hierarchy, between country and city:
    /// Țară → Județ → Localitate → Cartier. Romania's 41 județe plus municipiul București (kept
    /// as its own county-equivalent row, so the Country → County → City cascade stays uniform)
    /// are seeded by <c>DbSeeder</c>, and every existing city is assigned to its county there too.
    ///
    /// <c>Cities.CountyId</c> is required. This is safe only because a schema change always goes
    /// through <c>reset-app.command</c>, which drops and recreates the database before reseeding —
    /// there is never existing city data for this column to be backfilled against.
    /// </summary>
    /// <inheritdoc />
    public partial class AddCounties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Counties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Counties_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "CountyId",
                table: "Cities",
                type: "int",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Counties_CountryId_Code",
                table: "Counties",
                columns: new[] { "CountryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountyId",
                table: "Cities",
                column: "CountyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cities_Counties_CountyId",
                table: "Cities",
                column: "CountyId",
                principalTable: "Counties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cities_Counties_CountyId",
                table: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_Cities_CountyId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "CountyId",
                table: "Cities");

            migrationBuilder.DropTable(
                name: "Counties");
        }
    }
}
