using Microsoft.EntityFrameworkCore;
using ReferWell.Domain.Entities;
using ReferWell.Domain.Enums;

namespace ReferWell.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<ApplicationUserRole> UserRoles => Set<ApplicationUserRole>();
    public DbSet<RoleMenuAccess> RoleMenuAccesses => Set<RoleMenuAccess>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<ReferralAttachment> ReferralAttachments => Set<ReferralAttachment>();
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
            e.Property(u => u.Password).IsRequired();
        });

        // ── ApplicationUserRole ──────────────────────────────────────────────
        modelBuilder.Entity<ApplicationUserRole>(e =>
        {
            e.ToTable("tblUserRoles");
            e.HasKey(ur => ur.Id);
            e.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RoleMenuAccess ───────────────────────────────────────────────────
        modelBuilder.Entity<RoleMenuAccess>(e =>
        {
            e.ToTable("tblRoleMenuAccess");
            e.HasKey(rma => rma.Id);
        });

        // ── Patient ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Patient>(e =>
        {
            e.ToTable("tblPatient");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.NhiNumber).HasMaxLength(50).IsRequired();
            e.Property(p => p.Gender).HasMaxLength(20);
        });

        // ── Referral ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Referral>(e =>
        {
            e.HasKey(r => r.Id);
            // Optimistic concurrency via SQL Server rowversion
            e.Property(r => r.RowVersion).IsRowVersion().IsConcurrencyToken();
            e.Property(r => r.SpecialistType).HasMaxLength(100).IsRequired();
            e.Property(r => r.Reason).HasMaxLength(2000);
            e.Property(r => r.CaseNo).HasMaxLength(20).IsRequired();

            e.HasOne(r => r.CreatedByUser)
                .WithMany(u => u.CreatedReferrals)
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.ClaimedByUser)
                .WithMany()
                .HasForeignKey(r => r.ClaimedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(r => r.AssignedToUser)
                .WithMany()
                .HasForeignKey(r => r.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Patient)
                .WithMany(p => p.Referrals)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ReferralAttachment ───────────────────────────────────────────────
        modelBuilder.Entity<ReferralAttachment>(e =>
        {
            e.ToTable("tblReferralAttachment");
            e.HasKey(ra => ra.Id);
            e.HasOne(ra => ra.Referral)
                .WithMany(r => r.Attachments)
                .HasForeignKey(ra => ra.ReferralId)
                .OnDelete(DeleteBehavior.Cascade);
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
                Id = adminId, FullName = "John Doe", Email = "admin@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Password = "Admin@123",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Title = "Mr.", Gender = "Male", PhoneNumber = "+64 21 111 2222"
            },
            new ApplicationUser
            {
                Id = nurseId, FullName = "Sarah Jenkins", Email = "nurse@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Nurse@123"),
                Password = "Nurse@123",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Title = "Mrs.", Gender = "Female", PhoneNumber = "+64 22 222 3333"
            },
            new ApplicationUser
            {
                Id = gp1Id, FullName = "James Wilson", Email = "gp1@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gp1@1234"),
                Password = "Gp1@1234",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Title = "Dr.", Gender = "Male", PhoneNumber = "+64 27 333 4444"
            },
            new ApplicationUser
            {
                Id = gp2Id, FullName = "Amelia Hart", Email = "gp2@referwell.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gp2@1234"),
                Password = "Gp2@1234",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Title = "Dr.", Gender = "Female", PhoneNumber = "+64 29 444 5555"
            }
        );

        // ── Seed Multiple User Roles ───────────────────────────────────────────
        modelBuilder.Entity<ApplicationUserRole>().HasData(
            new ApplicationUserRole { Id = Guid.Parse("11111111-2222-3333-4444-555555555551"), UserId = adminId, Role = UserRole.Admin },
            new ApplicationUserRole { Id = Guid.Parse("11111111-2222-3333-4444-555555555552"), UserId = nurseId, Role = UserRole.TriageNurse },
            new ApplicationUserRole { Id = Guid.Parse("11111111-2222-3333-4444-555555555553"), UserId = gp1Id, Role = UserRole.GP },
            new ApplicationUserRole { Id = Guid.Parse("11111111-2222-3333-4444-555555555554"), UserId = gp2Id, Role = UserRole.GP }
        );

        // ── Seed Role Menu Access Control Configuration ──────────────────────────
        var rmaIdIndex = 1;
        var rmaList = new List<RoleMenuAccess>();
        var menus = new[] { "Dashboard", "Priority Config", "Mass Communications", "User Management", "Menu Access" };
        foreach (var role in Enum.GetValues<UserRole>())
        {
            foreach (var menu in menus)
            {
                bool hasAccess = role switch
                {
                    UserRole.Admin => true,
                    UserRole.TriageNurse => menu is "Dashboard" or "Priority Config" or "Mass Communications",
                    UserRole.GP => menu is "Dashboard",
                    _ => false
                };

                rmaList.Add(new RoleMenuAccess
                {
                    Id = Guid.Parse($"99999999-9999-9999-9999-0000000000{rmaIdIndex++:D2}"),
                    Role = role,
                    MenuItem = menu,
                    HasAccess = hasAccess
                });
            }
        }
        modelBuilder.Entity<RoleMenuAccess>().HasData(rmaList);

        // ── Seed Standalone Patients ──────────────────────────────────────────
        var p1Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var p2Id = Guid.Parse("66666666-6666-6666-6666-666655555555");
        var p3Id = Guid.Parse("77777777-7777-7777-7777-777755555555");
        var p4Id = Guid.Parse("88888888-8888-8888-8888-888855555555");
        var p5Id = Guid.Parse("99999999-9999-9999-9999-999955555555");
        var p6Id = Guid.Parse("aaaaaaaa-5555-5555-5555-555555555555");
        var p7Id = Guid.Parse("bbbbbbbb-5555-5555-5555-555555555555");
        var p8Id = Guid.Parse("cccccccc-5555-5555-5555-555555555555");

        modelBuilder.Entity<Patient>().HasData(
            new Patient { Id = p1Id, Name = "Alice Martin", DateOfBirth = new DateTime(1955, 3, 10, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "ABC1234", Gender = "Female", Email = "alice.martin@example.com", PhoneNumber = "+64 21 555 0101" },
            new Patient { Id = p2Id, Name = "Bob Clarke", DateOfBirth = new DateTime(1970, 7, 22, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "DEF5678", Gender = "Male", Email = "bob.clarke@example.com", PhoneNumber = "+64 22 555 0102" },
            new Patient { Id = p3Id, Name = "Carol Ahmed", DateOfBirth = new DateTime(1940, 11, 5, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "GHI9012", Gender = "Female", Email = "carol.ahmed@example.com", PhoneNumber = "+64 27 555 0103" },
            new Patient { Id = p4Id, Name = "David Johnson", DateOfBirth = new DateTime(1985, 2, 14, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "JKL3456", Gender = "Male", Email = "david.johnson@example.com", PhoneNumber = "+64 29 555 0104" },
            new Patient { Id = p5Id, Name = "Eva Rodriguez", DateOfBirth = new DateTime(1932, 8, 30, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "MNO7890", Gender = "Female", Email = "eva.rodriguez@example.com", PhoneNumber = "+64 21 555 0105" },
            new Patient { Id = p6Id, Name = "Frank Lee", DateOfBirth = new DateTime(1960, 4, 18, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "PQR1234", Gender = "Male", Email = "frank.lee@example.com", PhoneNumber = "+64 22 555 0106" },
            new Patient { Id = p7Id, Name = "Grace Kim", DateOfBirth = new DateTime(1978, 12, 3, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "STU5678", Gender = "Female", Email = "grace.kim@example.com", PhoneNumber = "+64 27 555 0107" },
            new Patient { Id = p8Id, Name = "Henry Smith", DateOfBirth = new DateTime(1945, 6, 25, 0, 0, 0, DateTimeKind.Utc), NhiNumber = "VWX9012", Gender = "Male", Email = "henry.smith@example.com", PhoneNumber = "+64 29 555 0108" }
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
            new Referral { Id = Guid.NewGuid(), PatientId = p1Id, CreatedByUserId = gp1Id, AssignedToUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Cardiology",    Reason = "Chest pain",         Urgency = UrgencyLevel.Urgent,     Status = ReferralStatus.Received, ReceivedAt = now.AddDays(-5),  SlaDeadline = now.AddDays(-4), CreatedAt = now.AddDays(-5),  PriorityScore = 75.5, CaseNo = "Ref-000001" },
            new Referral { Id = Guid.NewGuid(), PatientId = p2Id, CreatedByUserId = gp1Id, AssignedToUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Orthopedics",   Reason = "Knee pain",          Urgency = UrgencyLevel.Routine,    Status = ReferralStatus.Triaged,  ReceivedAt = now.AddDays(-10), SlaDeadline = now.AddDays(20), CreatedAt = now.AddDays(-10), PriorityScore = 30.2, CaseNo = "Ref-000002" },
            new Referral { Id = Guid.NewGuid(), PatientId = p3Id, CreatedByUserId = gp2Id, AssignedToUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Neurology",     Reason = "Recurring headaches",Urgency = UrgencyLevel.Soon,       Status = ReferralStatus.Accepted, ReceivedAt = now.AddDays(-3),  SlaDeadline = now.AddDays(4),  CreatedAt = now.AddDays(-3),  PriorityScore = 55.0, CaseNo = "Ref-000003" },
            new Referral { Id = Guid.NewGuid(), PatientId = p4Id, CreatedByUserId = gp2Id, AssignedToUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Dermatology",   Reason = "Skin rash",          Urgency = UrgencyLevel.Routine,    Status = ReferralStatus.Booked,   ReceivedAt = now.AddDays(-15), SlaDeadline = now.AddDays(15), CreatedAt = now.AddDays(-15), PriorityScore = 22.8, CaseNo = "Ref-000004" },
            new Referral { Id = Guid.NewGuid(), PatientId = p5Id, CreatedByUserId = gp1Id, AssignedToUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Oncology",      Reason = "Mass detection",     Urgency = UrgencyLevel.Emergency,  Status = ReferralStatus.Received, ReceivedAt = now.AddDays(-1),  SlaDeadline = now.AddHours(-20),CreatedAt= now.AddDays(-1),  PriorityScore = 98.5, CaseNo = "Ref-000005" },
            new Referral { Id = Guid.NewGuid(), PatientId = p6Id, CreatedByUserId = gp2Id, AssignedToUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Gastroenterology",Reason = "Stomach issues",   Urgency = UrgencyLevel.Soon,       Status = ReferralStatus.Received, ReceivedAt = now.AddDays(-2),  SlaDeadline = now.AddDays(5),  CreatedAt = now.AddDays(-2),  PriorityScore = 48.3, CaseNo = "Ref-000006" },
            new Referral { Id = Guid.NewGuid(), PatientId = p7Id, CreatedByUserId = gp1Id, AssignedToUserId = gp1Id, ReferringGPId = gp1Id.ToString(), SpecialistType = "Ophthalmology", Reason = "Vision deterioration",Urgency = UrgencyLevel.Urgent,    Status = ReferralStatus.Declined, ReceivedAt = now.AddDays(-7),  SlaDeadline = now.AddDays(-6), CreatedAt = now.AddDays(-7),  PriorityScore = 66.1, CaseNo = "Ref-000007" },
            new Referral { Id = Guid.NewGuid(), PatientId = p8Id, CreatedByUserId = gp2Id, AssignedToUserId = gp2Id, ReferringGPId = gp2Id.ToString(), SpecialistType = "Pulmonology",   Reason = "Chronic cough",      Urgency = UrgencyLevel.Soon,       Status = ReferralStatus.Completed,ReceivedAt = now.AddDays(-20), SlaDeadline = now.AddDays(-13),CreatedAt= now.AddDays(-20), PriorityScore = 41.7, CaseNo = "Ref-000008" },
        };

        modelBuilder.Entity<Referral>().HasData(referrals);
    }
}
