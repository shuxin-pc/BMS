namespace Bms.Store.Application.Dtos.Products;

/// <summary>
/// 门店档案配置预览 DTO（对应设计文档 7.3 节二次确认）
/// 用于在统一配置前告知用户哪些门店已有档案（将被覆盖）、哪些门店无档案（将新建）
/// </summary>
public class ProductStoreConfigPreviewDto
{
    /// <summary>选中门店中已存在 Product 档案的门店ID列表（这些门店将被覆盖）</summary>
    public List<long> ExistingStoreIds { get; set; } = new();
}
