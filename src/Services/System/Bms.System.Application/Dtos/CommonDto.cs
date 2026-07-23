namespace Bms.System.Application.Dtos;

public class PagedRequestDto
{
    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 用户名搜索
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名搜索
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 状态筛选（0-禁用，1-启用）
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 关键字搜索（兼容旧版）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 租户名称搜索
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 租户编码搜索
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 租户ID筛选
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 组织ID筛选（单个）
    /// </summary>
    public long? OrganizationId { get; set; }

    /// <summary>
    /// 用户ID筛选（仅本人模式）
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 组织ID列表筛选（部门及以下、自定义模式）
    /// </summary>
    public List<long>? OrganizationIds { get; set; }

    /// <summary>
    /// 角色ID筛选
    /// </summary>
    public long? RoleId { get; set; }

    /// <summary>
    /// 创建者租户ID筛选
    /// 用于屏蔽平台租户跨租户创建的用户：非超管查询时由 Controller 注入当前租户ID
    /// </summary>
    public long? CreatorTenantId { get; set; }
}

public class PagedResponseDto<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> List { get; set; } = new();

    /// <summary>
    /// 总数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public class ApiResponseDto<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Code { get; set; } = 200;

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = "操作成功";

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; set; }

    public static ApiResponseDto<T> Success(T? data = default, string message = "操作成功")
    {
        return new ApiResponseDto<T>
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public static ApiResponseDto<T> Fail(string message, int code = 500)
    {
        return new ApiResponseDto<T>
        {
            Code = code,
            Message = message,
            Data = default
        };
    }
}

public class ApiResponseDto
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = "操作成功";
    public object? Data { get; set; }

    public static ApiResponseDto Success(object? data = null, string message = "操作成功")
    {
        return new ApiResponseDto
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public static ApiResponseDto Fail(string message, int code = 500)
    {
        return new ApiResponseDto
        {
            Code = code,
            Message = message,
            Data = null
        };
    }
}

/// <summary>
/// 批量删除请求
/// </summary>
public class BatchDeleteRequest
{
    /// <summary>
    /// 要删除的ID列表
    /// </summary>
    public List<long> Ids { get; set; } = new();
}