using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Inventories;
using Bms.Store.Domain.Entities;
using InventoryCheckEntity = Bms.Store.Domain.Entities.InventoryCheck;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存盘点记录应用服务实现
/// 状态机闭环：创建草稿 -> 提交（已完成，联动库存调整与流水）/ 取消（已取消，不调整库存）
/// </summary>
public class InventoryCheckAppService : IInventoryCheckAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<InventoryCheckCreateDto> _createValidator;
    private readonly IValidator<InventoryCheckUpdateDto> _updateValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public InventoryCheckAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<InventoryCheckCreateDto> createValidator,
        IValidator<InventoryCheckUpdateDto> updateValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取库存盘点记录分页列表（按当前门店隔离）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryCheckDto>>> GetPagedListAsync(InventoryCheckQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryCheckDto>>.Fail("无法确定当前租户或门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.InventoryChecks
            .Where(c => c.TenantId == tenantId && c.StoreId == storeId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(c => c.ProductId == query.ProductId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(c => c.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(c => c.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<InventoryCheckDto>
        {
            List = items.Adapt<List<InventoryCheckDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryCheckDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存盘点记录详情
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto?>.Fail("无法确定当前租户或门店", 401);

        var entity = await _dbContext.InventoryChecks
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _currentUser.TenantId.Value && c.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto<InventoryCheckDto?>.Fail("库存盘点记录不存在", 404);
        return ApiResponseDto<InventoryCheckDto?>.Ok(entity.Adapt<InventoryCheckDto>());
    }

    /// <summary>
    /// 创建库存盘点单（草稿状态，不调整库存）
    /// 录入商品与账面数量，实际数量可留空待提交时录入
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto>> CreateAsync(InventoryCheckCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryCheckDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        var entity = dto.Adapt<InventoryCheckEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.Status = InventoryCheckStatus.Draft; // 草稿
        entity.DiffQuantity = 0; // 草稿阶段不计算差异
        entity.CreatedTime = now;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.InventoryChecks.Add(entity);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<InventoryCheckDto>.Ok(entity.Adapt<InventoryCheckDto>(), "创建成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新库存盘点记录（仅草稿状态可修改）
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto>> UpdateAsync(InventoryCheckUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryCheckDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.InventoryChecks
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.TenantId == tenantId && c.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<InventoryCheckDto>.Fail("库存盘点记录不存在", 404);

        if (entity.Status != InventoryCheckStatus.Draft)
            return ApiResponseDto<InventoryCheckDto>.Fail($"仅草稿状态的盘点单可修改（当前状态：{entity.Status}）", 400);

        entity.ProductId = dto.ProductId;
        entity.BeforeQuantity = dto.BeforeQuantity;
        entity.ActualQuantity = dto.ActualQuantity;
        entity.CheckTime = dto.CheckTime;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<InventoryCheckDto>.Ok(entity.Adapt<InventoryCheckDto>(), "更新成功");
    }

    /// <summary>
    /// 提交盘点单：录入实际数量，计算差异，自动调整库存并写入 InventoryLog，状态转已完成
    /// 仅草稿状态可提交；已完成/已取消为终态，不可再提交
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto>> SubmitCheckAsync(SubmitCheckDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto>.Fail("无法确定当前租户或门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        var entity = await _dbContext.InventoryChecks
            .FirstOrDefaultAsync(c => c.Id == dto.Id && c.TenantId == tenantId && c.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<InventoryCheckDto>.Fail("库存盘点记录不存在", 404);

        if (entity.Status != InventoryCheckStatus.Draft)
            return ApiResponseDto<InventoryCheckDto>.Fail($"仅草稿状态的盘点单可提交（当前状态：{entity.Status}）", 400);

        // 计算差异（实际 - 账面，正数盘盈，负数盘亏）
        var diffQuantity = dto.ActualQuantity - entity.BeforeQuantity;
        entity.ActualQuantity = dto.ActualQuantity;
        entity.DiffQuantity = diffQuantity;
        entity.Status = InventoryCheckStatus.Completed; // 已完成
        entity.CheckTime = now;
        entity.UpdatedTime = now;
        if (!string.IsNullOrWhiteSpace(dto.Remark))
            entity.Remark = dto.Remark;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 差异不为零时调整库存并记录流水
            if (diffQuantity != 0)
            {
                var inventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == entity.ProductId && inv.TenantId == tenantId && inv.StoreId == storeId);

                var beforeQty = inventory?.Quantity ?? 0;
                if (inventory == null)
                {
                    // 盘盈时可能尚无库存记录，新建
                    inventory = new Inventory
                    {
                        ProductId = entity.ProductId,
                        Quantity = diffQuantity,
                        AlertQuantity = 0,
                        TenantId = tenantId,
                        TenantCode = tenantCode,
                        StoreId = storeId,
                        CreatedTime = now
                    };
                    _dbContext.Inventories.Add(inventory);
                }
                else
                {
                    inventory.Quantity += diffQuantity;
                    inventory.UpdatedTime = now;
                }

                // 记录库存流水（盘盈=入库 Type=1，盘亏=出库 Type=2，SourceType 均为盘点调整）
                _dbContext.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = entity.ProductId,
                    Type = diffQuantity > 0 ? 1 : 2,
                    SourceType = InventoryLogSourceTypes.CheckAdjustment, // 盘点调整（盘盈入库/盘亏出库均使用此值）
                    Quantity = diffQuantity,
                    BeforeQuantity = beforeQty,
                    AfterQuantity = beforeQty + diffQuantity,
                    RelatedId = entity.Id,
                    Remark = $"盘点差异-{(diffQuantity > 0 ? "盘盈" : "盘亏")}",
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    CreatedTime = now
                });
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 盘亏后即时检测低库存预警
            if (diffQuantity < 0)
            {
                try
                {
                    await _alertAppService.CheckLowStockAsync(tenantId, storeId, entity.ProductId);
                }
                catch
                {
                    // 预警检测失败不影响主流程，定时任务会兜底
                }
            }

            return ApiResponseDto<InventoryCheckDto>.Ok(entity.Adapt<InventoryCheckDto>(), "盘点提交成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 取消盘点单：草稿转已取消（已完成的不可取消，需走反向盘点单）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.InventoryChecks
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && c.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto.Fail("库存盘点记录不存在", 404);

        if (entity.Status != InventoryCheckStatus.Draft)
            return ApiResponseDto.Fail($"仅草稿状态的盘点单可取消（当前状态：{entity.Status}），已完成盘点不可取消", 400);

        entity.Status = InventoryCheckStatus.Cancelled; // 已取消
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "取消成功");
    }

    /// <summary>
    /// 删除库存盘点记录（仅草稿/已取消可删除；已完成不可删除，需走反向盘点）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);

        var entity = await _dbContext.InventoryChecks
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _currentUser.TenantId.Value && c.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存盘点记录不存在", 404);

        if (entity.Status == InventoryCheckStatus.Completed)
            return ApiResponseDto.Fail("已完成的盘点单不可删除，如需调整请创建反向盘点单", 400);

        _dbContext.InventoryChecks.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存盘点记录（仅草稿/已取消可删除；已完成会被跳过并在消息中提示）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户或门店", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.InventoryChecks
            .Where(c => ids.Contains(c.Id) && c.TenantId == _currentUser.TenantId.Value && c.StoreId == _currentUser.StoreId.Value)
            .ToListAsync();

        var deletable = entities.Where(c => c.Status != InventoryCheckStatus.Completed).ToList();
        var skipped = entities.Count - deletable.Count;

        _dbContext.InventoryChecks.RemoveRange(deletable);
        await _dbContext.SaveChangesAsync();

        var message = $"成功删除 {deletable.Count} 条数据";
        if (skipped > 0)
            message += $"，跳过 {skipped} 条已完成盘点（不可删除）";

        return ApiResponseDto.Success(null, message);
    }
}
