using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户关怀应用服务接口
/// 生日提醒和消费感谢的待关怀/已关怀状态基于 CustomerCareLog 流水表实时计算
/// </summary>
public interface ICustomerCareAppService
{
    /// <summary>
    /// 获取生日提醒分页列表
    /// 范围：未来 7 天（含今天）过生日的客户
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<BirthdayReminderDto>>> GetBirthdayRemindersAsync(BirthdayReminderQueryDto query);

    /// <summary>
    /// 标记生日关怀（幂等：同年同客户重复标记只更新时间）
    /// </summary>
    /// <param name="customerId">客户ID</param>
    Task<ApiResponseDto> MarkBirthdayCaredAsync(long customerId);

    /// <summary>
    /// 获取消费感谢分页列表
    /// 范围：近 7 天有已完成订单的客户，每客户取最近一笔订单
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<ConsumeThankRecordDto>>> GetConsumeThanksAsync(ConsumeThankQueryDto query);

    /// <summary>
    /// 标记消费感谢（幂等：同订单重复标记只更新方式和时间）
    /// </summary>
    /// <param name="orderId">订单ID</param>
    /// <param name="method">感谢方式 1=短信 2=微信 3=电话</param>
    Task<ApiResponseDto> MarkConsumeThankedAsync(long orderId, int method);
}
