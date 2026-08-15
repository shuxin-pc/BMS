namespace Bms.Store.Domain.Entities;

/// <summary>
/// 供应商
/// </summary>
public class Supplier : StoreEntity
{
    /// <summary>
    /// 供应商名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 供应商编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 联系人
    /// </summary>
    public string? Contact { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 银行账户
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    /// 状态（1:合作中 2:已停止）
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 数据范围（1:门店通用 2:门店私用）
    /// 通用时 StoreId=0，全部门店可见；私用时 StoreId 为具体门店
    /// </summary>
    public int Scope { get; set; } = 2;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
