using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReferWell.Infrastructure.Migrations;

public partial class AddMassCommMessageAuditDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RecipientType",
            table: "MassCommMessages",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "Patient");

        migrationBuilder.AddColumn<Guid>(
            name: "ReferralId",
            table: "MassCommMessages",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<string>(
            name: "ReferralCaseNo",
            table: "MassCommMessages",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "RenderedSubject",
            table: "MassCommMessages",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RecipientType", table: "MassCommMessages");
        migrationBuilder.DropColumn(name: "ReferralId", table: "MassCommMessages");
        migrationBuilder.DropColumn(name: "ReferralCaseNo", table: "MassCommMessages");
        migrationBuilder.DropColumn(name: "RenderedSubject", table: "MassCommMessages");
    }
}
