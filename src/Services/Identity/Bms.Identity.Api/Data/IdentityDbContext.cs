using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace Bms.Identity.Api.Data;

/// <summary>
/// 认证服务数据库上下文（仅包含OpenIddict令牌表）
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    // OpenIddict 实体
    public DbSet<OpenIddictEntityFrameworkCoreApplication> Applications { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization> Authorizations { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreScope> Scopes { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreToken> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 配置 OpenIddict 表名
        builder.Entity<OpenIddictEntityFrameworkCoreApplication>(b =>
        {
            b.ToTable("oidc_applications");
        });

        builder.Entity<OpenIddictEntityFrameworkCoreAuthorization>(b =>
        {
            b.ToTable("oidc_authorizations");
        });

        builder.Entity<OpenIddictEntityFrameworkCoreScope>(b =>
        {
            b.ToTable("oidc_scopes");
        });

        builder.Entity<OpenIddictEntityFrameworkCoreToken>(b =>
        {
            b.ToTable("oidc_tokens");
        });
    }
}
