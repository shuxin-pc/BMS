namespace Bms.Store.Domain.Entities;

/// <summary>
/// 客户标签字典（门店级隔离）
/// 各门店独立维护标签字典，同门店内标签名称唯一
/// </summary>
public class CustomerTag : StoreEntity
{
    /// <summary>
    /// 标签名称（同门店内唯一）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签颜色（el-tag type 名：primary/success/warning/danger/info）
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
