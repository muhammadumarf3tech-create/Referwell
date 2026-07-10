using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseNoToReferral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<string>(
                name: "CaseNo",
                table: "Referrals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Referrals",
                columns: new[] { "Id", "AssignedToUserId", "CaseNo", "ClaimedAt", "ClaimedByUserId", "CreatedAt", "CreatedByUserId", "PatientId", "PriorityScore", "Reason", "ReceivedAt", "ReferringGPId", "SlaBreach", "SlaDeadline", "SpecialistType", "Status", "UpdatedAt", "Urgency" },
                values: new object[,]
                {
                    { new Guid("175cfe3f-b57d-47af-8ec0-3c2eb837c8be"), new Guid("33333333-3333-3333-3333-333333333333"), "Ref-000007", null, null, new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("bbbbbbbb-5555-5555-5555-555555555555"), 66.099999999999994, "Vision deterioration", new DateTime(2024, 5, 25, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Ophthalmology", 4, null, 3 },
                    { new Guid("18f84987-efd0-40e4-add8-ba1dd444aab2"), new Guid("33333333-3333-3333-3333-333333333333"), "Ref-000001", null, null, new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("55555555-5555-5555-5555-555555555555"), 75.5, "Chest pain", new DateTime(2024, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 28, 0, 0, 0, 0, DateTimeKind.Utc), "Cardiology", 1, null, 3 },
                    { new Guid("6b4c73ed-1955-4b3f-b1e8-739624ea04bd"), new Guid("44444444-4444-4444-4444-444444444444"), "Ref-000006", null, null, new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("aaaaaaaa-5555-5555-5555-555555555555"), 48.299999999999997, "Stomach issues", new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 6, 0, 0, 0, 0, DateTimeKind.Utc), "Gastroenterology", 1, null, 2 },
                    { new Guid("6cb559e6-9ae9-4848-a9ff-1779bf3a2bf2"), new Guid("44444444-4444-4444-4444-444444444444"), "Ref-000008", null, null, new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("cccccccc-5555-5555-5555-555555555555"), 41.700000000000003, "Chronic cough", new DateTime(2024, 5, 12, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 5, 19, 0, 0, 0, 0, DateTimeKind.Utc), "Pulmonology", 6, null, 2 },
                    { new Guid("821a4b94-eb5f-4d84-9d41-a87ddf014f85"), new Guid("33333333-3333-3333-3333-333333333333"), "Ref-000005", null, null, new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("99999999-9999-9999-9999-999955555555"), 98.5, "Mass detection", new DateTime(2024, 5, 31, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 5, 31, 4, 0, 0, 0, DateTimeKind.Utc), "Oncology", 1, null, 4 },
                    { new Guid("aa7b9257-d581-49b8-8129-e17efd5768cb"), new Guid("44444444-4444-4444-4444-444444444444"), "Ref-000003", null, null, new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("77777777-7777-7777-7777-777755555555"), 55.0, "Recurring headaches", new DateTime(2024, 5, 29, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), "Neurology", 3, null, 2 },
                    { new Guid("d282f527-84fb-424c-a60b-9b46e28c671d"), new Guid("33333333-3333-3333-3333-333333333333"), "Ref-000002", null, null, new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("33333333-3333-3333-3333-333333333333"), new Guid("66666666-6666-6666-6666-666655555555"), 30.199999999999999, "Knee pain", new DateTime(2024, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), "33333333-3333-3333-3333-333333333333", false, new DateTime(2024, 6, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Orthopedics", 2, null, 1 },
                    { new Guid("e64e3d2a-05fe-486c-ab4c-9a5aa4c23264"), new Guid("44444444-4444-4444-4444-444444444444"), "Ref-000004", null, null, new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("44444444-4444-4444-4444-444444444444"), new Guid("88888888-8888-8888-8888-888855555555"), 22.800000000000001, "Skin rash", new DateTime(2024, 5, 17, 0, 0, 0, 0, DateTimeKind.Utc), "44444444-4444-4444-4444-444444444444", false, new DateTime(2024, 6, 16, 0, 0, 0, 0, DateTimeKind.Utc), "Dermatology", 5, null, 1 }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "PasswordHash",
                value: "$2a$11$aEbsxRKpYr79oo/CSLVXqe9XiR4Z3uBzsZM6OXu7tKPs3MHByQdTa");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "PasswordHash",
                value: "$2a$11$NMD3xWV1DmWfAMkeUXCFgey/KMt/II4KQgaHoOQIi8PzzLCDLEYTu");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "PasswordHash",
                value: "$2a$11$BQPxCsnI3v8HQt0Svu7JI.cFEGkhVhb65LrEb3tffhqY62dv.tRiC");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "PasswordHash",
                value: "$2a$11$80Q7KtNOaJbm3Gn7q.rP.eX7oTBrYCFbJ4PwdVXhmRZesfpHzij8m");

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9344));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666655555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9351));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777755555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9353));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888855555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9356));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999955555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9357));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9359));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9365));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 12, 24, 6, 111, DateTimeKind.Utc).AddTicks(9372));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("175cfe3f-b57d-47af-8ec0-3c2eb837c8be"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("18f84987-efd0-40e4-add8-ba1dd444aab2"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("6b4c73ed-1955-4b3f-b1e8-739624ea04bd"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("6cb559e6-9ae9-4848-a9ff-1779bf3a2bf2"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("821a4b94-eb5f-4d84-9d41-a87ddf014f85"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("aa7b9257-d581-49b8-8129-e17efd5768cb"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("d282f527-84fb-424c-a60b-9b46e28c671d"));

            migrationBuilder.DeleteData(
                table: "Referrals",
                keyColumn: "Id",
                keyValue: new Guid("e64e3d2a-05fe-486c-ab4c-9a5aa4c23264"));

            migrationBuilder.DropColumn(
                name: "CaseNo",
                table: "Referrals");

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
                column: "PasswordHash",
                value: "$2a$11$FrydPyduqCznsYvyOV0jO.Ce9HLx60Nd1/Xtv99QSX67xPiVhQk4q");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "PasswordHash",
                value: "$2a$11$wYHUI.Tx8slHNPNpXZYlG.AbG9X4svahCVo3DJq8yEFVied3X1B6O");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "PasswordHash",
                value: "$2a$11$fohIVzxEYXK30BXKuxDKQuWMHWabUHaAvJQdFJh90kw3ycmQxuEua");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "PasswordHash",
                value: "$2a$11$mxQ6Qk1GDtKPPKCFDRtum.InjBx/ILI2vQ.BwxhwAUpdsIm/0Xhdq");

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8056));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666655555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8065));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777755555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8071));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888855555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8073));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999955555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8075));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8077));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8079));

            migrationBuilder.UpdateData(
                table: "tblPatient",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 9, 10, 7, 20, 816, DateTimeKind.Utc).AddTicks(8081));
        }
    }
}
