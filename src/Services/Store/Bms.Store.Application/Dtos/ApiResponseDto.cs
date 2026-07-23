namespace Bms.Store.Application.Dtos;

/// <summary>
/// API 响应封装
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponseDto<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => Code == 200;

    public static ApiResponseDto<T> Ok(T? data, string? message = null)
    {
        return new ApiResponseDto<T>
        {
            Code = 200,
            Data = data,
            Message = message
        };
    }

    public static ApiResponseDto<T> Fail(string message, int code = 400)
    {
        return new ApiResponseDto<T>
        {
            Code = code,
            Message = message
        };
    }
}