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
    /// 视角判定（文档 4.3）：指定 CustomerId 时为客户视角（跨店消费历史需完整可见，不按 StoreId 过滤）；
    /// 未指定 CustomerId 时为门店视角（门店充值/消费业绩，按当前门店过滤）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StoredValueLogDto>>> GetPagedListAsync(StoredValueLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StoredValueLogDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var logQueryable = _dbContext.StoredValueLogs.Where(l => l.TenantId == tenantId);

        if (query.CustomerId.HasValue)
        {
            logQueryable = logQueryable.Where(l => l.CustomerId == query.CustomerId.Value);
        }
        else
        {
            // 门店视角：只统计本门店发生的交易流水
            if (!_currentUser.StoreId.HasValue)
                return ApiResponseDto<PagedResponseDto<StoredValueLogDto>>.Fail("无法确定当前门店", 400);
            var storeId = _currentUser.StoreId.Value;
            logQueryable = logQueryable.Where(l => l.StoreId == storeId);
        }

        // 左连接客户档案取姓名/手机号：流水表只存 CustomerId，列表需展示客户身份并支持按姓名/手机号检索
        // 用左连接而非内连接，避免客户被软删除后历史流水从列表里消失
        var queryable =
            from l in logQueryable
            join c in _dbContext.Customers.Where(c => !c.IsDeleted && c.TenantId == tenantId)
                on l.CustomerId equals c.Id into customers
            from c in customers.DefaultIfEmpty()
            select new { Log = l, Customer = c };

        if (query.Type.HasValue)
            queryable = queryable.Where(x => x.Log.Type == query.Type.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(x => x.Customer != null &&
                (x.Customer.Name.Contains(query.Keyword) || x.Customer.Phone.Contains(query.Keyword)));
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.Log.CreatedTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            // EndDate 含当日：过滤条件为 CreatedTime <= EndDate 当天 23:59:59
            queryable = queryable.Where(x => x.Log.CreatedTime <= query.EndDate.Value.Date.AddDays(1).AddTicks(-1));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.Log.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StoredValueLogDto>
        {
            List = items.Select(x => ToDto(x.Log, x.Customer)).ToList(),
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
            return ApiResponseDto<StoredValueLogDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.StoredValueLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<StoredValueLogDto?>.Fail("储值流水不存在", 404);
        var customer = await FindCustomerAsync(entity.CustomerId, _currentUser.TenantId.Value);
        return ApiResponseDto<StoredValueLogDto?>.Ok(ToDto(entity, customer));
    }

    /// <summary>
    /// 创建储值流水
    /// </summary>
    public async Task<ApiResponseDto<StoredValueLogDto>> CreateAsync(StoredValueLogCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueLogDto>.Fail("登录状态异常，请重新登录", 401);

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
        // 姓名快照只在补录人就是当前登录用户时可信；代录他人时无法取到对方姓名，留空由前端显示占位符
        entity.OperatorName = dto.OperatorId == _currentUser.UserId
            ? _currentUser.RealName ?? _currentUser.UserName
            : null;
        entity.CreatedTime = DateTime.Now;

        _dbContext.StoredValueLogs.Add(entity);
        await _dbContext.SaveChangesAsync();
        var customer = await FindCustomerAsync(entity.CustomerId, entity.TenantId);
        return ApiResponseDto<StoredValueLogDto>.Ok(ToDto(entity, customer), "创建成功");
    }

    /// <summary>
    /// 更新储值流水
    /// </summary>
    public async Task<ApiResponseDto<StoredValueLogDto>> UpdateAsync(StoredValueLogUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueLogDto>.Fail("登录状态异常，请重新登录", 401);

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
        var customer = await FindCustomerAsync(entity.CustomerId, tenantId);
        return ApiResponseDto<StoredValueLogDto>.Ok(ToDto(entity, customer), "更新成功");
    }

    /// <summary>
    /// 删除储值流水（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

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
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
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
    /// 储值现金流统计（G7.3）：按日期范围统计新增储值/消费/退款；沉淀资金取结束日期当天的时点余额
    /// </summary>
    public async Task<ApiResponseDto<StoredValueCashFlowDto>> GetCashFlowAsync(DateTime? startDate, DateTime? endDate)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StoredValueCashFlowDto>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 前端传入的结束日期为纯日期（时间部分为 00:00:00），需按"次日零点前"闭区间处理，
        // 否则结束日期当天产生的流水会被全部漏掉
        var endExclusive = endDate?.Date.AddDays(1);

        var logsQuery = _dbContext.StoredValueLogs
            .Where(l => l.TenantId == tenantId);

        // 区间流水：统计新增储值/消费/退款
        var rangeQuery = logsQuery;
        if (startDate.HasValue)
            rangeQuery = rangeQuery.Where(l => l.CreatedTime >= startDate.Value.Date);
        if (endExclusive.HasValue)
            rangeQuery = rangeQuery.Where(l => l.CreatedTime < endExclusive.Value);

        var flowStats = await rangeQuery
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
        // Type=3 退款：Amount 为负数（余额退出），取负得到退款金额
        var refundStat = flowStats.FirstOrDefault(s => s.Type == 3);

        // 沉淀资金：余额是时点值，只受结束日期约束（起始日期对余额无意义）
        // 账户表只存当前余额、无历史快照，因此按流水累加还原时点余额：
        // 账户初始余额为 0 且每次余额变动都会写流水，故截至某时点的累加值即该时点余额
        var balanceQuery = logsQuery;
        if (endExclusive.HasValue)
            balanceQuery = balanceQuery.Where(l => l.CreatedTime < endExclusive.Value);

        var balanceStats = await balanceQuery
            .GroupBy(l => 1)
            .Select(g => new
            {
                TotalBalance = g.Sum(l => l.Amount),
                TotalRealBalance = g.Sum(l => l.RealBalanceChange),
                TotalGiftBalance = g.Sum(l => l.GiftBalanceChange)
            })
            .FirstOrDefaultAsync();

        var result = new StoredValueCashFlowDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRecharge = rechargeStat?.TotalRealAmount ?? 0m,
            TotalGift = rechargeStat?.TotalGiftAmount ?? 0m,
            TotalConsume = -(consumeStat?.TotalAmount ?? 0m),
            TotalRefund = -(refundStat?.TotalAmount ?? 0m),
            TotalBalance = balanceStats?.TotalBalance ?? 0m,
            TotalRealBalance = balanceStats?.TotalRealBalance ?? 0m,
            TotalGiftBalance = balanceStats?.TotalGiftBalance ?? 0m
        };

        return ApiResponseDto<StoredValueCashFlowDto>.Ok(result);
    }

    /// <summary>
    /// 查询客户档案（用于回填流水上的客户姓名/手机号）
    /// </summary>
    private Task<Customer?> FindCustomerAsync(long customerId, long tenantId)
        => _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId && !c.IsDeleted);

    /// <summary>
    /// 实体转 DTO（手动映射时间字段与客户信息）
    /// </summary>
    private static StoredValueLogDto ToDto(StoredValueLog entity, Customer? customer)
    {
        var dto = entity.Adapt<StoredValueLogDto>();
        dto.CreatedAt = entity.CreatedTime;
        dto.UpdatedAt = entity.UpdatedTime;
        dto.CustomerName = customer?.Name;
        dto.Phone = customer?.Phone;
        return dto;
    }
}
