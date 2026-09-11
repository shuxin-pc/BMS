using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
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
    private readonly IAuditLogContext _auditLogContext;

    public StoredValueAccountAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StoredValueAccountCreateDto> createValidator,
        IValidator<StoredValueAccountUpdateDto> updateValidator,
        ICrossStoreOperationAuditService auditService,
        IPointsRuleService pointsRuleService,
        IStoredValueGiftRuleService giftRuleService,
        IAuditLogContext auditLogContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditService = auditService;
        _pointsRuleService = pointsRuleService;
        _giftRuleService = giftRuleService;
        _auditLogContext = auditLogContext;
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
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(x => x.Customer != null && (x.Customer.Name.Contains(query.Keyword) || x.Customer.Phone.Contains(query.Keyword)));

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
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "储值充值";

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
                    // 累加充值发积分快照（储值退款时按退款比例扣回使用；发积分为 0 时不累加）
                    account.AwardedPoints += points;
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
    /// 现金部分只冲减实收余额(RealBalance)，赠送余额不退现金（文档 G7）但按充值流水 LIFO 比例冲销（防白嫖）
    /// 同步客户档案 Customer.Balance（口径为账户总余额，含赠送）
    /// 退款流水 StoreId 记录为原充值门店（或当前操作门店若原门店已关店）
    /// </summary>
    public async Task<ApiResponseDto<StoredValueAccountDto>> RefundAsync(StoredValueRefundDto dto)
    {
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "储值退款";

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

            // 文档 G7：退款只退剩余实收，赠送余额一律不退，故退款上限为实收余额而非总余额
            if (account.RealBalance < dto.Amount)
                return ApiResponseDto<StoredValueAccountDto>.Fail($"储值实收余额不足（当前实收 {account.RealBalance:F2}，需退款 {dto.Amount:F2}；赠送余额一律不退）", 400);

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

            var actions = new List<string>();
            // 扣回充值发放积分（文档 G7 储值退款规定）：按退款/累计充值比例，积分不足折算现金从退款扣除
            var pointsCashDeduction = await RefundAwardedPointsAsync(account, dto.Amount, actions, now);
            var actualRefundAmount = Math.Max(0m, dto.Amount - pointsCashDeduction);

            // 赠送余额冲销（防白嫖）：赠送不可退现金（文档 G7）但仍可在其他消费中抵扣，
            // 退款时按充值流水 LIFO（最近充值优先）比例收回对应赠送，避免「退款后白拿赠送抵扣额度」
            // 冲销基数用实际退还款额（积分折现后），真实退多少钱才收回多少赠送
            var giftWriteOff = await CalculateGiftWriteOffAsync(account, actualRefundAmount, actions);

            var beforeBalance = account.Balance;
            var beforeRealBalance = account.RealBalance;
            var beforeGiftBalance = account.GiftBalance;

            // 退款扣减：现金部分只退实收余额（文档 G7：赠送余额一律不退）；
            // 赠送余额按 LIFO 比例冲销（防白嫖），实收与赠送同步扣减，总余额=实收+赠送 亦随之减少
            // 退款金额已在上方校验 ≤ 实收余额，积分折算后更小，实收扣减必然足够
            var realDeduct = actualRefundAmount;
            var giftDeduct = giftWriteOff;

            account.RealBalance -= realDeduct;
            account.GiftBalance -= giftDeduct;
            account.Balance -= realDeduct + giftDeduct;
            account.UpdatedTime = now;

            // 同步客户档案余额：口径与储值账户总余额一致（实收+赠送）
            var refundCustomer = await FindCustomerAsync(account.CustomerId, tenantId);
            if (refundCustomer != null)
            {
                refundCustomer.Balance = account.Balance;
                refundCustomer.UpdatedTime = now;
            }

            // 记录退款流水（Type=3 退款），StoreId 为原充值门店（或当前操作门店若原门店已关店）
            // 文档 G7：赠送余额不退现金故 GiftAmount 恒为 0；GiftBalanceChange 记录 LIFO 冲销（负值）
            // Amount 口径 = 实收扣减 + 赠送冲销，与总余额变化一致（现金流统计 TotalRefund 仍以 Amount 汇总）
            _dbContext.StoredValueLogs.Add(new StoredValueLog
            {
                CustomerId = dto.CustomerId,
                Type = 3, // 退款
                Amount = -(realDeduct + giftDeduct),
                RealAmount = -realDeduct,
                GiftAmount = 0m,
                BeforeBalance = beforeBalance,
                AfterBalance = account.Balance,
                RealBalanceChange = -realDeduct,
                GiftBalanceChange = -giftDeduct,
                BeforeRealBalance = beforeRealBalance,
                AfterRealBalance = account.RealBalance,
                BeforeGiftBalance = beforeGiftBalance,
                AfterGiftBalance = account.GiftBalance,
                PayMethod = dto.PayMethod,
                Remark = actions.Count > 0
                    ? $"{dto.Remark ?? "储值退款"}；{string.Join("；", actions)}"
                    : (dto.Remark ?? "储值退款"),
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
                RelatedEntitySnapshot = $"{{\"RefundAmount\":{actualRefundAmount:F2},\"PointsCashDeduction\":{pointsCashDeduction:F2},\"RealDeduct\":{realDeduct:F2},\"GiftDeduct\":{giftDeduct:F2},\"RefundStoreId\":{refundStoreId},\"OriginalStoreClosed\":{originalStoreClosed.ToString().ToLower()}}}",
                Remark = actions.Count > 0
                    ? $"储值退款-{dto.Remark ?? string.Empty}；{string.Join("；", actions)}"
                    : $"储值退款-{dto.Remark ?? string.Empty}",
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

    /// <summary>
    /// 储值退款扣回充值发放积分（文档 G7 储值退款规定 + G5.3 退费场景积分处理）：
    /// 按退款/累计充值比例扣回 AwardedPoints 快照，积分不足时按当前生效 DeductRate 折算现金从退款扣除，
    /// 写 CustomerPointsLog(Type=RefundDeduct)；与订单 RefundPointsAsync 算法一致
    /// </summary>
    /// <param name="account">储值账户</param>
    /// <param name="refundAmount">申请退款金额（未扣积分折现前的原始退款）</param>
    /// <param name="actions">回退明细记录（写入退款流水与审计日志）</param>
    /// <param name="now">当前时间</param>
    /// <returns>积分不足折算扣除的现金金额（0 表示无折算）</returns>
    private async Task<decimal> RefundAwardedPointsAsync(StoredValueAccount account, decimal refundAmount, List<string> actions, DateTime now)
    {
        if (account.AwardedPoints <= 0 || account.TotalRecharge <= 0 || refundAmount <= 0)
            return 0;

        var customer = await FindCustomerAsync(account.CustomerId, account.TenantId);
        if (customer == null)
        {
            actions.Add("未找到客户，跳过充值积分扣回");
            return 0;
        }

        // 按退款/累计充值比例扣回充值发积分（对齐订单 RefundPointsAsync 算法，四舍五入）
        var ratio = refundAmount / account.TotalRecharge;
        var pointsToDeduct = -(int)Math.Round(account.AwardedPoints * ratio);
        if (pointsToDeduct == 0) return 0;

        // 积分不足：扣全部剩余积分，差额按当前生效 DeductRate 折算现金从退款扣除
        // 储值账户未保存 DeductRate 快照（与订单不同），故使用当前生效规则（与项目卡 RefundAwardedPointsAsync 口径一致）
        if (customer.TotalPoints + pointsToDeduct < 0)
        {
            var rule = await _pointsRuleService.GetEffectivePointsRuleAsync(account.TenantId, account.StoreId);
            var deductRate = rule?.DeductRate ?? 0m;

            var shortfallPoints = Math.Abs(pointsToDeduct) - customer.TotalPoints;
            var cashDeduction = shortfallPoints * deductRate;

            var beforePoints = customer.TotalPoints;
            customer.TotalPoints = 0;
            customer.UpdatedTime = now;

            _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
            {
                CustomerId = customer.Id,
                Type = CustomerPointsLogType.RefundDeduct, // 退款扣减
                Points = -beforePoints,
                BeforePoints = beforePoints,
                AfterPoints = 0,
                OperatorId = _currentUser.UserId,
                Remark = $"储值退款(AccountId={account.Id}) 扣回充值积分（积分不足，扣减全部剩余 {beforePoints} 积分，不足 {shortfallPoints} 积分折算现金 {cashDeduction:F2} 元从退款扣除）",
                TenantId = account.TenantId,
                TenantCode = account.TenantCode,
                StoreId = account.StoreId,
                StoreCode = account.StoreCode,
                CreatedTime = now
            });

            actions.Add($"客户 ID:{customer.Id} 积分不足，扣减全部剩余 {beforePoints} 积分，不足 {shortfallPoints} 积分折算现金 {cashDeduction:F2} 元从退款扣除");
            return cashDeduction;
        }

        // 积分充足：正常扣减
        var beforePointsNormal = customer.TotalPoints;
        customer.TotalPoints += pointsToDeduct;
        customer.UpdatedTime = now;

        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
        {
            CustomerId = customer.Id,
            Type = CustomerPointsLogType.RefundDeduct, // 退款扣减
            Points = pointsToDeduct,
            BeforePoints = beforePointsNormal,
            AfterPoints = customer.TotalPoints,
            OperatorId = _currentUser.UserId,
            Remark = $"储值退款(AccountId={account.Id}) 扣回充值积分",
            TenantId = account.TenantId,
            TenantCode = account.TenantCode,
            StoreId = account.StoreId,
            StoreCode = account.StoreCode,
            CreatedTime = now
        });

        actions.Add($"客户 ID:{customer.Id} 扣减充值积分 {pointsToDeduct}");
        return 0;
    }

    /// <summary>
    /// 储值退款冲销赠送余额（防白嫖）：
    /// 赠送余额不可退现金（文档 G7）但仍可在其他消费中抵扣，退款若不冲销则形成「白拿赠送抵扣额度」漏洞。
    /// 按充值流水 LIFO（最近充值优先）比例冲销：用退款金额依次覆盖最近的充值笔次，
    /// 每笔冲销 w = min(剩余待覆盖, 该笔实收)，比例 p = 该笔赠送/该笔实收，赠送冲销累计 = Σ(w × p)，
    /// 最终取 min(累计, 当前赠送余额) 避免负余额，金额保留 2 位小数（四舍五入，对齐项目金额舍入惯例）
    /// </summary>
    /// <param name="account">储值账户</param>
    /// <param name="refundAmount">实际退款金额（积分折现后的实收退款，真实退多少钱才收回多少赠送）</param>
    /// <param name="actions">回退明细记录（写入退款流水与审计日志）</param>
    /// <returns>应冲销的赠送金额（0 表示无需冲销）</returns>
    private async Task<decimal> CalculateGiftWriteOffAsync(StoredValueAccount account, decimal refundAmount, List<string> actions)
    {
        if (account.GiftBalance <= 0 || refundAmount <= 0)
            return 0;

        // 查询充值流水（Type=1），按创建时间倒序实现 LIFO（最近充值优先）
        var rechargeLogs = await _dbContext.StoredValueLogs
            .Where(l => l.CustomerId == account.CustomerId && l.TenantId == account.TenantId && l.Type == 1)
            .OrderByDescending(l => l.CreatedTime)
            .ToListAsync();

        var remaining = refundAmount;
        decimal accumulated = 0m;
        foreach (var log in rechargeLogs)
        {
            if (remaining <= 0) break;
            if (log.RealAmount <= 0) continue; // 异常流水（无实收），跳过避免除零

            // 该笔充值被退款覆盖的实收金额，乘以该笔赠送比例得到应冲销的赠送
            var w = Math.Min(remaining, log.RealAmount);
            accumulated += w * (log.GiftAmount / log.RealAmount);
            remaining -= w;
        }

        // 冲销不超过当前赠送余额，避免负余额；保留 2 位小数
        var giftWriteOff = Math.Round(Math.Min(accumulated, account.GiftBalance), 2, MidpointRounding.AwayFromZero);
        if (giftWriteOff > 0)
        {
            actions.Add($"按充值流水LIFO比例冲销赠送余额 {giftWriteOff:F2} 元（赠送不可退现金，冲销防白嫖）");
        }
        return giftWriteOff;
    }
}
