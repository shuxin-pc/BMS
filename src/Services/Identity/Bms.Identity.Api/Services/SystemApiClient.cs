using System.Text;
using System.Text.Json;
using Bms.Identity.Api.Models;

namespace Bms.Identity.Api.Services;

/// <summary>
/// System.Api 客户端实现
/// </summary>
public class SystemApiClient : ISystemApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SystemApiClient> _logger;

    public SystemApiClient(HttpClient httpClient, ILogger<SystemApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ValidateUserResponse> ValidateUserAsync(string userName, string password)
    {
        try
        {
            _logger.LogInformation("SystemApiClient.ValidateUserAsync 被调用，用户名：{UserName}", userName);
            _logger.LogInformation("SystemApiClient BaseAddress：{BaseAddress}", _httpClient.BaseAddress);

            var request = new ValidateUserRequest
            {
                UserName = userName,
                Password = password
            };

            var json = JsonSerializer.Serialize(request);
            _logger.LogInformation("请求JSON：{Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var requestUri = "api/internal/auth/validate";
            var fullUrl = $"{_httpClient.BaseAddress}{requestUri}";
            _logger.LogInformation("正在请求 URL：{FullUrl}", fullUrl);

            var response = await _httpClient.PostAsync(requestUri, content);

            _logger.LogInformation("收到响应，状态码：{StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("出现错误：验证用户失败，状态码：{StatusCode}", response.StatusCode);
                return new ValidateUserResponse
                {
                    IsValid = false,
                    ErrorMessage = $"验证请求失败: {response.StatusCode}"
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<ValidateUserResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // 允许将字符串解析为数字（处理 System API 返回的字符串类型的 long）
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            });


            return result ?? new ValidateUserResponse
            {
                IsValid = false,
                ErrorMessage = "响应解析失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出现错误：验证用户时发生异常");
            return new ValidateUserResponse
            {
                IsValid = false,
                ErrorMessage = $"验证异常: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 刷新令牌时获取用户最新状态
    /// </summary>
    public async Task<ValidateUserResponse> GetUserForRefreshAsync(long userId)
    {
        try
        {
            _logger.LogInformation("SystemApiClient.GetUserForRefreshAsync 被调用，用户ID：{UserId}", userId);

            var request = new RefreshUserInfoRequest { UserId = userId };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var requestUri = "api/internal/auth/refresh-user-info";

            var response = await _httpClient.PostAsync(requestUri, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("出现错误：刷新令牌获取用户信息失败，状态码：{StatusCode}", response.StatusCode);
                return new ValidateUserResponse
                {
                    IsValid = false,
                    ErrorMessage = $"获取用户信息失败: {response.StatusCode}"
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ValidateUserResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
            });

            return result ?? new ValidateUserResponse
            {
                IsValid = false,
                ErrorMessage = "响应解析失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出现错误：刷新令牌获取用户信息时发生异常");
            return new ValidateUserResponse
            {
                IsValid = false,
                ErrorMessage = $"获取用户信息异常: {ex.Message}"
            };
        }
    }

    public async Task RecordLoginAuditAsync(LoginAuditRequest request)
    {
        try
        {
            _logger.LogInformation("SystemApiClient.RecordLoginAuditAsync 被调用，操作类型：{OperationType}", request.OperationType);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var requestUri = "api/internal/auth/audit-login";

            var response = await _httpClient.PostAsync(requestUri, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("出现错误：记录登录审计日志失败，状态码：{StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogInformation("登录审计日志记录成功");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出现错误：记录登录审计日志时发生异常");
        }
    }

    public async Task UpdateLastLoginAsync(long userId, DateTime lastLoginTime, string? lastLoginIp)
    {
        try
        {
            _logger.LogInformation("SystemApiClient.UpdateLastLoginAsync 被调用，用户ID：{UserId}", userId);

            var request = new
            {
                UserId = userId,
                LastLoginTime = lastLoginTime,
                LastLoginIp = lastLoginIp
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var requestUri = "api/internal/auth/update-last-login";

            var response = await _httpClient.PostAsync(requestUri, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("出现错误：更新最后登录信息失败，状态码：{StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogInformation("用户最后登录信息更新成功");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出现错误：更新最后登录信息时发生异常");
        }
    }
}
