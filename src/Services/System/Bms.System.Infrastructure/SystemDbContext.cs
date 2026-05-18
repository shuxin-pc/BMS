using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.MultiTenant.Data;
using Bms.System.Domain.Entities;

namespace Bms.System.Infrastructure;

public class SystemDbContext : TenantDbContext
{
    public SystemDbContext(
        DbContextOptions<SystemDbContext> options)
        : base(options)
    {
    }

    // DbSet
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<DataPermission> DataPermissions => Set<DataPermission>();
    public DbSet<Subsystem> Subsystems => Set<Subsystem>();
    public DbSet<SubsystemMenu> SubsystemMenus => Set<SubsystemMenu>();
    public DbSet<TenantSubsystem> TenantSubsystems => Set<TenantSubsystem>();
    public DbSet<RoleMenuAuth> RoleMenuAuths => Set<RoleMenuAuth>();

    /// <summary>
    /// 租户表（不使用多租户隔离，存储所有租户信息）
    /// </summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure multi-tenant schema
        modelBuilder.HasDefaultSchema("bms_system");

        // Configure entities
        ConfigureUser(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureMenu(modelBuilder);
        ConfigurePermission(modelBuilder);
        ConfigureUserRole(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureOrganization(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureSystemConfig(modelBuilder);
        ConfigureDataPermission(modelBuilder);
        ConfigureTenant(modelBuilder);
        ConfigureSubsystem(modelBuilder);
        ConfigureSubsystemMenu(modelBuilder);
        ConfigureTenantSubsystem(modelBuilder);
        ConfigureRoleMenuAuth(modelBuilder);
    }

    private void ConfigureTenant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ConnectionString).HasMaxLength(1000);
            entity.Property(e => e.SchemaName).HasMaxLength(50);
            entity.Property(e => e.AllowedSubsystems).HasMaxLength(500);
            entity.Property(e => e.ContactName).HasMaxLength(50);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.ContactEmail).HasMaxLength(100);
            entity.Property(e => e.Remark).HasMaxLength(500);

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Status);
        });
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RealName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Avatar).HasColumnType("text");
            entity.Property(e => e.LastLoginIp).HasMaxLength(50);

            entity.HasIndex(e => e.UserName);
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Phone);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.Code);

            entity.HasOne(e => e.DataPermission)
                .WithOne(d => d.Role)
                .HasForeignKey<DataPermission>(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureMenu(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParentId).IsRequired(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Path).HasMaxLength(200);
            entity.Property(e => e.Component).HasMaxLength(200);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.PermissionCode).HasMaxLength(100);

            entity.HasIndex(e => e.Code);

            entity.HasOne(e => e.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.HttpMethod).HasMaxLength(10);
            entity.Property(e => e.ApiPath).HasMaxLength(500);

            entity.HasIndex(e => e.Code);

            entity.HasOne(e => e.Menu)
                .WithMany(m => m.Permissions)
                .HasForeignKey(e => e.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureUserRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParentId).IsRequired(false);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);

            entity.HasIndex(e => e.Code);

            entity.HasOne(e => e.Parent)
                .WithMany(o => o.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(50);
            entity.Property(e => e.RealName).HasMaxLength(50);
            entity.Property(e => e.RequestPath).HasMaxLength(500);
            entity.Property(e => e.RequestMethod).HasMaxLength(10);
            entity.Property(e => e.RequestIp).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.EntityChanges).HasColumnType("text");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OperationType);
            entity.HasIndex(e => e.CreatedTime);
        });
    }

    private void ConfigureSystemConfig(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ConfigValue).HasColumnType("text");
            entity.Property(e => e.ConfigGroup).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.ConfigKey);
            entity.HasIndex(e => e.ConfigGroup);

            // SystemConfig 不使用软删除
            entity.Ignore(e => e.IsDeleted);
        });
    }

    private void ConfigureDataPermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomOrganizationIds).HasMaxLength(1000);
            // DataPermission 不使用软删除
            entity.Ignore(e => e.IsDeleted);
        });
    }

    private void ConfigureSubsystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subsystem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(70000);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Sort).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Status);
        });
    }

    private void ConfigureSubsystemMenu(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubsystemMenu>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubsystemId).IsRequired();
            entity.Property(e => e.MenuId).IsRequired();

            entity.HasIndex(e => new { e.SubsystemId, e.MenuId }).IsUnique();

            entity.HasOne(e => e.Subsystem)
                .WithMany(s => s.SubsystemMenus)
                .HasForeignKey(e => e.SubsystemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Menu)
                .WithMany()
                .HasForeignKey(e => e.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureTenantSubsystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantSubsystem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.SubsystemId).IsRequired();

            entity.HasIndex(e => new { e.TenantId, e.SubsystemId }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.TenantSubsystems)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subsystem)
                .WithMany(s => s.TenantSubsystems)
                .HasForeignKey(e => e.SubsystemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureRoleMenuAuth(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleMenuAuth>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleId).IsRequired();
            entity.Property(e => e.MenuId).IsRequired();
            entity.Property(e => e.SubsystemId).IsRequired();

            entity.HasIndex(e => new { e.RoleId, e.MenuId }).IsUnique();
            entity.HasIndex(e => new { e.RoleId, e.SubsystemId });

            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Menu)
                .WithMany()
                .HasForeignKey(e => e.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subsystem)
                .WithMany(s => s.RoleMenuAuths)
                .HasForeignKey(e => e.SubsystemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}