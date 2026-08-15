using Bms.BuildingBlocks.Storage.Abstractions;

namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// 图片来源引用解析实现
/// </summary>
internal sealed class FileReferenceResolver : IFileReferenceResolver
{
    private readonly IFileStorageService _storage;
    private readonly IExternalUrlValidator _urlValidator;

    public FileReferenceResolver(IFileStorageService storage, IExternalUrlValidator urlValidator)
    {
        _storage = storage;
        _urlValidator = urlValidator;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Resolve(IEnumerable<string> references, PresignScope scope)
    {
        var distinctReferences = references
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct()
            .ToList();

        var resolved = new Dictionary<string, string>(distinctReferences.Count);
        var objectKeys = new List<string>();

        foreach (var reference in distinctReferences)
        {
            // 读取侧只对 http/https 透传，其余一律按 objectKey 处理：
            // 即便库中存在历史脏数据，也只会走签名路径（越权者被丢弃），不会原样渲染到前端
            if (_urlValidator.IsAllowedExternalUrl(reference))
            {
                resolved[reference] = reference;
            }
            else
            {
                objectKeys.Add(reference);
            }
        }

        foreach (var (objectKey, url) in _storage.GetPresignedUrls(objectKeys, scope))
        {
            resolved[objectKey] = url;
        }

        return resolved;
    }
}
