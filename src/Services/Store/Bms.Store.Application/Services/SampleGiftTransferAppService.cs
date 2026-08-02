using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.SampleGiftTransfers;
using Bms.Store.Domain.Entities;
using SampleGiftTransferEntity = Bms.Store.Domain.Entities.SampleGiftTransfer;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品赠品调拨单应用服务实现
/// 仅处理 Type∈{4,5}（样品/赠品）商品的跨门店调拨，库存逻辑复用 StockBatchTransferHelper
/// </summary>
public class SampleGiftTransferAppService : ISampleGiftTransferAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<SampleGiftTransferCreateDto> _createValidator;
    private readonly IValidator<SampleGiftTransferUpdateDto> _updateValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public SampleGiftTransferAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<SampleGiftTransferCreateDto> createValidator,
        IValidator<SampleGiftTransferUpdateDto> updateValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取样品赠品调拨单分页列表
    /// 跨店可见：当前门店作为调出方或调入方均可查看
    /// 支持按商品类型（4=样品/5=赠品）筛选含该类型商品的调拨单
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<SampleGiftTransferDto>>> GetPagedListAsync(SampleGiftTransferQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<SampleGiftTransferDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.SampleGiftTransfers
            .Where(t => t.TenantId == tenantId && (t.FromStoreId == storeId || t.ToStoreId == storeId));

        if (!string.IsNullOrWhiteSpace(query.TransferNo))
            queryable = queryable.Where(t => t.TransferNo.Contains(query.TransferNo));
        if (query.FromStoreId.HasValue)
            queryable = queryable.Where(t => t.FromStoreId == query.FromStoreId.Value);
        if (query.ToStoreId.HasValue)
            queryable = queryable.Where(t => t.ToStoreId == query.ToStoreId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(t => t.Status == query.Status.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(t => t.TransferDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            // EndDate 含当日：用次日0点作为上界（exclusive）避免时间部分漏掉当天数据
            queryable = queryable.Where(t => t.TransferDate < query.EndDate.Value.AddDays(1));
        if (query.ProductType.HasValue)
            // 按商品类型筛选：返回明细中含该类型商品的调拨单
            queryable = queryable.Where(t => t.Items.Any(i => i.Product != null && i.Product.Type == query.ProductType.Value));

        var total = await queryable.CountAsync();
        var items = await queryable
            .Include(t => t.Items)
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<SampleGiftTransferDto>
        {
            List = items.Adapt<List<SampleGiftTransferDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<SampleGiftTransferDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取样品赠品调拨单详情（含明细）
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftTransferDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SampleGiftTransferDto?>.Fail("无法确定当前租户", 401);

        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.SampleGiftTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _currentUser.TenantId.Value
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto<SampleGiftTransferDto?>.Fail("样品赠品调拨单不存在", 404);
        return ApiResponseDto<SampleGiftTransferDto?>.Ok(entity.Adapt<SampleGiftTransferDto>());
    }

    /// <summary>
    /// 创建样品赠品调拨单（待调出状态，不调整库存）
    /// 校验明细商品 Type∈{4,5} 且未删除；单号由后端自动生成
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftTransferDto>> CreateAsync(SampleGiftTransferCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<SampleGiftTransferDto>.Fail("无法确定当前租户或门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SampleGiftTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId.Value;
        var now = DateTime.Now;

        // 查询调出/调入门店信息（填充冗余字段）
        var fromStore = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == dto.FromStoreId && s.TenantId == tenantId);
        var toStore = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == dto.ToStoreId && s.TenantId == tenantId);
        if (fromStore == null)
            return ApiResponseDto<SampleGiftTransferDto>.Fail("调出门店不存在", 400);
        if (toStore == null)
            return ApiResponseDto<SampleGiftTransferDto>.Fail("调入门店不存在", 400);

        // 批量查询商品信息（填充明细冗余字段）
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => p.TenantId == tenantId && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        var missingProduct = productIds.FirstOrDefault(id => !products.ContainsKey(id));
        if (missingProduct > 0)
            return ApiResponseDto<SampleGiftTransferDto>.Fail($"商品(ID:{missingProduct})不存在", 400);

        // 校验商品 Type∈{4,5}（样品/赠品）且未删除
        foreach (var pid in productIds)
        {
            var p = products[pid];
            if (p.Type != 4 && p.Type != 5)
                return ApiResponseDto<SampleGiftTransferDto>.Fail($"商品{p.Name}非样品/赠品，不支持调拨", 400);
            if (p.IsDeleted)
                return ApiResponseDto<SampleGiftTransferDto>.Fail($"商品{p.Name}已删除，无法调拨", 400);
        }

        // 映射实体并设置审计字段
        var entity = dto.Adapt<SampleGiftTransferEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.StoreCode = fromStore.Code;
        entity.Status = StockTransferStatus.Draft; // 待调出(1)
        entity.CreatedTime = now;
        entity.TransferNo = await SampleGiftTransferNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, dto.TransferDate);
        entity.FromStoreCode = fromStore.Code;
        entity.FromStoreName = fromStore.Name;
        entity.ToStoreCode = toStore.Code;
        entity.ToStoreName = toStore.Name;
        entity.OperatorId = _currentUser.UserId;
        entity.OperatorName = _currentUser.RealName ?? _currentUser.UserName;

        // 设置明细审计字段与冗余字段，SampleGiftTransferId 由 EF 导航属性自动回填
        foreach (var item in entity.Items)
        {
            item.TenantId = tenantId;
            item.TenantCode = tenantCode;
            item.StoreId = storeId;
            item.CreatedTime = now;
            item.ProductName = products[item.ProductId].Name;
            item.ProductCode = products[item.ProductId].Code;
            item.Unit = products[item.ProductId].Unit;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.SampleGiftTransfers.Add(entity);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<SampleGiftTransferDto>.Ok(entity.Adapt<SampleGiftTransferDto>(), "创建成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新样品赠品调拨单（仅待调出状态可修改）
    /// </summary>
    public async Task<ApiResponseDto<SampleGiftTransferDto>> UpdateAsync(SampleGiftTransferUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<SampleGiftTransferDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<SampleGiftTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.SampleGiftTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == dto.Id && t.TenantId == tenantId
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto<SampleGiftTransferDto>.Fail("样品赠品调拨单不存在", 404);

        // 仅待调出状态可修改；已调入/已取消为终态
        if (entity.Status != StockTransferStatus.Draft)
            return ApiResponseDto<SampleGiftTransferDto>.Fail($"仅待调出状态的调拨单可修改（当前状态：{entity.Status}）", 400);

        entity.FromStoreId = dto.FromStoreId;
        entity.FromStoreCode = dto.FromStoreCode;
        entity.ToStoreId = dto.ToStoreId;
        entity.ToStoreCode = dto.ToStoreCode;
        entity.TransferDate = dto.TransferDate;
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<SampleGiftTransferDto>.Ok(entity.Adapt<SampleGiftTransferDto>(), "更新成功");
    }

    /// <summary>
    /// 删除样品赠品调拨单（物理删除，仅待调出状态可删）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.SampleGiftTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _currentUser.TenantId.Value
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto.Fail("样品赠品调拨单不存在", 404);

        // 仅待调出状态可删除（设计文档 3.1：物理删除，仅 待调出(1) 状态可删）
        if (entity.Status != StockTransferStatus.Draft)
            return ApiResponseDto.Fail("仅待调出状态的调拨单可删除", 400);

        _dbContext.SampleGiftTransfers.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除样品赠品调拨单（仅待调出状态可删，非待调出跳过）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var storeId = _currentUser.StoreId ?? 0;
        var entities = await _dbContext.SampleGiftTransfers
            .Where(t => ids.Contains(t.Id) && t.TenantId == _currentUser.TenantId.Value
                && (t.FromStoreId == storeId || t.ToStoreId == storeId))
            .ToListAsync();

        // 仅待调出状态可删除
        var deletable = entities.Where(t => t.Status == StockTransferStatus.Draft).ToList();
        var skipped = entities.Count - deletable.Count;

        _dbContext.SampleGiftTransfers.RemoveRange(deletable);
        await _dbContext.SaveChangesAsync();

        var message = $"成功删除 {deletable.Count} 条数据";
        if (skipped > 0)
            message += $"，跳过 {skipped} 条非待调出状态调拨单（不可删除）";

        return ApiResponseDto.Success(null, message);
    }

    /// <summary>
    /// 执行调拨：事务内调出门店扣减（Source=12）+ 调入门店增加（Source=13），状态转已调入
    /// 仅待调出状态可执行
    /// </summary>
    public async Task<ApiResponseDto> ExecuteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;
        var storeId = _currentUser.StoreId ?? 0;

        var transfer = await _dbContext.SampleGiftTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (transfer == null)
            return ApiResponseDto.Fail("样品赠品调拨单不存在", 404);

        // 仅待调出状态可执行；已调入/已取消为终态
        if (transfer.Status != StockTransferStatus.Draft)
            return ApiResponseDto.Fail($"仅待调出状态的调拨单可执行（当前状态：{transfer.Status}）", 400);

        var items = await _dbContext.SampleGiftTransferItems
            .Where(si => si.SampleGiftTransferId == id && si.TenantId == tenantId)
            .ToListAsync();

        if (!items.Any())
            return ApiResponseDto.Fail("调拨明细为空，无法执行", 400);

        // 遍历明细前，批量校验商品未删除
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(p => p.TenantId == tenantId && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        foreach (var item in items)
        {
            if (!products.ContainsKey(item.ProductId))
                throw new InvalidOperationException($"商品(ID:{item.ProductId})不存在");
            if (products[item.ProductId].IsDeleted)
                throw new InvalidOperationException($"商品{products[item.ProductId].Name}已删除，无法调拨");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in items)
            {
                // === 1. 调出门店：扣减批次（helper 处理，库存不足/批次不存在抛 InvalidOperationException） ===
                var deductions = await StockBatchTransferHelper.DeductBatchesAsync(
                    _dbContext, tenantId, transfer.FromStoreId, item.ProductId, item.BatchNo, item.Quantity, now);

                // 读取调出门店 Inventory 汇总（用于流水 BeforeQuantity/AfterQuantity 递推）
                var fromInventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId
                        && inv.TenantId == tenantId
                        && inv.StoreId == transfer.FromStoreId);
                if (fromInventory == null)
                    throw new InvalidOperationException($"调出门店商品(ID:{item.ProductId})无库存汇总记录");

                // === 2. 写出库流水（Source=12，fromRunningQty 递推） ===
                var fromRunningQty = fromInventory.Quantity;
                foreach (var d in deductions)
                {
                    fromRunningQty -= d.DeductQuantity;
                    StockBatchTransferHelper.WriteInventoryLog(
                        _dbContext, tenantId, tenantCode, transfer.FromStoreId,
                        item.ProductId, InventoryLogSourceTypes.SampleGiftTransferOutbound,
                        -d.DeductQuantity, d.BatchNo, d.ExpirationDate, d.UnitPrice,
                        fromRunningQty + d.DeductQuantity, fromRunningQty,
                        transfer.Id, $"样品赠品调拨出库-{transfer.TransferNo}", now);
                }

                // 同步扣减调出门店 Inventory 汇总表
                fromInventory.Quantity -= item.Quantity;
                fromInventory.UpdatedTime = now;

                // === 3. 调入门店：批次合并（helper 处理，继承调出批次属性） ===
                await StockBatchTransferHelper.MergeReceiveBatchesAsync(
                    _dbContext, tenantId, transfer.ToStoreId, item.ProductId, deductions, now, tenantCode);

                // 读取/新建调入门店 Inventory 汇总（用于流水 BeforeQuantity/AfterQuantity 递推）
                var toInventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId
                        && inv.TenantId == tenantId
                        && inv.StoreId == transfer.ToStoreId);
                var toRunningQty = toInventory?.Quantity ?? 0;

                // === 4. 写入库流水（Source=13，toRunningQty 递推） ===
                foreach (var d in deductions)
                {
                    toRunningQty += d.DeductQuantity;
                    StockBatchTransferHelper.WriteInventoryLog(
                        _dbContext, tenantId, tenantCode, transfer.ToStoreId,
                        item.ProductId, InventoryLogSourceTypes.SampleGiftTransferInbound,
                        d.DeductQuantity, d.BatchNo, d.ExpirationDate, d.UnitPrice,
                        toRunningQty - d.DeductQuantity, toRunningQty,
                        transfer.Id, $"样品赠品调拨入库-{transfer.TransferNo}", now);
                }

                // 同步增加调入门店 Inventory 汇总表（查找或新建）
                if (toInventory == null)
                {
                    toInventory = new Inventory
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        AlertQuantity = 0,
                        TenantId = tenantId,
                        TenantCode = tenantCode,
                        StoreId = transfer.ToStoreId,
                        CreatedTime = now
                    };
                    _dbContext.Inventories.Add(toInventory);
                }
                else
                {
                    toInventory.Quantity += item.Quantity;
                    toInventory.UpdatedTime = now;
                }
            }

            // 更新调拨单状态为已调入（已完成）
            transfer.Status = StockTransferStatus.Completed;
            transfer.UpdatedTime = now;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 调出方库存减少，即时检测低库存预警
            foreach (var item in items)
            {
                try
                {
                    await _alertAppService.CheckLowStockAsync(tenantId, transfer.FromStoreId, item.ProductId);
                }
                catch
                {
                    // 预警检测失败不影响主流程，定时任务会兜底
                }
            }

            return ApiResponseDto.Success(null, "调拨执行成功");
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return ApiResponseDto.Fail(ex.Message, 400);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 取消调拨单：待调出转已取消（已调入不可取消，需走反向调拨单）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.SampleGiftTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto.Fail("样品赠品调拨单不存在", 404);

        // 仅待调出状态可取消；已调入为终态，需走反向调拨单
        if (entity.Status != StockTransferStatus.Draft)
            return ApiResponseDto.Fail($"仅待调出状态的调拨单可取消（当前状态：{entity.Status}），已调入调拨不可取消，请创建反向调拨单", 400);

        entity.Status = StockTransferStatus.Cancelled;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "取消成功");
    }

    /// <summary>
    /// 校验当前用户对调出门店的访问权限
    /// 授权语义与 StoreAccessMiddleware 一致：super_admin 跳过，其他用户查 UserStores 表
    /// </summary>
    private async Task<ApiResponseDto?> ValidateFromStoreAccessAsync(long fromStoreId)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
            return ApiResponseDto.Fail("无法确定当前用户", 401);

        // super_admin 跳过（平台管理员可跨租户/门店访问）
        if (_currentUser.IsSuperAdmin)
            return null;

        var tenantId = _currentUser.TenantId.Value;
        var userId = _currentUser.UserId.Value;

        // 校验调出门店存在、未删除、营业中、且当前用户有授权
        var hasAccess = await (from s in _dbContext.Stores
                               join us in _dbContext.UserStores on s.Id equals us.StoreId
                               where s.Id == fromStoreId
                                  && !s.IsDeleted
                                  && s.TenantId == tenantId
                                  && s.Status == 1
                                  && !us.IsDeleted
                                  && us.UserId == userId
                                  && us.TenantId == tenantId
                               select s).AnyAsync();

        if (!hasAccess)
            return ApiResponseDto.Fail("无权访问该调出门店", 403);

        return null;
    }

    /// <summary>
    /// 获取调出门店的样品/赠品商品选项（仅返回 Type∈{4,5} 且 Stock > 0 的商品）
    /// </summary>
    public async Task<ApiResponseDto<List<SampleGiftTransferProductOptionDto>>> GetFromStoreProductsAsync(long fromStoreId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<SampleGiftTransferProductOptionDto>>.Fail("无法确定当前租户", 401);

        var accessCheck = await ValidateFromStoreAccessAsync(fromStoreId);
        if (accessCheck != null)
            return ApiResponseDto<List<SampleGiftTransferProductOptionDto>>.Fail(accessCheck.Message ?? "无权访问", accessCheck.Code);

        var tenantId = _currentUser.TenantId.Value;

        // 核心差异：仅返回 Type∈{4,5}（样品/赠品）商品，与正品调拨 Type!=4 && Type!=5 相反
        var options = await (from p in _dbContext.Products
                             from i in _dbContext.Inventories
                                 .Where(i => i.ProductId == p.Id && i.TenantId == tenantId && i.StoreId == fromStoreId)
                                 .DefaultIfEmpty()
                             where p.TenantId == tenantId
                                 && (p.Type == 4 || p.Type == 5)
                                 && !p.IsDeleted
                                 && i != null && i.Quantity > 0
                             select new SampleGiftTransferProductOptionDto
                             {
                                 Id = p.Id,
                                 Name = p.Name,
                                 Code = p.Code,
                                 Unit = p.Unit,
                                 Stock = i.Quantity
                             }).ToListAsync();

        return ApiResponseDto<List<SampleGiftTransferProductOptionDto>>.Ok(options);
    }

    /// <summary>
    /// 获取调出门店指定商品的在库批次列表（按过期日期升序，FEFO）
    /// 仅返回 Status=1 且 Quantity > 0 的批次
    /// </summary>
    public async Task<ApiResponseDto<List<SampleGiftTransferBatchOptionDto>>> GetFromStoreProductBatchesAsync(long fromStoreId, long productId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<SampleGiftTransferBatchOptionDto>>.Fail("无法确定当前租户", 401);

        var accessCheck = await ValidateFromStoreAccessAsync(fromStoreId);
        if (accessCheck != null)
            return ApiResponseDto<List<SampleGiftTransferBatchOptionDto>>.Fail(accessCheck.Message ?? "无权访问", accessCheck.Code);

        var tenantId = _currentUser.TenantId.Value;

        var batches = await _dbContext.InventoryBatches
            .Where(b => b.ProductId == productId
                && b.StoreId == fromStoreId
                && b.TenantId == tenantId
                && b.Status == 1
                && b.Quantity > 0)
            .OrderBy(b => b.ExpirationDate)
            .Select(b => new SampleGiftTransferBatchOptionDto
            {
                Id = b.Id,
                BatchNo = b.BatchNo,
                Quantity = b.Quantity,
                UnitPrice = b.UnitPrice,
                ExpirationDate = b.ExpirationDate,
                ProductionDate = b.ProductionDate
            })
            .ToListAsync();

        return ApiResponseDto<List<SampleGiftTransferBatchOptionDto>>.Ok(batches);
    }
}
