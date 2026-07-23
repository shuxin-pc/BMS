using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Points;
using Bms.Store.Domain.Constants;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 积分兑换记录应用服务实现
/// 兑换类型（ExchangeType）：
/// 1=零售商品（扣库存 + 创建 Order OrderType=1 PayMethod=6 + 扣积分）
/// 3=服务项目（创建 Order OrderType=2 PayMethod=6 + 扣积分）
/// 2=优惠券（仅记录，本服务不处理库存/订单）
/// </summary>
public class PointsExchangeAppService : IPointsExchangeAppService
{
    /// <summary>
    /// 兑换类型：零售商品
    /// </summary>
    private const int ExchangeTypeProduct = 1;

    /// <summary>
    /// 兑换类型：服务项目（与 OrderAppService.DeductPointsAsync 中 ExchangeType=3 保持一致）
    /// </summary>
    private const int ExchangeTypeService = 3;

    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryAppService _inventoryAppService;
    private readonly IValidator<PointsExchangeCreateDto> _createValidator;
    private readonly IValidator<PointsExchangeUpdateDto> _updateValidator;

    public PointsExchangeAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IInventoryAppService inventoryAppService,
        IValidator<PointsExchangeCreateDto> createValidator,
        IValidator<PointsExchangeUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _inventoryAppService = inventoryAppService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取积分兑换记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PointsExchangeDto>>> GetPagedListAsync(PointsExchangeQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PointsExchangeDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.PointsExchanges
            .Where(e => e.TenantId ==tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(e => e.CustomerId == query.CustomerId.Value);
        if (query.ExchangeType.HasValue)
            queryable = queryable.Where(e => e.ExchangeType == query.ExchangeType.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(e => e.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<PointsExchangeDto>
        {
            List = items.Select(ToDto).ToList(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<PointsExchangeDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取积分兑换记录详情
    /// </summary>
    public async Task<ApiResponseDto<PointsExchangeDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PointsExchangeDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PointsExchanges
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId ==_currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<PointsExchangeDto?>.Fail("积分兑换记录不存在", 404);
        return ApiResponseDto<PointsExchangeDto?>.Ok(ToDto(entity));
    }

    /// <summary>
    /// 创建积分兑换记录
    /// P-PTS-03: 按 ExchangeType 分流处理实际兑换业务
    /// 1=零售商品：扣减库存（FIFO）+ 创建订单（OrderType=1, PayMethod=6）+ 扣积分
    /// 3=服务项目：创建订单（OrderType=2, PayMethod=6）+ 扣积分
    /// 2=优惠券：仅扣积分 + 记录（不创建订单、不扣库存）
    /// 事务包裹确保原子性，任一步骤失败整体回滚
    /// </summary>
    public async Task<ApiResponseDto<PointsExchangeDto>> CreateAsync(PointsExchangeCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PointsExchangeDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PointsExchangeDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId ?? 0L;
        var storeCode = string.Empty;
        var now = DateTime.Now;

        // 查询客户当前积分，校验积分是否充足
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.TenantId == tenantId);
        if (customer == null)
            return ApiResponseDto<PointsExchangeDto>.Fail("客户不存在", 404);

        if (customer.TotalPoints < dto.PointsCost)
            return ApiResponseDto<PointsExchangeDto>.Fail("客户积分不足，无法兑换", 400);

        // 兑换数量校验
        if (dto.Quantity <= 0)
            return ApiResponseDto<PointsExchangeDto>.Fail("兑换数量必须大于0", 400);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 按 ExchangeType 分流处理
            long? orderId = null;
            string exchangeTargetName = dto.TargetName;

            switch (dto.ExchangeType)
            {
                case ExchangeTypeProduct:
                    var productResult = await ExchangeProductAsync(dto, customer, tenantId, tenantCode, storeId, storeCode, now);
                    if (!productResult.Success)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponseDto<PointsExchangeDto>.Fail(productResult.ErrorMessage!, 400);
                    }
                    orderId = productResult.OrderId;
                    if (string.IsNullOrEmpty(exchangeTargetName))
                        exchangeTargetName = productResult.TargetName;
                    break;

                case ExchangeTypeService:
                    var serviceResult = await ExchangeServiceAsync(dto, customer, tenantId, tenantCode, storeId, storeCode, now);
                    if (!serviceResult.Success)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponseDto<PointsExchangeDto>.Fail(serviceResult.ErrorMessage!, 400);
                    }
                    orderId = serviceResult.OrderId;
                    if (string.IsNullOrEmpty(exchangeTargetName))
                        exchangeTargetName = serviceResult.TargetName;
                    break;

                case 2: // 优惠券：仅记录，不创建订单/扣库存
                    break;

                default:
                    await transaction.RollbackAsync();
                    return ApiResponseDto<PointsExchangeDto>.Fail($"不支持的兑换类型：{dto.ExchangeType}", 400);
            }

            // 扣减客户积分，记录积分流水（Type=2 兑换消耗）
            var beforePoints = customer.TotalPoints;
            customer.TotalPoints -= dto.PointsCost;
            customer.UpdatedTime = now;

            _dbContext.CustomerPointsLogs.Add(new CustomerPointsLog
            {
                CustomerId = customer.Id,
                Type = CustomerPointsLogType.Exchange, // 兑换消耗
                Points = -dto.PointsCost,
                BeforePoints = beforePoints,
                AfterPoints = customer.TotalPoints,
                OrderId = orderId,
                OperatorId = _currentUser.UserId,
                Remark = $"积分兑换-{exchangeTargetName} x{dto.Quantity}",
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                StoreCode = storeCode,
                CreatedTime = now
            });

            // 创建兑换记录
            var entity = dto.Adapt<PointsExchange>();
            entity.TargetName = exchangeTargetName;
            entity.TenantId = tenantId;
            entity.TenantCode = tenantCode;
            entity.StoreId = storeId;
            entity.StoreCode = storeCode;
            entity.ExchangeTime = now;
            entity.OperatorId = _currentUser.UserId;
            entity.CreatedTime = now;

            _dbContext.PointsExchanges.Add(entity);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<PointsExchangeDto>.Ok(ToDto(entity), "兑换成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 兑换零售商品：扣减库存（FIFO）+ 创建订单（OrderType=1, PayMethod=6）
    /// 库存不足抛异常让事务回滚（积分不扣减）
    /// </summary>
    private async Task<ExchangeResult> ExchangeProductAsync(
        PointsExchangeCreateDto dto, Customer customer,
        long tenantId, string tenantCode, long storeId, string storeCode, DateTime now)
    {
        if (!dto.TargetId.HasValue)
            return ExchangeResult.Fail("兑换商品必须指定 TargetId");

        var productId = dto.TargetId.Value;
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId);
        if (product == null)
            return ExchangeResult.Fail("商品不存在");
        if (product.Type != 1)
            return ExchangeResult.Fail("目标商品不是零售商品");

        // 扣减库存（FIFO，调用 IInventoryAppService.DeductByBatchAsync）
        // 库存不足返回 Fail，调用方回滚事务
        var deductResult = await _inventoryAppService.DeductByBatchAsync(
            productId: productId,
            quantity: dto.Quantity,
            batchNo: null,
            sourceType: InventoryLogSourceTypes.Other,
            refId: null,
            remark: $"积分兑换出库-{customer.Id}");
        if (!deductResult.IsSuccess)
            return ExchangeResult.Fail(deductResult.Message);

        // 创建兑换订单（OrderType=1 零售, PayMethod=6 积分抵扣, Status=2 已完成, PaidAmount=0 不收款）
        var order = new Order
        {
            OrderNo = $"PEX-{now:yyyyMMddHHmmssfff}",
            CustomerId = customer.Id,
            OrderType = OrderTypes.Retail,
            Status = 2,
            ProductAmount = product.Price * dto.Quantity,
            PaidAmount = 0,
            Points = 0,
            PayMethod = 6,
            OrderTime = now,
            CompleteTime = now,
            OperatorId = _currentUser.UserId,
            Remark = $"积分兑换-{product.Name} x{dto.Quantity}",
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            StoreCode = storeCode,
            CreatedTime = now,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code,
                    Quantity = dto.Quantity,
                    Price = product.Price,
                    DiscountRate = 0m,
                    DiscountedAmount = 0m,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    StoreCode = storeCode,
                    CreatedTime = now
                }
            }
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        return ExchangeResult.Ok(order.Id, product.Name);
    }

    /// <summary>
    /// 兑换服务项目：创建订单（OrderType=2 服务, PayMethod=6）
    /// </summary>
    private async Task<ExchangeResult> ExchangeServiceAsync(
        PointsExchangeCreateDto dto, Customer customer,
        long tenantId, string tenantCode, long storeId, string storeCode, DateTime now)
    {
        if (!dto.TargetId.HasValue)
            return ExchangeResult.Fail("兑换服务项目必须指定 TargetId");

        var productId = dto.TargetId.Value;
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId);
        if (product == null)
            return ExchangeResult.Fail("服务项目不存在");
        if (product.Type != 2)
            return ExchangeResult.Fail("目标商品不是服务项目");

        // 创建兑换订单（OrderType=2 服务, PayMethod=6 积分抵扣, Status=2 已完成, PaidAmount=0）
        var order = new Order
        {
            OrderNo = $"PEX-{now:yyyyMMddHHmmssfff}",
            CustomerId = customer.Id,
            OrderType = OrderTypes.Service,
            Status = 2,
            ProductAmount = product.Price * dto.Quantity,
            PaidAmount = 0,
            Points = 0,
            PayMethod = 6,
            OrderTime = now,
            CompleteTime = now,
            OperatorId = _currentUser.UserId,
            Remark = $"积分兑换服务-{product.Name} x{dto.Quantity}",
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            StoreCode = storeCode,
            CreatedTime = now,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductCode = product.Code,
                    Quantity = dto.Quantity,
                    Price = product.Price,
                    DiscountRate = 0m,
                    DiscountedAmount = 0m,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    StoreCode = storeCode,
                    CreatedTime = now
                }
            }
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        return ExchangeResult.Ok(order.Id, product.Name);
    }

    /// <summary>
    /// 兑换结果内部载体
    /// </summary>
    private sealed class ExchangeResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public long? OrderId { get; init; }
        public string? TargetName { get; init; }

        public static ExchangeResult Ok(long orderId, string targetName) => new()
        {
            Success = true,
            OrderId = orderId,
            TargetName = targetName
        };

        public static ExchangeResult Fail(string errorMessage) => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// 更新积分兑换记录
    /// </summary>
    public async Task<ApiResponseDto<PointsExchangeDto>> UpdateAsync(PointsExchangeUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PointsExchangeDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<PointsExchangeDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.PointsExchanges
            .FirstOrDefaultAsync(e => e.Id == dto.Id && e.TenantId ==tenantId);
        if (entity == null)
            return ApiResponseDto<PointsExchangeDto>.Fail("积分兑换记录不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.ExchangeType = dto.ExchangeType;
        entity.TargetId = dto.TargetId;
        entity.TargetName = dto.TargetName;
        entity.PointsCost = dto.PointsCost;
        entity.Quantity = dto.Quantity;
        entity.ExchangeTime = dto.ExchangeTime;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<PointsExchangeDto>.Ok(ToDto(entity), "更新成功");
    }

    /// <summary>
    /// 删除积分兑换记录（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.PointsExchanges
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId ==_currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("积分兑换记录不存在", 404);

        _dbContext.PointsExchanges.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除积分兑换记录（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.PointsExchanges
            .Where(e => ids.Contains(e.Id) && e.TenantId ==_currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.PointsExchanges.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 实体转 DTO（手动映射时间字段）
    /// </summary>
    private static PointsExchangeDto ToDto(PointsExchange entity)
    {
        var dto = entity.Adapt<PointsExchangeDto>();
        dto.CreatedAt = entity.CreatedTime;
        dto.UpdatedAt = entity.UpdatedTime;
        return dto;
    }
}
