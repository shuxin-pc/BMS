namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 服务项目轻量选项（用于BOM下拉选择）
/// </summary>
public class ServiceProductOptionDto
{
    /// <summary>服务项目ID（ServiceProduct.Id）</summary>
    public long Id { get; set; }

    /// <summary>服务项目名称（来自 ProductMaster.Name）</summary>
    public string Name { get; set; } = string.Empty;
}
