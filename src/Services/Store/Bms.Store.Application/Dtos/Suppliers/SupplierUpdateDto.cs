namespace Bms.Store.Application.Dtos.Suppliers;

/// <summary>
/// 更新供应商请求 DTO
/// </summary>
public class SupplierUpdateDto : SupplierCreateDto
{
    /// <summary>
    /// 供应商ID
    /// </summary>
    public long Id { get; set; }
}
