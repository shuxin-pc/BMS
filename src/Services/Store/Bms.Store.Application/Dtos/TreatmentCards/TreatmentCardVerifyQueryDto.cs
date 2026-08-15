using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡核销记录分页查询 DTO
/// 查询视角（文档 4.2）：
/// - 门店视角：门店核销业绩报表，AppService 应按 TenantId + StoreId = 当前门店过滤
/// - 客户视角：客户核销历史，AppService 按 TenantId + CustomerId（通过 CardSaleId 关联）过滤，不按 StoreId 过滤
/// - 跨店统计视角：Verify.StoreId ≠ Sale.StoreId 的核销记录
/// 注意：当前 AppService 实现 GetPagedListAsync 仅按 TenantId 过滤，未按 StoreId 过滤
/// 门店视角报表调用方需在 AppService 层显式追加 StoreId 过滤条件
/// </summary>
public class TreatmentCardVerifyQueryDto : PagedRequestDto
{
    /// <summary>
    /// 疗程卡销售记录ID
    /// </summary>
    public long? CardSaleId { get; set; }

    /// <summary>
    /// 核销项目ID
    /// </summary>
    public long? VerifyProductId { get; set; }
}
