using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGiftTransfers;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品赠品调拨单应用服务接口
/// 状态机：待调出(1) -> 已调入(3) / 已取消(4)
/// 仅支持样品(4)/赠品(5)商品的跨门店调拨，库存处理与正品调拨一致
/// </summary>
public interface ISampleGiftTransferAppService
{
    Task<ApiResponseDto<PagedResponseDto<SampleGiftTransferDto>>> GetPagedListAsync(SampleGiftTransferQueryDto query);
    Task<ApiResponseDto<SampleGiftTransferDto?>> GetByIdAsync(long id);

    /// <summary>
    /// 创建调拨单（待调出状态，不调整库存）
    /// 校验明细商品 Type∈{4,5} 且未删除
    /// </summary>
    Task<ApiResponseDto<SampleGiftTransferDto>> CreateAsync(SampleGiftTransferCreateDto dto);

    Task<ApiResponseDto<SampleGiftTransferDto>> UpdateAsync(SampleGiftTransferUpdateDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> BatchDeleteAsync(List<long> ids);

    /// <summary>
    /// 执行调拨：事务内调出门店扣减（Source=12）+ 调入门店增加（Source=13），状态转已调入
    /// 仅待调出状态可执行
    /// </summary>
    Task<ApiResponseDto> ExecuteAsync(long id);

    /// <summary>
    /// 取消调拨单：待调出转已取消（已调入不可取消，需走反向调拨单）
    /// </summary>
    Task<ApiResponseDto> CancelAsync(long id);

    /// <summary>
    /// 获取调出门店的样品/赠品商品选项（仅返回 Type∈{4,5} 且 Stock > 0 的商品）
    /// </summary>
    Task<ApiResponseDto<List<SampleGiftTransferProductOptionDto>>> GetFromStoreProductsAsync(long fromStoreId);

    /// <summary>
    /// 获取调出门店指定商品的在库批次列表（按过期日期升序，FEFO）
    /// </summary>
    Task<ApiResponseDto<List<SampleGiftTransferBatchOptionDto>>> GetFromStoreProductBatchesAsync(long fromStoreId, long productId);
}
