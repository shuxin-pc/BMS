using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.ParkedOrders;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// POS 挂单应用服务实现
/// 挂单在门店内共享（同门店收银员均可查看/取单），按 TenantId + StoreId 隔离
/// </summary>
public class ParkedOrderAppService : IParkedOrderAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ParkedOrderCreateDto> _createValidator;
    private readonly IAuditLogContext _auditLogContext;

    public ParkedOrderAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ParkedOrderCreateDto> createValidator,
        IAuditLogContext auditLogContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _auditLogContext = auditLogContext;
    }

    /// <summary>
    /// 挂单：保存当前购物车快照，生成挂单号，状态置为挂起
    /// </summary>
    public async Task<ApiResponseDto<ParkedOrderDto>> CreateAsync(ParkedOrderCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<ParkedOrderDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ParkedOrderDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var now = DateTime.Now;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 事务级顾问锁：按 (租户, 门店, 挂单日期) 串行化并发请求
            // ParkNo 采用"查max+1"生成模式，并发下需串行化避免唯一约束冲突（对齐 Order/PurchaseOrder 模式）
            var lockKey = ParkedOrderNoGenerator.BuildLockKey(tenantId, storeId, now);
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", lockKey);

            // 在锁保护下生成挂单号（PK{yyyyMMdd}{序号}，同租户+门店+当日递增）
            var parkNo = await ParkedOrderNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, now);

            var order = new ParkedOrder
            {
                ParkNo = parkNo,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                Remark = dto.Remark,
                CartJson = dto.CartJson,
                Status = ParkedOrderStatuses.Parked,
                CreatedBy = _currentUser.UserId,
                CreatedByName = string.IsNullOrWhiteSpace(_currentUser.RealName) ? _currentUser.UserName : _currentUser.RealName,
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = _currentUser.StoreCode ?? string.Empty,
                CreatedTime = now
            };

            _dbContext.ParkedOrders.Add(order);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponseDto<ParkedOrderDto>.Ok(BuildDto(order), "挂单成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 挂单分页列表（仅挂起状态，按挂单时间倒序）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ParkedOrderDto>>> GetPagedListAsync(ParkedOrderQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<ParkedOrderDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        var queryable = _dbContext.ParkedOrders
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId
                        && p.Status == ParkedOrderStatuses.Parked);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            queryable = queryable.Where(p =>
                p.ParkNo.Contains(keyword)
                || (p.Remark != null && p.Remark.Contains(keyword))
                || (p.CustomerName != null && p.CustomerName.Contains(keyword)));
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 列表不返回 CartJson 大文本，仅返回统计字段（行数/金额），减少传输
        var result = new PagedResponseDto<ParkedOrderDto>
        {
            List = items.Select(p => BuildDto(p, includeCartJson: false)).ToList(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ParkedOrderDto>>.Ok(result);
    }

    /// <summary>
    /// 取单：校验挂单归属门店且为挂起状态，返回完整购物车快照后物理删除该挂单
    /// 挂单为临时草稿，取单后记录即失效，物理删除避免无主数据累积（挂单号按现存数据生成，当天内可能复用）
    /// </summary>
    public async Task<ApiResponseDto<ParkedOrderDto>> ResumeAsync(long id)
    {
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "取单";

        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<ParkedOrderDto>.Fail("登录状态异常，请重新登录", 401);

        var order = await FindParkedAsync(id);
        if (order == null)
            return ApiResponseDto<ParkedOrderDto>.Fail("挂单不存在或已被取走", 404);

        // 先构建含 CartJson 快照的响应，再物理删除记录
        var dto = BuildDto(order, includeCartJson: true);
        _dbContext.ParkedOrders.Remove(order);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<ParkedOrderDto>.Ok(dto, "取单成功");
    }

    /// <summary>
    /// 取消挂单：校验挂单归属门店且为挂起状态后物理删除该挂单
    /// 取消即废弃草稿，物理删除避免无主数据累积（挂单号按现存数据生成，当天内可能复用）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id)
    {
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "取消挂单";

        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var order = await FindParkedAsync(id);
        if (order == null)
            return ApiResponseDto.Fail("挂单不存在或已被取走", 404);

        _dbContext.ParkedOrders.Remove(order);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, "挂单已取消");
    }

    /// <summary>
    /// 按 ID 查询当前门店下挂起状态的挂单（取单/取消共用）
    /// </summary>
    private Task<ParkedOrder?> FindParkedAsync(long id)
        => _dbContext.ParkedOrders
            .FirstOrDefaultAsync(p => p.Id == id
                && p.TenantId == _currentUser.TenantId!.Value
                && p.StoreId == _currentUser.StoreId!.Value
                && p.Status == ParkedOrderStatuses.Parked);

    /// <summary>
    /// 构建挂单 DTO，解析购物车 JSON 计算行数与商品合计金额
    /// </summary>
    /// <param name="order">挂单实体</param>
    /// <param name="includeCartJson">是否返回完整购物车快照（取单接口返回，列表接口不返回）</param>
    private static ParkedOrderDto BuildDto(ParkedOrder order, bool includeCartJson = false)
    {
        var (itemCount, totalAmount) = SummarizeCart(order.CartJson);
        return new ParkedOrderDto
        {
            Id = order.Id,
            ParkNo = order.ParkNo,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Remark = order.Remark,
            CreatedByName = order.CreatedByName,
            Status = order.Status,
            HeldTime = order.CreatedTime,
            ItemCount = itemCount,
            TotalAmount = totalAmount,
            CartJson = includeCartJson ? order.CartJson : null
        };
    }

    /// <summary>
    /// 解析购物车 JSON，统计行数与商品合计金额（仅 lineType=product 的行参与金额计算）
    /// 解析失败（历史脏数据）时返回 0，不阻塞挂单列表展示
    /// </summary>
    private static (int ItemCount, decimal TotalAmount) SummarizeCart(string cartJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(cartJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return (0, 0m);

            var itemCount = 0;
            var totalAmount = 0m;
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                itemCount++;
                // 仅商品/服务行参与金额合计，核销/开卡行为占位行
                if (element.TryGetProperty("lineType", out var lineType)
                    && lineType.ValueKind == JsonValueKind.String
                    && lineType.GetString() != "product")
                {
                    continue;
                }
                var price = element.TryGetProperty("price", out var priceProp)
                    && priceProp.ValueKind == JsonValueKind.Number
                    ? priceProp.GetDecimal()
                    : 0m;
                var quantity = element.TryGetProperty("quantity", out var quantityProp)
                    && quantityProp.ValueKind == JsonValueKind.Number
                    ? quantityProp.GetDecimal()
                    : 0m;
                totalAmount += price * quantity;
            }
            return (itemCount, totalAmount);
        }
        catch (JsonException)
        {
            return (0, 0m);
        }
    }
}
