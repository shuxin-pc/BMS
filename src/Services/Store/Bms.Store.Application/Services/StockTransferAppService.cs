using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.StockTransfers;
using Bms.Store.Domain.Entities;
using StockTransferEntity = Bms.Store.Domain.Entities.StockTransfer;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 库存调拨单应用服务实现
/// </summary>
public class StockTransferAppService : IStockTransferAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<StockTransferCreateDto> _createValidator;
    private readonly IValidator<StockTransferUpdateDto> _updateValidator;
    private readonly IInventoryAlertAppService _alertAppService;

    public StockTransferAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StockTransferCreateDto> createValidator,
        IValidator<StockTransferUpdateDto> updateValidator,
        IInventoryAlertAppService alertAppService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _alertAppService = alertAppService;
    }

    /// <summary>
    /// 获取库存调拨单分页列表
    /// 跨店可见：当前门店作为调出方或调入方均可查看
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StockTransferDto>>> GetPagedListAsync(StockTransferQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<StockTransferDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.StockTransfers
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
            queryable = queryable.Where(t => t.Items.Any(i => i.Product != null && i.Product.Master.Type == query.ProductType.Value));

        var total = await queryable.CountAsync();
        var items = await queryable
            .Include(t => t.Items).ThenInclude(i => i.Product.Master)
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StockTransferDto>
        {
            List = items.Adapt<List<StockTransferDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StockTransferDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取库存调拨单详情（含明细）
    /// </summary>
    public async Task<ApiResponseDto<StockTransferDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StockTransferDto?>.Fail("登录状态异常，请重新登录", 401);

        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.StockTransfers
            .Include(t => t.Items).ThenInclude(i => i.Product.Master)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _currentUser.TenantId.Value
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto<StockTransferDto?>.Fail("库存调拨单不存在", 404);
        return ApiResponseDto<StockTransferDto?>.Ok(entity.Adapt<StockTransferDto>());
    }

    /// <summary>
    /// 创建库存调拨单（草稿状态：待调出，不调整库存）
    /// 调拨单号由后端自动生成；冗余字段（门店名称、操作员姓名、商品信息）一并填充
    /// </summary>
    public async Task<ApiResponseDto<StockTransferDto>> CreateAsync(StockTransferCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<StockTransferDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StockTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

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
            return ApiResponseDto<StockTransferDto>.Fail("调出门店不存在", 400);
        if (toStore == null)
            return ApiResponseDto<StockTransferDto>.Fail("调入门店不存在", 400);

        // 批量查询商品信息（填充明细冗余字段），Include Master 以访问主档字段
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Include(p => p.Master)
            .Where(p => p.TenantId == tenantId && productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        var missingProduct = productIds.FirstOrDefault(id => !products.ContainsKey(id));
        if (missingProduct > 0)
            return ApiResponseDto<StockTransferDto>.Fail($"商品(ID:{missingProduct})不存在", 400);

        // 映射实体并设置审计字段
        var entity = dto.Adapt<StockTransferEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = tenantCode;
        entity.StoreId = storeId;
        entity.StoreCode = fromStore.Code;
        entity.Status = StockTransferStatus.Draft; // 待调出（草稿）
        entity.CreatedTime = now;
        entity.TransferNo = await StockTransferNoGenerator.GenerateAsync(_dbContext, tenantId, storeId, dto.TransferDate);
        entity.FromStoreCode = fromStore.Code;
        entity.FromStoreName = fromStore.Name;
        entity.ToStoreCode = toStore.Code;
        entity.ToStoreName = toStore.Name;
        entity.OperatorId = _currentUser.UserId;
        entity.OperatorName = _currentUser.RealName ?? _currentUser.UserName;

        // 设置明细审计字段与冗余字段，StockTransferId 由 EF 导航属性自动回填
        foreach (var item in entity.Items)
        {
            item.TenantId = tenantId;
            item.TenantCode = tenantCode;
            item.StoreId = storeId;
            item.CreatedTime = now;
            item.ProductName = products[item.ProductId].Master?.Name;
            item.ProductCode = products[item.ProductId].Master?.Code;
            item.Unit = products[item.ProductId].Master?.Unit;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            _dbContext.StockTransfers.Add(entity);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiResponseDto<StockTransferDto>.Ok(entity.Adapt<StockTransferDto>(), "创建成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新库存调拨单
    /// </summary>
    public async Task<ApiResponseDto<StockTransferDto>> UpdateAsync(StockTransferUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<StockTransferDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<StockTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.StockTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == dto.Id && t.TenantId == tenantId
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto<StockTransferDto>.Fail("库存调拨单不存在", 404);

        // 仅待调出（草稿）状态可修改；已调入/已取消为终态
        if (entity.Status != StockTransferStatus.Draft)
            return ApiResponseDto<StockTransferDto>.Fail($"仅待调出状态的调拨单可修改（当前状态：{entity.Status}）", 400);

        entity.FromStoreId = dto.FromStoreId;
        entity.FromStoreCode = dto.FromStoreCode;
        entity.ToStoreId = dto.ToStoreId;
        entity.ToStoreCode = dto.ToStoreCode;
        entity.TransferDate = dto.TransferDate;
        // Status 不允许通过 Update 直接修改，只能通过 ExecuteAsync/CancelAsync 流转
        entity.OperatorId = dto.OperatorId;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<StockTransferDto>.Ok(entity.Adapt<StockTransferDto>(), "更新成功");
    }

    /// <summary>
    /// 删除库存调拨单（仅待调出/已取消可删除；已调入为终态不可删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == _currentUser.TenantId.Value
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto.Fail("库存调拨单不存在", 404);

        if (entity.Status == StockTransferStatus.Completed)
            return ApiResponseDto.Fail("已调入的调拨单不可删除，如需调整请创建反向调拨单", 400);

        _dbContext.StockTransfers.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除库存调拨单（仅待调出/已取消可删除；已调入会被跳过）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var storeId = _currentUser.StoreId ?? 0;
        var entities = await _dbContext.StockTransfers
            .Where(t => ids.Contains(t.Id) && t.TenantId == _currentUser.TenantId.Value
                && (t.FromStoreId == storeId || t.ToStoreId == storeId))
            .ToListAsync();

        var deletable = entities.Where(t => t.Status != StockTransferStatus.Completed).ToList();
        var skipped = entities.Count - deletable.Count;

        _dbContext.StockTransfers.RemoveRange(deletable);
        await _dbContext.SaveChangesAsync();

        var message = $"成功删除 {deletable.Count} 条数据";
        if (skipped > 0)
            message += $"，跳过 {skipped} 条已调入调拨单（不可删除）";

        return ApiResponseDto.Success(null, message);
    }

    /// <summary>
    /// 执行调拨：同一事务内调出门店库存扣减（SourceType=调拨出库）+ 调入门店库存增加（SourceType=调拨入库），
    /// 写两条 InventoryLog 并关联 RefId=StockTransfer.Id，状态转已调入（已完成）
    /// 仅待调出（草稿）状态可执行
    /// </summary>
    public async Task<ApiResponseDto> ExecuteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;
        var storeId = _currentUser.StoreId ?? 0;
        var operatorId = _currentUser.UserId;
        var operatorName = _currentUser.RealName ?? _currentUser.UserName;

        var transfer = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (transfer == null)
            return ApiResponseDto.Fail("库存调拨单不存在", 404);

        // 仅待调出（草稿）状态可执行；已调入/已取消为终态
        if (transfer.Status != StockTransferStatus.Draft)
            return ApiResponseDto.Fail($"仅待调出状态的调拨单可执行（当前状态：{transfer.Status}）", 400);

        var items = await _dbContext.StockTransferItems
            .Where(si => si.StockTransferId == id && si.TenantId == tenantId)
            .ToListAsync();

        if (!items.Any())
            return ApiResponseDto.Fail("调拨明细为空，无法执行", 400);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // === P3.3: 按 MasterId 在调入门店查/建 Product 档案，构建 fromProductId -> toProductId 映射 ===
            // 设计文档 6.3 节：调入门店无档案 -> 自动克隆一份 Product（仅 Store 字段，MasterId 指向同一 Master）
            // 库存三件套的 ProductId 需替换为调入门店的 Product.Id，消除"B 门店看不到调拨入库商品"问题
            var fromProductIds = items.Select(i => i.ProductId).Distinct().ToList();
            var fromProducts = await _dbContext.Products
                .Where(p => fromProductIds.Contains(p.Id) && p.TenantId == tenantId)
                .ToDictionaryAsync(p => p.Id);

            var masterIds = fromProducts.Values.Select(p => p.MasterId).Distinct().ToList();
            var toProducts = await _dbContext.Products
                .Where(p => masterIds.Contains(p.MasterId)
                    && p.StoreId == transfer.ToStoreId
                    && p.TenantId == tenantId
                    && !p.IsDeleted)
                .ToDictionaryAsync(p => p.MasterId);

            // 调入门店信息（克隆 Product 时填充 StoreCode 冗余字段）
            var toStore = await _dbContext.Stores
                .FirstOrDefaultAsync(s => s.Id == transfer.ToStoreId && s.TenantId == tenantId);

            // 构建 ProductId 映射：缺失的克隆一份（复制调出门店 Store 字段，保证调入后立即可销售）
            var productIdMap = new Dictionary<long, long>();
            foreach (var item in items)
            {
                if (!fromProducts.TryGetValue(item.ProductId, out var fromProduct))
                    throw new InvalidOperationException($"调出商品(ID:{item.ProductId})不存在");

                if (!toProducts.TryGetValue(fromProduct.MasterId, out var toProduct))
                {
                    toProduct = new Product
                    {
                        MasterId = fromProduct.MasterId,
                        // 复制 Store 字段：Price=0 会导致 POS 异常，故复制调出门店值保证可销售
                        Price = fromProduct.Price,
                        CostPrice = fromProduct.CostPrice,
                        LastPurchasePrice = fromProduct.LastPurchasePrice,
                        LowStockThreshold = fromProduct.LowStockThreshold,
                        ExpiryAlertDays = fromProduct.ExpiryAlertDays,
                        OverstockThreshold = fromProduct.OverstockThreshold,
                        Status = fromProduct.Status,
                        Remark = fromProduct.Remark,
                        TenantId = tenantId,
                        TenantCode = tenantCode,
                        StoreId = transfer.ToStoreId,
                        StoreCode = toStore?.Code ?? string.Empty,
                        CreatedTime = now
                    };
                    _dbContext.Products.Add(toProduct);
                    await _dbContext.SaveChangesAsync(); // 获取 toProduct.Id
                    toProducts[fromProduct.MasterId] = toProduct;
                }
                productIdMap[item.ProductId] = toProduct.Id;
            }

            // 跨 items 累计调入门店已生成批次数，避免事务内 CountAsync 漏算未落库批次导致批次号序号重复
            var batchCountGenerated = 0;
            foreach (var item in items)
            {
                // 调入门店对应的 Product.Id（按 MasterId 映射）
                var toProductId = productIdMap[item.ProductId];

                // === 1. 调出门店：扣减批次（helper 处理，库存不足/批次不存在抛 InvalidOperationException） ===
                var deductions = await StockBatchTransferHelper.DeductBatchesAsync(
                    _dbContext, tenantId, transfer.FromStoreId, item.ProductId, item.BatchNo, item.Quantity, now);

                // FEFO 模式（item.BatchNo 为空）回写实际扣减批次号到明细，便于详情查看；多批次用逗号拼接
                if (string.IsNullOrEmpty(item.BatchNo) && deductions.Count > 0)
                {
                    item.BatchNo = string.Join(", ", deductions.Select(d => d.BatchNo).Distinct());
                    item.UpdatedTime = now;
                }

                // 读取调出门店 Inventory 汇总（用于流水 BeforeQuantity/AfterQuantity 递推）
                var fromInventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId
                        && inv.TenantId == tenantId
                        && inv.StoreId == transfer.FromStoreId);
                if (fromInventory == null)
                    throw new InvalidOperationException($"调出门店商品(ID:{item.ProductId})无库存汇总记录");

                // === 2. 写出库流水（按扣减明细，fromRunningQty 递推） ===
                var fromRunningQty = fromInventory.Quantity;
                foreach (var d in deductions)
                {
                    fromRunningQty -= d.DeductQuantity;
                    StockBatchTransferHelper.WriteInventoryLog(
                        _dbContext, tenantId, tenantCode, transfer.FromStoreId,
                        item.ProductId, InventoryLogSourceTypes.TransferOutbound,
                        -d.DeductQuantity, d.BatchNo, d.ExpirationDate, d.UnitPrice,
                        fromRunningQty + d.DeductQuantity, fromRunningQty,
                        transfer.Id, transfer.TransferNo, operatorId, operatorName, now);
                }

                // 同步扣减调出门店 Inventory 汇总表
                fromInventory.Quantity -= item.Quantity;
                fromInventory.UpdatedTime = now;

                // === 3. 调入门店：批次合并（批次号在调入门店重新生成，不沿用调出门店批次号） ===
                // 使用调入门店的 Product.Id（按 MasterId 映射），确保批次归属调入门店档案
                var received = await StockBatchTransferHelper.MergeReceiveBatchesAsync(
                    _dbContext, tenantId, transfer.ToStoreId, toProductId, deductions, now, tenantCode, batchCountGenerated);
                batchCountGenerated += received.Count;

                // 读取/新建调入门店 Inventory 汇总（用于流水 BeforeQuantity/AfterQuantity 递推）
                var toInventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(inv => inv.ProductId == toProductId
                        && inv.TenantId == tenantId
                        && inv.StoreId == transfer.ToStoreId);
                var toRunningQty = toInventory?.Quantity ?? 0;

                // === 4. 写入库流水（按收货批次，使用调入门店新生成的批次号） ===
                foreach (var r in received)
                {
                    toRunningQty += r.Quantity;
                    StockBatchTransferHelper.WriteInventoryLog(
                        _dbContext, tenantId, tenantCode, transfer.ToStoreId,
                        toProductId, InventoryLogSourceTypes.TransferInbound,
                        r.Quantity, r.NewBatchNo, r.ExpirationDate, r.UnitPrice,
                        toRunningQty - r.Quantity, toRunningQty,
                        transfer.Id, transfer.TransferNo, operatorId, operatorName, now);
                }

                // 同步增加调入门店 Inventory 汇总表（查找或新建）
                if (toInventory == null)
                {
                    toInventory = new Inventory
                    {
                        ProductId = toProductId,
                        Quantity = item.Quantity,
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

            // 调拨后即时检测双方门店预警（双向：调出方可能触发低库存，调入方可能触发积压）
            foreach (var item in items)
            {
                try
                {
                    await _alertAppService.CheckInventoryAlertsAsync(tenantId, transfer.FromStoreId, item.ProductId);
                }
                catch
                {
                    // 预警检测失败不影响主流程，定时任务会兜底
                }

                try
                {
                    await _alertAppService.CheckInventoryAlertsAsync(tenantId, transfer.ToStoreId, item.ProductId);
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
    /// 取消调拨单：草稿转已取消（已调入的不可取消，需走反向调拨单）
    /// </summary>
    public async Task<ApiResponseDto> CancelAsync(long id, string? reason)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId
                && (t.FromStoreId == storeId || t.ToStoreId == storeId));
        if (entity == null)
            return ApiResponseDto.Fail("库存调拨单不存在", 404);

        // 仅待调出（草稿）状态可取消；已调入为终态，需走反向调拨单
        if (entity.Status != StockTransferStatus.Draft)
            return ApiResponseDto.Fail($"仅待调出状态的调拨单可取消（当前状态：{entity.Status}），已调入调拨不可取消，请创建反向调拨单", 400);

        entity.Status = StockTransferStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            entity.Remark = string.IsNullOrWhiteSpace(entity.Remark) ? $"取消原因：{reason}" : $"{entity.Remark}；取消原因：{reason}";
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "取消成功");
    }

    /// <summary>
    /// 校验当前用户对调出门店的访问权限
    /// 授权语义与 StoreAccessMiddleware 一致：super_admin 跳过，其他用户查 UserStores 表
    /// 用于跨门店查询场景（前端 X-Store-Id 固定为当前门店，fromStoreId 参数绕过）
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
    /// 获取调出门店的库存商品选项（仅返回 Stock > 0 的商品，含正品/样品/赠品）
    /// 左联 Inventory 以过滤有库存的商品；用 fromStoreId 替代 _currentUser.StoreId 实现跨门店查询
    /// </summary>
    public async Task<ApiResponseDto<List<StockTransferProductOptionDto>>> GetFromStoreProductsAsync(long fromStoreId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<StockTransferProductOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var accessCheck = await ValidateFromStoreAccessAsync(fromStoreId);
        if (accessCheck != null)
            return ApiResponseDto<List<StockTransferProductOptionDto>>.Fail(accessCheck.Message ?? "无权访问", accessCheck.Code);

        var tenantId = _currentUser.TenantId.Value;

        var options = await (from p in _dbContext.Products
                             from i in _dbContext.Inventories
                                 .Where(i => i.ProductId == p.Id && i.TenantId == tenantId && i.StoreId == fromStoreId)
                                 .DefaultIfEmpty()
                             where p.TenantId == tenantId
                                 && p.Master != null
                                 && !p.IsDeleted
                                 && i != null && i.Quantity > 0
                             select new StockTransferProductOptionDto
                             {
                                 Id = p.Id,
                                 Name = p.Master.Name,
                                 Code = p.Master.Code,
                                 Type = p.Master.Type,
                                 Unit = p.Master.Unit,
                                 Stock = i.Quantity
                             }).ToListAsync();

        return ApiResponseDto<List<StockTransferProductOptionDto>>.Ok(options);
    }

    /// <summary>
    /// 获取调出门店指定商品的在库批次列表（按过期日期升序，FEFO）
    /// 仅返回 Status=1 且 Quantity > 0 的批次
    /// </summary>
    public async Task<ApiResponseDto<List<StockTransferBatchOptionDto>>> GetFromStoreProductBatchesAsync(long fromStoreId, long productId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<StockTransferBatchOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var accessCheck = await ValidateFromStoreAccessAsync(fromStoreId);
        if (accessCheck != null)
            return ApiResponseDto<List<StockTransferBatchOptionDto>>.Fail(accessCheck.Message ?? "无权访问", accessCheck.Code);

        var tenantId = _currentUser.TenantId.Value;

        var batches = await _dbContext.InventoryBatches
            .Where(b => b.ProductId == productId
                && b.StoreId == fromStoreId
                && b.TenantId == tenantId
                && b.Status == 1
                && b.Quantity > 0)
            .OrderBy(b => b.ExpirationDate)
            .Select(b => new StockTransferBatchOptionDto
            {
                Id = b.Id,
                BatchNo = b.BatchNo,
                Quantity = b.Quantity,
                UnitPrice = b.UnitPrice,
                ExpirationDate = b.ExpirationDate,
                ProductionDate = b.ProductionDate
            })
            .ToListAsync();

        return ApiResponseDto<List<StockTransferBatchOptionDto>>.Ok(batches);
    }
}
