using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Models;
using Npgsql;

namespace Bms.Store.Infrastructure.Stores;

/// <summary>
/// System 服务租户存储实现
/// Store 服务通过此类从 bms_system 数据库读取租户信息
/// </summary>
public class SystemTenantStore : ITenantStore
{
    private readonly string _connectionString;

    public SystemTenantStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<TenantInfo?> GetTenantByIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT ""Id"", ""Code"", ""Name"", ""Status"", ""IsolationLevel"", ""ConnectionString"",
                   ""SchemaName"", ""ExpireTime"", ""AllowedSubsystems"", ""Remark""
            FROM bms_system.""Tenants""
            WHERE ""Id"" = @id AND ""IsDeleted"" = false";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", tenantId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToTenantInfo(reader);
        }

        return null;
    }

    public async Task<TenantInfo?> GetTenantByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT ""Id"", ""Code"", ""Name"", ""Status"", ""IsolationLevel"", ""ConnectionString"",
                   ""SchemaName"", ""ExpireTime"", ""AllowedSubsystems"", ""Remark""
            FROM bms_system.""Tenants""
            WHERE ""Code"" = @code AND ""IsDeleted"" = false";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("code", tenantCode);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapToTenantInfo(reader);
        }

        return null;
    }

    public async Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = new List<TenantInfo>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT ""Id"", ""Code"", ""Name"", ""Status"", ""IsolationLevel"", ""ConnectionString"",
                   ""SchemaName"", ""ExpireTime"", ""AllowedSubsystems"", ""Remark""
            FROM bms_system.""Tenants""
            WHERE ""IsDeleted"" = false";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tenants.Add(MapToTenantInfo(reader));
        }

        return tenants;
    }

    public Task<bool> UpdateTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        // Store 服务只读租户信息，不负责更新
        return Task.FromResult(false);
    }

    public Task<long> AddTenantAsync(TenantInfo tenantInfo, CancellationToken cancellationToken = default)
    {
        // Store 服务只读租户信息，不负责添加
        return Task.FromResult(0L);
    }

    public Task<bool> DeleteTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        // Store 服务只读租户信息，不负责删除
        return Task.FromResult(false);
    }

    public Task<int> BatchDeleteAsync(IEnumerable<long> tenantIds, CancellationToken cancellationToken = default)
    {
        // Store 服务只读租户信息，不负责删除
        return Task.FromResult(0);
    }

    private static TenantInfo MapToTenantInfo(NpgsqlDataReader reader)
    {
        return new TenantInfo
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            Code = reader.GetString(reader.GetOrdinal("Code")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Status = reader.GetInt32(reader.GetOrdinal("Status")),
            IsolationLevel = (TenantIsolationLevel)reader.GetInt32(reader.GetOrdinal("IsolationLevel")),
            ConnectionString = reader.IsDBNull(reader.GetOrdinal("ConnectionString"))
                ? null
                : reader.GetString(reader.GetOrdinal("ConnectionString")),
            SchemaName = reader.IsDBNull(reader.GetOrdinal("SchemaName"))
                ? null
                : reader.GetString(reader.GetOrdinal("SchemaName")),
            ExpireTime = reader.IsDBNull(reader.GetOrdinal("ExpireTime"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ExpireTime")),
            AllowedSubsystems = reader.IsDBNull(reader.GetOrdinal("AllowedSubsystems"))
                ? null
                : reader.GetString(reader.GetOrdinal("AllowedSubsystems")),
            Remark = reader.IsDBNull(reader.GetOrdinal("Remark"))
                ? null
                : reader.GetString(reader.GetOrdinal("Remark")),
            IsEnabled = reader.GetInt32(reader.GetOrdinal("Status")) == 1
        };
    }
}
