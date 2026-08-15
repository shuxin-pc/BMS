namespace Bms.Store.Api.Filters;

/// <summary>
/// 标记 Controller 或 Action 不需要门店上下文（X-Store-Id）。
/// 默认所有 /api/store/ 下的 Controller 都需要门店参数（安全默认），
/// 不需要门店参数的 Controller/Action 显式标记 [AllowWithoutStore]。
/// 这是声明式机制：每个 Controller 自己声明是否依赖门店参数，不维护集中白名单。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AllowWithoutStoreAttribute : Attribute
{
}
