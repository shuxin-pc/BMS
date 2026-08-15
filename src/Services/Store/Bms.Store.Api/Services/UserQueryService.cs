using Bms.Store.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bms.Store.Api.Services;

/// <summary>
/// 跨服务用户查询实现
/// 通过 Npgsql 直查 bms_system."Users" 表（与 UserStatusCheckMiddleware/SystemTenantStore 一致），
/// 不引入 SystemDbContext，保持 Store 服务边界清晰。
/// 实现放在 Api 项目（Application 引用 Infrastructure，反向引用会形成循环依赖）。
/// </summary>
public class UserQueryService : IUserQueryService
{
    private readonly string _connectionString;
    private readonly ILogger<UserQueryService> _logger;

    public UserQueryService(IConfiguration configuration, ILogger<UserQueryService> logger)
    {
        _connectionString = configuration.GetConnectionString("SystemDb")
            ?? throw new InvalidOperationException("SystemDb 连接字符串未配置");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<TenantUserDto>> GetActiveUsersByTenantAsync(long tenantId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT ""Id"", ""UserName"", ""RealName""
                FROM bms_system.""Users""
                WHERE ""TenantId"" = @tenantId
                  AND ""Status"" = 1
                  AND ""IsDeleted"" = false
                ORDER BY ""Id""";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenantId", tenantId);

            var users = new List<TenantUserDto>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new TenantUserDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    RealName = reader.IsDBNull(reader.GetOrdinal("RealName"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("RealName"))
                });
            }

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询租户有效用户失败，TenantId={TenantId}", tenantId);
            // 查询失败返回空列表，由调用方处理"无可用用户"场景
            return new List<TenantUserDto>();
        }
    }

    /// <inheritdoc />
    public async Task<List<TenantUserDto>> GetTenantAdminUsersAsync(long tenantId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 联查 Users + UserRoles + Roles，筛选本租户下 tenant_admin 角色的有效用户
            const string sql = @"
                SELECT u.""Id"", u.""UserName"", u.""RealName""
                FROM bms_system.""Users"" u
                INNER JOIN bms_system.""UserRoles"" ur ON ur.""UserId"" = u.""Id""
                INNER JOIN bms_system.""Roles"" r ON r.""Id"" = ur.""RoleId""
                WHERE u.""TenantId"" = @tenantId
                  AND u.""Status"" = 1
                  AND u.""IsDeleted"" = false
                  AND r.""Code"" = 'tenant_admin'
                  AND r.""IsDeleted"" = false
                ORDER BY u.""Id""";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tenantId", tenantId);

            var users = new List<TenantUserDto>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new TenantUserDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    RealName = reader.IsDBNull(reader.GetOrdinal("RealName"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("RealName"))
                });
            }

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询租户 tenant_admin 用户失败，TenantId={TenantId}", tenantId);
            return new List<TenantUserDto>();
        }
    }
}
