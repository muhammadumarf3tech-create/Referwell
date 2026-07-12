using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralSlaPause : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SlaPauseReason",
                table: "Referrals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SlaPaused",
                table: "Referrals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaPausedAt",
                table: "Referrals",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlaPauseReason",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "SlaPaused",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "SlaPausedAt",
                table: "Referrals");
        }
    }
}
