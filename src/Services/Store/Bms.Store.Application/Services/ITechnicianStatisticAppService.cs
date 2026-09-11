using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技师统计应用服务接口
/// </summary>
public interface ITechnicianStatisticAppService
{
    Task<ApiResponseDto<PagedResponseDto<TechnicianStatisticDto>>> GetPagedListAsync(TechnicianStatisticQueryDto query);
    Task<ApiResponseDto<TechnicianStatisticDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<TechnicianStatisticDto>> CreateAsync(TechnicianStatisticCreateDto dto);
    Task<ApiResponseDto<TechnicianStatisticDto>> UpdateAsync(TechnicianStatisticUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 获取技师业绩统计报表（从 OrderItem 实时聚合，仅商家技师）
    /// 对应需求 B4.5：服务人次、时长、回头客率、技师服务费用汇总
    /// 纯平台技师门店（无自有技师）返回 IsPurePlatformStore=true + 空列表，业绩由平台统一统计
    /// </summary>
    Task<ApiResponseDto<TechnicianStatReportDto>> GetReportAsync(TechnicianStatisticQueryDto query);

    /// <summary>
    /// 按"技师+日期"全量重算技师统计并 upsert（自动归集算法）
    /// 过滤口径与 GetReportAsync 一致：同门店 + 仅商家技师(Source=1) + 仅已完成订单(Status=2) + 服务时间日期==statDate（为空回退下单日）
    /// 天然幂等：重算即回退（退款=3/取消=4/冲正=4 后订单不再满足 Status=2，重算结果自然剔除）
    /// 无有效明细则删除该统计记录
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="technicianId">技师ID</param>
    /// <param name="statDate">统计日期（服务时间日期）</param>
    Task RecalculateTechnicianStatisticAsync(long tenantId, long storeId, long technicianId, DateTime statDate);
}
