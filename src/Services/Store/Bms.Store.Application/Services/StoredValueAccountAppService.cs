using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StoredValues;
using Bms.Store.Domain.Entities;
using Bms.Store.Domain.Constants;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 储值账户应用服务实现
/// </summary>
public class StoredValueAccountAppService : IStoredValueAccountAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<StoredValueAccountCreateDto> _createValidator;
    private readonly IValidator<StoredValueAccountUpdateDto> _updateValidator;

    public StoredValueAccountAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StoredValueAccountCreateDto> createValidator,
        IValidator<StoredValueAccountUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取储值账户分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueAccountDto>>> GetPagedListAsync(StoredValueAccountQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StoredValueAccountDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.StoredValueAccounts
            .Where(a => !a.IsDeleted && a.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(a => a.CustomerId == query.CustomerId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(a => a.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StoredValueAccountDto>
        {
            List = items.Select(ToDto).ToList(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StoredValueAccountDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取储值账户详情
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<StoredValueAccountDto?>.Fail("储值账户不存在", 404);
        return ApiResponseDto<StoredValueAccountDto?>.Ok(ToDto(entity));
    }

    /// <summary>
    /// 创建储值账户
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> CreateAsync(StoredValueAccountCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueAccountDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var entity = dto.Adapt<StoredValueAccount>();
        entity.TenantId = _currentUser.TenantId.Value;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.StoredValueAccounts.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoredValueAccountDto>.Ok(ToDto(entity), "创建成功");
    }

    /// <summary>
    /// 更新储值账户
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> UpdateAsync(StoredValueAccountUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueAccountDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.Id == dto.Id && !a.IsDeleted && a.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<StoredValueAccountDto>.Fail("储值账户不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.Balance = dto.Balance;
        entity.RealBalance = dto.RealBalance;
        entity.GiftBalance = dto.GiftBalance;
        entity.TotalRecharge = dto.TotalRecharge;
        entity.TotalGift = dto.TotalGift;
        entity.TotalConsume = dto.TotalConsume;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StoredValueAccountDto>.Ok(ToDto(entity), "更新成功");
    }

    /// <summary>
    /// 删除储值账户（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("储值账户不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除储值账户（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.StoredValueAccounts
            .Where(a => ids.Contains(a.Id) && !a.IsDeleted && a.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 实体转 DTO（手动映射时间字段）
    /// </summary>
    private static StoredValueAccountDto ToDto(StoredValueAccount entity)
    {
        var dto = entity.Adapt<StoredValueAccountDto>();
        dto.CreatedAt = entity.CreatedTime;
        dto.UpdatedAt = entity.UpdatedTime;
        return dto;
    }

    /// <summary>
    /// 储值充值（独立于订单系统，由操作人员手动录入金额）
    /// 充值发积分，按积分规则 PointsRate 计算，Floor 取整
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> RechargeAsync(StoredValueRechargeDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("无法确定当前租户", 401);

        if (dto.Amount <= 0)
            return ApiResponseDto<StoredValueAccountDto>.Fail("充值金额必须大于0", 400);

        if (dto.CustomerId <= 0)
            return ApiResponseDto<StoredValueAccountDto>.Fail("客户ID无效", 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        // 计算赠送金额：手动输入优先于规则
        var giftAmount = 0m;
        if (dto.GiftAmount.HasValue)
        {
            giftAmount = dto.GiftAmount.Value;
        }
        else if (dto.StoredValueRuleId.HasValue)
        {
            var rule = await _dbContext.StoredValueRules
                .FirstOrDefaultAsync(r => r.Id == dto.StoredValueRuleId.Value && r.TenantId == tenantId && r.IsEnabled);
            if (rule != null)
            {
                giftAmount = rule.GiftAmount;
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 查找或创建储值账户
            var account = await _dbContext.StoredValueAccounts
                .FirstOrDefaultAsync(a => a.CustomerId == dto.CustomerId && a.TenantId == tenantId && !a.IsDeleted);

            var beforeBalance = account?.Balance ?? 0m;
            var beforeRealBalance = account?.RealBalance ?? 0m;
            var beforeGiftBalance = account?.GiftBalance ?? 0m;

            if (account == null)
            {
                account = new StoredValueAccount
                {
                    CustomerId = dto.CustomerId,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    CreatedTime = now
                };
                _dbContext.StoredValueAccounts.Add(account);
            }

            account.RealBalance = beforeRealBalance + dto.Amount;
            account.GiftBalance = beforeGiftBalance + giftAmount;
            account.Balance = beforeBalance + dto.Amount + giftAmount;
            account.TotalRecharge += dto.Amount;
            account.TotalGift += giftAmount;
            account.UpdatedTime = now;

            // 记录储值流水
            _dbContext.StoredValueLogs.Add(new StoredValueLog
            {
                CustomerId = dto.CustomerId,
                Type = 1, // 充值
                Amount = dto.Amount + giftAmount,
                RealAmount = dto.Amount,
                GiftAmount = giftAmount,
                BeforeBalance = beforeBalance,
                AfterBalance = account.Balance,
                RealBalanceChange = dto.Amount,
                GiftBalanceChange = giftAmount,
                BeforeRealBalance = beforeRealBalance,
                AfterRealBalance = account.RealBalance,
                BeforeGiftBalance = beforeGiftBalance,
                AfterGiftBalance = account.GiftBalance,
                PayMethod = dto.PayMethod,
                Remark = dto.Remark ?? "储值充值",
                OperatorId = _currentUser.UserId,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now
            });

            // 充值发积分（按积分规则 PointsRate 计算，Floor 取整）
            var pointsRule = await _dbContext.PointsRules
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Status == 1);
            if (pointsRule != null && pointsRule.PointsRate > 0)
            {
                var points = (int)Math.Floor(dto.Amount * pointsRule.PointsRate);
                if (points > 0)
                {
                    var customer = await _dbContext.Customers
                        .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.TenantId == tenantId);
                    if (customer != null)
                    {
                        var beforePoints = customer.TotalPoints;
                        customer.TotalPoints += points;
                        customer.UpdatedTime = now;

                        var expireDate = pointsRule.PointsValidityDays.HasValue
                            ? now.AddDays(pointsRule.PointsValidityDays.Value)
                            : (DateTime?)null;

                        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
                        {
                            CustomerId = customer.Id,
                            Type = CustomerPointsLogType.Recharge, // 充值获得
                            Points = points,
                            BeforePoints = beforePoints,
                            AfterPoints = customer.TotalPoints,
                            OperatorId = _currentUser.UserId,
                            ExpireDate = expireDate,
                            Remark = $"储值充值 {dto.Amount:F2} 元获得积分",
                            TenantId = tenantId,
                            TenantCode = tenantCode,
                            StoreId = _currentUser.StoreId ?? 0L,
                            StoreCode = string.Empty,
                            CreatedTime = now
                        });
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<StoredValueAccountDto>.Ok(ToDto(account), "充值成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 储值消费（供 OrderAppService.CreateAsync 在事务内调用）
    /// 不开启独立事务，由调用方事务包裹；先扣 RealBalance，不足扣 GiftBalance
    /// 并发防护：通过乐观锁校验余额，扣减时若账户已被并发修改导致余额不足，抛异常让调用方回滚
    /// </summary>
    public async Task<StoredValueConsumeResult> ConsumeAsync(
        long customerId, long tenantId, string tenantCode,
        long storeId, string storeCode,
        decimal amount, long orderId, string orderNo,
        int? payMethod, DateTime now)
    {
        if (amount <= 0)
            return new StoredValueConsumeResult { Success = false, ErrorMessage = "消费金额必须大于0" };

        var account = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.TenantId == tenantId && !a.IsDeleted);
        if (account == null)
            return new StoredValueConsumeResult { Success = false, ErrorMessage = "客户储值账户不存在" };

        // 余额校验：不足直接抛错让事务回滚（P-SV-01 修复）
        if (account.Balance < amount)
            return new StoredValueConsumeResult
            {
                Success = false,
                ErrorMessage = $"储值余额不足（当前 {account.Balance:F2}，需要 {amount:F2}）",
                BeforeBalance = account.Balance
            };

        var beforeBalance = account.Balance;
        var beforeRealBalance = account.RealBalance;
        var beforeGiftBalance = account.GiftBalance;

        // 先扣实收余额，再扣赠送余额
        var realDeduct = Math.Min(account.RealBalance, amount);
        var giftDeduct = amount - realDeduct;

        account.RealBalance -= realDeduct;
        account.GiftBalance -= giftDeduct;
        account.Balance -= amount;
        account.TotalConsume += amount;
        account.UpdatedTime = now;

        _dbContext.StoredValueLogs.Add(new StoredValueLog
        {
            CustomerId = account.CustomerId,
            Type = 2, // 消费
            Amount = -amount,
            RealAmount = -realDeduct,
            GiftAmount = -giftDeduct,
            BeforeBalance = beforeBalance,
            AfterBalance = account.Balance,
            RealBalanceChange = -realDeduct,
            GiftBalanceChange = -giftDeduct,
            BeforeRealBalance = beforeRealBalance,
            AfterRealBalance = account.RealBalance,
            BeforeGiftBalance = beforeGiftBalance,
            AfterGiftBalance = account.GiftBalance,
            OrderId = orderId,
            PayMethod = payMethod,
            Remark = $"储值消费-{orderNo}",
            OperatorId = _currentUser.UserId,
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            StoreCode = storeCode,
            CreatedTime = now
        });

        return new StoredValueConsumeResult
        {
            Success = true,
            RealDeduct = realDeduct,
            GiftDeduct = giftDeduct,
            BeforeBalance = beforeBalance,
            AfterBalance = account.Balance
        };
    }
}
