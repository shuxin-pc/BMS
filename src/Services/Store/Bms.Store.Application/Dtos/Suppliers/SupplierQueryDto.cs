using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Suppliers;

/// <summary>
/// 供应商分页查询 DTO
/// </summary>
public class SupplierQueryDto : PagedRequestDto
{
    /// <summary>
    /// 供应商名称（模糊查询）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 供应商编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 状态筛选（0:禁用 1:启用）
    /// </summary>
    public int? Status { get; set; }
}
