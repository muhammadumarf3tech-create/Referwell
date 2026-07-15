using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReferWell.Infrastructure.Data;

#nullable disable

namespace ReferWell.Infrastructure.Migrations
{
    /// <summary>
    /// Adds a second triage nurse and realigns AssignedTo to hospital-queue semantics
    /// (unassigned on intake; nurses own in-progress items — never the referring GP).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260715100000_SharedQueueVisibilityAndNurse2")]
    public partial class SharedQueueVisibilityAndNurse2 : Migration
    {
        private const string NurseId = "22222222-2222-2222-2222-222222222222";
        private const string Nurse2Id = "22222222-2222-2222-2222-222222222223";
        private const string Nurse2RoleId = "11111111-2222-3333-4444-555555555555";
        private const string Gp1Id = "33333333-3333-3333-3333-333333333333";
        private const string Gp2Id = "44444444-4444-4444-4444-444444444444";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Id] = '{Nurse2Id}')
BEGIN
    INSERT INTO [Users] ([Id], [CreatedAt], [Email], [FullName], [Gender], [IsActive], [PasswordHash], [PhoneNumber], [Title])
    VALUES (
        '{Nurse2Id}',
        '2024-01-01T00:00:00',
        N'nurse2@referwell.com',
        N'Mia Thompson',
        N'Female',
        1,
        N'$2a$11$A5/voTCx9k4WWH.BiyW2Se5PPFYSvc5Z2i8a2we/T.4TnlXHH3xqW',
        N'+64 22 222 3344',
        N'Ms.'
    );
END

IF NOT EXISTS (SELECT 1 FROM [tblUserRoles] WHERE [Id] = '{Nurse2RoleId}')
BEGIN
    INSERT INTO [tblUserRoles] ([Id], [Role], [UserId])
    VALUES ('{Nurse2RoleId}', 2, '{Nurse2Id}');
END

-- Clear GP self-assignment on open intake (shared hospital queue).
UPDATE [Referrals]
SET [AssignedToUserId] = NULL
WHERE [Status] = 1
  AND [AssignedToUserId] IN ('{Gp1Id}', '{Gp2Id}');

-- Demo seed cases: hospital staff ownership by lifecycle stage (by CaseNo for resilience).
UPDATE [Referrals] SET [AssignedToUserId] = NULL WHERE [CaseNo] IN (N'Ref-000001', N'Ref-000005', N'Ref-000006');
UPDATE [Referrals] SET [AssignedToUserId] = '{NurseId}' WHERE [CaseNo] IN (N'Ref-000002', N'Ref-000004', N'Ref-000007');
UPDATE [Referrals] SET [AssignedToUserId] = '{Nurse2Id}' WHERE [CaseNo] IN (N'Ref-000003', N'Ref-000008');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
UPDATE [Referrals] SET [AssignedToUserId] = '{Gp1Id}' WHERE [CaseNo] IN (N'Ref-000001', N'Ref-000002', N'Ref-000005', N'Ref-000007');
UPDATE [Referrals] SET [AssignedToUserId] = '{Gp2Id}' WHERE [CaseNo] IN (N'Ref-000003', N'Ref-000004', N'Ref-000006', N'Ref-000008');

DELETE FROM [tblUserRoles] WHERE [Id] = '{Nurse2RoleId}';
DELETE FROM [Users] WHERE [Id] = '{Nurse2Id}';
");
        }
    }
}
