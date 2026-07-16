using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReferWell.Infrastructure.Data;

#nullable disable

namespace ReferWell.Infrastructure.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260716120000_AddPatientsMenuAccess")]
public partial class AddPatientsMenuAccess : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [tblRoleMenuAccess] WHERE [Id] = '99999999-9999-9999-9999-000000000019')
    INSERT INTO [tblRoleMenuAccess] ([Id], [Role], [MenuItem], [HasAccess])
    VALUES ('99999999-9999-9999-9999-000000000019', 1, N'Patients', 1);

IF NOT EXISTS (SELECT 1 FROM [tblRoleMenuAccess] WHERE [Id] = '99999999-9999-9999-9999-00000000001A')
    INSERT INTO [tblRoleMenuAccess] ([Id], [Role], [MenuItem], [HasAccess])
    VALUES ('99999999-9999-9999-9999-00000000001A', 2, N'Patients', 1);

IF NOT EXISTS (SELECT 1 FROM [tblRoleMenuAccess] WHERE [Id] = '99999999-9999-9999-9999-00000000001B')
    INSERT INTO [tblRoleMenuAccess] ([Id], [Role], [MenuItem], [HasAccess])
    VALUES ('99999999-9999-9999-9999-00000000001B', 3, N'Patients', 1);
");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DELETE FROM [tblRoleMenuAccess] WHERE [Id] IN (
    '99999999-9999-9999-9999-000000000019',
    '99999999-9999-9999-9999-00000000001A',
    '99999999-9999-9999-9999-00000000001B'
);
");
    }
}
