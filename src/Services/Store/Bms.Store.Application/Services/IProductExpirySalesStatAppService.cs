using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;

namespace Bms.Store.Application.Services;

/// <summary>
/// 效期销售统计服务（B5.5）
/// </summary>
/// <remarks>
/// 按商品 + 效期区间聚合统计销售/消耗情况，数据源为 OrderItemBatch 表。
/// 支持实物商品（零售出库）和耗材（服务耗材出库）两类。
/// </remarks>
public interface IProductExpirySalesStatAppService
{
    /// <summary>
    /// 获取效期销售统计报表（按商品 × 效期区间分组）
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ProductExpirySalesStatDto>>> GetReportAsync(ProductExpirySalesQueryDto query);
}
