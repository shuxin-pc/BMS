using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.StoredValues;

/// <summary>
/// 储值账户分页查询 DTO
/// 查询视角（文档 4.3）：
/// - 门店视角：门店开户业绩报表，AppService 应按 TenantId + StoreId = 当前门店过滤
/// - 客户视角：客户可用余额，AppService 按 TenantId + CustomerId 过滤，不按 StoreId 过滤（跨店充值均可见）
/// 注意：当前 AppService 实现 GetPagedListAsync 仅按 TenantId 过滤，未按 StoreId 过滤
/// 门店视角报表调用方需在 AppService 层显式追加 StoreId 过滤条件
/// </summary>
public class StoredValueAccountQueryDto : PagedRequestDto
{
    /// <summary>
    /// 客户ID
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 客户名称（模糊匹配）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 客户手机号（模糊匹配）
    /// </summary>
    public string? Phone { get; set; }
}
