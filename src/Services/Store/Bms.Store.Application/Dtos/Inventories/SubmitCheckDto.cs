namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 提交盘点单输入 DTO
/// 草稿状态的盘点单通过此 DTO 录入实际数量并提交，系统自动计算差异、调整库存、写入流水
/// </summary>
public class SubmitCheckDto
{
    /// <summary>
    /// 盘点单ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 实际盘点数量
    /// </summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>
    /// 备注（可选）
    /// </summary>
    public string? Remark { get; set; }
}
