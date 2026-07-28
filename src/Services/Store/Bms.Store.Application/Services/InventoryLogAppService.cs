using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Domain.Entities;
using InventoryLogEntity = Bms.Store.Domain.Entities.InventoryLog;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存流水应用服务实现
/// </summary>
public class InventoryLogAppService : IInventoryLogAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<InventoryLogCreateDto> _createValidator;
    private readonly IValidator<InventoryLogUpdateDto> _updateValidator;

    public InventoryLogAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<InventoryLogCreateDto> createValidator,
        IValidator<InventoryLogUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取库存流水分页列表
    /// 查询时 Include Product 与 Supplier 导航属性，内存中填充显示字段
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryLogDto>>> GetPagedListAsync(InventoryLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryLogDto>>.Fail("无法确定当前租户或门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.InventoryLogs
            .Include(l => l.Product)
            .Include(l => l.Supplier)
            .Where(l => l.TenantId == tenantId && l.StoreId == storeId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(l => l.ProductId == query.ProductId.Value);
        if (query.Type.HasValue)
            queryable = queryable.Where(l => l.Type == query.Type.Value);
        if (query.SourceType.HasValue)
            queryable = queryable.Where(l => l.SourceType == query.SourceType.Value);
        if (!string.IsNullOrWhiteSpace(query.ProductName))
            queryable = queryable.Where(l => l.Product != null && l.Product.Name.Contains(query.ProductName));
        if (query.StartDate.HasValue)
            queryable = queryable.Where(l => l.CreatedTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            // EndDate 含当日：过滤条件为 CreatedTime <= EndDate 当天 23:59:59
            queryable = queryable.Where(l => l.CreatedTime <= query.EndDate.Value.Date.AddDays(1).AddTicks(-1));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(l => l.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 内存中投影到 DTO，填充显示字段（Adapt 会自动映射同名字段，手工补齐关联字段）
        var dtoList = items.Select(l => new InventoryLogDto
        {
            Id = l.Id,
            ProductId = l.ProductId,
            Type = l.Type,
            SourceType = l.SourceType,
            SupplierId = l.SupplierId,
            UnitPrice = l.UnitPrice,
            Quantity = l.Quantity,
            BeforeQuantity = l.BeforeQuantity,
            AfterQuantity = l.AfterQuantity,
            BatchNo = l.BatchNo,
            ExpirationDate = l.ExpirationDate,
            RelatedId = l.RelatedId,
            Remark = l.Remark,
            CreatedAt = l.CreatedTime,
            UpdatedAt = l.UpdatedTime,
            ProductName = l.Product?.Name,
            ProductCode = l.Product?.Code,
            SupplierName = l.Supplier?.Name,
            OperatorName = l.OperatorName
        }).ToList();

        var result = new PagedResponseDto<InventoryLogDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryLogDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存流水详情
    /// </summary>
    public async Task<ApiResponseDto<InventoryLogDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryLogDto?>.Fail("无法确定当前租户或门店", 401);

        var entity = await _dbContext.InventoryLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == _currentUser.TenantId.Value && l.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto<InventoryLogDto?>.Fail("库存流水不存在", 404);
        return ApiResponseDto<InventoryLogDto?>.Ok(entity.Adapt<InventoryLogDto>());
    }

    /// <summary>
    /// 创建库存流水
    /// </summary>
    /// <remarks>
    /// 业务约束：
    /// 1. 禁止手动创建"销售出库类"流水（SalesOutbound / TreatmentCardOutbound），
    ///    这两类流水必须由 OrderAppService / TreatmentCardVerifyAppService 在事务内绑定订单写入，
    ///    杜绝"销售出库无订单"的数据异常。
    /// 2. 允许手动创建的出库类型：调拨出库、盘点盘亏、采购退货、样品赠品、其他。
    /// </remarks>
    public async Task<ApiResponseDto<InventoryLogDto>> CreateAsync(InventoryLogCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryLogDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        // 销售出库类流水禁止手动创建，必须通过订单/核销流程写入
        if (dto.Type == 2 && dto.SourceType.HasValue
            && InventoryLogSourceTypes.IsSalesCategory(dto.SourceType.Value))
        {
            return ApiResponseDto<InventoryLogDto>.Fail(
                $"销售出库类流水（SourceType={dto.SourceType}）必须通过订单/疗程卡核销流程创建，禁止手动录入", 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = dto.Adapt<InventoryLogEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.CreatedTime = DateTime.Now;

        _dbContext.InventoryLogs.Add(entity);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<InventoryLogDto>.Ok(entity.Adapt<InventoryLogDto>(), "创建成功");
    }

    /// <summary>
    /// 更新库存流水
    /// </summary>
    public async Task<ApiResponseDto<InventoryLogDto>> UpdateAsync(InventoryLogUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryLogDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.InventoryLogs
            .FirstOrDefaultAsync(l => l.Id == dto.Id && l.TenantId == tenantId && l.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<InventoryLogDto>.Fail("库存流水不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.Type = dto.Type;
        entity.SourceType = dto.SourceType;
        entity.SupplierId = dto.SupplierId;
        entity.UnitPrice = dto.UnitPrice;
        entity.Quantity = dto.Quantity;
        entity.BeforeQuantity = dto.BeforeQuantity;
        entity.AfterQuantity = dto.AfterQuantity;
        entity.BatchNo = dto.BatchNo;
        entity.ExpirationDate = dto.ExpirationDate;
        entity.RelatedId = dto.RelatedId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<InventoryLogDto>.Ok(entity.Adapt<InventoryLogDto>(), "更新成功");
    }

    /// <summary>
    /// 删除库存流水（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);

        var entity = await _dbContext.InventoryLogs
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == _currentUser.TenantId.Value && l.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存流水不存在", 404);

        _dbContext.InventoryLogs.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存流水（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.InventoryLogs
            .Where(l => ids.Contains(l.Id) && l.TenantId == _currentUser.TenantId.Value && l.StoreId == _currentUser.StoreId.Value)
            .ToListAsync();

        _dbContext.InventoryLogs.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
