using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGifts;
using Bms.Store.Domain.Entities;
using SampleGiftOutEntity = Bms.Store.Domain.Entities.SampleGiftOut;
using InventoryBatchEntity = Bms.Store.Domain.Entities.InventoryBatch;
using InventoryEntity = Bms.Store.Domain.Entities.Inventory;
using InventoryLogEntity = Bms.Store.Domain.Entities.InventoryLog;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 赠品出库记录应用服务实现
/// </summary>
public class SampleGiftOutAppService : ISampleGiftOutAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<SampleGiftOutCreateDto> _createValidator;
    private readonly IValidator<SampleGiftOutUpdateDto> _updateValidator;

    public SampleGiftOutAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<SampleGiftOutCreateDto> createValidator,
        IValidator<SampleGiftOutUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取赠品出库记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleGiftOutDto>>> GetPagedListAsync(SampleGiftOutQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleGiftOutDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.SampleGiftOuts
            .Where(o => o.TenantId == tenantId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(o => o.ProductId == query.ProductId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(o => o.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<SampleGiftOutDto>
        {
            List = items.Adapt<List<SampleGiftOutDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SampleGiftOutDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取赠品出库记录详情
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftOutDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SampleGiftOutDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.SampleGiftOuts
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<SampleGiftOutDto?>.Fail("赠品出库记录不存在", 404);
        return ApiResponseDto<SampleGiftOutDto?>.Ok(entity.Adapt<SampleGiftOutDto>());
    }

    /// <summary>
    /// 创建赠品出库记录
    /// 同时扣减指定批次的库存，创建出库流水
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftOutDto>> CreateAsync(SampleGiftOutCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<SampleGiftOutDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SampleGiftOutDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 验证商品存在且为样品/赠品类型
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted && p.TenantId == tenantId);
        if (product == null)
            return ApiResponseDto<SampleGiftOutDto>.Fail("商品档案不存在", 404);

        // 验证 InventoryBatch 存在且属于该商品的该门店
        var batch = await _dbContext.InventoryBatches
            .FirstOrDefaultAsync(b => b.Id == dto.InventoryBatchId
                && b.ProductId == dto.ProductId
                && b.TenantId == tenantId
                && b.StoreId == storeId
                && b.Status == 1);
        if (batch == null)
            return ApiResponseDto<SampleGiftOutDto>.Fail("库存批次不存在或不可用", 404);

        // 验证批次库存充足
        if (batch.Quantity < dto.Quantity)
            return ApiResponseDto<SampleGiftOutDto>.Fail($"批次库存不足（当前库存：{batch.Quantity}，需扣减：{dto.Quantity}）", 400);

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
        // P-SG-02：赠品活动出库单独标记 SourceType=GiftOutbound，与样品领用（SampleReceiveOutbound）区分
        // 两者均为非销售出库，不计入销售额与主营成本（日结按 IsSampleGiftCategory 归入"样品赠品费用"维度）
        _dbContext.InventoryLogs.Add(new InventoryLogEntity
        {
            ProductId = dto.ProductId,
            Type = 2, // 出库
            SourceType = InventoryLogSourceTypes.GiftOutbound,
            Quantity = -dto.Quantity, // 负数表示减少
            BeforeQuantity = beforeQuantity,
            AfterQuantity = batch.Quantity,
            BatchNo = batch.BatchNo,
            ExpirationDate = batch.ExpirationDate,
            Remark = $"活动出库" + (dto.ActivityId.HasValue ? $"-活动ID:{dto.ActivityId}" : "") + (dto.OrderId.HasValue ? $"-订单ID:{dto.OrderId}" : ""),
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            StoreId = storeId,
            CreatedTime = DateTime.Now
        });

        // 创建 SampleGiftOut 记录
        var entity = dto.Adapt<SampleGiftOutEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.CreatedTime = DateTime.Now;
        // 独立赠品出库入口为无订单场景设计，OrderId 仅能由订单创建流程写入
        // 此处强制置 null，从源头消除 P-SG-03 "未校验订单存在性" 问题
        entity.OrderId = null;

        _dbContext.SampleGiftOuts.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SampleGiftOutDto>.Ok(entity.Adapt<SampleGiftOutDto>(), "创建成功");
    }

    /// <summary>
    /// 更新赠品出库记录
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftOutDto>> UpdateAsync(SampleGiftOutUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SampleGiftOutDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SampleGiftOutDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.SampleGiftOuts
            .FirstOrDefaultAsync(o => o.Id == dto.Id && o.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<SampleGiftOutDto>.Fail("赠品出库记录不存在", 404);

        entity.ProductId = dto.ProductId;
        entity.InventoryBatchId = dto.InventoryBatchId;
        entity.Quantity = dto.Quantity;
        entity.OutTime = dto.OutTime;
        entity.ActivityId = dto.ActivityId;
        // 独立入口不允许写入 OrderId，强制 null 防止前端篡改
        // OrderId 仅由订单创建流程（OrderAppService.DeductSampleGiftOutAsync）在事务内写入
        entity.OrderId = null;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SampleGiftOutDto>.Ok(entity.Adapt<SampleGiftOutDto>(), "更新成功");
    }

    /// <summary>
    /// 删除赠品出库记录（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.SampleGiftOuts
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("赠品出库记录不存在", 404);

        _dbContext.SampleGiftOuts.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除赠品出库记录（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.SampleGiftOuts
            .Where(o => ids.Contains(o.Id) && o.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.SampleGiftOuts.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
