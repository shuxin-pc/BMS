using Bms.System.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bms.Store.Infrastructure.Stores;

/// <summary>
/// 审计日志 DbContext
/// 连接 bms_system 库，仅供 Store 服务写入审计日志（AuditLogs 表为 System 服务统一存储，
/// 复用 Bms.System.Domain 的 AuditLog 实体映射，与 SystemTenantStore 同为跨库访问 System 数据的组件）
/// </summary>
public class AuditLogDbContext : DbContext
{
    public AuditLogDbContext(DbContextOptions<AuditLogDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 审计日志集合
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // AuditLogs 表位于 bms_system schema，Id 为 identity 列（由数据库生成）
        modelBuilder.HasDefaultSchema("bms_system");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
        });
    }
}
