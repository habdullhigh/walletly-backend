using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
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

        SeedData(modelBuilder);
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
