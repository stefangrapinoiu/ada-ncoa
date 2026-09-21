using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaMaiDeparte.Web.Data.Migrations
{
    /// <summary>
    /// The handover details ("Detaliile predării") are now collected when a listing is created,
    /// and the free-text description field was removed from the form, so:
    ///
    ///   - <c>DonationItems.Description</c> is dropped;
    ///   - <c>DonationItems.PickupLocation</c> (required) and <c>PickupNotes</c> are added;
    ///   - <c>FoodCategories.RequiresDescription</c> is dropped — with no description field there
    ///     is nothing for it to require. The prohibited-word screen now reads the title and the
    ///     handover notes instead.
    ///
    /// Existing rows get an empty <c>PickupLocation</c>, which the donor has to fill in the next
    /// time they edit the listing. The column carries a default constraint for that reason.
    /// </summary>
    /// <inheritdoc />
    public partial class PickupDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Description", table: "DonationItems");

            migrationBuilder.DropColumn(name: "RequiresDescription", table: "FoodCategories");

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation",
                table: "DonationItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupNotes",
                table: "DonationItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PickupLocation", table: "DonationItems");

            migrationBuilder.DropColumn(name: "PickupNotes", table: "DonationItems");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DonationItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDescription",
                table: "FoodCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
