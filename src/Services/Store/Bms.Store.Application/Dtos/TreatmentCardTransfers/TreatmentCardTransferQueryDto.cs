using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCardTransfers;

/// <summary>
/// 项目卡转让分页查询 DTO
/// 查询视角（文档 4.2）：门店视角（门店转让记录）
/// AppService 应按 TenantId + StoreId = 当前门店过滤
/// 注意：当前 AppService 实现 GetPagedListAsync 仅按 TenantId 过滤，未按 StoreId 过滤
/// 门店视角报表调用方需在 AppService 层显式追加 StoreId 过滤条件
/// </summary>
public class TreatmentCardTransferQueryDto : PagedRequestDto
{
    /// <summary>
    /// 项目卡销售记录ID
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

    /// <summary>
    /// 转让日期范围 - 开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 转让日期范围 - 结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中原客户/新客户的姓名或手机号其一即满足）
    /// </summary>
    public string? Keyword { get; set; }
}
