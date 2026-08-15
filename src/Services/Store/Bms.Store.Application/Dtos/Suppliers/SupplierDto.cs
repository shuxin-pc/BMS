namespace Bms.Store.Application.Dtos.Suppliers;

/// <summary>
/// 供应商 DTO
/// </summary>
public class SupplierDto
{
    public long Id { get; set; }

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
    public int Status { get; set; }

    /// <summary>
    /// 数据范围（1:门店通用 2:门店私用）
    /// </summary>
    public int Scope { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 累计采购额（只读，后端计算）
    /// </summary>
    public decimal TotalPurchaseAmount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
