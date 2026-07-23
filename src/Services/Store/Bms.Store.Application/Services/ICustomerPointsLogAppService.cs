using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户积分流水应用服务接口
/// </summary>
public interface ICustomerPointsLogAppService
{
    Task<ApiResponseDto<PagedResponseDto<CustomerPointsLogDto>>> GetPagedListAsync(CustomerPointsLogQueryDto query);
    Task<ApiResponseDto<CustomerPointsLogDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<CustomerPointsLogDto>> CreateAsync(CustomerPointsLogCreateDto dto);
    Task<ApiResponseDto<CustomerPointsLogDto>> UpdateAsync(CustomerPointsLogUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 查询客户即将过期的积分明细（P-PTS-04 第一阶段）
    /// 用于客户详情页展示"即将过期积分"提醒徽章
    /// 第二阶段（站内信提醒）待站内信模块完善后实现
    /// </summary>
    /// <param name="customerId">客户ID</param>
    /// <param name="days">查询未来 N 天内将过期的积分，默认 7 天</param>
    Task<ApiResponseDto<List<ExpiringPointsDto>>> GetExpiringPointsAsync(long customerId, int days = 7);
}

