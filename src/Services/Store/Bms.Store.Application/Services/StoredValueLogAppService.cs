using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 储值流水应用服务实现
/// </summary>
public class StoredValueLogAppService : IStoredValueLogAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<StoredValueLogCreateDto> _createValidator;
    private readonly IValidator<StoredValueLogUpdateDto> _updateValidator;

    public StoredValueLogAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StoredValueLogCreateDto> createValidator,
        IValidator<StoredValueLogUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取储值流水分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueLogDto>>> GetPagedListAsync(StoredValueLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StoredValueLogDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.StoredValueLogs
            .Where(l => l.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(l => l.CustomerId == query.CustomerId.Value);
        if (query.Type.HasValue)
            queryable = queryable.Where(l => l.Type == query.Type.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(l => l.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StoredValueLogDto>
        {
            List = items.Select(ToDto).ToList(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StoredValueLogDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取储值流水详情
    /// </summary>
    public async Task<ApiResponseDto<StoredValueLogDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueLogDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.StoredValueLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<StoredValueLogDto?>.Fail("储值流水不存在", 404);
        return ApiResponseDto<StoredValueLogDto?>.Ok(ToDto(entity));
    }

    /// <summary>
    /// 创建储值流水
    /// </summary>
    public async Task<ApiResponseDto<StoredValueLogDto>> CreateAsync(StoredValueLogCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueLogDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        // 储值消费(Type=2)/退款(Type=3)流水禁止手动创建，必须通过订单支付/退款流程写入
        // 业务走向约束：储值扣款必须可追溯至具体订单（PayMethod=5 触发 ConsumeAsync 时绑定 OrderId）
        // 门店补录场景：通过创建订单（OrderType=1零售/2服务，PayMethod=5储值扣款）实现储值消费回溯
        if (dto.Type == 2 || dto.Type == 3)
        {
            return ApiResponseDto<StoredValueLogDto>.Fail(
                $"储值消费(Type=2)/退款(Type=3)流水必须通过订单支付/退款流程创建，禁止手动录入；" +
                "补录场景请创建订单（PayMethod=5）触发储值扣款流程", 400);
        }

        var entity = dto.Adapt<StoredValueLog>();
        entity.TenantId = _currentUser.TenantId.Value;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        // 手动补录场景由 Validator 保证 OperatorId 必填；此处显式赋值保持与项目其他服务一致
        entity.OperatorId = dto.OperatorId;
        entity.CreatedTime = DateTime.Now;

        _dbContext.StoredValueLogs.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoredValueLogDto>.Ok(ToDto(entity), "创建成功");
    }

    /// <summary>
    /// 更新储值流水
    /// </summary>
    public async Task<ApiResponseDto<StoredValueLogDto>> UpdateAsync(StoredValueLogUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueLogDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.StoredValueLogs
            .FirstOrDefaultAsync(l => l.Id == dto.Id && l.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<StoredValueLogDto>.Fail("储值流水不存在", 404);

        // 储值消费(Type=2)/退款(Type=3)流水禁止通过更新转换类型，必须通过订单支付/退款流程创建
        if ((dto.Type == 2 || dto.Type == 3) && entity.Type != dto.Type)
        {
            return ApiResponseDto<StoredValueLogDto>.Fail(
                $"储值消费(Type=2)/退款(Type=3)流水必须通过订单支付/退款流程创建，禁止通过更新转换类型", 400);
        }
        // 储值消费/退款流水必须保留订单关联，确保业务走向可追溯
        if ((dto.Type == 2 || dto.Type == 3) && !dto.OrderId.HasValue)
        {
            return ApiResponseDto<StoredValueLogDto>.Fail(
                "储值消费/退款流水必须关联订单（OrderId），确保业务走向可追溯", 400);
        }

        entity.CustomerId = dto.CustomerId;
        entity.Type = dto.Type;
        entity.Amount = dto.Amount;
        entity.RealAmount = dto.RealAmount;
        entity.GiftAmount = dto.GiftAmount;
        entity.BeforeBalance = dto.BeforeBalance;
        entity.AfterBalance = dto.AfterBalance;
        entity.RealBalanceChange = dto.RealBalanceChange;
        entity.GiftBalanceChange = dto.GiftBalanceChange;
        entity.BeforeRealBalance = dto.BeforeRealBalance;
        entity.AfterRealBalance = dto.AfterRealBalance;
        entity.BeforeGiftBalance = dto.BeforeGiftBalance;
        entity.AfterGiftBalance = dto.AfterGiftBalance;
        entity.OrderId = dto.OrderId;
        entity.PayMethod = dto.PayMethod;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoredValueLogDto>.Ok(ToDto(entity), "更新成功");
    }

    /// <summary>
    /// 删除储值流水（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.StoredValueLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("储值流水不存在", 404);

        _dbContext.StoredValueLogs.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除储值流水（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.StoredValueLogs
            .Where(l => ids.Contains(l.Id) && l.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.StoredValueLogs.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 储值现金流统计（G7.3）：按日期范围统计新增储值/消费/退款/沉淀资金
    /// </summary>
    public async Task<ApiResponseDto<StoredValueCashFlowDto>> GetCashFlowAsync(DateTime? startDate, DateTime? endDate)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueCashFlowDto>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 按日期范围过滤流水，按 Type 分组聚合
        var logsQuery = _dbContext.StoredValueLogs
            .Where(l => l.TenantId == tenantId);
        if (startDate.HasValue)
            logsQuery = logsQuery.Where(l => l.CreatedTime >= startDate.Value);
        if (endDate.HasValue)
            logsQuery = logsQuery.Where(l => l.CreatedTime <= endDate.Value);

        var flowStats = await logsQuery
            .GroupBy(l => l.Type)
            .Select(g => new
            {
                Type = g.Key,
                TotalAmount = g.Sum(l => l.Amount),
                TotalRealAmount = g.Sum(l => l.RealAmount),
                TotalGiftAmount = g.Sum(l => l.GiftAmount)
            })
            .ToListAsync();

        // Type=1 充值：RealAmount 为实收充值金额，GiftAmount 为赠送金额
        var rechargeStat = flowStats.FirstOrDefault(s => s.Type == 1);
        // Type=2 消费：Amount 为负数，取负得到消费金额
        var consumeStat = flowStats.FirstOrDefault(s => s.Type == 2);
        // Type=3 退款：Amount 为正数（退回余额）
        var refundStat = flowStats.FirstOrDefault(s => s.Type == 3);

        // 沉淀资金：期末所有储值账户余额合计（不受日期范围限制）
        var accountStats = await _dbContext.StoredValueAccounts
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .GroupBy(a => 1)
            .Select(g => new
            {
                TotalBalance = g.Sum(a => a.Balance),
                TotalRealBalance = g.Sum(a => a.RealBalance),
                TotalGiftBalance = g.Sum(a => a.GiftBalance)
            })
            .FirstOrDefaultAsync();

        var result = new StoredValueCashFlowDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRecharge = rechargeStat?.TotalRealAmount ?? 0m,
            TotalGift = rechargeStat?.TotalGiftAmount ?? 0m,
            TotalConsume = -(consumeStat?.TotalAmount ?? 0m),
            TotalRefund = refundStat?.TotalAmount ?? 0m,
            TotalBalance = accountStats?.TotalBalance ?? 0m,
            TotalRealBalance = accountStats?.TotalRealBalance ?? 0m,
            TotalGiftBalance = accountStats?.TotalGiftBalance ?? 0m
        };

        return ApiResponseDto<StoredValueCashFlowDto>.Ok(result);
    }

    /// <summary>
    /// 实体转 DTO（手动映射时间字段）
    /// </summary>
    private static StoredValueLogDto ToDto(StoredValueLog entity)
    {
        var dto = entity.Adapt<StoredValueLogDto>();
        dto.CreatedAt = entity.CreatedTime;
        dto.UpdatedAt = entity.UpdatedTime;
        return dto;
    }
}
