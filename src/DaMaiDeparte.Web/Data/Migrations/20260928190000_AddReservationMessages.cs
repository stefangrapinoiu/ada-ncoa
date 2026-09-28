using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaMaiDeparte.Web.Data.Migrations
{
    /// <summary>
    /// Adds a simple per-reservation message thread, so the donor and the receiver can
    /// coordinate the handover in-app instead of exchanging phone numbers (which are no longer
    /// shown between users at all — see the Reservations/Details.cshtml privacy fix).
    /// </summary>
    /// <inheritdoc />
    public partial class AddReservationMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationMessages_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationMessages_ReservationId_CreatedAt",
                table: "ReservationMessages",
                columns: new[] { "ReservationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationMessages_SenderId",
                table: "ReservationMessages",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReservationMessages");
        }
    }
}
