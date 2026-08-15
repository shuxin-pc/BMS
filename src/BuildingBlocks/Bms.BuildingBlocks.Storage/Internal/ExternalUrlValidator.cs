using System.Text.RegularExpressions;
using Bms.BuildingBlocks.Storage.Abstractions;

namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// 外部图片直链校验器实现
/// </summary>
internal sealed class ExternalUrlValidator : IExternalUrlValidator
{
    /// <summary>
    /// URL 协议头匹配。用它而非直接判断 http 前缀，是为了让 javascript:、data: 等伪协议
    /// 也被识别为「外链」，从而进入协议白名单校验并被拒绝；
    /// 若只判断 http 前缀，伪协议会被当成 objectKey 静默存入数据库
    /// </summary>
    private static readonly Regex SchemePattern = new(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:", RegexOptions.Compiled);

    /// <inheritdoc />
    public bool IsAllowedExternalUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <inheritdoc />
    public bool LooksLikeExternalUrl(string value)
        => !string.IsNullOrWhiteSpace(value) && SchemePattern.IsMatch(value.Trim());
}
