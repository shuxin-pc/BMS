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
}
