using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户档案应用服务接口
/// </summary>
public interface ICustomerAppService
{
    Task<ApiResponseDto<PagedResponseDto<CustomerDto>>> GetPagedListAsync(CustomerQueryDto query);
    Task<ApiResponseDto<CustomerDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<CustomerDto>> CreateAsync(CustomerCreateDto dto);
    Task<ApiResponseDto<CustomerDto>> UpdateAsync(CustomerUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);
    Task<ApiResponseDto<CustomerConsumptionStatDto>> GetConsumptionStatAsync(long customerId);

    /// <summary>
    /// 永久删除客户档案（物理删除）
    /// 物理删除客户档案及关联的个人信息，订单与消费记录脱敏保留
    /// 前置条件：无未完成订单、无未核销项目卡、无储值余额
    /// 依据：《个人信息保护法》第 47 条
    /// </summary>
    /// <param name="id">客户ID</param>
    /// <param name="dto">永久删除输入（二次确认码 + 删除原因）</param>
    Task<ApiResponseDto> PermanentlyDeleteAsync(long id, CustomerPermanentDeleteDto dto);
}
