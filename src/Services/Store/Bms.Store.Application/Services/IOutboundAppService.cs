using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;

namespace Bms.Store.Application.Services;

/// <summary>
/// 出库应用服务接口
/// </summary>
public interface IOutboundAppService
{
    /// <summary>
    /// 创建出库记录（事务内按 FEFO 或手动指定扣减批次，写多条流水，更新汇总表）
    /// </summary>
    Task<ApiResponseDto<OutboundResultDto>> CreateAsync(OutboundCreateDto dto);
}