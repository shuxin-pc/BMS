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
    /// 查询时 Include Product 与 Supplier 导航属性，内存中填充显示字段。
    /// 流水抽屉场景（ProductId 有值）：后端按该商品全量流水累加计算批次库存与商品总库存，
    /// 筛选只影响显示行不影响累加基准，保证累加值可追溯验证；
    /// 其他场景（ProductId 无值）：沿用 IQueryable 筛选 + 分页，4 个累加字段返回 null。
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryLogDto>>> GetPagedListAsync(InventoryLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryLogDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.InventoryLogs
            .Include(l => l.Product).ThenInclude(p => p.Master!)
            .Include(l => l.Supplier)
            .Where(l => l.TenantId == tenantId && l.StoreId == storeId);

        // 流水抽屉场景：按商品查全量流水，内存累加计算批次/商品总库存后再筛选分页
        if (query.ProductId.HasValue)
        {
            var productId = query.ProductId.Value;
            var allLogs = await queryable
                .Where(l => l.ProductId == productId)
                .OrderBy(l => l.CreatedTime).ThenBy(l => l.Id)
                .ToListAsync();

            // 累加计算：batchRunning 按 BatchNo 分组累加，totalRunning 按商品累加。
            // BatchNo 为 null 的行不参与批次累加（BatchBefore/After 返回 null）。
            var batchRunning = new Dictionary<string, decimal>();
            decimal totalRunning = 0;
            var enriched = new List<(InventoryLogEntity Log, decimal? BatchBefore, decimal? BatchAfter, decimal TotalBefore, decimal TotalAfter)>(allLogs.Count);
            foreach (var log in allLogs)
            {
                decimal? batchBefore = null, batchAfter = null;
                if (!string.IsNullOrEmpty(log.BatchNo))
                {
                    batchBefore = batchRunning.GetValueOrDefault(log.BatchNo, 0);
                    batchAfter = batchBefore + log.Quantity;
                    batchRunning[log.BatchNo] = batchAfter.Value;
                }
                var totalBefore = totalRunning;
                var totalAfter = totalRunning + log.Quantity;
                totalRunning = totalAfter;
                enriched.Add((log, batchBefore, batchAfter, totalBefore, totalAfter));
            }

            // 应用筛选（累加基于全量流水，筛选只影响显示行）
            IEnumerable<(InventoryLogEntity Log, decimal? BatchBefore, decimal? BatchAfter, decimal TotalBefore, decimal TotalAfter)> filtered = enriched;
            if (query.Type.HasValue)
                filtered = filtered.Where(x => x.Log.Type == query.Type.Value);
            if (query.SourceType.HasValue)
                filtered = filtered.Where(x => x.Log.SourceType == query.SourceType.Value);
            if (!string.IsNullOrWhiteSpace(query.ProductName))
                filtered = filtered.Where(x => x.Log.Product != null && x.Log.Product.Master != null && x.Log.Product.Master.Name.Contains(query.ProductName));
            if (!string.IsNullOrWhiteSpace(query.BatchNo))
                filtered = filtered.Where(x => x.Log.BatchNo != null && x.Log.BatchNo.Contains(query.BatchNo));
            if (query.StartDate.HasValue)
                filtered = filtered.Where(x => x.Log.CreatedTime >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                // EndDate 含当日：过滤条件为 CreatedTime <= EndDate 当天 23:59:59
                filtered = filtered.Where(x => x.Log.CreatedTime <= query.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var filteredTotal = filtered.Count();
            // 展示排序：时间倒序 + 同时间块内按 Id 倒序（与累加正序严格反向），
            // 保证同时间操作的多条流水"商品总库存"列从上到下连续，
            // 最上方为该时间块最后执行的结果：入库块总量大的在上、出库块总量小的在上。
            var pageItems = filtered
                .OrderByDescending(x => x.Log.CreatedTime)
                .ThenByDescending(x => x.Log.Id)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var dtoList = pageItems.Select(x => new InventoryLogDto
            {
                Id = x.Log.Id,
                ProductId = x.Log.ProductId,
                Type = x.Log.Type,
                SourceType = x.Log.SourceType,
                SupplierId = x.Log.SupplierId,
                UnitPrice = x.Log.UnitPrice,
                Quantity = x.Log.Quantity,
                BeforeQuantity = x.Log.BeforeQuantity,
                AfterQuantity = x.Log.AfterQuantity,
                BatchNo = x.Log.BatchNo,
                ExpirationDate = x.Log.ExpirationDate,
                RelatedId = x.Log.RelatedId,
                Remark = x.Log.Remark,
                CreatedAt = x.Log.CreatedTime,
                UpdatedAt = x.Log.UpdatedTime,
                ProductName = x.Log.Product?.Master?.Name,
                ProductCode = x.Log.Product?.Master?.Code,
                SupplierName = x.Log.Supplier?.Name,
                OperatorId = x.Log.OperatorId,
                OperatorName = x.Log.OperatorName,
                BatchBeforeQuantity = x.BatchBefore,
                BatchAfterQuantity = x.BatchAfter,
                TotalBeforeQuantity = x.TotalBefore,
                TotalAfterQuantity = x.TotalAfter
            }).ToList();

            var result = new PagedResponseDto<InventoryLogDto>
            {
                List = dtoList,
                Total = filteredTotal,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };
            return ApiResponseDto<PagedResponseDto<InventoryLogDto>>.Ok(result);
        }

        // 其他场景：沿用 IQueryable 筛选 + 分页逻辑
        if (query.Type.HasValue)
            queryable = queryable.Where(l => l.Type == query.Type.Value);
        if (query.SourceType.HasValue)
            queryable = queryable.Where(l => l.SourceType == query.SourceType.Value);
        if (!string.IsNullOrWhiteSpace(query.ProductName))
            queryable = queryable.Where(l => l.Product != null && l.Product.Master != null && l.Product.Master.Name.Contains(query.ProductName));
        if (!string.IsNullOrWhiteSpace(query.BatchNo))
            queryable = queryable.Where(l => l.BatchNo != null && l.BatchNo.Contains(query.BatchNo));
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

        // 内存中投影到 DTO，填充显示字段（4 个累加字段保持默认 null：无 productId 查询时不计算）
        var dtoList2 = items.Select(l => new InventoryLogDto
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
            ProductName = l.Product?.Master?.Name,
            ProductCode = l.Product?.Master?.Code,
            SupplierName = l.Supplier?.Name,
            OperatorId = l.OperatorId,
            OperatorName = l.OperatorName
        }).ToList();

        var result2 = new PagedResponseDto<InventoryLogDto>
        {
            List = dtoList2,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryLogDto>>.Ok(result2);
    }

    /// <summary>
    /// 根据ID获取库存流水详情
    /// </summary>
    public async Task<ApiResponseDto<InventoryLogDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryLogDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.InventoryLogs
            .Include(l => l.Product).ThenInclude(p => p.Master!)
            .Include(l => l.Supplier)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == _currentUser.TenantId.Value && l.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto<InventoryLogDto?>.Fail("库存流水不存在", 404);

        var dto = new InventoryLogDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            Type = entity.Type,
            SourceType = entity.SourceType,
            SupplierId = entity.SupplierId,
            UnitPrice = entity.UnitPrice,
            Quantity = entity.Quantity,
            BeforeQuantity = entity.BeforeQuantity,
            AfterQuantity = entity.AfterQuantity,
            BatchNo = entity.BatchNo,
            ExpirationDate = entity.ExpirationDate,
            RelatedId = entity.RelatedId,
            Remark = entity.Remark,
            CreatedAt = entity.CreatedTime,
            UpdatedAt = entity.UpdatedTime,
            ProductName = entity.Product?.Master?.Name,
            ProductCode = entity.Product?.Master?.Code,
            SupplierName = entity.Supplier?.Name,
            OperatorId = entity.OperatorId,
            OperatorName = entity.OperatorName
        };
        return ApiResponseDto<InventoryLogDto?>.Ok(dto);
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
            return ApiResponseDto<InventoryLogDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        // 销售出库类流水禁止手动创建，必须通过订单/核销流程写入
        if (dto.Type == 2 && dto.SourceType.HasValue
            && InventoryLogSourceTypes.IsSalesCategory(dto.SourceType.Value))
        {
            return ApiResponseDto<InventoryLogDto>.Fail(
                $"销售出库类流水（SourceType={dto.SourceType}）必须通过订单/项目卡核销流程创建，禁止手动录入", 400);
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
            return ApiResponseDto<InventoryLogDto>.Fail("登录状态异常，请重新登录", 401);

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
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

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
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
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
