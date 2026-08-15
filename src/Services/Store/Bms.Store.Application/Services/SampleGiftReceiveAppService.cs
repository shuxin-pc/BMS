using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGifts;
using Bms.Store.Domain.Entities;
using SampleGiftReceiveEntity = Bms.Store.Domain.Entities.SampleGiftReceive;
using InventoryBatchEntity = Bms.Store.Domain.Entities.InventoryBatch;
using InventoryEntity = Bms.Store.Domain.Entities.Inventory;
using InventoryLogEntity = Bms.Store.Domain.Entities.InventoryLog;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品领用记录应用服务实现
/// </summary>
public class SampleGiftReceiveAppService : ISampleGiftReceiveAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<SampleGiftReceiveCreateDto> _createValidator;
    private readonly IValidator<SampleGiftReceiveUpdateDto> _updateValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public SampleGiftReceiveAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<SampleGiftReceiveCreateDto> createValidator,
        IValidator<SampleGiftReceiveUpdateDto> updateValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取样品领用记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleGiftReceiveDto>>> GetPagedListAsync(SampleGiftReceiveQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleGiftReceiveDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.SampleGiftReceives
            .Include(r => r.Activity)
            .Where(r => r.TenantId == tenantId && r.StoreId == storeId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(r => r.ProductId == query.ProductId.Value);
        if (query.CustomerId.HasValue)
            queryable = queryable.Where(r => r.CustomerId == query.CustomerId.Value);
        if (query.ActivityId.HasValue)
            queryable = queryable.Where(r => r.ActivityId == query.ActivityId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(r => r.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = items.Adapt<List<SampleGiftReceiveDto>>();
        // Adapt 无法自动映射导航属性 Activity.Name -> ActivityName，手动填充
        foreach (var dto in dtos)
        {
            var entity = items.FirstOrDefault(e => e.Id == dto.Id);
            dto.ActivityName = entity?.Activity?.Name;
        }

        var result = new PagedResponseDto<SampleGiftReceiveDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SampleGiftReceiveDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取样品领用记录详情
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftReceiveDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SampleGiftReceiveDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.SampleGiftReceives
            .Include(r => r.Activity)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId.Value && r.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<SampleGiftReceiveDto?>.Fail("样品领用记录不存在", 404);
        var dto = entity.Adapt<SampleGiftReceiveDto>();
        dto.ActivityName = entity.Activity?.Name;
        return ApiResponseDto<SampleGiftReceiveDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建样品领用记录
    /// 同时扣减指定批次的库存，创建出库流水
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftReceiveDto>> CreateAsync(SampleGiftReceiveCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 验证商品存在且为样品/赠品类型
        var product = await _dbContext.Products
            .Include(p => p.Master)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId);
        if (product == null)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail("商品档案不存在", 404);

        // 修复遗留隐患：补充商品类型校验，仅允许样品(4)或赠品(5)类型商品创建领用记录
        if (product.Master.Type != 4 && product.Master.Type != 5)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail("仅支持样品或赠品类型的商品", 400);

        // 验证 InventoryBatch 存在且属于该商品的该门店
        var batch = await _dbContext.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == dto.InventoryBatchId
                && b.ProductId == dto.ProductId
                && b.TenantId == tenantId
                && b.StoreId == storeId
                && b.Status == 1);
        if (batch == null)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail("库存批次不存在或不可用", 404);

        // 客户可选：传入值时校验客户存在且属本租户，同时获取客户信息用于流水备注展示
        Customer? customer = null;
        if (dto.CustomerId.HasValue)
        {
            customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == dto.CustomerId.Value && !c.IsDeleted && c.TenantId == tenantId);
            if (customer == null)
                return ApiResponseDto<SampleGiftReceiveDto>.Fail("客户不存在", 404);
        }

        // 关联活动可选：传入值时校验活动存在且未删除（软删除活动历史记录仍可显示）
        if (dto.ActivityId.HasValue)
        {
            var activityExists = await _dbContext.Activities
                .AnyAsync(a => a.Id == dto.ActivityId.Value && !a.IsDeleted && a.TenantId == tenantId && a.StoreId == storeId);
            if (!activityExists)
                return ApiResponseDto<SampleGiftReceiveDto>.Fail("关联活动不存在", 404);
        }

        // 验证批次库存充足
        if (batch.Quantity < dto.Quantity)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail($"批次库存不足（当前库存：{batch.Quantity}，需扣减：{dto.Quantity}）", 400);

        // 扣减批次库存
        var beforeQuantity = batch.Quantity;
        batch.Quantity -= dto.Quantity;
        if (batch.Quantity == 0)
            batch.Status = 2; // 已用完
        batch.UpdatedTime = DateTime.Now;

        // 更新 Inventory 汇总表
        var inventory = await _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId && i.TenantId == tenantId && i.StoreId == storeId);
        if (inventory != null)
        {
            inventory.Quantity -= dto.Quantity;
            inventory.UpdatedTime = DateTime.Now;
        }

        // 创建 InventoryLog（出库流水）
        // P-SG-02：样品领用单独标记 SourceType=SampleReceiveOutbound，与赠品出库（GiftOutbound）区分
        // 两者均为非销售出库，不计入销售额与主营成本（日结按 IsSampleGiftCategory 归入"样品赠品费用"维度）
        _dbContext.InventoryLogs.Add(new InventoryLogEntity
        {
            ProductId = dto.ProductId,
            Type = 2, // 出库
            SourceType = InventoryLogSourceTypes.SampleReceiveOutbound,
            Quantity = -dto.Quantity, // 负数表示减少
            BeforeQuantity = beforeQuantity,
            AfterQuantity = batch.Quantity,
            BatchNo = batch.BatchNo,
            ExpirationDate = batch.ExpirationDate,
            ActivityId = dto.ActivityId,
            Remark = customer != null
                ? $"{customer.Name}({customer.Phone})"
                : null,
            OperatorId = _currentUser.UserId,
            OperatorName = _currentUser.RealName ?? _currentUser.UserName,
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            StoreId = storeId,
            CreatedTime = DateTime.Now
        });

        // 创建 SampleGiftReceive 记录
        var entity = dto.Adapt<SampleGiftReceiveEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.CreatedTime = DateTime.Now;

        _dbContext.SampleGiftReceives.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 即时检测预警（失败不影响领用结果，定时任务兜底）
        try
        {
            await _alertAppService.CheckInventoryAlertsAsync(tenantId, storeId, dto.ProductId);
        }
        catch
        {
            // 预警检测失败不影响主流程
        }

        return ApiResponseDto<SampleGiftReceiveDto>.Ok(entity.Adapt<SampleGiftReceiveDto>(), "创建成功");
    }

    /// <summary>
    /// 更新样品领用记录
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftReceiveDto>> UpdateAsync(SampleGiftReceiveUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.SampleGiftReceives
            .FirstOrDefaultAsync(r => r.Id == dto.Id && r.TenantId == tenantId && r.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<SampleGiftReceiveDto>.Fail("样品领用记录不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.InventoryBatchId = dto.InventoryBatchId;
        entity.CustomerId = dto.CustomerId;
        entity.Quantity = dto.Quantity;
        entity.ReceiveTime = dto.ReceiveTime;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SampleGiftReceiveDto>.Ok(entity.Adapt<SampleGiftReceiveDto>(), "更新成功");
    }

    /// <summary>
    /// 删除样品领用记录（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.SampleGiftReceives
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == _currentUser.TenantId.Value && r.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto.Fail("样品领用记录不存在", 404);

        _dbContext.SampleGiftReceives.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除样品领用记录（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.SampleGiftReceives
            .Where(r => ids.Contains(r.Id) && r.TenantId == _currentUser.TenantId.Value && r.StoreId == (_currentUser.StoreId ?? 0))
            .ToListAsync();

        _dbContext.SampleGiftReceives.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
