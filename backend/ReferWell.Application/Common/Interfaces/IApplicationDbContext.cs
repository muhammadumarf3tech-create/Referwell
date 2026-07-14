using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ReferWell.Domain.Entities;

namespace ReferWell.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationUserRole> UserRoles { get; }
    DbSet<RoleMenuAccess> RoleMenuAccesses { get; }
    DbSet<Patient> Patients { get; }
    DbSet<Referral> Referrals { get; }
    DbSet<ReferralAttachment> ReferralAttachments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<SecurityAuditEvent> SecurityAuditEvents { get; }
    DbSet<SystemConfig> SystemConfigs { get; }
    DbSet<MassCommCampaign> MassCommCampaigns { get; }
    DbSet<MassCommMessage> MassCommMessages { get; }
    DbSet<ReferralImportBatch> ReferralImportBatches { get; }
    DbSet<ReferralImportRow> ReferralImportRows { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    EntityEntry Entry(object entity);
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
