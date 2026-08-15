namespace Bms.Store.Domain.Entities;

/// <summary>
/// 用户-门店授权关联实体（细粒度授权）
/// 记录普通用户被分配可访问的门店列表。
/// tenant_admin/super_admin 不依赖此表，保持本租户/跨租户全门店访问权限。
/// 冗余 UserName/RealName 字段便于列表展示，避免每次跨库查用户表；
/// 用户改名时由 System 服务通知同步（当前暂不处理，可接受短暂不一致）。
/// </summary>
public class UserStore : StoreTenantEntity
{
    /// <summary>
    /// 用户ID（System 服务 Users 表主键）
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 门店ID（本服务 Stores 表主键）
    /// </summary>
    public long StoreId { get; set; }

    /// <summary>
    /// 用户名（冗余字段，便于列表展示）
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名（冗余字段，便于列表展示）
    /// </summary>
    public string RealName { get; set; } = string.Empty;
}
