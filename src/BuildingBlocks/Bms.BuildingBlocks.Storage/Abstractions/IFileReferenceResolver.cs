namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 图片来源引用解析器（读取编排入口）。
/// 库中保存的来源值可能是 objectKey，也可能是用户粘贴的外部图片直链，
/// 两者的分支判断集中在实现内部，业务代码不应出现协议前缀判断。
/// </summary>
public interface IFileReferenceResolver
{
    /// <summary>
    /// 把库中保存的来源值批量解析为可直接放入 img src 的地址：
    /// http/https 外链原样透传，其余按 objectKey 现场签名（前缀越权者被丢弃，不出现在结果中）
    /// </summary>
    IReadOnlyDictionary<string, string> Resolve(IEnumerable<string> references, PresignScope scope);
}
