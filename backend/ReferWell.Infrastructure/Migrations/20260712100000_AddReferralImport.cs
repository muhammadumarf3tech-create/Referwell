using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReferWell.Infrastructure.Data;

#nullable disable

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260712100000_AddReferralImport")]
    public partial class AddReferralImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblReferralImportBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SucceededRows = table.Column<int>(type: "int", nullable: false),
                    FailedRows = table.Column<int>(type: "int", nullable: false),
                    CreatedPatients = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblReferralImportBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblReferralImportBatch_Users_ImportedByUserId",
                        column: x => x.ImportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblReferralImportRow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NhiNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SpecialistType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Urgency = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ReferralStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LegacyCaseNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CaseNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReferralId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PatientCreated = table.Column<bool>(type: "bit", nullable: false),
                    ErrorColumn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RawData = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblReferralImportRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblReferralImportRow_tblReferralImportBatch_BatchId",
                        column: x => x.BatchId,
                        principalTable: "tblReferralImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblReferralImportBatch_ImportedByUserId",
                table: "tblReferralImportBatch",
                column: "ImportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblReferralImportRow_BatchId",
                table: "tblReferralImportRow",
                column: "BatchId");

            // Seed Referral Import menu access (Admin only by default)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [tblRoleMenuAccess] WHERE [Id] = '99999999-9999-9999-9999-000000000016')
    INSERT INTO [tblRoleMenuAccess] ([Id], [Role], [MenuItem], [HasAccess])
    VALUES ('99999999-9999-9999-9999-000000000016', 1, N'Referral Import', 1);

IF NOT EXISTS (SELECT 1 FROM [tblRoleMenuAccess] WHERE [Id] = '99999999-9999-9999-9999-000000000017')
    INSERT INTO [tblRoleMenuAccess] ([Id], [Role], [MenuItem], [HasAccess])
    VALUES ('99999999-9999-9999-9999-000000000017', 2, N'Referral Import', 0);

IF NOT EXISTS (SELECT 1 FROM [tblRoleMenuAccess] WHERE [Id] = '99999999-9999-9999-9999-000000000018')
    INSERT INTO [tblRoleMenuAccess] ([Id], [Role], [MenuItem], [HasAccess])
    VALUES ('99999999-9999-9999-9999-000000000018', 3, N'Referral Import', 0);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [tblRoleMenuAccess] WHERE [Id] IN (
    '99999999-9999-9999-9999-000000000016',
    '99999999-9999-9999-9999-000000000017',
    '99999999-9999-9999-9999-000000000018'
);
");

            migrationBuilder.DropTable(name: "tblReferralImportRow");
            migrationBuilder.DropTable(name: "tblReferralImportBatch");
        }
    }
}
