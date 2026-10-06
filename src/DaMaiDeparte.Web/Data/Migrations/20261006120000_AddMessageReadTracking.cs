using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaMaiDeparte.Web.Data.Migrations
{
    /// <summary>
    /// Adds per-participant "last read" timestamps to reservations, which drive the unread
    /// message notifications (bell badge, "Mesaje noi" page, "Mesaje noi" badges on the lists).
    /// Existing reservations are stamped with the migration time, so conversations that already
    /// happened before this feature don't all light up as unread on the first deploy.
    /// </summary>
    /// <inheritdoc />
    public partial class AddMessageReadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DonorLastReadAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiverLastReadAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Reservations] SET [DonorLastReadAt] = SYSUTCDATETIME(), [ReceiverLastReadAt] = SYSUTCDATETIME();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DonorLastReadAt", table: "Reservations");

            migrationBuilder.DropColumn(name: "ReceiverLastReadAt", table: "Reservations");
        }
    }
}
