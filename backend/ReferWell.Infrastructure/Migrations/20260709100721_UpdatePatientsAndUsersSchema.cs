using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePatientsAndUsersSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.RenameColumn(
                name: "MedicalRecordNumber",
                table: "tblPatient",
                newName: "NhiNumber");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "tblPatient",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Referrals",
                columns: new[] { "Id", "AssignedToUserId", "ClaimedAt", "ClaimedByUserId", "CreatedAt", "CreatedByUserId", "PatientId", "PriorityScore", "Reason", "ReceivedAt", "ReferringGPId", "SlaBreach", "SlaDeadline", "SpecialistType", "Status", "UpdatedAt", "Urgency" },
                values: new object[,]
                {
                    { new Guid("0b018bf8-ae28-4247-96fb-aefa57aaaccb"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("99999999-9999-9999-9999-999955555555"), 98.5, "Mass detection", new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 31, 4, 0, 0, 0, DateTimeKind.Utc), "Oncology", 1, null, 4 },
                    { new Guid("41b64237-b7a7-4195-b464-2d56d01fcec1"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("66666666-6666-6666-6666-666655555555"), 30.199999999999999, "Knee pain", new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 6, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Orthopedics", 2, null, 1 },
                    { new Guid("5dd3c0e6-4c73-4d2a-8b94-bbff291dcffa"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("bbbbbbbb-5555-5555-5555-555555555555"), 66.099999999999994, "Vision deterioration", new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Ophthalmology", 4, null, 3 },
                    { new Guid("89ca8759-bd49-4e20-9a0b-255b20c6323f"), new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("55555555-5555-5555-5555-555555555555"), 75.5, "Chest pain", new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Cardiology", 1, null, 3 },
                    { new Guid("ad7663a0-0645-45fb-a461-f8401ab65696"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("aaaaaaaa-5555-5555-5555-555555555555"), 48.299999999999997, "Stomach issues", new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Gastroenterology", 1, null, 2 },
                    { new Guid("cf04eff1-c15c-491c-9932-b958670d1eaf"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("88888888-8888-8888-8888-888855555555"), 22.800000000000001, "Skin rash", new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 16, 0, 0, 0, 0, DateTimeKind.Utc), "Dermatology", 5, null, 1 },
                    { new Guid("df4de84a-eb2d-4c19-bb88-124ad165b505"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("77777777-7777-7777-7777-777755555555"), 55.0, "Recurring headaches", new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Neurology", 3, null, 2 },
                    { new Guid("fbbc20e9-c3b1-474d-98da-c0819bd350fa"), new Guid("44444444-4444-4444-4444-444444444444"), null, null, new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("cccccccc-5555-5555-5555-555555555555"), 41.700000000000003, "Chronic cough", new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 5, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Pulmonology", 6, null, 2 }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "FullName", "Gender", "PasswordHash", "PhoneNumber", "Title" },
                values: new object[] { "John Doe", "Male", "$2a$11$FrydPyduqCznsYvyOV0jO.Ce9HLx60Nd1/Xtv99QSX67xPiVhQk4q", "+64 21 111 2222", "Mr." });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "FullName", "Gender", "PasswordHash", "PhoneNumber", "Title" },
                values: new object[] { "Sarah Jenkins", "Female", "$2a$11$wYHUI.Tx8slHNPNpXZYlG.AbG9X4svahCVo3DJq8yEFVied3X1B6O", "+64 22 222 3333", "Mrs." });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "FullName", "Gender", "PasswordHash", "PhoneNumber", "Title" },
                values: new object[] { "James Wilson", "Male", "$2a$11$fohIVzxEYXK30BXKuxDKQuWMHWabUHaAvJQdFJh90kw3ycmQxuEua", "+64 27 333 4444", "Dr." });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "FullName", "Gender", "PasswordHash", "PhoneNumber", "Title" },
                values: new object[] { "Amelia Hart", "Female", "$2a$11$mxQ6Qk1GDtKPPKCFDRtum.InjBx/ILI2vQ.BwxhwAUpdsIm/0Xhdq", "+64 29 444 5555", "Dr." });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8056), "Female", "ABC1234", "+64 21 555 0101" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666655555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8065), "Male", "DEF5678", "+64 22 555 0102" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777755555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8071), "Female", "GHI9012", "+64 27 555 0103" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888855555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8073), "Male", "JKL3456", "+64 29 555 0104" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999955555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8075), "Female", "MNO7890", "+64 21 555 0105" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8077), "Male", "PQR1234", "+64 22 555 0106" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8079), "Female", "STU5678", "+64 27 555 0107" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "Gender", "NhiNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8081), "Male", "VWX9012", "+64 29 555 0108" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("0b018bf8-ae28-4247-96fb-aefa57aaaccb"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("41b64237-b7a7-4195-b464-2d56d01fcec1"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("5dd3c0e6-4c73-4d2a-8b94-bbff291dcffa"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("89ca8759-bd49-4e20-9a0b-255b20c6323f"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("ad7663a0-0645-45fb-a461-f8401ab65696"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("cf04eff1-c15c-491c-9932-b958670d1eaf"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("df4de84a-eb2d-4c19-bb88-124ad165b505"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("fbbc20e9-c3b1-474d-98da-c0819bd350fa"));

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "tblPatient");

            migrationBuilder.RenameColumn(
                name: "NhiNumber",
                table: "tblPatient",
                newName: "MedicalRecordNumber");

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

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "FullName", "PasswordHash" },
                values: new object[] { "System Admin", "$2a$11$rF1qMEnD/rmWZydoEDH5gOvwq04Oz1emOfEbtjglaQgJR7uMc3jNy" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "FullName", "PasswordHash" },
                values: new object[] { "Nurse Sarah", "$2a$11$.jyjLn0R0vUskP96RMnlNeXGy41IGor1Cl0DPYkjFiWg4vMfMrka6" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "FullName", "PasswordHash" },
                values: new object[] { "Dr. James Wilson", "$2a$11$jgsBOyFPMC4guVpj29tyDebEP6.mZ.qOyPafTg2T88mbOSZ4WqnL." });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "FullName", "PasswordHash" },
                values: new object[] { "Dr. Amelia Hart", "$2a$11$txN28uqc1K01gP2sd83vLOVuAYvvtLBUHJS3oJQEEEAn2Yok2OffW" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5177), "MRN-1001", "555-0101" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666655555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5190), "MRN-1002", "555-0102" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777755555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5192), "MRN-1003", "555-0103" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888855555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5195), "MRN-1004", "555-0104" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999955555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5197), "MRN-1005", "555-0105" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5199), "MRN-1006", "555-0106" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5201), "MRN-1007", "555-0107" });

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "MedicalRecordNumber", "PhoneNumber" },
                values: new object[] { new DateTime(2026, 7, 9, 8, 9, 7, 414, DateTimeKind.Utc).AddTicks(5203), "MRN-1008", "555-0108" });
        }
    }
}
