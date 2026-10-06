using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DaMaiDeparte.Web.Data.Migrations
{
    /// <summary>Security log for the hidden admin panel (sign-ins, failed sign-ins, lockouts, ...).</summary>
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_AuditEvents_OccurredAt", table: "AuditEvents", column: "OccurredAt");
            migrationBuilder.CreateIndex(name: "IX_AuditEvents_UserId", table: "AuditEvents", column: "UserId");
            migrationBuilder.CreateIndex(name: "IX_AuditEvents_IpAddress_OccurredAt", table: "AuditEvents", columns: new[] { "IpAddress", "OccurredAt" });
            migrationBuilder.CreateIndex(name: "IX_AuditEvents_Type_OccurredAt", table: "AuditEvents", columns: new[] { "Type", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditEvents");
        }
    }
}
