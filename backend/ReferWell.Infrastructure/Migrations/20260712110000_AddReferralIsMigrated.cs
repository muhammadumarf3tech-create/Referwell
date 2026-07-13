using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReferWell.Infrastructure.Data;

#nullable disable

namespace ReferWell.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260712110000_AddReferralIsMigrated")]
    public partial class AddReferralIsMigrated : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CaseNo",
                table: "Referrals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<bool>(
                name: "IsMigrated",
                table: "Referrals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_CaseNo",
                table: "Referrals",
                column: "CaseNo");

            migrationBuilder.AlterColumn<string>(
                name: "CaseNo",
                table: "tblReferralImportRow",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            // Preserve legacy case numbers on already-imported referrals
            migrationBuilder.Sql(@"
UPDATE r
SET r.[CaseNo] = i.[LegacyCaseNo],
    r.[IsMigrated] = 1
FROM [Referrals] r
INNER JOIN [tblReferralImportRow] i ON i.[ReferralId] = r.[Id]
WHERE i.[Status] = N'Succeeded'
  AND i.[LegacyCaseNo] IS NOT NULL
  AND LTRIM(RTRIM(i.[LegacyCaseNo])) <> N''
  AND NOT EXISTS (
      SELECT 1 FROM [Referrals] x
      WHERE x.[CaseNo] = i.[LegacyCaseNo] AND x.[Id] <> r.[Id]
  );

UPDATE r
SET r.[IsMigrated] = 1
FROM [Referrals] r
INNER JOIN [tblReferralImportRow] i ON i.[ReferralId] = r.[Id]
WHERE i.[Status] = N'Succeeded';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Referrals_CaseNo",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "IsMigrated",
                table: "Referrals");

            migrationBuilder.AlterColumn<string>(
                name: "CaseNo",
                table: "Referrals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "CaseNo",
                table: "tblReferralImportRow",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
