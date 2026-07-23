using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Menus;
using Bms.Store.Application.Dtos.Subsystems;
using Bms.Store.Application.SeedData;

namespace Bms.Store.Application.Services;

/// <summary>
/// 门店管理菜单注册服务
/// 在应用启动时向 System 服务注册门店管理系统的菜单
/// </summary>
public class StoreMenuRegistrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StoreMenuRegistrationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _systemApiBaseUrl;
    private readonly string _internalServiceName = "StoreService";
    private readonly string _internalServiceKey = "StoreService-Internal-Key-2026";

    public StoreMenuRegistrationService(
        IServiceProvider serviceProvider,
        ILogger<StoreMenuRegistrationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _systemApiBaseUrl = "http://localhost:5000";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始注册门店管理系统菜单...");

        try
        {
            // 使用 scoped service 获取配置
            using var scope = _serviceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var systemApiUrl = configuration["SystemApi:BaseUrl"] ?? _systemApiBaseUrl;
            var internalServiceKey = configuration["InternalServices:ServiceKey"] ?? _internalServiceKey;
            var internalServiceName = configuration["InternalServices:ServiceName"] ?? _internalServiceName;

            var httpClient = _httpClientFactory.CreateClient("SystemApi");
            httpClient.BaseAddress = new Uri(systemApiUrl);

            // 添加内部服务调用 header
            httpClient.DefaultRequestHeaders.Add("X-Internal-Service", internalServiceName);
            httpClient.DefaultRequestHeaders.Add("X-Internal-Service-Key", internalServiceKey);

            List<long> menuIds;

            // 先检查子系统是否已存在
            var existingSubsystemId = await GetExistingSubsystemIdAsync(httpClient, cancellationToken);
            if (existingSubsystemId.HasValue)
            {
                _logger.LogInformation("门店管理子系统已存在，ID: {SubsystemId}", existingSubsystemId.Value);
                // 获取子系统已有的菜单ID
                menuIds = await GetSubsystemMenuIdsAsync(httpClient, existingSubsystemId.Value, cancellationToken);
            }
            else
            {
                // 获取菜单定义
                var menuDefinitions = StoreMenuSeedData.GetMenus();

                // 创建子系统
                var subsystemId = await CreateSubsystemAsync(httpClient, cancellationToken);
                if (subsystemId == null)
                {
                    _logger.LogError("创建门店管理子系统失败");
                    return;
                }

                _logger.LogInformation("门店管理子系统创建成功，ID: {SubsystemId}", subsystemId);

                // 创建菜单
                var menuIdMap = await CreateMenusAsync(httpClient, menuDefinitions, subsystemId.Value, cancellationToken);

                // 分配菜单给子系统
                await AssignMenusToSubsystemAsync(httpClient, subsystemId.Value, menuIdMap.Values.ToList(), cancellationToken);

                // 将子系统分配给平台租户
                await AssignSubsystemToTenantAsync(httpClient, subsystemId.Value, cancellationToken);

                menuIds = menuIdMap.Values.ToList();
            }

            // 将所有菜单授权给超级管理员
            await AssignMenusToSuperAdminAsync(httpClient, menuIds, cancellationToken);

            _logger.LogInformation("门店管理系统菜单注册完成。");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册门店管理系统菜单时发生错误");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 查找已存在的门店管理子系统，返回其ID（不存在则返回null）
    /// </summary>
    private async Task<long?> GetExistingSubsystemIdAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync("/api/system/subsystems/list-all", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<List<SubsystemDto>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });

            var subsystem = apiResponse?.Data?.FirstOrDefault(s => s.Code == StoreMenuSeedData.SubsystemCode);
            return subsystem?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查子系统是否存在时发生错误");
        }

        return null;
    }

    /// <summary>
    /// 创建子系统
    /// </summary>
    private async Task<long?> CreateSubsystemAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        try
        {
            var createDto = new SubsystemCreateDto
            {
                Code = StoreMenuSeedData.SubsystemCode,
                Name = StoreMenuSeedData.SubsystemName,
                Icon = "Shop",
                Description = "门店管理系统，包含门店档案、商品管理、收银、客户、会员、预约、疗程卡等功能",
                Sort = 99,
                Status = 1
            };

            var json = JsonSerializer.Serialize(createDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/system/subsystems", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<SubsystemDto>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                });

                return apiResponse?.Data?.Id;
            }
            else
            {
                _logger.LogWarning("创建子系统响应: {Response}", responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建子系统时发生错误");
        }

        return null;
    }

    /// <summary>
    /// 创建菜单
    /// </summary>
    private async Task<Dictionary<string, long>> CreateMenusAsync(
        HttpClient httpClient,
        List<MenuDefinition> menuDefinitions,
        long subsystemId,
        CancellationToken cancellationToken)
    {
        var menuIdMap = new Dictionary<string, long>();

        foreach (var menuDef in menuDefinitions)
        {
            try
            {
                var createDto = new MenuCreateDto
                {
                    Name = menuDef.Name,
                    Code = menuDef.Code,
                    Path = menuDef.Path,
                    Component = menuDef.Component,
                    Icon = menuDef.Icon,
                    Sort = menuDef.Sort,
                    Type = menuDef.Type,
                    Status = 1,
                    IsVisible = menuDef.IsVisible,
                    IsCache = menuDef.IsCache,
                    IsAlwaysShow = menuDef.IsAlwaysShow,
                    // type=2 按钮的权限码与 Code 统一，确保后端正确收集按钮权限
                    PermissionCode = menuDef.Type == 2 ? menuDef.Code : null
                };

                // 设置父菜单ID
                if (!string.IsNullOrEmpty(menuDef.ParentCode) && menuIdMap.TryGetValue(menuDef.ParentCode, out var parentId))
                {
                    createDto.ParentId = parentId;
                }

                var json = JsonSerializer.Serialize(createDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/api/system/menus", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<MenuDto>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
                    });

                    if (apiResponse?.Data != null)
                    {
                        menuIdMap[menuDef.Code] = apiResponse.Data.Id;
                        _logger.LogDebug("菜单创建成功: {MenuName}, ID: {MenuId}", menuDef.Name, apiResponse.Data.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("创建菜单失败: {MenuName}, 响应: {Response}", menuDef.Name, responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建菜单时发生错误: {MenuName}", menuDef.Name);
            }
        }

        return menuIdMap;
    }

    /// <summary>
    /// 分配菜单给子系统
    /// </summary>
    private async Task AssignMenusToSubsystemAsync(
        HttpClient httpClient,
        long subsystemId,
        List<long> menuIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = new { MenuIds = menuIds };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"/api/system/subsystems/{subsystemId}/menus", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("菜单分配给子系统成功，子系统ID: {SubsystemId}", subsystemId);
            }
            else
            {
                _logger.LogWarning("分配菜单响应: {Response}", responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配菜单给子系统时发生错误");
        }
    }

    /// <summary>
    /// 将子系统分配给平台租户
    /// </summary>
    private async Task AssignSubsystemToTenantAsync(
        HttpClient httpClient,
        long subsystemId,
        CancellationToken cancellationToken)
    {
        try
        {
            // 平台租户ID为1
            var dto = new { TenantId = 1L, SubsystemId = subsystemId };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/system/tenant-subsystems", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("子系统分配给平台租户成功");
            }
            else
            {
                _logger.LogWarning("分配子系统给租户响应: {Response}", responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配子系统给租户时发生错误");
        }
    }

    /// <summary>
    /// 获取子系统关联的菜单ID列表
    /// </summary>
    private async Task<List<long>> GetSubsystemMenuIdsAsync(
        HttpClient httpClient,
        long subsystemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/system/subsystems/{subsystemId}/menus", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取子系统菜单列表失败，状态码: {StatusCode}", response.StatusCode);
                return new List<long>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<List<long>>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });

            return apiResponse?.Data ?? new List<long>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取子系统菜单列表时发生错误");
            return new List<long>();
        }
    }

    /// <summary>
    /// 将菜单授权给超级管理员角色（合并已有权限，不覆盖）
    /// </summary>
    private async Task AssignMenusToSuperAdminAsync(
        HttpClient httpClient,
        List<long> menuIds,
        CancellationToken cancellationToken)
    {
        try
        {
            // 获取超级管理员角色ID
            var superAdminRoleId = await GetSuperAdminRoleIdAsync(httpClient, cancellationToken);
            if (superAdminRoleId == null)
            {
                _logger.LogWarning("未找到超级管理员角色，跳过菜单授权");
                return;
            }

            // 获取超级管理员已有的菜单权限
            var existingMenuIds = await GetRoleMenuAuthsAsync(httpClient, superAdminRoleId.Value, cancellationToken);

            // 合并菜单ID（去重）
            var mergedMenuIds = existingMenuIds.Union(menuIds).ToList();

            // 如果没有新增的菜单，跳过更新
            if (mergedMenuIds.Count == existingMenuIds.Count)
            {
                _logger.LogInformation("超级管理员已拥有所有门店管理菜单权限，无需更新");
                return;
            }

            // 更新超级管理员的菜单权限（覆盖式，需传入完整列表）
            var dto = new { MenuIds = mergedMenuIds };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"/api/system/roles/{superAdminRoleId.Value}/menus/auth", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("门店管理菜单授权给超级管理员成功，新增 {Count} 个菜单", mergedMenuIds.Count - existingMenuIds.Count);
            }
            else
            {
                _logger.LogWarning("授权菜单给超级管理员响应: {Response}", responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "授权菜单给超级管理员时发生错误");
        }
    }

    /// <summary>
    /// 获取超级管理员角色ID
    /// </summary>
    private async Task<long?> GetSuperAdminRoleIdAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("/api/system/roles/all-without-filter", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("获取角色列表失败，状态码: {StatusCode}", response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<List<RoleListItemDto>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        });

        var superAdminRole = apiResponse?.Data?.FirstOrDefault(r => r.Code == "super_admin");
        return superAdminRole?.Id;
    }

    /// <summary>
    /// 获取角色已授权的菜单ID列表
    /// </summary>
    private async Task<List<long>> GetRoleMenuAuthsAsync(
        HttpClient httpClient,
        long roleId,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/api/system/roles/{roleId}/menus/auth", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("获取角色菜单权限失败，状态码: {StatusCode}", response.StatusCode);
            return new List<long>();
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<ApiResponseDto<List<long>>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        });

        return apiResponse?.Data ?? new List<long>();
    }
}

/// <summary>
/// 角色列表项DTO（仅用于反序列化System服务返回的角色信息）
/// </summary>
internal class RoleListItemDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}