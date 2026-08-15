using Bms.BuildingBlocks.Core.IdGenerator;
using Bms.BuildingBlocks.Storage.Abstractions;

namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// 上传编排实现：元数据校验 → 内容校验 → 拼装 objectKey → 上传
/// </summary>
internal sealed class FileUploadService : IFileUploadService
{
    private readonly IFileStorageService _storage;
    private readonly ImageFileValidator _validator;
    private readonly ISnowflakeIdGenerator _idGenerator;

    public FileUploadService(
        IFileStorageService storage,
        ImageFileValidator validator,
        ISnowflakeIdGenerator idGenerator)
    {
        _storage = storage;
        _validator = validator;
        _idGenerator = idGenerator;
    }

    /// <inheritdoc />
    public async Task<FileUploadOutcome> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        var metadataError = _validator.ValidateMetadata(request);
        if (metadataError is not null)
        {
            return FileUploadOutcome.Fail(metadataError);
        }

        var header = new byte[ImageFileValidator.MagicByteLength];
        var headerLength = await ReadHeaderAsync(request.Content, header, cancellationToken);

        var contentError = _validator.ValidateContent(header.AsSpan(0, headerLength));
        if (contentError is not null)
        {
            return FileUploadOutcome.Fail(contentError);
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var objectKey = ObjectKeyBuilder.Build(
            request.TenantId,
            request.StoreId,
            request.BizType,
            extension,
            _idGenerator.NewId());

        await _storage.UploadAsync(objectKey, request.Content, request.ContentType, request.Length, cancellationToken);

        return FileUploadOutcome.Ok(new FileUploadResult
        {
            ObjectKey = objectKey,
            FileName = request.FileName,
            Size = request.Length
        });
    }

    /// <summary>
    /// 读取流开头供魔术字节校验，读完复位到起点以供后续上传
    /// </summary>
    private static async Task<int> ReadHeaderAsync(Stream content, byte[] buffer, CancellationToken cancellationToken)
    {
        var read = await content.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        content.Seek(0, SeekOrigin.Begin);
        return read;
    }
}
