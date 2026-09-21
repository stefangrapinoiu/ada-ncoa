using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaMaiDeparte.Web.Data.Migrations
{
    /// <summary>
    /// Converts the general second-hand marketplace schema into the food platform schema:
    /// one user type, structured locations, food categories with an allowlist, a mandatory
    /// expiry date and one-to-three photos per listing.
    ///
    /// DATA LOSS WARNING — existing DonationItems and Reservations rows are deleted.
    /// They describe furniture, clothing and electronics, and the new required columns
    /// (FoodCategoryId, ExpirationDate, CityId, CountryId) cannot be derived from a free-text
    /// "PickupArea" or from a non-food category. User accounts are preserved.
    /// </summary>
    /// <inheritdoc />
    public partial class FoodAndLocationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------------
            // 0. Remove legacy non-food listings (see the warning above).
            //    Reservations first: they reference DonationItems.
            // ---------------------------------------------------------------
            migrationBuilder.Sql("DELETE FROM [Reservations];");
            migrationBuilder.Sql("DELETE FROM [DonationItems];");

            // ---------------------------------------------------------------
            // 1. Drop what the product no longer has: account types and the
            //    generic marketplace fields on a listing.
            // ---------------------------------------------------------------
            migrationBuilder.DropForeignKey(
                name: "FK_DonationItems_Categories_CategoryId",
                table: "DonationItems");

            migrationBuilder.DropIndex(
                name: "IX_DonationItems_CategoryId",
                table: "DonationItems");

            migrationBuilder.DropTable(name: "Categories");

            migrationBuilder.DropColumn(name: "CategoryId", table: "DonationItems");
            migrationBuilder.DropColumn(name: "Condition", table: "DonationItems");
            migrationBuilder.DropColumn(name: "PickupArea", table: "DonationItems");
            migrationBuilder.DropColumn(name: "ImagePath", table: "DonationItems");

            migrationBuilder.DropColumn(name: "AccountType", table: "AspNetUsers");

            // ---------------------------------------------------------------
            // 2. Geography: Country → City → Neighborhood.
            // ---------------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    // Reserved for a future proximity search; unused in Version 1.
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Neighborhoods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Neighborhoods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Neighborhoods_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ---------------------------------------------------------------
            // 3. Food categories. IsAllowed carries the allowlist; prohibited
            //    groups exist as rows with IsAllowed = 0 rather than being absent.
            // ---------------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "FoodCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    RequiresDescription = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodCategories", x => x.Id);
                });

            // ---------------------------------------------------------------
            // 4. Listing columns. The table was emptied in step 0, so the new
            //    NOT NULL columns need no default constraint.
            // ---------------------------------------------------------------
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DonationItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<int>(
                name: "FoodCategoryId",
                table: "DonationItems",
                type: "int",
                nullable: false);

            // A local calendar date, not a timestamp: stored as DATE on purpose.
            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpirationDate",
                table: "DonationItems",
                type: "date",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "DonationItems",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "DonationItems",
                type: "int",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "NeighborhoodId",
                table: "DonationItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SafetyConfirmedAt",
                table: "DonationItems",
                type: "datetime2",
                nullable: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "DonationItems",
                type: "datetime2",
                nullable: true);

            // ---------------------------------------------------------------
            // 5. One to three photos per listing.
            // ---------------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "DonationImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationItemId = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationImages_DonationItems_DonationItemId",
                        column: x => x.DonationItemId,
                        principalTable: "DonationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ---------------------------------------------------------------
            // 6. Preferred ("home") location on the account.
            // ---------------------------------------------------------------
            migrationBuilder.AddColumn<int>(
                name: "PreferredCountryId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredCityId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredNeighborhoodId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            // ---------------------------------------------------------------
            // 7. Indexes. The composite one on DonationItems mirrors how the
            //    dashboard queries: city, then status, then expiry.
            // ---------------------------------------------------------------
            migrationBuilder.CreateIndex(
                name: "IX_Countries_Code",
                table: "Countries",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_IsActive",
                table: "Cities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountryId_Slug",
                table: "Cities",
                columns: new[] { "CountryId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Neighborhoods_CityId_Name",
                table: "Neighborhoods",
                columns: new[] { "CityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FoodCategories_Key",
                table: "FoodCategories",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonationImages_DonationItemId_SortOrder",
                table: "DonationImages",
                columns: new[] { "DonationItemId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DonationItems_CountryId",
                table: "DonationItems",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationItems_ExpirationDate",
                table: "DonationItems",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_DonationItems_FoodCategoryId",
                table: "DonationItems",
                column: "FoodCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationItems_CityId_Status_ExpirationDate",
                table: "DonationItems",
                columns: new[] { "CityId", "Status", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DonationItems_NeighborhoodId_Status",
                table: "DonationItems",
                columns: new[] { "NeighborhoodId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PreferredCityId",
                table: "AspNetUsers",
                column: "PreferredCityId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PreferredCountryId",
                table: "AspNetUsers",
                column: "PreferredCountryId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PreferredNeighborhoodId",
                table: "AspNetUsers",
                column: "PreferredNeighborhoodId");

            // ---------------------------------------------------------------
            // 8. Foreign keys.
            // ---------------------------------------------------------------
            migrationBuilder.AddForeignKey(
                name: "FK_DonationItems_Cities_CityId",
                table: "DonationItems",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationItems_Countries_CountryId",
                table: "DonationItems",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationItems_Neighborhoods_NeighborhoodId",
                table: "DonationItems",
                column: "NeighborhoodId",
                principalTable: "Neighborhoods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationItems_FoodCategories_FoodCategoryId",
                table: "DonationItems",
                column: "FoodCategoryId",
                principalTable: "FoodCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Cities_PreferredCityId",
                table: "AspNetUsers",
                column: "PreferredCityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Countries_PreferredCountryId",
                table: "AspNetUsers",
                column: "PreferredCountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Neighborhoods_PreferredNeighborhoodId",
                table: "AspNetUsers",
                column: "PreferredNeighborhoodId",
                principalTable: "Neighborhoods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The food listings cannot be turned back into second-hand items either, so this
            // direction also clears the tables it can no longer describe.
            migrationBuilder.Sql("DELETE FROM [Reservations];");
            migrationBuilder.Sql("DELETE FROM [DonationImages];");
            migrationBuilder.Sql("DELETE FROM [DonationItems];");

            migrationBuilder.DropForeignKey(name: "FK_AspNetUsers_Cities_PreferredCityId", table: "AspNetUsers");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUsers_Countries_PreferredCountryId", table: "AspNetUsers");
            migrationBuilder.DropForeignKey(name: "FK_AspNetUsers_Neighborhoods_PreferredNeighborhoodId", table: "AspNetUsers");
            migrationBuilder.DropForeignKey(name: "FK_DonationItems_Cities_CityId", table: "DonationItems");
            migrationBuilder.DropForeignKey(name: "FK_DonationItems_Countries_CountryId", table: "DonationItems");
            migrationBuilder.DropForeignKey(name: "FK_DonationItems_Neighborhoods_NeighborhoodId", table: "DonationItems");
            migrationBuilder.DropForeignKey(name: "FK_DonationItems_FoodCategories_FoodCategoryId", table: "DonationItems");

            migrationBuilder.DropTable(name: "DonationImages");
            migrationBuilder.DropTable(name: "FoodCategories");
            migrationBuilder.DropTable(name: "Neighborhoods");
            migrationBuilder.DropTable(name: "Cities");
            migrationBuilder.DropTable(name: "Countries");

            migrationBuilder.DropIndex(name: "IX_AspNetUsers_PreferredCityId", table: "AspNetUsers");
            migrationBuilder.DropIndex(name: "IX_AspNetUsers_PreferredCountryId", table: "AspNetUsers");
            migrationBuilder.DropIndex(name: "IX_AspNetUsers_PreferredNeighborhoodId", table: "AspNetUsers");
            migrationBuilder.DropIndex(name: "IX_DonationItems_CountryId", table: "DonationItems");
            migrationBuilder.DropIndex(name: "IX_DonationItems_ExpirationDate", table: "DonationItems");
            migrationBuilder.DropIndex(name: "IX_DonationItems_FoodCategoryId", table: "DonationItems");
            migrationBuilder.DropIndex(name: "IX_DonationItems_CityId_Status_ExpirationDate", table: "DonationItems");
            migrationBuilder.DropIndex(name: "IX_DonationItems_NeighborhoodId_Status", table: "DonationItems");

            migrationBuilder.DropColumn(name: "PreferredCityId", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "PreferredCountryId", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "PreferredNeighborhoodId", table: "AspNetUsers");

            migrationBuilder.DropColumn(name: "CityId", table: "DonationItems");
            migrationBuilder.DropColumn(name: "CountryId", table: "DonationItems");
            migrationBuilder.DropColumn(name: "NeighborhoodId", table: "DonationItems");
            migrationBuilder.DropColumn(name: "ExpirationDate", table: "DonationItems");
            migrationBuilder.DropColumn(name: "ExpiredAt", table: "DonationItems");
            migrationBuilder.DropColumn(name: "FoodCategoryId", table: "DonationItems");
            migrationBuilder.DropColumn(name: "SafetyConfirmedAt", table: "DonationItems");

            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DonationItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "DonationItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "DonationItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PickupArea",
                table: "DonationItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "DonationItems",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Key",
                table: "Categories",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonationItems_CategoryId",
                table: "DonationItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonationItems_Categories_CategoryId",
                table: "DonationItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
