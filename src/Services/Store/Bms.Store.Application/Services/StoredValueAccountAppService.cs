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
    private readonly ICrossStoreOperationAuditService _auditService;
    private readonly IPointsRuleService _pointsRuleService;
    private readonly IStoredValueGiftRuleService _giftRuleService;

    public StoredValueAccountAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StoredValueAccountCreateDto> createValidator,
        IValidator<StoredValueAccountUpdateDto> updateValidator,
        ICrossStoreOperationAuditService auditService,
        IPointsRuleService pointsRuleService,
        IStoredValueGiftRuleService giftRuleService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditService = auditService;
        _pointsRuleService = pointsRuleService;
        _giftRuleService = giftRuleService;
    }

    /// <summary>
    /// 获取储值账户分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueAccountDto>>> GetPagedListAsync(StoredValueAccountQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StoredValueAccountDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        // 左连接客户档案取名称/手机号：账户表只存 CustomerId，列表需要展示客户身份并支持按姓名/手机号检索
        // 用左连接而非内连接，避免客户被软删除后账户余额从列表里消失
        var queryable =
            from a in _dbContext.StoredValueAccounts
                .Where(a => !a.IsDeleted && a.TenantId == tenantId && a.StoreId == storeId)
            join c in _dbContext.Customers.Where(c => !c.IsDeleted && c.TenantId == tenantId)
                on a.CustomerId equals c.Id into customers
            from c in customers.DefaultIfEmpty()
            select new { Account = a, Customer = c };

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.Account.CustomerId == query.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(query.CustomerName))
            queryable = queryable.Where(x => x.Customer != null && x.Customer.Name.Contains(query.CustomerName));
        if (!string.IsNullOrWhiteSpace(query.Phone))
            queryable = queryable.Where(x => x.Customer != null && x.Customer.Phone.Contains(query.Phone));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.Account.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StoredValueAccountDto>
        {
            List = items.Select(x => ToDto(x.Account, x.Customer)).ToList(),
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
            return ApiResponseDto<StoredValueAccountDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.TenantId == _currentUser.TenantId.Value && a.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<StoredValueAccountDto?>.Fail("储值账户不存在", 404);

        var customer = await FindCustomerAsync(entity.CustomerId, _currentUser.TenantId.Value);
        return ApiResponseDto<StoredValueAccountDto?>.Ok(ToDto(entity, customer));
    }

    /// <summary>
    /// 创建储值账户
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> CreateAsync(StoredValueAccountCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("登录状态异常，请重新登录", 401);

        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("无法确定当前门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueAccountDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var entity = dto.Adapt<StoredValueAccount>();
        entity.TenantId = _currentUser.TenantId.Value;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = _currentUser.StoreId.Value;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.StoredValueAccounts.Add(entity);
        await _dbContext.SaveChangesAsync();

        var customer = await FindCustomerAsync(entity.CustomerId, entity.TenantId);
        return ApiResponseDto<StoredValueAccountDto>.Ok(ToDto(entity, customer), "创建成功");
    }

    /// <summary>
    /// 更新储值账户
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> UpdateAsync(StoredValueAccountUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StoredValueAccountDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.Id == dto.Id && !a.IsDeleted && a.TenantId == tenantId && a.StoreId == storeId);
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

        var customer = await FindCustomerAsync(entity.CustomerId, tenantId);
        return ApiResponseDto<StoredValueAccountDto>.Ok(ToDto(entity, customer), "更新成功");
    }

    /// <summary>
    /// 删除储值账户（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted && a.TenantId == _currentUser.TenantId.Value && a.StoreId == (_currentUser.StoreId ?? 0));
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
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.StoredValueAccounts
            .Where(a => ids.Contains(a.Id) && !a.IsDeleted && a.TenantId == _currentUser.TenantId.Value && a.StoreId == (_currentUser.StoreId ?? 0))
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
    /// <param name="entity">储值账户实体</param>
    /// <param name="customer">关联客户，用于填充名称/手机号，客户已删除时传 null</param>
    private static StoredValueAccountDto ToDto(StoredValueAccount entity, Customer? customer)
    {
        var dto = entity.Adapt<StoredValueAccountDto>();
        dto.CreatedAt = entity.CreatedTime;
        dto.UpdatedAt = entity.UpdatedTime;
        dto.CustomerName = customer?.Name;
        dto.Phone = customer?.Phone;
        return dto;
    }

    /// <summary>
    /// 查询账户关联的客户，用于填充单个 DTO 的客户名称/手机号
    /// 列表接口走左连接一次取回，单条返回场景需要单独查一次
    /// </summary>
    private Task<Customer?> FindCustomerAsync(long customerId, long tenantId)
        => _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted && c.TenantId == tenantId);

    /// <summary>
    /// 储值充值（独立于订单系统，由操作人员手动录入金额）
    /// 充值发积分，按积分规则 PointsRate 计算，Floor 取整
    /// 同步客户档案 Customer.Balance（口径为账户总余额，含赠送）
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> RechargeAsync(StoredValueRechargeDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("登录状态异常，请重新登录", 401);

        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("无法确定当前门店", 401);

        if (dto.Amount <= 0)
            return ApiResponseDto<StoredValueAccountDto>.Fail("充值金额必须大于0", 400);

        if (dto.CustomerId <= 0)
            return ApiResponseDto<StoredValueAccountDto>.Fail("客户ID无效", 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;
        var storeCode = _currentUser.StoreCode ?? string.Empty;
        var now = DateTime.Now;

        // 赠送金额一律由储值规则计算（档位整倍叠加口径），不接受前端传入
        var giftAmount = await _giftRuleService.CalculateGiftAmountAsync(tenantId, dto.Amount, now);

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
                    StoreId = storeId,
                    StoreCode = storeCode,
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

            // 客户档案：余额同步、生日双倍积分判断、审计日志都需要客户信息，统一查一次复用
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.TenantId == tenantId);

            // 同步客户档案余额：口径与储值账户总余额一致（实收+赠送）
            if (customer != null)
            {
                customer.Balance = account.Balance;
                customer.UpdatedTime = now;
            }

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
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                StoreCode = storeCode,
                CreatedTime = now
            });

            // 充值发积分（按积分规则 PointsRate 计算，Floor 取整，含生日当天双倍）
            // 规则按门店精确匹配，门店未配置积分规则则本次充值不发积分
            var pointsRule = await _pointsRuleService.GetEffectivePointsRuleAsync(tenantId, storeId);
            if (pointsRule != null && customer != null)
            {
                // 计算应发积分（含生日当天双倍）
                var points = _pointsRuleService.CalculateAwardPoints(pointsRule, customer.Birthday, dto.Amount, now);
                if (points > 0)
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
                        StoreId = storeId,
                        StoreCode = storeCode,
                        CreatedTime = now
                    });
                }
            }

            // 阶段6：跨店充值审计日志（文档 6.1 节）
            // 记录操作门店、操作员、IP、账户开户门店
            // 仅当充值门店 ≠ 开户门店时为跨店充值
            await _auditService.LogAsync(new CrossStoreOperationLog
            {
                OperationType = "CrossStoreRecharge",
                OperatorId = _currentUser.UserId ?? 0,
                OperatorName = _currentUser.RealName,
                OperationTime = now,
                CustomerId = dto.CustomerId,
                CustomerName = customer?.Name,
                CustomerPhoneTail = customer?.Phone?.Length >= 4
                    ? customer.Phone[^4..]
                    : customer?.Phone,
                HomeStoreId = account.StoreId,
                IsCrossStore = storeId != account.StoreId,
                RelatedEntityId = account.Id,
                RelatedEntitySnapshot = $"{{\"Amount\":{dto.Amount:F2},\"GiftAmount\":{giftAmount:F2},\"BeforeBalance\":{beforeBalance:F2},\"AfterBalance\":{account.Balance:F2}}}",
                Remark = $"储值充值-{dto.Remark ?? string.Empty}",
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                StoreCode = storeCode
            });

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<StoredValueAccountDto>.Ok(ToDto(account, customer), "充值成功");
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
    /// 同步客户档案 Customer.Balance（口径为账户总余额，含赠送）
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

        // 同步客户档案余额：口径与储值账户总余额一致（实收+赠送）
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId);
        if (customer != null)
        {
            customer.Balance = account.Balance;
            customer.UpdatedTime = now;
        }

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
            OperatorName = _currentUser.RealName ?? _currentUser.UserName,
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            StoreCode = storeCode,
            CreatedTime = now
        });

        // 阶段6：跨店消费审计日志（文档 6.1 节）
        // 记录操作门店、操作员、IP、客户身份核验记录、账户信息
        // 仅当消费门店 ≠ 开户门店时为跨店消费
        await _auditService.LogAsync(new CrossStoreOperationLog
        {
            OperationType = "CrossStoreConsume",
            OperatorId = _currentUser.UserId ?? 0,
            OperatorName = _currentUser.RealName,
            OperationTime = now,
            CustomerId = account.CustomerId,
            HomeStoreId = account.StoreId,
            IsCrossStore = storeId != account.StoreId,
            RelatedEntityId = orderId,
            RelatedEntitySnapshot = $"{{\"Amount\":{amount:F2},\"RealDeduct\":{realDeduct:F2},\"GiftDeduct\":{giftDeduct:F2},\"BeforeBalance\":{beforeBalance:F2},\"AfterBalance\":{account.Balance:F2}}}",
            Remark = $"储值消费-{orderNo}",
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            StoreCode = storeCode
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

    /// <summary>
    /// 储值退款（规则8）
    /// 冲减原充值发生门店的充值业绩；门店关店则冲减当前操作门店
    /// 退款金额冲减实收余额(RealBalance)，不足冲减赠送余额(GiftBalance)
    /// 同步客户档案 Customer.Balance（口径为账户总余额，含赠送）
    /// 退款流水 StoreId 记录为原充值门店（或当前操作门店若原门店已关店）
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> RefundAsync(StoredValueRefundDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("登录状态异常，请重新登录", 401);

        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<StoredValueAccountDto>.Fail("无法确定当前门店", 401);

        if (dto.Amount <= 0)
            return ApiResponseDto<StoredValueAccountDto>.Fail("退款金额必须大于0", 400);

        if (dto.CustomerId <= 0)
            return ApiResponseDto<StoredValueAccountDto>.Fail("客户ID无效", 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var operationStoreId = _currentUser.StoreId.Value;
        var now = DateTime.Now;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var account = await _dbContext.StoredValueAccounts
                .FirstOrDefaultAsync(a => a.CustomerId == dto.CustomerId && a.TenantId == tenantId && !a.IsDeleted);
            if (account == null)
                return ApiResponseDto<StoredValueAccountDto>.Fail("客户储值账户不存在", 404);

            if (account.Balance < dto.Amount)
                return ApiResponseDto<StoredValueAccountDto>.Fail($"储值余额不足（当前 {account.Balance:F2}，需退款 {dto.Amount:F2}）", 400);

            // 规则8：退款冲减原充值门店充值业绩
            // 查询首次充值流水，确定原充值门店
            var firstRechargeLog = await _dbContext.StoredValueLogs
                .Where(l => l.CustomerId == dto.CustomerId && l.TenantId == tenantId && l.Type == 1 && l.StoreId > 0)
                .OrderBy(l => l.CreatedTime)
                .FirstOrDefaultAsync();

            long refundStoreId;
            string refundStoreCode;
            bool originalStoreClosed = false;

            if (firstRechargeLog != null)
            {
                // 检查原充值门店是否仍在营业
                var originalStore = await _dbContext.Stores
                    .FirstOrDefaultAsync(s => s.Id == firstRechargeLog.StoreId && s.TenantId == tenantId && !s.IsDeleted);
                if (originalStore != null && originalStore.Status == 1)
                {
                    refundStoreId = originalStore.Id;
                    refundStoreCode = originalStore.Code;
                }
                else
                {
                    // 原充值门店关店，冲减当前操作门店
                    originalStoreClosed = true;
                    refundStoreId = operationStoreId;
                    var opStore = await _dbContext.Stores
                        .FirstOrDefaultAsync(s => s.Id == operationStoreId && s.TenantId == tenantId);
                    refundStoreCode = opStore?.Code ?? string.Empty;
                }
            }
            else
            {
                // 无充值流水记录（异常情况），使用当前操作门店
                refundStoreId = operationStoreId;
                var opStore = await _dbContext.Stores
                    .FirstOrDefaultAsync(s => s.Id == operationStoreId && s.TenantId == tenantId);
                refundStoreCode = opStore?.Code ?? string.Empty;
            }

            var beforeBalance = account.Balance;
            var beforeRealBalance = account.RealBalance;
            var beforeGiftBalance = account.GiftBalance;

            // 退款扣减：先扣实收余额，再扣赠送余额（与消费扣减一致）
            var realDeduct = Math.Min(account.RealBalance, dto.Amount);
            var giftDeduct = dto.Amount - realDeduct;

            account.RealBalance -= realDeduct;
            account.GiftBalance -= giftDeduct;
            account.Balance -= dto.Amount;
            account.UpdatedTime = now;

            // 同步客户档案余额：口径与储值账户总余额一致（实收+赠送）
            var refundCustomer = await FindCustomerAsync(account.CustomerId, tenantId);
            if (refundCustomer != null)
            {
                refundCustomer.Balance = account.Balance;
                refundCustomer.UpdatedTime = now;
            }

            // 记录退款流水（Type=3 退款），StoreId 为原充值门店（或当前操作门店若原门店已关店）
            _dbContext.StoredValueLogs.Add(new StoredValueLog
            {
                CustomerId = dto.CustomerId,
                Type = 3, // 退款
                Amount = -dto.Amount,
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
                PayMethod = dto.PayMethod,
                Remark = dto.Remark ?? "储值退款",
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = refundStoreId,
                StoreCode = refundStoreCode,
                CreatedTime = now
            });

            // 写入退款审计日志
            await _auditService.LogAsync(new CrossStoreOperationLog
            {
                OperationType = "StoredValueRefund",
                OperatorId = _currentUser.UserId ?? 0,
                OperatorName = _currentUser.RealName,
                OperationTime = now,
                CustomerId = dto.CustomerId,
                HomeStoreId = account.StoreId,
                IsCrossStore = refundStoreId != account.StoreId,
                RelatedEntityId = account.Id,
                RelatedEntitySnapshot = $"{{\"RefundAmount\":{dto.Amount:F2},\"RealDeduct\":{realDeduct:F2},\"GiftDeduct\":{giftDeduct:F2},\"RefundStoreId\":{refundStoreId},\"OriginalStoreClosed\":{originalStoreClosed.ToString().ToLower()}}}",
                Remark = $"储值退款-{dto.Remark ?? string.Empty}",
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = refundStoreId,
                StoreCode = refundStoreCode
            });

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponseDto<StoredValueAccountDto>.Ok(ToDto(account, refundCustomer), "退款成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
