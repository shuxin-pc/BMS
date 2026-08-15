namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 文件上传结果。
/// 校验失败通过返回值表达而非抛异常 —— 项目既没有统一的业务异常类型，
/// 也没有全局异常处理中间件，抛异常会退化成 HTTP 500，前端无法拿到可读提示。
/// </summary>
public sealed class FileUploadOutcome
{
    private FileUploadOutcome(bool succeeded, FileUploadResult? result, string? errorMessage)
    {
        Succeeded = succeeded;
        Result = result;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 是否上传成功
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// 上传结果，仅 Succeeded 为 true 时非空
    /// </summary>
    public FileUploadResult? Result { get; }

    /// <summary>
    /// 失败原因，仅 Succeeded 为 false 时非空。可直接作为接口返回的提示消息
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 构造成功结果
    /// </summary>
    public static FileUploadOutcome Ok(FileUploadResult result) => new(true, result, null);

    /// <summary>
    /// 构造失败结果
    /// </summary>
    public static FileUploadOutcome Fail(string errorMessage) => new(false, null, errorMessage);
}
