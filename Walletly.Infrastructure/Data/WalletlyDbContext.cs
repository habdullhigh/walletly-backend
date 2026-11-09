using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Walletly.Domain.Entities;
using Walletly.Domain.Enums;
using Walletly.Domain.Interfaces;

namespace Walletly.Infrastructure.Data;

public class WalletlyDbContext : DbContext, IUnitOfWork
{
    public WalletlyDbContext(DbContextOptions<WalletlyDbContext> options) : base(options)
    {
    }

    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PlatformUserRole> PlatformUserRoles => Set<PlatformUserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantEnvironment> TenantEnvironments => Set<TenantEnvironment>();
    public DbSet<TenantAllowedUrl> TenantAllowedUrls => Set<TenantAllowedUrl>();
    public DbSet<TenantApiKey> TenantApiKeys => Set<TenantApiKey>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<TenantAccountOwner> TenantAccountOwners => Set<TenantAccountOwner>();
    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();
    public DbSet<TenantRolePermission> TenantRolePermissions => Set<TenantRolePermission>();
    public DbSet<TemporaryTenant> TemporaryTenants => Set<TemporaryTenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PlatformUser mapping
        modelBuilder.Entity<PlatformUser>(entity =>
        {
            entity.ToTable("platform_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Ignore(e => e.FullName);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.LastLoginAt);
        });

        // Role mapping
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
        });

        // Permission mapping
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Join tables reference their parents by FK id only (no navigations, per CLAUDE.md)
        modelBuilder.Entity<PlatformUserRole>(entity =>
        {
            entity.ToTable("platform_user_roles");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne<PlatformUser>()
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureTenancy(modelBuilder);
        ConfigureOnboarding(modelBuilder);

        SeedData(modelBuilder);
    }

    private static void ConfigureTenancy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            // The database default backfills existing tenants as Active. Only the unset CLR value (0) uses it.
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(TenantStatus.Active).HasSentinel(default(TenantStatus));
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt);
        });

        modelBuilder.Entity<TenantEnvironment>(entity =>
        {
            entity.ToTable("tenant_environments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Mode).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.ProtectedWebhookSecret).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.Mode }).IsUnique();

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantAllowedUrl>(entity =>
        {
            entity.ToTable("tenant_allowed_urls");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Kind).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2048);
            entity.HasIndex(e => new { e.TenantEnvironmentId, e.Kind, e.Url }).IsUnique();

            entity.HasOne<TenantEnvironment>()
                .WithMany()
                .HasForeignKey(e => e.TenantEnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantApiKey>(entity =>
        {
            entity.ToTable("tenant_api_keys");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeyId).IsRequired().HasMaxLength(32);
            entity.HasIndex(e => e.KeyId).IsUnique();
            entity.Property(e => e.SecretHash).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.RevokedAt);
            entity.Ignore(e => e.IsActive);

            entity.HasOne<TenantEnvironment>()
                .WithMany()
                .HasForeignKey(e => e.TenantEnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantAccountOwner>(entity =>
        {
            entity.ToTable("tenant_account_owners");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(e => e.FullName);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.LastLoginAt);

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantRole>(entity =>
        {
            entity.ToTable("tenant_roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt);
            entity.HasIndex(e => new { e.TenantEnvironmentId, e.Code }).IsUnique();

            entity.HasOne<TenantEnvironment>()
                .WithMany()
                .HasForeignKey(e => e.TenantEnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Permissions)
                .WithOne()
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantRolePermission>(entity =>
        {
            entity.ToTable("tenant_role_permissions");
            entity.HasKey(e => new { e.RoleId, e.Permission });
            entity.Property(e => e.Permission).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<TenantUser>(entity =>
        {
            entity.ToTable("tenant_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ExternalReference).HasMaxLength(100);
            entity.Ignore(e => e.FullName);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt);

            entity.HasIndex(e => new { e.TenantEnvironmentId, e.Email }).IsUnique();
            entity.HasIndex(e => new { e.TenantEnvironmentId, e.ExternalReference });

            entity.HasOne<TenantEnvironment>()
                .WithMany()
                .HasForeignKey(e => e.TenantEnvironmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<TenantRole>()
                .WithMany()
                .HasForeignKey(e => e.TenantRoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureOnboarding(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TemporaryTenant>(entity =>
        {
            entity.ToTable("temporary_tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TenantName).HasColumnName("tenant_name").IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            entity.Ignore(e => e.IsOpen);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasColumnType("text");
            entity.Property(e => e.Source).HasColumnName("source").HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(30);
            entity.Property(e => e.OtpCode).HasColumnName("otp_code").HasMaxLength(10);
            entity.Property(e => e.OtpExpiresAt).HasColumnName("otp_expires_at").HasColumnType("timestamptz");
            entity.Property(e => e.OtpAttemptsCount).HasColumnName("otp_attempts_count").HasDefaultValue(0);
            entity.Property(e => e.InvitationTokenHash).HasColumnName("invitation_token").HasMaxLength(128);
            entity.HasIndex(e => e.InvitationTokenHash);
            entity.Property(e => e.InvitationExpiresAt).HasColumnName("invitation_expires_at").HasColumnType("timestamptz");
            entity.Property(e => e.AdminNotes).HasColumnName("admin_notes").HasColumnType("text");
            entity.Property(e => e.ExpectedMonthlyVolume).HasColumnName("expected_monthly_volume").HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.UseCases)
                .HasColumnName("use_cases")
                .HasColumnType("text[]")
                .HasDefaultValueSql("'{}'")
                .IsRequired()
                .HasConversion(
                    useCases => useCases.Select(useCase => useCase.ToString()).ToArray(),
                    values => values.Select(value => Enum.Parse<TenantUseCase>(value)).ToList())
                .Metadata.SetValueComparer(new ValueComparer<List<TenantUseCase>>(
                    (left, right) => left!.SequenceEqual(right!),
                    value => value.Aggregate(0, (hash, useCase) => HashCode.Combine(hash, useCase)),
                    value => value.ToList()));
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at").HasColumnType("timestamptz");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var permTenantReadId = new Guid("11111111-1111-1111-1111-111111111111");
        var permTenantWriteId = new Guid("22222222-2222-2222-2222-222222222222");
        var permWalletReadId = new Guid("33333333-3333-3333-3333-333333333333");
        var permWalletWriteId = new Guid("44444444-4444-4444-4444-444444444444");
        var permUsersManageId = new Guid("55555555-5555-5555-5555-555555555555");
        var permRolesManageId = new Guid("66666666-6666-6666-6666-666666666666");

        // Seeds use anonymous objects because the entities have private constructors and setters.
        modelBuilder.Entity<Permission>().HasData(
            new { Id = permTenantReadId, Code = "tenants.read" },
            new { Id = permTenantWriteId, Code = "tenants.write" },
            new { Id = permWalletReadId, Code = "wallets.read" },
            new { Id = permWalletWriteId, Code = "wallets.write" },
            new { Id = permUsersManageId, Code = "platform_users.manage" },
            new { Id = permRolesManageId, Code = "roles.manage" }
        );

        var adminRoleId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var devRoleId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var supportRoleId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");

        modelBuilder.Entity<Role>().HasData(
            new { Id = adminRoleId, Code = "admin", Name = "Admin" },
            new { Id = devRoleId, Code = "developer", Name = "Developer" },
            new { Id = supportRoleId, Code = "support", Name = "Support" }
        );

        modelBuilder.Entity<RolePermission>().HasData(
            // Admin permissions
            new RolePermission(adminRoleId, permTenantReadId),
            new RolePermission(adminRoleId, permTenantWriteId),
            new RolePermission(adminRoleId, permWalletReadId),
            new RolePermission(adminRoleId, permWalletWriteId),
            new RolePermission(adminRoleId, permUsersManageId),
            new RolePermission(adminRoleId, permRolesManageId),

            // Developer permissions
            new RolePermission(devRoleId, permTenantReadId),
            new RolePermission(devRoleId, permWalletReadId),

            // Support permissions
            new RolePermission(supportRoleId, permTenantReadId)
        );
    }
}
