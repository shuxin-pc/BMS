using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using TreatmentCardSaleEntity = Bms.Store.Domain.Entities.TreatmentCardSale;
using TreatmentCardSaleItemEntity = Bms.Store.Domain.Entities.TreatmentCardSaleItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 项目卡销售记录应用服务实现
/// </summary>
public class TreatmentCardSaleAppService : ITreatmentCardSaleAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TreatmentCardSaleCreateDto> _createValidator;
    private readonly IValidator<TreatmentCardSaleUpdateDto> _updateValidator;
    private readonly ICrossStoreOperationAuditService _auditService;
    private readonly IPointsRuleService _pointsRuleService;
    private readonly IStoredValueAccountAppService _storedValueAccountAppService;
    private readonly IPointsDeductionService _pointsDeductionService;
    private readonly IAuditLogContext _auditLogContext;

    public TreatmentCardSaleAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TreatmentCardSaleCreateDto> createValidator,
        IValidator<TreatmentCardSaleUpdateDto> updateValidator,
        ICrossStoreOperationAuditService auditService,
        IPointsRuleService pointsRuleService,
        IStoredValueAccountAppService storedValueAccountAppService,
        IPointsDeductionService pointsDeductionService,
        IAuditLogContext auditLogContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditService = auditService;
        _pointsRuleService = pointsRuleService;
        _storedValueAccountAppService = storedValueAccountAppService;
        _pointsDeductionService = pointsDeductionService;
        _auditLogContext = auditLogContext;
    }

    /// <summary>
    /// 获取项目卡销售记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardSaleDto>>> GetPagedListAsync(TreatmentCardSaleQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TreatmentCardSaleDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.TreatmentCardSales
            .Where(s => s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(s => s.CustomerId == query.CustomerId.Value);
        if (query.CardId.HasValue)
            queryable = queryable.Where(s => s.CardId == query.CardId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(s => s.Status == query.Status.Value);

        // 按卡名称筛选（子查询 join TreatmentCard 表）
        if (!string.IsNullOrWhiteSpace(query.CardName))
        {
            var cardIds = _dbContext.TreatmentCards
                .Where(c => c.TenantId == tenantId && c.Name.Contains(query.CardName))
                .Select(c => c.Id);
            queryable = queryable.Where(s => cardIds.Contains(s.CardId));
        }

        // 客户名称/手机号合并关键字查询：命中姓名或手机号其一即满足（对齐预约列表）
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keywordCustomerIds = _dbContext.Customers
                .Where(c => c.Name.Contains(query.Keyword) || c.Phone.Contains(query.Keyword))
                .Select(c => c.Id);
            queryable = queryable.Where(s => keywordCustomerIds.Contains(s.CustomerId));
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            // 加载项目明细：核销流程（快速开单核销弹窗/核销页）依赖 Sale.Items 选择核销项目，
            // 与 GetByIdAsync 的 Include 保持一致，否则分页查询返回的 Items 为空
            .Include(s => s.Items)
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = items.Adapt<List<TreatmentCardSaleDto>>();
        // 批量填充客户名称/手机号/卡名称/购买总次数（join Customer/TreatmentCard，避免 N+1）
        await FillSaleDisplayInfoAsync(dtos, tenantId);

        var result = new PagedResponseDto<TreatmentCardSaleDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardSaleDto>>.Ok(result);
    }

    /// <summary>
    /// 批量填充销售记录的客户名称/手机号/卡名称/购买总次数（join Customer/TreatmentCard，避免 N+1）
    /// </summary>
    /// <param name="dtos">销售记录 DTO 列表（含 Items 明细）</param>
    /// <param name="tenantId">租户ID</param>
    private async Task FillSaleDisplayInfoAsync(List<TreatmentCardSaleDto> dtos, long tenantId)
    {
        if (dtos.Count == 0) return;

        var customerIds = dtos.Select(d => d.CustomerId).Distinct().ToList();
        var cardIds = dtos.Select(d => d.CardId).Distinct().ToList();
        var saleIds = dtos.Select(d => d.Id).ToList();

        var customers = await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id) && c.TenantId == tenantId)
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToDictionaryAsync(c => c.Id);

        var cards = await _dbContext.TreatmentCards
            .Where(c => cardIds.Contains(c.Id) && c.TenantId == tenantId)
            .Select(c => new { c.Id, c.Name, c.ValidityDays })
            .ToDictionaryAsync(c => c.Id);

        // 批量聚合各卡各项目的已核销次数与金额（排除已冲正 ReverseStatus=0 的记录，含跨店核销），
        // 用于计算单项目剩余可核销次数（核销弹窗限制单一项目核销次数不超过其在项目卡中的剩余次数）
        // 与单项目已核销累计金额（该项目最后一次核销时的兜底金额计算，B2 小数处理规则）
        var usedBySaleAndProduct = (await _dbContext.TreatmentCardVerifyItems
                .Where(vi => saleIds.Contains(vi.Verify!.CardSaleId) && vi.Verify!.ReverseStatus == 0)
                .GroupBy(vi => new { vi.Verify!.CardSaleId, vi.ProductId })
                .Select(g => new { g.Key.CardSaleId, g.Key.ProductId, Used = g.Sum(vi => vi.VerifyTimes), Amount = g.Sum(vi => vi.SubAmount) })
                .ToListAsync())
            .ToDictionary(x => (x.CardSaleId, x.ProductId), x => (Used: x.Used, Amount: x.Amount));

        foreach (var dto in dtos)
        {
            if (customers.TryGetValue(dto.CustomerId, out var customer))
            {
                dto.CustomerName = customer.Name;
                dto.Phone = customer.Phone;
            }
            if (cards.TryGetValue(dto.CardId, out var card))
            {
                dto.CardName = card.Name;
                dto.ValidityDays = card.ValidityDays;
            }
            dto.TotalCount = dto.Items.Sum(i => i.Quantity);
            foreach (var item in dto.Items)
            {
                var used = usedBySaleAndProduct.TryGetValue((dto.Id, item.ProductId), out var u) ? u : default;
                item.RemainingQuantity = Math.Max(0, item.Quantity - used.Used);
                item.ConsumedAmount = used.Amount;
            }
        }
    }

    /// <summary>
    /// 根据ID获取项目卡销售记录详情
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardSaleDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardSaleDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardSales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0) && !s.IsDeleted);
        if (entity == null)
            return ApiResponseDto<TreatmentCardSaleDto?>.Fail("项目卡销售记录不存在", 404);

        var dto = entity.Adapt<TreatmentCardSaleDto>();
        // 填充客户名称/手机号/卡名称/购买总次数
        await FillSaleDisplayInfoAsync(new List<TreatmentCardSaleDto> { dto }, _currentUser.TenantId.Value);
        return ApiResponseDto<TreatmentCardSaleDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建项目卡销售记录
    /// 自动计算有效期、剩余次数；按实际购买金额以"分"为单位整数计算各项目折算单价并锁定（B2 方案B）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardSaleDto>> CreateAsync(TreatmentCardSaleCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("登录状态异常，请重新登录", 401);

        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("无法确定当前门店", 401);

        if (dto.Items == null || !dto.Items.Any())
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("项目卡项目明细不能为空", 400);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;
        var storeCode = _currentUser.StoreCode ?? string.Empty;

        // 查询项目卡配置（校验存在性、启用状态、租户归属）
        var card = await _dbContext.TreatmentCards
            .FirstOrDefaultAsync(c => c.Id == dto.CardId && c.TenantId == tenantId && c.IsEnabled);
        if (card == null)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("项目卡不存在或已停用", 404);

        // 构建销售记录，自动计算有效期、剩余次数、累计消费（后端控制，忽略前端传入）
        var entity = dto.Adapt<TreatmentCardSaleEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.StoreCode = storeCode;
        entity.CreatedTime = DateTime.Now;
        // 到期时间按自然日结束计算：购买日 + 有效期的当天 23:59:59
        // 例：购买 2026-08-21 15:09 + 有效期 5 天 → 到期 2026-08-26 23:59:59
        // ValidityDays=0（不限到期时间）时存 DateTime.MaxValue，核销/到期提醒判断自然跳过
        entity.ExpiryDate = card.ValidityDays > 0
            ? dto.PurchaseDate.Date.AddDays(card.ValidityDays + 1).AddSeconds(-1)
            : DateTime.MaxValue;
        entity.RemainingTimes = card.TotalTimes;
        entity.TotalConsumedAmount = 0;

        // 以"分"为单位整数计算折算单价，避免浮点数误差（需求 B2 核销折算规则）
        // 公式：分摊总价值 = 售价 × (项目原价 × 次数) / Σ(各项目原价 × 各项目次数)
        //       折算单价 = 分摊总价值 / 次数
        long amountCents = (long)Math.Round(dto.Amount * 100, MidpointRounding.AwayFromZero);

        long totalWeightCents = 0;
        var itemWeights = new List<long>();
        foreach (var item in dto.Items)
        {
            long originalPriceCents = (long)Math.Round(item.OriginalPrice * 100, MidpointRounding.AwayFromZero);
            long weight = originalPriceCents * item.Quantity;
            itemWeights.Add(weight);
            totalWeightCents += weight;
        }

        // 第一遍：按权重整数分摊（向下取整）计算各项目分摊总价值
        // B2 小数处理规则（精确到分）：Σ(分摊总价值) 必须严格等于售价，
        // 否则最后一次核销兜底（分摊总价值 - 已核销累计金额）依赖的"分摊价值"无法全局闭合，
        // 前端核销合计会显示 698.99/699 这类 1 分级偏差（整数除法丢弃的余数累积）
        var allocatedTotals = new long[dto.Items.Count];
        for (int i = 0; i < dto.Items.Count; i++)
        {
            allocatedTotals[i] = totalWeightCents > 0
                ? amountCents * itemWeights[i] / totalWeightCents
                : 0;
        }

        // 余数再分配：整数除法丢弃的余数（0 ~ 项目数-1 分）按权重降序逐项 +1 分，
        // 权重大者分摊价值大，+1 分占比影响最小；保证 Σ(分摊总价值) 严格等于售价
        var remainder = amountCents - allocatedTotals.Sum();
        if (remainder > 0)
        {
            var byWeightDesc = Enumerable.Range(0, dto.Items.Count)
                .OrderByDescending(i => itemWeights[i])
                .ToList();
            for (var k = 0; k < remainder && k < byWeightDesc.Count; k++)
                allocatedTotals[byWeightDesc[k]] += 1;
        }

        for (int i = 0; i < dto.Items.Count; i++)
        {
            var dtoItem = dto.Items[i];

            long allocatedTotalCents = allocatedTotals[i];
            long allocatedUnitCents = dtoItem.Quantity > 0
                ? allocatedTotalCents / dtoItem.Quantity
                : 0;

            entity.Items.Add(new TreatmentCardSaleItemEntity
            {
                ProductId = dtoItem.ProductId,
                Quantity = dtoItem.Quantity,
                OriginalPrice = dtoItem.OriginalPrice,
                AllocatedUnitPrice = allocatedUnitCents / 100m,
                AllocatedTotalPrice = allocatedTotalCents / 100m,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                StoreCode = storeCode,
                CreatedTime = DateTime.Now
            });
        }

        // 事务级顾问锁：按 (租户, 门店, 销售日期) 串行化并发请求
        // SaleNo 采用"查max+1"生成模式，并发下需串行化避免重复
        // 混合结算（PosCheckoutAppService）调用时复用其外部事务，不自开/不提交/不回滚
        var ownsTransaction = _dbContext.Database.CurrentTransaction == null;
        await using var transaction = ownsTransaction
            ? await _dbContext.Database.BeginTransactionAsync()
            : null;
        try
        {
            var lockKey = TreatmentCardSaleNoGenerator.BuildLockKey(tenantId, storeId, dto.PurchaseDate);
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);

            // 在锁保护下生成销售单号（TC{yyyyMMdd}{序号}，同租户+门店+当日递增）
            entity.SaleNo = await TreatmentCardSaleNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, dto.PurchaseDate);

            _dbContext.TreatmentCardSales.Add(entity);
            await _dbContext.SaveChangesAsync();

            // 组合支付开卡联动扣减（与开卡记录同事务，任一失败整体回滚）：
            // 开卡 3 栏由 PosCheckoutAppService 按整单拆分计算（订单部分 + 开卡部分），
            // 此处真实扣减开卡部分的储值/积分，与订单部分扣减合计 = 整单填写金额（"填多少扣多少"）
            await DeductCombinedPaymentForCardAsync(entity, DateTime.Now);

            if (ownsTransaction)
                await transaction!.CommitAsync();
        }
        catch
        {
            if (ownsTransaction)
                await transaction!.RollbackAsync();
            throw;
        }

        // P-PTS-01: 项目卡购买发积分（一次性发放，核销不再重复发）
        // 依据：G5.3 项目卡购买时按售价一次性发放积分
        // 核销流程（TreatmentCardVerifyAppService.CreateAsync）已显式不发积分，与本处形成互补
        await AwardPointsForTreatmentCardPurchaseAsync(entity, storeId, storeCode, tenantId, tenantCode);

        var createdDto = entity.Adapt<TreatmentCardSaleDto>();
        createdDto.ValidityDays = card.ValidityDays;
        return ApiResponseDto<TreatmentCardSaleDto>.Ok(createdDto, "创建成功");
    }

    /// <summary>
    /// 组合支付开卡联动扣减（PayMethod=7）：
    /// - 类别1（CashAmount）：仅记录，线下或第三方收款，系统不联动扣减
    /// - 类别2（StoredValueAmount）：调用 IStoredValueAccountAppService.ConsumeAsync 扣减储值余额
    /// - 类别3（PointsAmount）：调用 IPointsDeductionService.DeductAsync 扣减积分
    /// 在调用方事务内执行（开卡记录与扣减同事务，任一失败整体回滚）；
    /// 单一支付方式（1-6）无 3 栏拆分，开卡费由店员线下独立收款，此处不联动扣减
    /// </summary>
    /// <param name="sale">项目卡销售记录（已 SaveChanges，含 Id/SaleNo）</param>
    /// <param name="now">当前时间</param>
    private async Task DeductCombinedPaymentForCardAsync(TreatmentCardSaleEntity sale, DateTime now)
    {
        if (sale.PayMethod != 7) return; // 仅组合支付触发联动扣减

        var svAmount = sale.StoredValueAmount ?? 0m;
        var pointsAmount = sale.PointsAmount ?? 0m;
        if (svAmount <= 0 && pointsAmount <= 0) return;

        // 类别2：储值扣减（委托 ConsumeAsync 在当前事务内完成）
        if (svAmount > 0)
        {
            var result = await _storedValueAccountAppService.ConsumeAsync(
                sale.CustomerId, sale.TenantId, sale.TenantCode,
                sale.StoreId, sale.StoreCode,
                svAmount, sale.Id, sale.SaleNo ?? string.Empty,
                7, // 组合支付
                now);
            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage);
        }

        // 类别3：积分抵扣（委托共享积分抵扣服务）
        if (pointsAmount > 0)
        {
            await _pointsDeductionService.DeductAsync(
                sale.CustomerId, sale.TenantId, sale.TenantCode,
                sale.StoreId, sale.StoreCode,
                pointsAmount, sale.Id, sale.SaleNo ?? string.Empty, "项目卡",
                _currentUser.UserId, now);
        }
    }

    /// <summary>
    /// 项目卡购买发积分（按发积分基数 * PointsRate 一次性发放，Floor 取整）
    /// 生效规则查询与生日当天双倍逻辑由 IPointsRuleService 统一处理
    /// 幂等性：CreateAsync 每次创建新的 Sale 记录，不会重复调用本方法
    /// </summary>
    /// <param name="sale">项目卡销售记录（含 Id）</param>
    /// <param name="storeId">购买门店ID（可空，项目卡租户内跨店通用）</param>
    /// <param name="storeCode">购买门店编码</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="tenantCode">租户编码</param>
    private async Task AwardPointsForTreatmentCardPurchaseAsync(
        TreatmentCardSaleEntity sale, long? storeId, string? storeCode,
        long tenantId, string tenantCode)
    {
        // 发积分基数 = 售价 - 积分抵扣金额（组合支付时积分抵扣部分不发放积分，口径与订单一致，避免双重发放）
        // 单一支付方式（PayMethod != 7）时 PointsAmount 为空，基数 = 售价（保持原行为）
        var awardBase = sale.Amount - (sale.PointsAmount ?? 0m);
        if (awardBase <= 0) return;

        var effectiveStoreId = storeId ?? 0L;
        var pointsRule = await _pointsRuleService.GetEffectivePointsRuleAsync(tenantId, effectiveStoreId);
        if (pointsRule == null) return;

        var now = DateTime.Now;
        // 先查客户（生日当天双倍判断需要客户生日）
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == tenantId);
        if (customer == null) return;

        // 计算应发积分（含生日当天双倍）
        var points = _pointsRuleService.CalculateAwardPoints(pointsRule, customer.Birthday, awardBase, now);
        if (points <= 0) return;

        // 写入发卡积分快照（退卡时按应退比例扣回使用；发积分为 0 时保持 0，退卡不扣）
        sale.AwardedPoints = points;

        var beforePoints = customer.TotalPoints;
        customer.TotalPoints += points;
        customer.UpdatedTime = now;

        var expireDate = pointsRule.PointsValidityDays.HasValue
            ? now.AddDays(pointsRule.PointsValidityDays.Value)
            : (DateTime?)null;

        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
        {
            CustomerId = customer.Id,
            Type = CustomerPointsLogType.TreatmentCardPurchase, // 项目卡购买获得
            Points = points,
            BeforePoints = beforePoints,
            AfterPoints = customer.TotalPoints,
            OrderId = null, // 项目卡购买不通过 Order 流程，OrderId 留空
            OperatorId = _currentUser.UserId,
            ExpireDate = expireDate,
            Remark = $"项目卡购买(SaleId={sale.Id}) {sale.Amount:F2} 元获得积分",
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = effectiveStoreId,
            StoreCode = storeCode ?? string.Empty,
            CreatedTime = now
        });

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 更新项目卡销售记录
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardSaleDto>> UpdateAsync(TreatmentCardSaleUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted);
        if (entity == null)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("项目卡销售记录不存在", 404);

        // StoreId/StoreCode 为发卡门店永久归属，禁止修改（文档 5.4 节）
        entity.CardId = dto.CardId;
        entity.CustomerId = dto.CustomerId;
        entity.PurchaseDate = dto.PurchaseDate;
        entity.Amount = dto.Amount;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardSaleDto>.Ok(entity.Adapt<TreatmentCardSaleDto>(), "更新成功");
    }

    /// <summary>
    /// 删除项目卡销售记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0) && !s.IsDeleted);
        if (entity == null)
            return ApiResponseDto.Fail("项目卡销售记录不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除项目卡销售记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.TreatmentCardSales
            .Where(s => ids.Contains(s.Id) && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0) && !s.IsDeleted)
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
    /// 获取项目卡到期提醒分页列表（客户视角：不按 StoreId 过滤，跨店购卡均可见），
    /// 仅返回 30 天内到期或已过期的记录，
    /// 并附带当前搜索条件下全量预警级别统计（即将到期/已到期）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardExpiryPageDto>> GetExpiryListAsync(TreatmentCardExpiryQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardExpiryPageDto>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var now = DateTime.Now;
        var alertThreshold = now.AddDays(30);

        // 客户视角：不按 StoreId 过滤（跨店购卡均可见），符合《跨店权益数据隔离与查询规范》4.1/5.4 节
        var queryable = _dbContext.TreatmentCardSales
            .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.ExpiryDate <= alertThreshold);

        // 按客户名称或手机号关键字模糊匹配（子查询 join Customer 表，OR 语义）
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var matchedCustomerIds = _dbContext.Customers
                .Where(c => c.Name.Contains(query.Keyword) || c.Phone.Contains(query.Keyword))
                .Select(c => c.Id);
            queryable = queryable.Where(s => matchedCustomerIds.Contains(s.CustomerId));
        }

        // 全量预警级别统计：基于基础范围（含客户名称条件），不受预警级别筛选与分页影响
        // 基础范围已限定 ExpiryDate <= alertThreshold（30 天内），此处仅按是否已过期切分
        var expiringCount = await queryable.CountAsync(s => s.ExpiryDate >= now);
        var expiredCount = await queryable.CountAsync(s => s.ExpiryDate < now);

        // 按预警级别筛选：1 即将到期（0-30 天）、2 已到期
        if (query.AlertLevel.HasValue)
        {
            queryable = query.AlertLevel.Value switch
            {
                1 => queryable.Where(s => s.ExpiryDate >= now && s.ExpiryDate <= alertThreshold),
                2 => queryable.Where(s => s.ExpiryDate < now),
                _ => queryable
            };
        }

        var total = await queryable.CountAsync();
        var sales = await queryable
            .OrderBy(s => s.ExpiryDate)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        if (!sales.Any())
        {
            return ApiResponseDto<TreatmentCardExpiryPageDto>.Ok(new TreatmentCardExpiryPageDto
            {
                List = new List<TreatmentCardExpiryDto>(),
                Total = total,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                ExpiringCount = expiringCount,
                ExpiredCount = expiredCount
            });
        }

        // 批量查询客户和项目卡信息，避免 N+1
        var customerIds = sales.Select(s => s.CustomerId).Distinct().ToList();
        var cardIds = sales.Select(s => s.CardId).Distinct().ToList();

        var customers = await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToDictionaryAsync(c => c.Id);

        var cards = await _dbContext.TreatmentCards
            .Where(c => cardIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id);

        var list = sales.Select(s =>
        {
            var remainingDays = (s.ExpiryDate - now).Days;
            return new TreatmentCardExpiryDto
            {
                Id = s.Id,
                CustomerName = customers.TryGetValue(s.CustomerId, out var c) ? c.Name : string.Empty,
                Phone = customers.TryGetValue(s.CustomerId, out c) ? c.Phone : string.Empty,
                CardName = cards.TryGetValue(s.CardId, out var card) ? card.Name : string.Empty,
                SaleId = s.Id,
                PurchaseDate = s.PurchaseDate,
                ExpiryDate = s.ExpiryDate,
                RemainingDays = remainingDays,
                RemainingTimes = s.RemainingTimes,
                AlertLevel = remainingDays < 0 ? 2 : 1,
                Status = s.Status
            };
        }).ToList();

        var result = new TreatmentCardExpiryPageDto
        {
            List = list,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            ExpiringCount = expiringCount,
            ExpiredCount = expiredCount
        };
        return ApiResponseDto<TreatmentCardExpiryPageDto>.Ok(result);
    }

    /// <summary>
    /// 退卡（规则6）
    /// 全额冲减发卡门店销售业绩，已发生的核销业绩不冲回（服务已实际发生）
    /// 退卡金额 = 售价 - 已核销金额（未消费部分退还客户）
    /// 退卡后 Sale.Status = 3（已退卡），报表应过滤 Status=3 的销售业绩
    /// 资金按原渠道回退（与订单组合支付退款口径一致）：按销售记录三栏比例分摊退款金额，
    /// 储值退到实收余额、积分按 DeductRate 换算退还、现金类线下退款（仅记录），与退卡状态变更同事务
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardSaleRefundResultDto>> RefundAsync(TreatmentCardSaleRefundDto dto)
    {
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "项目卡退卡";

        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardSaleRefundResultDto>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId ?? 0;
        var now = DateTime.Now;

        var sale = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted);
        if (sale == null)
            return ApiResponseDto<TreatmentCardSaleRefundResultDto>.Fail("项目卡销售记录不存在", 404);

        // 状态校验：仅有效(1)或已用完(2)可退卡，已退卡(3)不可重复退卡
        if (sale.Status == 3)
            return ApiResponseDto<TreatmentCardSaleRefundResultDto>.Fail("该项目卡已退卡，不可重复退卡", 400);

        // 计算应退金额 = 售价 - 已核销金额（未消费部分）
        var refundAmount = Math.Max(0m, sale.Amount - sale.TotalConsumedAmount);
        var refundActions = new List<string>();

        // 扣回开卡发放积分（按应退比例，文档 G5.3 退费场景积分处理）：积分不足时差额折算现金从退款扣除
        var pointsCashDeduction = await RefundAwardedPointsAsync(sale, refundAmount, refundActions, now);
        var actualRefundAmount = Math.Max(0m, refundAmount - pointsCashDeduction);

        // 更新销售记录状态为已退卡
        sale.Status = 3; // 已退卡
        sale.UpdatedTime = now;

        // 原渠道资金回退（与订单组合支付退款口径一致）：按销售记录三栏比例分摊退款金额，
        // 储值退到实收余额、积分按 DeductRate 换算退还、现金类线下退款（仅记录），与退卡状态变更同事务
        await RefundPaymentByChannelAsync(sale, actualRefundAmount, refundActions, now);

        // 写入退卡审计日志（含资金回退明细）
        await _auditService.LogAsync(new CrossStoreOperationLog
        {
            OperationType = "TreatmentCardRefund",
            OperatorId = _currentUser.UserId ?? 0,
            OperatorName = _currentUser.RealName,
            OperationTime = now,
            CustomerId = sale.CustomerId,
            HomeStoreId = sale.StoreId,
            IsCrossStore = false, // 退卡业绩冲减发卡门店，不涉及跨店
            RelatedEntityId = sale.Id,
            RelatedEntitySnapshot = $"{{\"OriginalAmount\":{sale.Amount:F2},\"ConsumedAmount\":{sale.TotalConsumedAmount:F2},\"RefundAmount\":{actualRefundAmount:F2},\"PointsCashDeduction\":{pointsCashDeduction:F2}}}",
            Remark = refundActions.Count > 0
                ? $"项目卡退卡-{dto.Remark}；{string.Join("；", refundActions)}"
                : $"项目卡退卡-{dto.Remark}",
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = sale.StoreId,
            StoreCode = sale.StoreCode
        });

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<TreatmentCardSaleRefundResultDto>.Ok(new TreatmentCardSaleRefundResultDto
        {
            SaleId = sale.Id,
            OriginalAmount = sale.Amount,
            ConsumedAmount = sale.TotalConsumedAmount,
            RefundAmount = actualRefundAmount,
            Status = sale.Status
        }, "退卡成功");
    }

    /// <summary>
    /// 退卡资金原渠道回退（与订单组合支付退款 RefundCombinedPaymentAsync 口径一致）：
    /// 按销售记录三栏比例分摊退款金额，
    /// - 类别1（现金类）：线下退款，无系统联动，仅记录说明
    /// - 类别2（储值）：按比例金额退到 StoredValueAccount.RealBalance（赠送余额不退），写 StoredValueLog(Type=3)
    /// - 类别3（积分）：按比例金额按 DeductRate 换算积分退还客户账户
    /// 最后一栏用减法补齐，避免分摊精度误差导致总和不等；
    /// 在调用方事务内执行（退卡状态变更与资金回退同事务，任一失败整体回滚）
    /// </summary>
    /// <param name="sale">项目卡销售记录</param>
    /// <param name="refundAmount">应退金额</param>
    /// <param name="actions">回退明细记录（写入审计日志）</param>
    /// <param name="now">当前时间</param>
    private async Task RefundPaymentByChannelAsync(TreatmentCardSaleEntity sale, decimal refundAmount, List<string> actions, DateTime now)
    {
        if (refundAmount <= 0)
        {
            actions.Add("应退金额为0，无需资金回退");
            return;
        }

        var cashAmount = sale.CashAmount ?? 0m;
        var svAmount = sale.StoredValueAmount ?? 0m;
        var pointsAmount = sale.PointsAmount ?? 0m;
        var total = cashAmount + svAmount + pointsAmount;
        if (total <= 0)
        {
            // 单一支付方式（PayMethod 1-6）开卡费由店员线下独立收款，无系统三栏拆分，需线下退款
            actions.Add("该卡无组合支付三栏拆分（单一支付/线下独立收款），需线下原路退款");
            return;
        }

        // 按比例分摊退款金额（前两栏按比例，最后一栏用减法补齐，避免精度误差）
        var svRefund = Math.Round(refundAmount * (svAmount / total), 2, MidpointRounding.AwayFromZero);
        var pointsRefund = Math.Round(refundAmount * (pointsAmount / total), 2, MidpointRounding.AwayFromZero);
        var cashRefund = Math.Round(refundAmount - svRefund - pointsRefund, 2, MidpointRounding.AwayFromZero);

        // 类别1：现金类线下退款，无系统联动
        if (cashRefund > 0)
            actions.Add($"组合支付-类别1(现金类)退款 {cashRefund:F2}（线下/第三方退款，无系统联动）");

        // 类别2：储值退款（退到 RealBalance，赠送余额不退）
        if (svRefund > 0)
            await RefundStoredValueForCardAsync(sale, svRefund, actions, now);

        // 类别3：积分退还（按 DeductRate 换算积分退还）
        if (pointsRefund > 0)
            await RefundPointsForCardAsync(sale, pointsRefund, actions, now);
    }

    /// <summary>
    /// 退卡储值退款：退到 StoredValueAccount.RealBalance（赠送余额不退），
    /// 更新 Balance/TotalConsume 并同步客户档案余额，写 StoredValueLog(Type=3)，与订单 RefundStoredValueAsync 口径一致
    /// </summary>
    private async Task RefundStoredValueForCardAsync(TreatmentCardSaleEntity sale, decimal refundAmount, List<string> actions, DateTime now)
    {
        var account = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == sale.CustomerId && a.TenantId == sale.TenantId);
        if (account == null)
        {
            actions.Add("未找到客户储值账户，跳过储值退款");
            return;
        }

        var beforeBalance = account.Balance;
        var beforeRealBalance = account.RealBalance;
        var beforeGiftBalance = account.GiftBalance;

        // 仅退实收余额，赠送余额不退
        account.RealBalance += refundAmount;
        account.Balance += refundAmount;
        account.TotalConsume -= refundAmount;
        account.UpdatedTime = now;

        // 同步客户档案余额：口径与储值账户总余额一致（实收+赠送）
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == sale.TenantId);
        if (customer != null)
        {
            customer.Balance = account.Balance;
            customer.UpdatedTime = now;
        }

        _dbContext.StoredValueLogs.Add(new StoredValueLog
        {
            CustomerId = account.CustomerId,
            Type = 3, // 退款
            Amount = refundAmount,
            RealAmount = refundAmount,
            GiftAmount = 0,
            BeforeBalance = beforeBalance,
            AfterBalance = beforeBalance + refundAmount,
            RealBalanceChange = refundAmount,
            GiftBalanceChange = 0,
            BeforeRealBalance = beforeRealBalance,
            AfterRealBalance = beforeRealBalance + refundAmount,
            BeforeGiftBalance = beforeGiftBalance,
            AfterGiftBalance = beforeGiftBalance,
            OrderId = sale.Id,
            Remark = $"项目卡退卡 {sale.SaleNo ?? sale.Id.ToString()} 退款",
            OperatorId = _currentUser.UserId,
            OperatorName = _currentUser.RealName ?? _currentUser.UserName,
            TenantId = sale.TenantId,
            TenantCode = sale.TenantCode,
            StoreId = sale.StoreId,
            StoreCode = sale.StoreCode,
            CreatedTime = now
        });

        actions.Add($"储值账户 ID:{account.Id} 退余额 {refundAmount:F2}（仅实收余额）");
    }

    /// <summary>
    /// 退卡积分退还：按 DeductRate 将退款金额换算为积分退还客户账户（向下取整，避免多退）
    /// 开卡记录未保存 DeductRate 快照（与订单不同），故使用当前生效规则换算（与开卡扣减口径一致）；
    /// 不写积分流水，与订单 RefundPointsPaymentAsync 口径一致
    /// </summary>
    private async Task RefundPointsForCardAsync(TreatmentCardSaleEntity sale, decimal refundAmount, List<string> actions, DateTime now)
    {
        var rule = await _pointsRuleService.GetEffectivePointsRuleAsync(sale.TenantId, sale.StoreId);
        var deductRate = rule?.DeductRate ?? 0m;
        if (deductRate <= 0)
        {
            actions.Add("未配置生效的积分规则或抵扣比例为0，跳过积分退还");
            return;
        }

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == sale.TenantId);
        if (customer == null)
        {
            actions.Add("未找到客户，跳过积分退还");
            return;
        }

        // 退还积分按比例换算（向下取整，避免多退）
        var pointsToRefund = (int)Math.Floor(refundAmount / deductRate);
        if (pointsToRefund <= 0)
        {
            actions.Add($"退款金额 {refundAmount:F2} 不足1积分，跳过积分退还");
            return;
        }

        customer.TotalPoints += pointsToRefund;
        customer.UpdatedTime = now;
        actions.Add($"客户 ID:{customer.Id} 退还积分 {pointsToRefund}（退款 {refundAmount:F2} × 比例 1/{deductRate}）");
    }

    /// <summary>
    /// 退卡扣回开卡发放积分（文档 G5.3 退费场景积分处理：项目卡退款扣回购买时发放的积分）：
    /// 按应退比例（应退金额/售价）扣回 AwardedPoints 快照，积分不足时按当前生效 DeductRate 折算现金从退款扣除，
    /// 写 CustomerPointsLog(Type=RefundDeduct)；与订单 RefundPointsAsync 算法一致
    /// </summary>
    /// <param name="sale">项目卡销售记录</param>
    /// <param name="refundAmount">应退金额（未扣积分折现前的原始应退）</param>
    /// <param name="actions">回退明细记录（写入审计日志）</param>
    /// <param name="now">当前时间</param>
    /// <returns>积分不足折算扣除的现金金额（0 表示无折算）</returns>
    private async Task<decimal> RefundAwardedPointsAsync(TreatmentCardSaleEntity sale, decimal refundAmount, List<string> actions, DateTime now)
    {
        if (sale.AwardedPoints <= 0 || sale.Amount <= 0 || refundAmount <= 0)
            return 0;

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == sale.TenantId);
        if (customer == null)
        {
            actions.Add("未找到客户，跳过开卡积分扣回");
            return 0;
        }

        // 按应退比例扣回发卡积分（对齐订单 RefundPointsAsync 算法，四舍五入）
        var ratio = refundAmount / sale.Amount;
        var pointsToDeduct = -(int)Math.Round(sale.AwardedPoints * ratio);
        if (pointsToDeduct == 0) return 0;

        // 积分不足：扣全部剩余积分，差额按当前生效 DeductRate 折算现金从退款扣除
        // 开卡记录未保存 DeductRate 快照（与订单不同），故使用当前生效规则（与 RefundPointsForCardAsync 口径一致）
        if (customer.TotalPoints + pointsToDeduct < 0)
        {
            var rule = await _pointsRuleService.GetEffectivePointsRuleAsync(sale.TenantId, sale.StoreId);
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
                OrderId = null,
                OperatorId = _currentUser.UserId,
                Remark = $"项目卡退卡(SaleId={sale.Id}) 扣回开卡积分（积分不足，扣减全部剩余 {beforePoints} 积分，不足 {shortfallPoints} 积分折算现金 {cashDeduction:F2} 元从退款扣除）",
                TenantId = sale.TenantId,
                TenantCode = sale.TenantCode,
                StoreId = sale.StoreId,
                StoreCode = sale.StoreCode,
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
            OrderId = null,
            OperatorId = _currentUser.UserId,
            Remark = $"项目卡退卡(SaleId={sale.Id}) 扣回开卡积分",
            TenantId = sale.TenantId,
            TenantCode = sale.TenantCode,
            StoreId = sale.StoreId,
            StoreCode = sale.StoreCode,
            CreatedTime = now
        });

        actions.Add($"客户 ID:{customer.Id} 扣减开卡积分 {pointsToDeduct}");
        return 0;
    }
}
