namespace Bms.Store.Application.Dtos;

/// <summary>
/// 分页请求基类
/// </summary>
public class PagedRequestDto
{
    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 分页响应封装
/// </summary>
/// <typeparam name="T">数据元素类型</typeparam>
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
}

/// <summary>
/// 非泛型 API 响应封装（用于无返回数据的操作）
/// </summary>
public class ApiResponseDto
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
    public object? Data { get; set; }

    /// <summary>
    /// 是否成功（Code == 200）
    /// 与泛型版本 ApiResponseDto&lt;T&gt;.IsSuccess 保持一致，便于调用方统一判断
    /// </summary>
    public bool IsSuccess => Code == 200;

    /// <summary>
    /// 构造成功响应
    /// </summary>
    public static ApiResponseDto Success(object? data = null, string message = "操作成功")
    {
        return new ApiResponseDto
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 构造失败响应
    /// </summary>
    public static ApiResponseDto Fail(string message, int code = 400)
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

/// <summary>
/// 门店用户分配请求（全量替换语义）
/// </summary>
public class AssignUsersRequest
{
    /// <summary>
    /// 最终选中的用户ID列表（后端按此列表做 diff：新增/删除）
    /// </summary>
    public List<long> UserIds { get; set; } = new();
}

/// <summary>
/// 服务项目绑定耗材的效期选择输入（快速开单加购服务项目时店员选择的耗材效期）
/// 服务项目订单（OrderItemCreateDto.ConsumableExpiries）与项目卡核销项（TreatmentCardVerifyItemInput.ConsumableExpiries）共用
/// </summary>
public class ConsumableExpiryInput
{
    /// <summary>
    /// 耗材商品ID（对应 ServiceBom.ConsumableProductId）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 店员选择的耗材效期列表（按扣减顺序）。
    /// 空表示系统自动按近效期扣减（FEFO）；
    /// 非空时按顺序依次扣减，元素为 null 表示"无效期限制"批次。
    /// </summary>
    public List<DateTime?> ExpirationDates { get; set; } = new();
}
