using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;

namespace ReferWell.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<MassCommCampaign> MassCommCampaigns => Set<MassCommCampaign>();
    public DbSet<MassCommMessage> MassCommMessages => Set<MassCommMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── ApplicationUser ──────────────────────────────────────────────────
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        // ── Referral ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Referral>(e =>
        {
            e.HasKey(r => r.Id);
            // Optimistic concurrency via SQL Server rowversion
            e.Property(r => r.RowVersion).IsRowVersion().IsConcurrencyToken();
            e.Property(r => r.PatientName).HasMaxLength(200).IsRequired();
            e.Property(r => r.SpecialistType).HasMaxLength(100).IsRequired();
            e.Property(r => r.Reason).HasMaxLength(2000);

            e.HasOne(r => r.CreatedByUser)
                .WithMany(u => u.CreatedReferrals)
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.ClaimedByUser)
                .WithMany()
                .HasForeignKey(r => r.ClaimedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── AuditLog ─────────────────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Referral)
                .WithMany(r => r.AuditLogs)
                .HasForeignKey(a => a.ReferralId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.PerformedByUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SystemConfig ─────────────────────────────────────────────────────
        modelBuilder.Entity<SystemConfig>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.Key).IsUnique();
            e.Property(c => c.Key).HasMaxLength(100).IsRequired();
            e.Property(c => c.Value).HasMaxLength(500).IsRequired();
        });

        // ── MassComm ─────────────────────────────────────────────────────────
        modelBuilder.Entity<MassCommCampaign>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasMany(c => c.Messages)
                .WithOne(m => m.Campaign)
                .HasForeignKey(m => m.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MassCommMessage>(e =>
        {
            e.HasKey(m => m.Id);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // ── Users ─────────────────────────────────────────────────────────────
        var adminId  = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var nurseId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var gp1Id    = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var gp2Id    = Guid.Parse("44444444-4444-4444-4444-444444444444");

        modelBuilder.Entity<ApplicationUser>().HasData(
            new ApplicationUser
            {
                Id = adminId, FullName = "System Admin", Email = "admin@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = UserRole.Admin, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ApplicationUser
            {
                Id = nurseId, FullName = "Nurse Sarah", Email = "nurse@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Nurse@123"),
                Role = UserRole.TriageNurse, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ApplicationUser
            {
                Id = gp1Id, FullName = "Dr. James Wilson", Email = "gp1@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gp1@1234"),
                Role = UserRole.GP, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new ApplicationUser
            {
                Id = gp2Id, FullName = "Dr. Amelia Hart", Email = "gp2@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gp2@1234"),
                Role = UserRole.GP, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // ── System Config (Priority Weights) ──────────────────────────────────
        modelBuilder.Entity<SystemConfig>().HasData(
            new SystemConfig { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Key = "weight_urgency",  Value = "50", Description = "Weight % for urgency in priority score",   UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new SystemConfig { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Key = "weight_waittime", Value = "30", Description = "Weight % for wait time in priority score",  UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new SystemConfig { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Key = "weight_patient",  Value = "20", Description = "Weight % for patient age in priority score", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // ── Sample Referrals ──────────────────────────────────────────────────
        var now = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var referrals = new[]
        {
            new Referral { Id = Guid.NewGuid(), PatientName = "Alice Martin",   PatientDateOfBirth = new DateTime(1955, 3, 10, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Cardiology",    Reason = "Chest pain",         Urgency = UrgencyLevel.Urgent,     Status = ReferralStatus.Received, ReceivedAt = now.AddDays(-5),  SlaDeadline = now.AddDays(-4), CreatedAt = now.AddDays(-5),  PriorityScore = 75.5 },
            new Referral { Id = Guid.NewGuid(), PatientName = "Bob Clarke",     PatientDateOfBirth = new DateTime(1970, 7, 22, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Orthopedics",   Reason = "Knee pain",          Urgency = UrgencyLevel.Routine,    Status = ReferralStatus.Triaged,  ReceivedAt = now.AddDays(-10), SlaDeadline = now.AddDays(20), CreatedAt = now.AddDays(-10), PriorityScore = 30.2 },
            new Referral { Id = Guid.NewGuid(), PatientName = "Carol Ahmed",    PatientDateOfBirth = new DateTime(1940, 11, 5, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Neurology",     Reason = "Recurring headaches",Urgency = UrgencyLevel.Soon,       Status = ReferralStatus.Accepted, ReceivedAt = now.AddDays(-3),  SlaDeadline = now.AddDays(4),  CreatedAt = now.AddDays(-3),  PriorityScore = 55.0 },
            new Referral { Id = Guid.NewGuid(), PatientName = "David Johnson",  PatientDateOfBirth = new DateTime(1985, 2, 14, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Dermatology",   Reason = "Skin rash",          Urgency = UrgencyLevel.Routine,    Status = ReferralStatus.Booked,   ReceivedAt = now.AddDays(-15), SlaDeadline = now.AddDays(15), CreatedAt = now.AddDays(-15), PriorityScore = 22.8 },
            new Referral { Id = Guid.NewGuid(), PatientName = "Eva Rodriguez",  PatientDateOfBirth = new DateTime(1932, 8, 30, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Oncology",      Reason = "Mass detection",     Urgency = UrgencyLevel.Emergency,  Status = ReferralStatus.Received, ReceivedAt = now.AddDays(-1),  SlaDeadline = now.AddHours(-20),CreatedAt= now.AddDays(-1),  PriorityScore = 98.5 },
            new Referral { Id = Guid.NewGuid(), PatientName = "Frank Lee",      PatientDateOfBirth = new DateTime(1960, 4, 18, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Gastroenterology",Reason = "Stomach issues",   Urgency = UrgencyLevel.Soon,       Status = ReferralStatus.Received, ReceivedAt = now.AddDays(-2),  SlaDeadline = now.AddDays(5),  CreatedAt = now.AddDays(-2),  PriorityScore = 48.3 },
            new Referral { Id = Guid.NewGuid(), PatientName = "Grace Kim",      PatientDateOfBirth = new DateTime(1978, 12, 3, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Ophthalmology", Reason = "Vision deterioration",Urgency = UrgencyLevel.Urgent,    Status = ReferralStatus.Declined, ReceivedAt = now.AddDays(-7),  SlaDeadline = now.AddDays(-6), CreatedAt = now.AddDays(-7),  PriorityScore = 66.1 },
            new Referral { Id = Guid.NewGuid(), PatientName = "Henry Smith",    PatientDateOfBirth = new DateTime(1945, 6, 25, 0, 0, 0, DateTimeKind.Utc), CreatedByUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Pulmonology",   Reason = "Chronic cough",      Urgency = UrgencyLevel.Soon,       Status = ReferralStatus.Completed,ReceivedAt = now.AddDays(-20), SlaDeadline = now.AddDays(-13),CreatedAt= now.AddDays(-20), PriorityScore = 41.7 },
        };

        modelBuilder.Entity<Referral>().HasData(referrals);
    }
}
