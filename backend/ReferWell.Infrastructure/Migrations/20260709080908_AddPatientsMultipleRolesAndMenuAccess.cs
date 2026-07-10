using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientsMultipleRolesAndMenuAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("2f7d3d63-4b9e-454c-bbec-ff5ffea09033"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("30fdba99-25df-4e70-9353-0805e3c51ad2"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("46e92d71-fe4e-45cb-9781-cae95b336c3d"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("5da6a25f-9982-45b9-9501-d4a3ae7ca24d"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("99b45e6b-3193-4d33-b80b-3feb2b7d609a"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("b52ae6f9-4118-4009-9627-7b1107b8f1e1"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("b8524c28-a6cf-4683-8dee-a706e9da82ce"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("ffa5b158-cd92-45f0-b2e8-0e6a22c87dac"));

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PatientDateOfBirth",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "Referrals");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Referrals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "Referrals",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "tblPatient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedicalRecordNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPatient", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblReferralAttachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblReferralAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblReferralAttachment_Referrals_ReferralId",
                        column: x => x.ReferralId,
                        principalTable: "Referrals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblRoleMenuAccess",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    MenuItem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasAccess = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRoleMenuAccess", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblUserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Password", "PasswordHash" },
                values: new object[] { "Admin@123", "$2a$11$rF1qMEnD/rmWZydoEDH5gOvwq04Oz1emOfEbtjglaQgJR7uMc3jNy" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "Password", "PasswordHash" },
                values: new object[] { "Nurse@123", "$2a$11$.jyjLn0R0vUskP96RMnlNeXGy41IGor1Cl0DPYkjFiWg4vMfMrka6" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "Password", "PasswordHash" },
                values: new object[] { "Gp1@1234", "$2a$11$jgsBOyFPMC4guVpj29tyDebEP6.mZ.qOyPafTg2T88mbOSZ4WqnL." });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "Password", "PasswordHash" },
                values: new object[] { "Gp2@1234", "$2a$11$txN28uqc1K01gP2sd83vLOVuAYvvtLBUHJS3oJQEEEAn2Yok2OffW" });

            migrationBuilder.InsertData(
                table: "tblPatient",
                columns: new[] { "Id", "CreatedAt", "DateOfBirth", "Email", "MedicalRecordNumber", "Name", "PhoneNumber" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5177), new DateTime(1955, 3, 10, 0, 0, 0, 0, DateTimeKind.Utc), "alice.martin@example.com", "MRN-1001", "Alice Martin", "555-0101" },
                    { new Guid("66666666-6666-6666-6666-666655555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5190), new DateTime(1970, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "bob.clarke@example.com", "MRN-1002", "Bob Clarke", "555-0102" },
                    { new Guid("77777777-7777-7777-7777-777755555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5192), new DateTime(1940, 11, 5, 0, 0, 0, 0, DateTimeKind.Utc), "carol.ahmed@example.com", "MRN-1003", "Carol Ahmed", "555-0103" },
                    { new Guid("88888888-8888-8888-8888-888855555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5195), new DateTime(1985, 2, 14, 0, 0, 0, 0, DateTimeKind.Utc), "david.johnson@example.com", "MRN-1004", "David Johnson", "555-0104" },
                    { new Guid("99999999-9999-9999-9999-999955555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5197), new DateTime(1932, 8, 30, 0, 0, 0, 0, DateTimeKind.Utc), "eva.rodriguez@example.com", "MRN-1005", "Eva Rodriguez", "555-0105" },
                    { new Guid("aaaaaaaa-5555-5555-5555-555555555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5199), new DateTime(1960, 4, 18, 0, 0, 0, 0, DateTimeKind.Utc), "frank.lee@example.com", "MRN-1006", "Frank Lee", "555-0106" },
                    { new Guid("bbbbbbbb-5555-5555-5555-555555555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5201), new DateTime(1978, 12, 3, 0, 0, 0, 0, DateTimeKind.Utc), "grace.kim@example.com", "MRN-1007", "Grace Kim", "555-0107" },
                    { new Guid("cccccccc-5555-5555-5555-555555555555"), new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5203), new DateTime(1945, 6, 25, 0, 0, 0, 0, DateTimeKind.Utc), "henry.smith@example.com", "MRN-1008", "Henry Smith", "555-0108" }
                });

            migrationBuilder.InsertData(
                table: "tblRoleMenuAccess",
                columns: new[] { "Id", "HasAccess", "MenuItem", "Role" },
                values: new object[,]
                {
                    { new Guid("99999999-9999-9999-9999-000000000001"), true, "Dashboard", 1 },
                    { new Guid("99999999-9999-9999-9999-000000000002"), true, "Priority Config", 1 },
                    { new Guid("99999999-9999-9999-9999-000000000003"), true, "Mass Communications", 1 },
                    { new Guid("99999999-9999-9999-9999-000000000004"), true, "User Management", 1 },
                    { new Guid("99999999-9999-9999-9999-000000000005"), true, "Menu Access", 1 },
                    { new Guid("99999999-9999-9999-9999-000000000006"), true, "Dashboard", 2 },
                    { new Guid("99999999-9999-9999-9999-000000000007"), true, "Priority Config", 2 },
                    { new Guid("99999999-9999-9999-9999-000000000008"), true, "Mass Communications", 2 },
                    { new Guid("99999999-9999-9999-9999-000000000009"), false, "User Management", 2 },
                    { new Guid("99999999-9999-9999-9999-000000000010"), false, "Menu Access", 2 },
                    { new Guid("99999999-9999-9999-9999-000000000011"), true, "Dashboard", 3 },
                    { new Guid("99999999-9999-9999-9999-000000000012"), false, "Priority Config", 3 },
                    { new Guid("99999999-9999-9999-9999-000000000013"), false, "Mass Communications", 3 },
                    { new Guid("99999999-9999-9999-9999-000000000014"), false, "User Management", 3 },
                    { new Guid("99999999-9999-9999-9999-000000000015"), false, "Menu Access", 3 }
                });

            migrationBuilder.InsertData(
                table: "tblUserRoles",
                columns: new[] { "Id", "Role", "UserId" },
                values: new object[,]
                {
                    { new Guid("11111111-2222-3333-4444-555555555551"), 1, new Guid("11111111-1111-1111-1111-111111111111") },
                    { new Guid("11111111-2222-3333-4444-555555555552"), 2, new Guid("22222222-2222-2222-2222-222222222222") },
                    { new Guid("11111111-2222-3333-4444-555555555553"), 3, new Guid("33333333-3333-3333-3333-333333333333") },
                    { new Guid("11111111-2222-3333-4444-555555555554"), 3, new Guid("44444444-4444-4444-4444-444444444444") }
                });

            migrationBuilder.InsertData(
                table: "Referrals",
                columns: new[] { "Id", "AssignedToUserId", "ClaimedAt", "ClaimedByUserId", "CreatedAt", "CreatedByUserId", "PatientId", "PriorityScore", "Reason", "ReceivedAt", "ReferringGPId", "SlaBreach", "SlaDeadline", "SpecialistType", "Status", "UpdatedAt", "Urgency" },
                values: new object[,]
                {
                    { new Guid("14feb7f6-00a8-4519-8172-93d2b4cf1622"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("77777777-7777-7777-7777-777755555555"), 55.0, "Recurring headaches", new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Neurology", 3, null, 2 },
                    { new Guid("225e2065-1639-4a2c-af6c-71fac191fd95"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("66666666-6666-6666-6666-666655555555"), 30.199999999999999, "Knee pain", new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 6, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Orthopedics", 2, null, 1 },
                    { new Guid("2e3c5ba3-4d13-4fb0-a785-3a3fb7485d87"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("cccccccc-5555-5555-5555-555555555555"), 41.700000000000003, "Chronic cough", new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 5, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Pulmonology", 6, null, 2 },
                    { new Guid("76d27d9d-538d-4686-9bf1-ead43c9bcf66"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("88888888-8888-8888-8888-888855555555"), 22.800000000000001, "Skin rash", new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 16, 0, 0, 0, 0, DateTimeKind.Utc), "Dermatology", 5, null, 1 },
                    { new Guid("8f630e4f-060a-4ae9-a9a1-5d7d17cb24d9"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("aaaaaaaa-5555-5555-5555-555555555555"), 48.299999999999997, "Stomach issues", new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Gastroenterology", 1, null, 2 },
                    { new Guid("93f0a492-0ec1-4b28-acf8-372f0cdd4546"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("55555555-5555-5555-5555-555555555555"), 75.5, "Chest pain", new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Cardiology", 1, null, 3 },
                    { new Guid("e1724b56-9b05-47ca-8211-bec3ad8b8532"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("bbbbbbbb-5555-5555-5555-555555555555"), 66.099999999999994, "Vision deterioration", new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Ophthalmology", 4, null, 3 },
                    { new Guid("f484b7c8-0e81-4fc0-a4a8-f83b4c271712"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("99999999-9999-9999-9999-999955555555"), 98.5, "Mass detection", new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 31, 4, 0, 0, 0, DateTimeKind.Utc), "Oncology", 1, null, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_AssignedToUserId",
                table: "Referrals",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_PatientId",
                table: "Referrals",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_tblReferralAttachment_ReferralId",
                table: "tblReferralAttachment",
                column: "ReferralId");

            migrationBuilder.CreateIndex(
                name: "IX_tblUserRoles_UserId",
                table: "tblUserRoles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_Users_AssignedToUserId",
                table: "Referrals",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Referrals_tblPatient_PatientId",
                table: "Referrals",
                column: "PatientId",
                principalTable: "tblPatient",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_Users_AssignedToUserId",
                table: "Referrals");

            migrationBuilder.DropForeignKey(
                name: "FK_Referrals_tblPatient_PatientId",
                table: "Referrals");

            migrationBuilder.DropTable(
                name: "tblPatient");

            migrationBuilder.DropTable(
                name: "tblReferralAttachment");

            migrationBuilder.DropTable(
                name: "tblRoleMenuAccess");

            migrationBuilder.DropTable(
                name: "tblUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_AssignedToUserId",
                table: "Referrals");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_PatientId",
                table: "Referrals");

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("14feb7f6-00a8-4519-8172-93d2b4cf1622"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("225e2065-1639-4a2c-af6c-71fac191fd95"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("2e3c5ba3-4d13-4fb0-a785-3a3fb7485d87"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("76d27d9d-538d-4686-9bf1-ead43c9bcf66"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("8f630e4f-060a-4ae9-a9a1-5d7d17cb24d9"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("93f0a492-0ec1-4b28-acf8-372f0cdd4546"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("e1724b56-9b05-47ca-8211-bec3ad8b8532"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("f484b7c8-0e81-4fc0-a4a8-f83b4c271712"));

            migrationBuilder.DropColumn(
                name: "Password",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Referrals");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PatientDateOfBirth",
                table: "Referrals",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "Referrals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "$2a$11$XEiDr9QOoCLcTH9lyxsiK.xAA7U.bXZxAkUZmt01W5PzvnL3IVNDa", 1 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "$2a$11$lCaB5GQ49O7zEiUoDSF0JOFH8Ouq/TSyPTIPXV52plIIyfOmYkjgy", 2 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "$2a$11$KLbW14SVatQ25b62lvogieQ3BTUaUJxzEQX8ij/etiuA/BrsuVucS", 3 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "$2a$11$c462mFKpwQIu4TPmtMfQy.B8S.EDx/ByKBKO.AH7GYe8xVauoGJiy", 3 });
        }
    }
}
