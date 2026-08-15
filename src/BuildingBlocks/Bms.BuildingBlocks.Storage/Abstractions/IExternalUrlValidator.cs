namespace Bms.BuildingBlocks.Storage.Abstractions;

/// <summary>
/// 外部图片直链校验器（写入路径使用）。
/// 外链会原样渲染进前端的 img src，若不限制协议，javascript:、data:text/html
/// 就是现成的 XSS 入口。校验统一在此实现，各服务不得自行手写判断 —— 漏一处即是一个 XSS 入口。
/// </summary>
public interface IExternalUrlValidator
{
    /// <summary>
    /// 判断该来源值是否允许作为外部图片直链入库。仅接受 http / https
    /// </summary>
    bool IsAllowedExternalUrl(string value);

    /// <summary>
    /// 判断该来源值是否形似外链（带 URL 协议头）。
    /// 用于写入侧决定是否走协议白名单校验：伪协议同样会被判定为外链，
    /// 从而进入 IsAllowedExternalUrl 校验并被拒绝，而不会被误当作 objectKey 存入
    /// </summary>
    bool LooksLikeExternalUrl(string value);
}
