using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 项目卡核销记录分页查询 DTO
/// 查询视角（文档 4.2）：
/// - 门店视角：门店核销业绩报表，AppService 按 TenantId + StoreId = 当前门店过滤
/// - 客户视角：客户核销历史，AppService 按 TenantId + CustomerId（通过 CardSaleId 关联）过滤，不按 StoreId 过滤
/// - 跨店统计视角：Verify.StoreId ≠ Sale.StoreId 的核销记录
/// </summary>
public class TreatmentCardVerifyQueryDto : PagedRequestDto
{
    /// <summary>
    /// 项目卡销售记录ID
    /// </summary>
    public long? CardSaleId { get; set; }

    /// <summary>
    /// 核销项目ID
    /// </summary>
    public long? VerifyProductId { get; set; }

    /// <summary>
    /// 客户名称或手机号关键字（模糊匹配，OR 语义：命中姓名或手机号其一即满足，子查询 join TreatmentCardSale→Customer）
    /// 与预约列表 AppointmentQueryDto.Keyword 口径一致
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 核销开始日期（含，按 VerifyTime 过滤）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 核销结束日期（含当天，查询时自动加一天）
    /// </summary>
    public DateTime? EndDate { get; set; }
}
