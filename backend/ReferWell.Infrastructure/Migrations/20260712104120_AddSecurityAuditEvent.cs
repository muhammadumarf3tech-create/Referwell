using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAuditEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblSecurityAuditEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSecurityAuditEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblSecurityAuditEvent_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblSecurityAuditEvent_ActorUserId",
                table: "tblSecurityAuditEvent",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblSecurityAuditEvent_Timestamp",
                table: "tblSecurityAuditEvent",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblSecurityAuditEvent");
        }
    }
}
