using System.Text.Json;
using System.Text.Json.Serialization;
using Bms.BuildingBlocks.MultiTenant.Abstractions;
using Bms.BuildingBlocks.MultiTenant.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Bms.Gateway.Api.Stores;

/// <summary>
/// API租户存储实现（通过 HTTP 调用 System.Api 内部接口查询租户，带内存缓存）
/// </summary>
public class ApiTenantStore : ITenantStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    private const string HttpClientName = "SystemApi";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public ApiTenantStore(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<TenantInfo?> GetTenantByIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant:id:{tenantId}";
        if (_cache.TryGetValue(cacheKey, out TenantInfo? cached))
        {
            return cached;
        }

        var tenant = await FetchTenantAsync($"api/internal/tenants/{tenantId}", cancellationToken);
        if (tenant != null)
        {
            _cache.Set(cacheKey, tenant, CacheDuration);
        }
        return tenant;
    }

    /// <inheritdoc />
    public async Task<TenantInfo?> GetTenantByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tenant:code:{tenantCode}";
        if (_cache.TryGetValue(cacheKey, out TenantInfo? cached))
        {
            return cached;
        }

        var tenant = await FetchTenantAsync($"api/internal/tenants/code/{Uri.EscapeDataString(tenantCode)}", cancellationToken);
        if (tenant != null)
        {
            _cache.Set(cacheKey, tenant, CacheDuration);
        }
        return tenant;
    }

    private async Task<TenantInfo?> FetchTenantAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<TenantApiResponse>(json, JsonOpts);
            if (apiResponse?.Data == null || apiResponse.Code != 200)
            {
                return null;
            }

            return MapToTenantInfo(apiResponse.Data);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var serviceKey = _configuration["InternalServices:ServiceKey"] ?? string.Empty;
        client.DefaultRequestHeaders.Remove("X-Internal-Service");
        client.DefaultRequestHeaders.Remove("X-Internal-Service-Key");
        client.DefaultRequestHeaders.Add("X-Internal-Service", "Gateway");
        client.DefaultRequestHeaders.Add("X-Internal-Service-Key", serviceKey);
        return client;
    }

    private static TenantInfo MapToTenantInfo(TenantDataDto dto)
    {
        return new TenantInfo
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Status = dto.Status,
            IsEnabled = dto.IsEnabled,
            ExpireTime = dto.ExpireTime,
            AllowedSubsystems = dto.AllowedSubsystems
        };
    }

    // 以下方法网关不需要，抛出 NotSupportedException
    public Task<IEnumerable<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("网关不支持获取所有租户");

    public Task<bool> UpdateTenantAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("网关不支持更新租户");

    public Task<long> AddTenantAsync(TenantInfo tenant, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("网关不支持添加租户");

    public Task<bool> DeleteTenantAsync(long tenantId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("网关不支持删除租户");

    public Task<int> BatchDeleteAsync(IEnumerable<long> tenantIds, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("网关不支持批量删除租户");
}

/// <summary>
/// System.Api 响应包装（匹配 ApiResponseDto&lt;T&gt; 的 JSON 格式）
/// </summary>
internal class TenantApiResponse
{
    public int Code { get; set; }
    public string? Message { get; set; }
    public TenantDataDto? Data { get; set; }
}

/// <summary>
/// 租户数据 DTO（匹配 TenantDto 的 JSON 格式，只包含网关需要的字段）
/// </summary>
internal class TenantDataDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? ExpireTime { get; set; }
    public string? AllowedSubsystems { get; set; }
}
