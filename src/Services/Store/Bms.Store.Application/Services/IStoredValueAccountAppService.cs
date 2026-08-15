using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;

namespace Bms.Store.Application.Services;

/// <summary>
/// 储值账户应用服务接口
/// </summary>
public interface IStoredValueAccountAppService
{
    Task<ApiResponseDto<PagedResponseDto<StoredValueAccountDto>>> GetPagedListAsync(StoredValueAccountQueryDto query);
    Task<ApiResponseDto<StoredValueAccountDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<StoredValueAccountDto>> CreateAsync(StoredValueAccountCreateDto dto);
    Task<ApiResponseDto<StoredValueAccountDto>> UpdateAsync(StoredValueAccountUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 储值充值（独立于订单系统，由操作人员手动录入金额）
    /// 充值发积分，按积分规则 PointsRate 计算，Floor 取整
    /// </summary>
    Task<ApiResponseDto<StoredValueAccountDto>> RechargeAsync(StoredValueRechargeDto dto);

    /// <summary>
    /// 储值消费（供 OrderAppService.CreateAsync 在事务内调用，支持单一储值支付与组合支付类别2）
    /// 不开启独立事务，由调用方事务包裹；先扣实收余额(RealBalance)，不足扣赠送余额(GiftBalance)
    /// 同时完成 P-SV-01 修复：余额不足抛异常而非静默失败
    /// </summary>
    /// <param name="customerId">客户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="storeCode">门店编码</param>
    /// <param name="amount">扣减金额（必须>0）</param>
    /// <param name="orderId">关联订单ID</param>
    /// <param name="orderNo">关联订单号</param>
    /// <param name="payMethod">支付方式（5=单一储值，7=组合支付，写入 StoredValueLog.PayMethod）</param>
    /// <param name="now">操作时间</param>
    /// <returns>扣减结果（含实收/赠送扣减明细与余额变化）</returns>
    Task<StoredValueConsumeResult> ConsumeAsync(
        long customerId, long tenantId, string tenantCode,
        long storeId, string storeCode,
        decimal amount, long orderId, string orderNo,
        int? payMethod, DateTime now);

    /// <summary>
    /// 储值退款（规则8）
    /// 冲减原充值发生门店的充值业绩；门店关店则冲减当前操作门店
    /// 退款金额冲减实收余额(RealBalance)，不足冲减赠送余额(GiftBalance)
    /// </summary>
    Task<ApiResponseDto<StoredValueAccountDto>> RefundAsync(StoredValueRefundDto dto);
}
