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
    private readonly IValidator<SubmitCheckDto> _submitValidator;
    private readonly IValidator<CreateAndSubmitCheckDto> _createAndSubmitValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public InventoryCheckAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<InventoryCheckCreateDto> createValidator,
        IValidator<InventoryCheckUpdateDto> updateValidator,
        IValidator<SubmitCheckDto> submitValidator,
        IValidator<CreateAndSubmitCheckDto> createAndSubmitValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _submitValidator = submitValidator;
        _createAndSubmitValidator = createAndSubmitValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取库存盘点记录分页列表（按当前门店隔离）
    /// 联表 Product 获取展示字段（ProductName/ProductCode），批次与差异金额读 InventoryCheck 持久化字段
    /// 支持 ProductName 模糊搜索、StartDate/EndDate 日期范围、ProductId/Status 筛选
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<InventoryCheckDto>>> GetPagedListAsync(InventoryCheckQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<InventoryCheckDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        // 联表 Product 以输出展示字段并支持商品名称筛选
        var queryable = from c in _dbContext.InventoryChecks
                        join p in _dbContext.Products on c.ProductId equals p.Id
                        where c.TenantId == tenantId && c.StoreId == storeId
                            && !p.IsDeleted
                        select new { c, p };

        if (query.ProductId.HasValue)
            queryable = queryable.Where(x => x.c.ProductId == query.ProductId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(x => x.c.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.ProductName))
            queryable = queryable.Where(x => x.p.Master.Name.Contains(query.ProductName));
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.c.CheckTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(x => x.c.CheckTime <= query.EndDate.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.c.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new InventoryCheckDto
            {
                Id = x.c.Id,
                ProductId = x.c.ProductId,
                BeforeQuantity = x.c.BeforeQuantity,
                ActualQuantity = x.c.ActualQuantity,
                DiffQuantity = x.c.DiffQuantity,
                CheckTime = x.c.CheckTime,
                OperatorId = x.c.OperatorId,
                OperatorName = x.c.OperatorName,
                Status = x.c.Status,
                Remark = x.c.Remark,
                CreatedAt = x.c.CreatedTime,
                UpdatedAt = x.c.UpdatedTime,
                ProductName = x.p.Master.Name,
                ProductCode = x.p.Master.Code,
                UnitCost = x.c.UnitPrice,
                DiffAmount = x.c.DiffAmount,
                BatchNo = x.c.BatchNo,
                ExpirationDate = x.c.ExpirationDate
            })
            .ToListAsync();

        // 批量加载本次盘点批次明细（一次查询避免 N+1）
        var checkIds = items.Select(i => i.Id).ToList();
        var batchDetails = await _dbContext.InventoryCheckBatches
            .Where(b => checkIds.Contains(b.CheckId) && b.TenantId == tenantId && b.StoreId == storeId)
            .OrderBy(b => b.CreatedTime)
            .Select(b => new
            {
                b.CheckId,
                Dto = new InventoryCheckBatchDto
                {
                    Id = b.Id,
                    BatchId = b.BatchId,
                    BatchNo = b.BatchNo,
                    Quantity = b.Quantity,
                    UnitPrice = b.UnitPrice,
                    ExpirationDate = b.ExpirationDate
                }
            })
            .ToListAsync();
        var detailsByCheck = batchDetails.GroupBy(x => x.CheckId).ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());
        foreach (var item in items)
        {
            if (detailsByCheck.TryGetValue(item.Id, out var details))
                item.Batches = details;
        }

        var result = new PagedResponseDto<InventoryCheckDto>
        {
            List = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<InventoryCheckDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存盘点记录详情（联查商品名称/编码，加载批次明细）
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        var entity = await _dbContext.InventoryChecks
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId && c.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<InventoryCheckDto?>.Fail("库存盘点记录不存在", 404);

        // 联查商品展示字段（Include Master 导航，未启用延迟加载，否则 Master 为 null）
        var product = await _dbContext.Products
            .Include(p => p.Master)
            .FirstOrDefaultAsync(p => p.Id == entity.ProductId && p.TenantId == tenantId && p.StoreId == storeId);

        // 加载本次盘点批次明细
        var batches = await _dbContext.InventoryCheckBatches
            .Where(b => b.CheckId == id && b.TenantId == tenantId && b.StoreId == storeId)
            .OrderBy(b => b.CreatedTime)
            .Select(b => new InventoryCheckBatchDto
            {
                Id = b.Id,
                BatchId = b.BatchId,
                BatchNo = b.BatchNo,
                Quantity = b.Quantity,
                UnitPrice = b.UnitPrice,
                ExpirationDate = b.ExpirationDate
            })
            .ToListAsync();

        var dto = entity.Adapt<InventoryCheckDto>();
        dto.ProductName = product?.Master?.Name;
        dto.ProductCode = product?.Master?.Code;
        dto.Batches = batches;
        return ApiResponseDto<InventoryCheckDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建库存盘点单（草稿状态，不调整库存）
    /// 录入商品与账面数量，实际数量可留空待提交时录入
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto>> CreateAsync(InventoryCheckCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto>.Fail("登录状态异常，请重新登录", 401);

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
        entity.OperatorId = _currentUser.UserId;
        entity.OperatorName = _currentUser.RealName ?? _currentUser.UserName;
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
    /// 创建并提交盘点单（原子操作）：事务内完成创建+提交，不产生草稿残留
    /// 按差异方向分支：盘亏扣批次/盘盈累加批次/无差异仅记录
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto>> CreateAndSubmitAsync(CreateAndSubmitCheckDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createAndSubmitValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryCheckDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        var entity = new InventoryCheckEntity
        {
            ProductId = dto.ProductId,
            BeforeQuantity = dto.BeforeQuantity,
            ActualQuantity = dto.ActualQuantity,
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            Status = InventoryCheckStatus.Draft, // 临时草稿，事务内由 ApplyCheckDiffAsync 转为已完成
            DiffQuantity = 0,
            OperatorId = _currentUser.UserId,
            OperatorName = _currentUser.RealName ?? _currentUser.UserName,
            CreatedTime = now
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.InventoryChecks.Add(entity);
            await _dbContext.SaveChangesAsync(); // 保存以获取 entity.Id（InventoryLog.RelatedId 需要）

            var errorMessage = await ApplyCheckDiffAsync(entity, dto.ActualQuantity, dto.DeductBatches, dto.GainBatches, dto.Remark, tenantId, storeId, tenantCode, now);
            if (errorMessage != null)
            {
                await transaction.RollbackAsync();
                return ApiResponseDto<InventoryCheckDto>.Fail(errorMessage, 400);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 盘点后即时检测预警（双向：盘亏可能触发低库存，盘盈可能触发积压）
            try
            {
                await _alertAppService.CheckInventoryAlertsAsync(tenantId, storeId, entity.ProductId);
            }
            catch
            {
                // 预警检测失败不影响主流程，定时任务会兜底
            }

            return ApiResponseDto<InventoryCheckDto>.Ok(entity.Adapt<InventoryCheckDto>(), "盘点成功");
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
            return ApiResponseDto<InventoryCheckDto>.Fail("登录状态异常，请重新登录", 401);

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
    /// 提交盘点单：录入实际数量，按差异方向分支处理库存调整与批次联动，状态转已完成
    /// - 盘亏（DiffQuantity &lt; 0）：扣减指定批次（DeductBatches）或 FIFO 兜底，按批次实际单价累加 DiffAmount
    /// - 盘盈（DiffQuantity &gt; 0）：新建盘盈批次（操作员录入属性），按录入单价计算 DiffAmount
    /// - 无差异：仅更新状态，不调整库存
    /// 仅草稿状态可提交；已完成/已取消为终态，不可再提交
    /// </summary>
    public async Task<ApiResponseDto<InventoryCheckDto>> SubmitCheckAsync(SubmitCheckDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<InventoryCheckDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _submitValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<InventoryCheckDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

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

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var errorMessage = await ApplyCheckDiffAsync(entity, dto.ActualQuantity, dto.DeductBatches, dto.GainBatches, dto.Remark, tenantId, storeId, tenantCode, now);
            if (errorMessage != null)
            {
                await transaction.RollbackAsync();
                return ApiResponseDto<InventoryCheckDto>.Fail(errorMessage, 400);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 盘点后即时检测预警（双向：盘亏可能触发低库存，盘盈可能触发积压）
            try
            {
                await _alertAppService.CheckInventoryAlertsAsync(tenantId, storeId, entity.ProductId);
            }
            catch
            {
                // 预警检测失败不影响主流程，定时任务会兜底
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
    /// 应用盘点差异到库存（盘亏扣批次/盘盈累加批次/无差异不处理）
    /// 盘亏与盘盈均将涉及批次明细写入 InventoryCheckBatches，用于追溯与详情展示
    /// 提取自 SubmitCheckAsync 和 CreateAndSubmitAsync 的公共逻辑，不含事务管理，由调用方负责事务边界
    /// 返回 null 表示成功，返回错误信息表示失败（调用方应回滚事务）
    /// </summary>
    private async Task<string?> ApplyCheckDiffAsync(
        InventoryCheckEntity entity,
        decimal actualQuantity,
        List<BatchDeductItem>? deductBatches,
        List<GainBatchItem>? gainBatches,
        string? remark,
        long tenantId, long storeId, string tenantCode, DateTime now)
    {
        // 计算差异（实际 - 账面，正数盘盈，负数盘亏）
        var diffQuantity = actualQuantity - entity.BeforeQuantity;

        // 按差异方向分支处理
        if (diffQuantity < 0)
        {
            // 盘亏：扣减批次库存
            var deductResult = await DeductBatchesForCheckAsync(entity.ProductId, -diffQuantity, deductBatches, tenantId, storeId, tenantCode, now, entity.Id);
            if (!deductResult.Success)
                return deductResult.ErrorMessage;

            entity.BatchNo = deductResult.FirstBatchNo;
            entity.UnitPrice = deductResult.FirstUnitPrice;
            entity.ExpirationDate = deductResult.FirstExpirationDate;
            entity.DiffAmount = -deductResult.TotalAmount; // 盘亏金额为负
        }
        else if (diffQuantity > 0)
        {
            // 盘盈：可累加到多个已有批次（不新建批次），每个批次分别录入累加数量
            if (gainBatches == null || gainBatches.Count == 0)
                return "盘盈必须指定至少一个累加批次";

            var batchIds = gainBatches.Select(i => i.BatchId).Distinct().ToList();
            var targetBatches = await _dbContext.InventoryBatches
                .Where(b => batchIds.Contains(b.Id)
                    && b.ProductId == entity.ProductId
                    && b.TenantId == tenantId
                    && b.StoreId == storeId)
                .ToListAsync();

            // 校验所有批次均存在且属于当前商品/门店
            var missingIds = batchIds.Except(targetBatches.Select(b => b.Id)).ToList();
            if (missingIds.Any())
                return $"批次 {string.Join(",", missingIds)} 不存在或不属于当前商品/门店";

            // 校验累加数量合计与差异数量匹配
            var totalGain = gainBatches.Sum(i => i.Quantity);
            if (totalGain != diffQuantity)
                return $"盘盈累加数量合计 {totalGain} 与差异数量 {diffQuantity} 不匹配";

            // 读取库存汇总（用于流水 BeforeQuantity 递进）
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(inv => inv.ProductId == entity.ProductId && inv.TenantId == tenantId && inv.StoreId == storeId);
            var beforeQty = inventory?.Quantity ?? 0;

            // 逐个批次累加 + 写盘点批次明细 + 写库存流水
            var firstBatchNo = string.Empty;
            decimal? firstUnitPrice = null;
            DateTime? firstExpiration = null;
            var totalAmount = 0m;

            foreach (var batch in targetBatches)
            {
                var gainQty = gainBatches.First(i => i.BatchId == batch.Id).Quantity;

                // 累加到原批次，不修改原批次属性（单价/生产日期/保质期/过期日期保持不变）
                batch.Quantity += gainQty;
                batch.Status = 1; // 盘盈恢复在库（可能从已用完状态恢复）
                batch.UpdatedTime = now;

                totalAmount += gainQty * batch.UnitPrice;

                if (firstBatchNo.Length == 0)
                {
                    firstBatchNo = batch.BatchNo;
                    firstUnitPrice = batch.UnitPrice;
                    firstExpiration = batch.ExpirationDate;
                }

                // 写盘点批次明细（盘盈累加批次）
                _dbContext.InventoryCheckBatches.Add(new InventoryCheckBatch
                {
                    CheckId = entity.Id,
                    BatchId = batch.Id,
                    BatchNo = batch.BatchNo,
                    Quantity = gainQty,
                    UnitPrice = batch.UnitPrice,
                    ExpirationDate = batch.ExpirationDate,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    CreatedTime = now
                });

                // 写库存流水（盘盈入库，Type=1，SourceType=盘点调整，用该批次信息）
                _dbContext.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = entity.ProductId,
                    Type = 1,
                    SourceType = InventoryLogSourceTypes.CheckAdjustment,
                    Quantity = gainQty,
                    BeforeQuantity = beforeQty,
                    AfterQuantity = beforeQty + gainQty,
                    BatchNo = batch.BatchNo,
                    UnitPrice = batch.UnitPrice,
                    ExpirationDate = batch.ExpirationDate,
                    RelatedId = entity.Id,
                    Remark = "盘盈入库",
                    OperatorId = _currentUser.UserId,
                    OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = storeId,
                    CreatedTime = now
                });

                beforeQty += gainQty; // 递进后续批次流水的前值
            }

            // 更新 Inventory 汇总表
            if (inventory == null)
            {
                inventory = new Inventory
                {
                    ProductId = entity.ProductId,
                    Quantity = diffQuantity,
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

            entity.BatchNo = firstBatchNo;
            entity.UnitPrice = firstUnitPrice;
            entity.ExpirationDate = firstExpiration;
            entity.DiffAmount = totalAmount; // 盘盈金额为正
        }
        else
        {
            // 无差异：不调整库存、不写批次、不写流水
            entity.BatchNo = null;
            entity.UnitPrice = null;
            entity.ExpirationDate = null;
            entity.DiffAmount = null;
        }

        entity.ActualQuantity = actualQuantity;
        entity.DiffQuantity = diffQuantity;
        entity.Status = InventoryCheckStatus.Completed; // 已完成
        entity.CheckTime = now;
        entity.UpdatedTime = now;
        if (!string.IsNullOrWhiteSpace(remark))
            entity.Remark = remark;

        return null; // 成功
    }

    /// <summary>
    /// 盘亏批次扣减内部方法（不调用 SaveChanges，由外层事务统一提交）
    /// 实现要点：
    /// 1. DeductBatches 非空时按指定批次扣减，校验数量合计与扣减总量匹配
    /// 2. DeductBatches 为空时按 FIFO（CreatedTime 升序）扣减所有在库批次
    /// 3. 扣减对应批次 Quantity，扣完置 Status=2
    /// 4. 同步扣减 Inventory 汇总表
    /// 5. 写一条汇总 InventoryLog（首批次 BatchNo/UnitPrice/ExpirationDate）
    /// 6. 累加 TotalAmount 用于 InventoryCheck.DiffAmount 持久化
    /// </summary>
    private async Task<(bool Success, string? ErrorMessage, string? FirstBatchNo, decimal? FirstUnitPrice, DateTime? FirstExpirationDate, decimal TotalAmount)> DeductBatchesForCheckAsync(
        long productId, decimal deductQty, List<BatchDeductItem>? deductBatches,
        long tenantId, long storeId, string tenantCode, DateTime now, long checkId)
    {
        List<InventoryBatch> batches;
        Dictionary<long, decimal> deductMap; // BatchId -> 扣减数量

        if (deductBatches != null && deductBatches.Count > 0)
        {
            // 指定批次模式
            var batchIds = deductBatches.Select(i => i.BatchId).Distinct().ToList();
            batches = await _dbContext.InventoryBatches
                .Where(b => batchIds.Contains(b.Id)
                    && b.ProductId == productId
                    && b.TenantId == tenantId
                    && b.StoreId == storeId
                    && b.Status == 1
                    && b.Quantity > 0)
                .ToListAsync();

            // 校验所有 BatchId 都存在且可用
            var missingIds = batchIds.Except(batches.Select(b => b.Id)).ToList();
            if (missingIds.Any())
                return (false, $"批次 {string.Join(",", missingIds)} 不存在、已用完或不属于当前商品", null, null, null, 0);

            deductMap = deductBatches.ToDictionary(i => i.BatchId, i => i.Quantity);

            // 校验每个批次库存充足
            foreach (var batch in batches)
            {
                if (batch.Quantity < deductMap[batch.Id])
                    return (false, $"批次 {batch.BatchNo} 库存不足（在库 {batch.Quantity}，需扣减 {deductMap[batch.Id]}）", null, null, null, 0);
            }

            // 校验扣减总量匹配
            var totalDeduct = deductMap.Values.Sum();
            if (totalDeduct != deductQty)
                return (false, $"指定批次扣减数量合计 {totalDeduct} 与差异数量 {deductQty} 不匹配", null, null, null, 0);
        }
        else
        {
            // FIFO 兜底模式：按 CreatedTime 升序取所有在库批次
            batches = await _dbContext.InventoryBatches
                .Where(b => b.ProductId == productId
                    && b.TenantId == tenantId
                    && b.StoreId == storeId
                    && b.Status == 1
                    && b.Quantity > 0)
                .OrderBy(b => b.CreatedTime)
                .ToListAsync();

            if (!batches.Any())
                return (false, "无可用库存批次", null, null, null, 0);

            var totalAvailable = batches.Sum(b => b.Quantity);
            if (totalAvailable < deductQty)
                return (false, $"库存不足：需要 {deductQty}，可用 {totalAvailable}", null, null, null, 0);

            // FIFO 分配扣减量
            deductMap = new Dictionary<long, decimal>();
            var remaining = deductQty;
            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                var deduct = Math.Min(batch.Quantity, remaining);
                deductMap[batch.Id] = deduct;
                remaining -= deduct;
            }
        }

        // 读取当前库存汇总（用于流水 BeforeQuantity/AfterQuantity）
        var inventory = await _dbContext.Inventories
            .FirstOrDefaultAsync(inv => inv.ProductId == productId && inv.TenantId == tenantId && inv.StoreId == storeId);
        var beforeQty = inventory?.Quantity ?? 0;

        // 执行扣减并累加金额
        string? firstBatchNo = null;
        decimal? firstUnitPrice = null;
        DateTime? firstExpiration = null;
        var firstLogged = false;
        var totalAmount = 0m;

        foreach (var batch in batches)
        {
            if (!deductMap.TryGetValue(batch.Id, out var deduct) || deduct <= 0)
                continue;

            batch.Quantity -= deduct;
            batch.UpdatedTime = now;
            if (batch.Quantity == 0)
                batch.Status = 2; // 已用完

            totalAmount += deduct * batch.UnitPrice;

            // 写盘点批次明细（盘亏扣减批次，每个批次一条）
            _dbContext.InventoryCheckBatches.Add(new InventoryCheckBatch
            {
                CheckId = checkId,
                BatchId = batch.Id,
                BatchNo = batch.BatchNo,
                Quantity = deduct,
                UnitPrice = batch.UnitPrice,
                ExpirationDate = batch.ExpirationDate,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                CreatedTime = now
            });

            if (!firstLogged)
            {
                firstBatchNo = batch.BatchNo;
                firstUnitPrice = batch.UnitPrice;
                firstExpiration = batch.ExpirationDate;
                firstLogged = true;
            }
        }

        // 写一条汇总 InventoryLog（首批次属性，与 DeductByBatchAsync 一致）
        _dbContext.InventoryLogs.Add(new InventoryLog
        {
            ProductId = productId,
            Type = 2, // 出库
            SourceType = InventoryLogSourceTypes.CheckAdjustment,
            Quantity = -deductQty, // 出库为负
            BeforeQuantity = beforeQty,
            AfterQuantity = beforeQty - deductQty,
            BatchNo = firstBatchNo,
            UnitPrice = firstUnitPrice,
            ExpirationDate = firstExpiration,
            RelatedId = checkId,
            Remark = "盘点差异-盘亏",
            OperatorId = _currentUser.UserId,
            OperatorName = _currentUser.RealName ?? _currentUser.UserName,
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            CreatedTime = now
        });

        // 同步扣减 Inventory 汇总表
        if (inventory != null)
        {
            inventory.Quantity -= deductQty;
            inventory.UpdatedTime = now;
        }
        else
        {
            // 汇总表不存在但批次存在（异常数据）：防御性补建，库存为负表示数据不一致
            inventory = new Inventory
            {
                ProductId = productId,
                Quantity = -deductQty,
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = storeId,
                CreatedTime = now
            };
            _dbContext.Inventories.Add(inventory);
        }

        return (true, null, firstBatchNo, firstUnitPrice, firstExpiration, totalAmount);
    }

    /// <summary>
    /// 取消盘点单：草稿转已取消（已完成的不可取消，需走反向盘点单）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

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
    /// 删除库存盘点记录（仅 DiffQuantity=0 可删除；已调整库存的不可删除，需走反向盘点）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.InventoryChecks
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _currentUser.TenantId.Value && c.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("库存盘点记录不存在", 404);

        // 已调整库存（Diff≠0）的盘点单不可删除，需通过反向盘点纠正
        if (entity.DiffQuantity != 0)
            return ApiResponseDto.Fail("已调整库存的盘点单不可删除，请通过反向盘点纠正", 400);

        _dbContext.InventoryChecks.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存盘点记录（仅 DiffQuantity=0 可删除；已调整库存的会被跳过并在消息中提示）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.InventoryChecks
            .Where(c => ids.Contains(c.Id) && c.TenantId == _currentUser.TenantId.Value && c.StoreId == _currentUser.StoreId.Value)
            .ToListAsync();

        // 仅 DiffQuantity=0 的盘点单可删除（未调整库存，安全）
        var deletable = entities.Where(c => c.DiffQuantity == 0).ToList();
        var skipped = entities.Count - deletable.Count;

        _dbContext.InventoryChecks.RemoveRange(deletable);
        await _dbContext.SaveChangesAsync();

        var message = $"成功删除 {deletable.Count} 条数据";
        if (skipped > 0)
            message += $"，跳过 {skipped} 条已调整库存的盘点单（不可删除）";

        return ApiResponseDto.Success(null, message);
    }

    /// <summary>
    /// 获取盘点专用商品选项（含当前门店账面库存、成本价、在库批次列表，用于新增盘点下拉选择）
    /// 左联 Inventory 以包含无库存记录的商品（stock=0）；排除服务商品(2)/样品(4)/赠品(5)
    /// 在库批次（Status=1 且 Quantity>0）用于盘亏时选择扣减批次
    /// </summary>
    public async Task<ApiResponseDto<List<InventoryCheckProductOptionDto>>> GetProductOptionsForCheckAsync()
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<InventoryCheckProductOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 1. 商品 + 账面库存（左联 Inventory）
        var options = await (from p in _dbContext.Products
                             from i in _dbContext.Inventories
                                 .Where(i => i.ProductId == p.Id && i.TenantId == tenantId && i.StoreId == storeId)
                                 .DefaultIfEmpty()
                             where p.TenantId == tenantId && p.StoreId == storeId
                                 && p.Master.Type != 2 && p.Master.Type != 4 && p.Master.Type != 5
                                 && !p.IsDeleted
                             select new InventoryCheckProductOptionDto
                             {
                                 Id = p.Id,
                                 Name = p.Master.Name,
                                 Code = p.Master.Code,
                                 Unit = p.Master.Unit,
                                 Stock = i != null ? i.Quantity : 0,
                                 CostPrice = p.CostPrice
                             }).ToListAsync();

        // 2. 加载所有相关商品的在库批次（一次查询避免 N+1）
        var productIds = options.Select(o => o.Id).ToList();
        var batches = await _dbContext.InventoryBatches
            .Where(b => productIds.Contains(b.ProductId)
                && b.TenantId == tenantId
                && b.StoreId == storeId
                && b.Status == 1
                && b.Quantity > 0)
            .OrderBy(b => b.ProductId).ThenBy(b => b.CreatedTime)
            .Select(b => new
            {
                b.ProductId,
                Dto = new InventoryCheckBatchOptionDto
                {
                    Id = b.Id,
                    BatchNo = b.BatchNo,
                    Quantity = b.Quantity,
                    UnitPrice = b.UnitPrice,
                    ExpirationDate = b.ExpirationDate,
                    CreatedTime = b.CreatedTime
                }
            })
            .ToListAsync();

        // 3. 按商品分组挂载批次
        var batchesByProduct = batches.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());
        foreach (var option in options)
        {
            if (batchesByProduct.TryGetValue(option.Id, out var productBatches))
                option.Batches = productBatches;
        }

        return ApiResponseDto<List<InventoryCheckProductOptionDto>>.Ok(options);
    }

    /// <summary>
    /// 盘盈批次选项查询：按商品+门店列出可累加的目标批次（含已用完/已过期），供盘盈弹窗选择
    /// - expirationDate 有值：精确匹配该过期日期的批次（无论批次是否已过期/已用完，均提供）
    /// - noExpiry=true：匹配未录入效期（过期日期为空）的批次
    /// 两个条件互斥，未传过期日期且非无效期时返回参数错误
    /// </summary>
    public async Task<ApiResponseDto<List<InventoryCheckBatchLookupDto>>> GetBatchOptionsForCheckAsync(long productId, DateTime? expirationDate, bool noExpiry)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<InventoryCheckBatchLookupDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;

        // 按商品+门店取全部批次（数据层无软删除字段，含在库/已用完/已过期）
        var query = _dbContext.InventoryBatches
            .Where(b => b.ProductId == productId
                && b.TenantId == tenantId
                && b.StoreId == storeId);

        if (noExpiry)
        {
            // 无效期：未录入效期（过期日期为空）的批次
            query = query.Where(b => b.ExpirationDate == null);
        }
        else if (expirationDate.HasValue)
        {
            // 按过期日期：精确匹配所选日期（区间查询避免 DateTime 时间部分干扰）
            var start = expirationDate.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(b => b.ExpirationDate >= start && b.ExpirationDate < end);
        }
        else
        {
            return ApiResponseDto<List<InventoryCheckBatchLookupDto>>.Fail("请选择过期日期或选择无效期", 400);
        }

        var batches = await query
            .OrderBy(b => b.CreatedTime)
            .Select(b => new InventoryCheckBatchLookupDto
            {
                Id = b.Id,
                BatchNo = b.BatchNo,
                Quantity = b.Quantity,
                UnitPrice = b.UnitPrice,
                ProductionDate = b.ProductionDate,
                ShelfLifeDays = b.ShelfLifeDays,
                ExpirationDate = b.ExpirationDate,
                Status = b.Status
            })
            .ToListAsync();

        return ApiResponseDto<List<InventoryCheckBatchLookupDto>>.Ok(batches);
    }

    /// <summary>
    /// 查询当日该商品是否已有非取消状态的盘点记录（用于前端软约束提示）
    /// </summary>
    public async Task<ApiResponseDto<bool>> HasProductCheckedTodayAsync(long productId)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<bool>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var hasChecked = await _dbContext.InventoryChecks
            .AnyAsync(c => c.ProductId == productId
                && c.TenantId == tenantId
                && c.StoreId == storeId
                && c.Status != InventoryCheckStatus.Cancelled
                && c.CheckTime >= today
                && c.CheckTime < tomorrow);

        return ApiResponseDto<bool>.Ok(hasChecked);
    }
}
