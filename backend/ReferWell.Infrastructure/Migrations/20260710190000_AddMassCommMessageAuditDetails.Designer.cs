using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReferWell.Infrastructure.Data;

#nullable disable

namespace ReferWell.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260710190000_AddMassCommMessageAuditDetails")]
public partial class AddMassCommMessageAuditDetails
{
}
