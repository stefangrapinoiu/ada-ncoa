using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaMaiDeparte.Web.Data.Migrations
{
    /// <summary>
    /// Adds the GDPR consent checkbox at registration and the first-login Terms and Conditions
    /// acceptance screen. Both are tracked as nullable timestamps on the account: existing
    /// accounts (including seeded test users) get <c>null</c> for both, which is exactly the
    /// state that makes <c>RequireTermsAcceptedFilter</c> send them to the acceptance screen
    /// once, at their next login.
    /// </summary>
    /// <inheritdoc />
    public partial class AddGdprAndTermsConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GdprConsentAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GdprConsentAt", table: "AspNetUsers");

            migrationBuilder.DropColumn(name: "TermsAcceptedAt", table: "AspNetUsers");
        }
    }
}
