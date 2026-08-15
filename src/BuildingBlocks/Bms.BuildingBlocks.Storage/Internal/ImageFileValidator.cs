using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.BuildingBlocks.Storage.Options;
using Microsoft.Extensions.Options;

namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// 图片文件校验器。刻意不接触文件流位置（不读、不 Seek），
/// 流的读取与复位由 FileUploadService 负责，避免校验器产生隐藏副作用
/// </summary>
internal sealed class ImageFileValidator
{
    /// <summary>
    /// 魔术字节校验所需的前导字节数。WebP 需要 12 字节（RIFF....WEBP）
    /// </summary>
    public const int MagicByteLength = 12;

    private readonly ObjectStorageOptions _options;

    public ImageFileValidator(IOptions<ObjectStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 校验元数据：大小、业务类型、扩展名、声明的 MIME 类型。通过返回 null，否则返回错误提示
    /// </summary>
    public string? ValidateMetadata(FileUploadRequest request)
    {
        if (request.Length <= 0)
        {
            return "上传文件为空";
        }

        if (request.Length > _options.MaxFileSizeBytes)
        {
            return $"文件大小超出上限 {_options.MaxFileSizeBytes / 1024 / 1024}MB";
        }

        if (!_options.AllowedBizTypes.Contains(request.BizType, StringComparer.OrdinalIgnoreCase))
        {
            return "业务类型不在允许范围内";
        }

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrEmpty(extension) || !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return $"不支持的文件格式，仅允许 {string.Join("、", _options.AllowedExtensions)}";
        }

        if (request.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
        {
            return "文件类型必须为图片";
        }

        return null;
    }

    /// <summary>
    /// 魔术字节校验：确认文件内容真的是图片，防止可执行文件改扩展名后上传。
    /// 扩展名与实际格式不一致不视为错误 —— 安全边界是「必须是图片」，而非「格式必须对应」
    /// </summary>
    public string? ValidateContent(ReadOnlySpan<byte> header)
    {
        if (IsJpeg(header) || IsPng(header) || IsWebp(header))
        {
            return null;
        }

        return "文件内容不是有效的图片";
    }

    private static bool IsJpeg(ReadOnlySpan<byte> header)
        => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> header)
        => header.Length >= 8
           && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
           && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

    private static bool IsWebp(ReadOnlySpan<byte> header)
        => header.Length >= 12
           && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
           && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
}
