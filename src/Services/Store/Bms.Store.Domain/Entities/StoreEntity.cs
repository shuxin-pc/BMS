using Bms.BuildingBlocks.MultiTenant.Abstractions;

namespace Bms.Store.Domain.Entities;

/// <summary>
/// 基础实体基类（含软删除，用于核心表）
/// 与 System 服务的 BaseEntity 对应
/// </summary>
public abstract class StoreBaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.Now;
    public DateTime? UpdatedTime { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 创建人ID（System 服务用户ID）
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 更新人ID（System 服务用户ID）
    /// </summary>
    public long? UpdatedBy { get; set; }
}

/// <summary>
/// 多租户实体基类（含软删除，用于核心表）
/// </summary>
public abstract class StoreTenantEntity : StoreBaseEntity, ITenant
{
    public long TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
}

/// <summary>
/// 门店业务实体基类（含软删除，用于核心表）
/// 继承自 StoreTenantEntity，包含租户ID
/// 其他业务数据（商品、订单等）应继承此类，以实现：
/// - 多租户数据隔离（通过 TenantId）
/// - 门店数据归属（通过 StoreId）
/// </summary>
public abstract class StoreEntity : StoreTenantEntity
{
    /// <summary>
    /// 门店ID
    /// </summary>
    public long StoreId { get; set; }

    /// <summary>
    /// 门店编码
    /// </summary>
    public string StoreCode { get; set; } = string.Empty;
}

/// <summary>
/// 无软删除的基础实体基类（用于流水、明细、单据、统计等非核心表）
/// 与 System 服务的 EntityBase 对应
/// </summary>
public abstract class StoreEntityBase
{
    public long Id { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.Now;
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 创建人ID（System 服务用户ID）
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// 更新人ID（System 服务用户ID）
    /// </summary>
    public long? UpdatedBy { get; set; }
}

/// <summary>
/// 无软删除的多租户实体基类（用于非核心表）
/// </summary>
public abstract class StoreTenantEntityBase : StoreEntityBase, ITenant
{
    public long TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
}

/// <summary>
/// 无软删除的门店业务实体基类（用于非核心表）
/// </summary>
public abstract class StoreBusinessEntityBase : StoreTenantEntityBase
{
    /// <summary>
    /// 门店ID
    /// </summary>
    public long StoreId { get; set; }

    /// <summary>
    /// 门店编码
    /// </summary>
    public string StoreCode { get; set; } = string.Empty;
}
