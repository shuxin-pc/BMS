namespace Bms.Store.Application.Dtos.Suppliers;

/// <summary>
/// 供应商轻量选项 DTO（用于下拉选择场景，不分页）
/// </summary>
public class SupplierOptionDto
{
    /// <summary>供应商ID</summary>
    public long Id { get; set; }

    /// <summary>供应商名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>数据范围（1:门店通用 2:门店私用），用于前端区分显示标签</summary>
    public int Scope { get; set; }
}
