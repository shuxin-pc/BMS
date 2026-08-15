using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using TreatmentCardSaleEntity = Bms.Store.Domain.Entities.TreatmentCardSale;
using TreatmentCardSaleItemEntity = Bms.Store.Domain.Entities.TreatmentCardSaleItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 疗程卡销售记录应用服务实现
/// </summary>
public class TreatmentCardSaleAppService : ITreatmentCardSaleAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TreatmentCardSaleCreateDto> _createValidator;
    private readonly IValidator<TreatmentCardSaleUpdateDto> _updateValidator;
    private readonly ICrossStoreOperationAuditService _auditService;
    private readonly IPointsRuleService _pointsRuleService;

    public TreatmentCardSaleAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TreatmentCardSaleCreateDto> createValidator,
        IValidator<TreatmentCardSaleUpdateDto> updateValidator,
        ICrossStoreOperationAuditService auditService,
        IPointsRuleService pointsRuleService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditService = auditService;
        _pointsRuleService = pointsRuleService;
    }

    /// <summary>
    /// 获取疗程卡销售记录分页列表
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

        // 按客户名称或手机号筛选（子查询 join Customer 表）
        if (!string.IsNullOrWhiteSpace(query.CustomerName) || !string.IsNullOrWhiteSpace(query.Phone))
        {
            var customerIds = _dbContext.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.CustomerName))
                customerIds = customerIds.Where(c => c.Name.Contains(query.CustomerName));
            if (!string.IsNullOrWhiteSpace(query.Phone))
                customerIds = customerIds.Where(c => c.Phone.Contains(query.Phone));

            queryable = queryable.Where(s => customerIds.Select(c => c.Id).Contains(s.CustomerId));
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<TreatmentCardSaleDto>
        {
            List = items.Adapt<List<TreatmentCardSaleDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardSaleDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取疗程卡销售记录详情
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardSaleDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardSaleDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardSales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0) && !s.IsDeleted);
        if (entity == null)
            return ApiResponseDto<TreatmentCardSaleDto?>.Fail("疗程卡销售记录不存在", 404);
        return ApiResponseDto<TreatmentCardSaleDto?>.Ok(entity.Adapt<TreatmentCardSaleDto>());
    }

    /// <summary>
    /// 创建疗程卡销售记录
    /// 自动计算有效期、剩余次数；按实际购买金额以"分"为单位整数计算各项目折算单价并锁定（B2 方案B）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardSaleDto>> CreateAsync(TreatmentCardSaleCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("登录状态异常，请重新登录", 401);

        if (!_currentUser.StoreId.HasValue)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("无法确定当前门店", 401);

        if (dto.Items == null || !dto.Items.Any())
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("疗程卡项目明细不能为空", 400);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;
        var storeCode = _currentUser.StoreCode ?? string.Empty;

        // 查询疗程卡配置（校验存在性、启用状态、租户归属）
        var card = await _dbContext.TreatmentCards
            .FirstOrDefaultAsync(c => c.Id == dto.CardId && c.TenantId == tenantId && c.IsEnabled);
        if (card == null)
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("疗程卡不存在或已停用", 404);

        // 构建销售记录，自动计算有效期、剩余次数、累计消费（后端控制，忽略前端传入）
        var entity = dto.Adapt<TreatmentCardSaleEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.StoreCode = storeCode;
        entity.CreatedTime = DateTime.Now;
        entity.ExpiryDate = dto.PurchaseDate.AddDays(card.ValidityDays);
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

        for (int i = 0; i < dto.Items.Count; i++)
        {
            var dtoItem = dto.Items[i];
            long weight = itemWeights[i];

            long allocatedTotalCents = totalWeightCents > 0
                ? amountCents * weight / totalWeightCents
                : 0;

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

        _dbContext.TreatmentCardSales.Add(entity);
        await _dbContext.SaveChangesAsync();

        // P-PTS-01: 疗程卡购买发积分（一次性发放，核销不再重复发）
        // 依据：G5.3 疗程卡购买时按售价一次性发放积分
        // 核销流程（TreatmentCardVerifyAppService.CreateAsync）已显式不发积分，与本处形成互补
        await AwardPointsForTreatmentCardPurchaseAsync(entity, storeId, storeCode, tenantId, tenantCode);

        return ApiResponseDto<TreatmentCardSaleDto>.Ok(entity.Adapt<TreatmentCardSaleDto>(), "创建成功");
    }

    /// <summary>
    /// 疗程卡购买发积分（按售价 * PointsRate 一次性发放，Floor 取整）
    /// 生效规则查询与生日当天双倍逻辑由 IPointsRuleService 统一处理
    /// 幂等性：CreateAsync 每次创建新的 Sale 记录，不会重复调用本方法
    /// </summary>
    /// <param name="sale">疗程卡销售记录（含 Id）</param>
    /// <param name="storeId">购买门店ID（可空，疗程卡租户内跨店通用）</param>
    /// <param name="storeCode">购买门店编码</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="tenantCode">租户编码</param>
    private async Task AwardPointsForTreatmentCardPurchaseAsync(
        TreatmentCardSaleEntity sale, long? storeId, string? storeCode,
        long tenantId, string tenantCode)
    {
        if (sale.Amount <= 0) return;

        var effectiveStoreId = storeId ?? 0L;
        var pointsRule = await _pointsRuleService.GetEffectivePointsRuleAsync(tenantId, effectiveStoreId);
        if (pointsRule == null) return;

        var now = DateTime.Now;
        // 先查客户（生日当天双倍判断需要客户生日）
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == tenantId);
        if (customer == null) return;

        // 计算应发积分（含生日当天双倍）
        var points = _pointsRuleService.CalculateAwardPoints(pointsRule, customer.Birthday, sale.Amount, now);
        if (points <= 0) return;

        var beforePoints = customer.TotalPoints;
        customer.TotalPoints += points;
        customer.UpdatedTime = now;

        var expireDate = pointsRule.PointsValidityDays.HasValue
            ? now.AddDays(pointsRule.PointsValidityDays.Value)
            : (DateTime?)null;

        _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
        {
            CustomerId = customer.Id,
            Type = CustomerPointsLogType.TreatmentCardPurchase, // 疗程卡购买获得
            Points = points,
            BeforePoints = beforePoints,
            AfterPoints = customer.TotalPoints,
            OrderId = null, // 疗程卡购买不通过 Order 流程，OrderId 留空
            OperatorId = _currentUser.UserId,
            ExpireDate = expireDate,
            Remark = $"疗程卡购买(SaleId={sale.Id}) {sale.Amount:F2} 元获得积分",
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = effectiveStoreId,
            StoreCode = storeCode ?? string.Empty,
            CreatedTime = now
        });

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 更新疗程卡销售记录
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
            return ApiResponseDto<TreatmentCardSaleDto>.Fail("疗程卡销售记录不存在", 404);

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
    /// 删除疗程卡销售记录（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0) && !s.IsDeleted);
        if (entity == null)
            return ApiResponseDto.Fail("疗程卡销售记录不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除疗程卡销售记录（软删除）
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
    /// 获取疗程卡到期提醒分页列表（仅返回 30 天内到期或已过期的记录）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardExpiryDto>>> GetExpiryListAsync(TreatmentCardExpiryQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TreatmentCardExpiryDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var now = DateTime.Now;
        var alertThreshold = now.AddDays(30);

        var queryable = _dbContext.TreatmentCardSales
            .Where(s => s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted && s.ExpiryDate <= alertThreshold);

        // 按客户名称模糊匹配（子查询 join Customer 表）
        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var matchedCustomerIds = _dbContext.Customers
                .Where(c => c.Name.Contains(query.CustomerName))
                .Select(c => c.Id);
            queryable = queryable.Where(s => matchedCustomerIds.Contains(s.CustomerId));
        }

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
            return ApiResponseDto<PagedResponseDto<TreatmentCardExpiryDto>>.Ok(new PagedResponseDto<TreatmentCardExpiryDto>
            {
                List = new List<TreatmentCardExpiryDto>(),
                Total = total,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            });
        }

        // 批量查询客户和疗程卡信息，避免 N+1
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

        var result = new PagedResponseDto<TreatmentCardExpiryDto>
        {
            List = list,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardExpiryDto>>.Ok(result);
    }

    /// <summary>
    /// 退卡（规则6）
    /// 全额冲减发卡门店销售业绩，已发生的核销业绩不冲回（服务已实际发生）
    /// 退卡金额 = 售价 - 已核销金额（未消费部分退还客户）
    /// 退卡后 Sale.Status = 3（已退卡），报表应过滤 Status=3 的销售业绩
    /// 注：实际退款支付（原路退回/现金退还）需通过独立的退款订单流程处理，此处仅处理状态变更与业绩冲销
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardSaleRefundResultDto>> RefundAsync(TreatmentCardSaleRefundDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardSaleRefundResultDto>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId ?? 0;
        var now = DateTime.Now;

        var sale = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId && s.StoreId == storeId && !s.IsDeleted);
        if (sale == null)
            return ApiResponseDto<TreatmentCardSaleRefundResultDto>.Fail("疗程卡销售记录不存在", 404);

        // 状态校验：仅有效(1)或已用完(2)可退卡，已退卡(3)不可重复退卡
        if (sale.Status == 3)
            return ApiResponseDto<TreatmentCardSaleRefundResultDto>.Fail("该疗程卡已退卡，不可重复退卡", 400);

        // 计算应退金额 = 售价 - 已核销金额（未消费部分）
        var refundAmount = Math.Max(0m, sale.Amount - sale.TotalConsumedAmount);

        // 更新销售记录状态为已退卡
        sale.Status = 3; // 已退卡
        sale.UpdatedTime = now;

        // 写入退卡审计日志
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
            RelatedEntitySnapshot = $"{{\"OriginalAmount\":{sale.Amount:F2},\"ConsumedAmount\":{sale.TotalConsumedAmount:F2},\"RefundAmount\":{refundAmount:F2}}}",
            Remark = $"疗程卡退卡-{dto.Remark}",
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
            RefundAmount = refundAmount,
            Status = sale.Status
        }, "退卡成功");
    }
}
