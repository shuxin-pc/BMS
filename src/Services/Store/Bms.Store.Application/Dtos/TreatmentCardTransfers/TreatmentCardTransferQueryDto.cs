using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCardTransfers;

/// <summary>
/// 疗程卡转让分页查询 DTO
/// 查询视角（文档 4.2）：门店视角（门店转让记录）
/// AppService 应按 TenantId + StoreId = 当前门店过滤
/// 注意：当前 AppService 实现 GetPagedListAsync 仅按 TenantId 过滤，未按 StoreId 过滤
/// 门店视角报表调用方需在 AppService 层显式追加 StoreId 过滤条件
/// </summary>
public class TreatmentCardTransferQueryDto : PagedRequestDto
{
    /// <summary>
    /// 疗程卡销售记录ID
    /// </summary>
    public long? CardSaleId { get; set; }

    /// <summary>
    /// 原客户ID
    /// </summary>
    public long? FromCustomerId { get; set; }

    /// <summary>
    /// 新客户ID
    /// </summary>
    public long? ToCustomerId { get; set; }

    /// <summary>
    /// 状态筛选（1:已转让）
    /// </summary>
    public int? Status { get; set; }
}
