using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MassCommCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilterCriteria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MassCommCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MassCommCampaigns_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PatientDateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferringGPId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecialistType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Urgency = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlaDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlaBreach = table.Column<bool>(type: "bit", nullable: false),
                    ClaimedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PriorityScore = table.Column<double>(type: "float", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ClaimedByUserId",
                        column: x => x.ClaimedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MassCommMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RenderedBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MassCommMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MassCommMessages_MassCommCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "MassCommCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Id", "Description", "Key", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Weight % for urgency in priority score", "weight_urgency", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "50" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Weight % for wait time in priority score", "weight_waittime", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "30" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Weight % for patient age in priority score", "weight_patient", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "20" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@referwell.com", "System Admin", true, null, "$2a$11$XEiDr9QOoCLcTH9lyxsiK.xAA7U.bXZxAkUZmt01W5PzvnL3IVNDa", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "nurse@referwell.com", "Nurse Sarah", true, null, "$2a$11$lCaB5GQ49O7zEiUoDSF0JOFH8Ouq/TSyPTIPXV52plIIyfOmYkjgy", 2 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "gp1@referwell.com", "Dr. James Wilson", true, null, "$2a$11$KLbW14SVatQ25b62lvogieQ3BTUaUJxzEQX8ij/etiuA/BrsuVucS", 3 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "gp2@referwell.com", "Dr. Amelia Hart", true, null, "$2a$11$c462mFKpwQIu4TPmtMfQy.B8S.EDx/ByKBKO.AH7GYe8xVauoGJiy", 3 }
                });

            migrationBuilder.InsertData(
                table: "Referrals",
                columns: new[] { "Id", "ClaimedAt", "ClaimedByUserId", "CreatedAt", "CreatedByUserId", "PatientDateOfBirth", "PatientName", "PriorityScore", "Reason", "ReceivedAt", "ReferringGPId", "SlaBreach", "SlaDeadline", "SpecialistType", "Status", "UpdatedAt", "Urgency" },
                values: new object[,]
                {
                    { new Guid("2f7d3d63-4b9e-454c-bbec-ff5ffea09033"), null, null, new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(1945, 6, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Henry Smith", 41.700000000000003, "Chronic cough", new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 5, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Pulmonology", 6, null, 2 },
                    { new Guid("30fdba99-25df-4e70-9353-0805e3c51ad2"), null, null, new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(1978, 12, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Grace Kim", 66.099999999999994, "Vision deterioration", new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Ophthalmology", 4, null, 3 },
                    { new Guid("46e92d71-fe4e-45cb-9781-cae95b336c3d"), null, null, new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(1955, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Alice Martin", 75.5, "Chest pain", new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Cardiology", 1, null, 3 },
                    { new Guid("5da6a25f-9982-45b9-9501-d4a3ae7ca24d"), null, null, new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(1940, 11, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Carol Ahmed", 55.0, "Recurring headaches", new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Neurology", 3, null, 2 },
                    { new Guid("99b45e6b-3193-4d33-b80b-3feb2b7d609a"), null, null, new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(1960, 4, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Frank Lee", 48.299999999999997, "Stomach issues", new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Gastroenterology", 1, null, 2 },
                    { new Guid("b52ae6f9-4118-4009-9627-7b1107b8f1e1"), null, null, new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(1985, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), "David Johnson", 22.800000000000001, "Skin rash", new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 16, 0, 0, 0, 0, DateTimeKind.Utc), "Dermatology", 5, null, 1 },
                    { new Guid("b8524c28-a6cf-4683-8dee-a706e9da82ce"), null, null, new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(1970, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Bob Clarke", 30.199999999999999, "Knee pain", new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 6, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Orthopedics", 2, null, 1 },
                    { new Guid("ffa5b158-cd92-45f0-b2e8-0e6a22c87dac"), null, null, new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(1932, 8, 30, 0, 0, 0, 0, DateTimeKind.Utc), "Eva Rodriguez", 98.5, "Mass detection", new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 31, 4, 0, 0, 0, DateTimeKind.Utc), "Oncology", 1, null, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ReferralId",
                table: "AuditLogs",
                column: "ReferralId");

            migrationBuilder.CreateIndex(
                name: "IX_MassCommCampaigns_CreatedByUserId",
                table: "MassCommCampaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MassCommMessages_CampaignId",
                table: "MassCommMessages",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ClaimedByUserId",
                table: "Referrals",
                column: "ClaimedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_CreatedByUserId",
                table: "Referrals",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_Key",
                table: "SystemConfigs",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "MassCommMessages");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "MassCommCampaigns");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
