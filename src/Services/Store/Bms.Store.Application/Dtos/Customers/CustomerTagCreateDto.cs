namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建客户标签输入 DTO
/// </summary>
public class CustomerTagCreateDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签颜色（el-tag type 名：primary/success/warning/danger/info）
    /// </summary>
    public string? Color { get; set; }

    public int Sort { get; set; }
    public string? Remark { get; set; }
}
