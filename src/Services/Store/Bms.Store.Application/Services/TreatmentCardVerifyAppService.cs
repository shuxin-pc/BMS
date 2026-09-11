using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Core.Context;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCards;
using Bms.Store.Application.Services.Resources;
using Bms.Store.Domain.Entities;
using TreatmentCardVerifyEntity = Bms.Store.Domain.Entities.TreatmentCardVerify;
using TreatmentCardVerifyItemEntity = Bms.Store.Domain.Entities.TreatmentCardVerifyItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 项目卡核销记录应用服务实现
/// </summary>
public class TreatmentCardVerifyAppService : ITreatmentCardVerifyAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TreatmentCardVerifyCreateDto> _createValidator;
    private readonly IValidator<TreatmentCardVerifyUpdateDto> _updateValidator;
    private readonly ICrossStoreOperationAuditService _auditService;
    private readonly IResourceConflictCheckService _resourceConflictCheckService;
    private readonly ITechnicianStatisticAppService _technicianStatisticAppService;
    private readonly IAuditLogContext _auditLogContext;

    public TreatmentCardVerifyAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TreatmentCardVerifyCreateDto> createValidator,
        IValidator<TreatmentCardVerifyUpdateDto> updateValidator,
        ICrossStoreOperationAuditService auditService,
        IResourceConflictCheckService resourceConflictCheckService,
        ITechnicianStatisticAppService technicianStatisticAppService,
        IAuditLogContext auditLogContext)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditService = auditService;
        _resourceConflictCheckService = resourceConflictCheckService;
        _technicianStatisticAppService = technicianStatisticAppService;
        _auditLogContext = auditLogContext;
    }

    /// <summary>
    /// 获取项目卡核销记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>> GetPagedListAsync(TreatmentCardVerifyQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.TreatmentCardVerifies
            .Where(v => v.TenantId == tenantId && v.StoreId == storeId);

        if (query.CardSaleId.HasValue)
            queryable = queryable.Where(v => v.CardSaleId == query.CardSaleId.Value);
        if (query.VerifyProductId.HasValue)
            queryable = queryable.Where(v => v.Items.Any(i => i.ProductId == query.VerifyProductId.Value));

        // 客户名称/手机号合并关键字筛选（OR 语义：命中姓名或手机号其一即满足，子查询 join TreatmentCardSale→Customer）
        // 核销表无冗余客户字段，需经 CardSaleId→TreatmentCardSale.CustomerId→Customer 关联
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var customerQuery = _dbContext.Customers
                .Where(c => c.Name.Contains(query.Keyword) || c.Phone.Contains(query.Keyword));
            var saleIds = _dbContext.TreatmentCardSales
                .Where(s => s.TenantId == tenantId && customerQuery.Select(c => c.Id).Contains(s.CustomerId))
                .Select(s => s.Id);
            queryable = queryable.Where(v => saleIds.Contains(v.CardSaleId));
        }

        // 按核销时间范围筛选（StartDate 含，EndDate 含当天）
        if (query.StartDate.HasValue)
            queryable = queryable.Where(v => v.VerifyTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(v => v.VerifyTime < query.EndDate.Value.AddDays(1));

        var total = await queryable.CountAsync();
        var items = await queryable
            // 加载项目明细：列表需聚合核销项目名称（VerifyItem），与 GetByIdAsync 的 Include 保持一致
            .Include(v => v.Items)
            .OrderByDescending(v => v.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = items.Adapt<List<TreatmentCardVerifyDto>>();
        // 批量填充客户名称/卡名称/核销项目/剩余次数（join TreatmentCardSale/Customer/TreatmentCard/Product，避免 N+1）
        // 对齐 TreatmentCardSaleAppService.FillSaleDisplayInfoAsync 的展示字段补全模式
        await FillVerifyDisplayInfoAsync(dtos, tenantId);

        var result = new PagedResponseDto<TreatmentCardVerifyDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardVerifyDto>>.Ok(result);
    }

    /// <summary>
    /// 批量填充核销记录的客户名称/卡名称/核销项目/剩余次数（join TreatmentCardSale/Customer/TreatmentCard/Product，避免 N+1）
    /// 对齐 TreatmentCardSaleAppService.FillSaleDisplayInfoAsync 的展示字段补全模式
    /// </summary>
    /// <param name="dtos">核销记录 DTO 列表（含 Items 明细，需先 Include）</param>
    /// <param name="tenantId">租户ID</param>
    private async Task FillVerifyDisplayInfoAsync(List<TreatmentCardVerifyDto> dtos, long tenantId)
    {
        if (dtos.Count == 0) return;

        var cardSaleIds = dtos.Select(d => d.CardSaleId).Distinct().ToList();
        var sales = await _dbContext.TreatmentCardSales
            .Where(s => cardSaleIds.Contains(s.Id) && s.TenantId == tenantId)
            .Select(s => new { s.Id, s.CustomerId, s.CardId })
            .ToDictionaryAsync(s => s.Id);

        // 项目卡总次数 = 销售明细 Quantity 之和。
        // 不用销售记录的 RemainingTimes 冗余快照字段：该字段可能因历史数据/维护不同步而偏离真实核销记录，
        // 与核销明细同源聚合即可精确反映「总次数 - 已核销次数」。
        var saleItemTotals = await _dbContext.TreatmentCardSaleItems
            .Where(si => cardSaleIds.Contains(si.SaleId) && si.TenantId == tenantId)
            .GroupBy(si => si.SaleId)
            .Select(g => new { SaleId = g.Key, TotalTimes = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.SaleId, x => x.TotalTimes);

        // 该卡全部有效核销记录（含当前分页外的部分），按核销时间升序（同时刻按 ID 兜底）。
        // 用途：计算每条核销记录「核销完成后该卡剩余次数」的历史快照——同一张卡的多条记录显示各自时点的剩余，
        // 而非全部显示同一「当前剩余」值（如 3 次卡分 2+1 两次核销，两条记录应分别显示 1 和 0）。
        var verifies = await _dbContext.TreatmentCardVerifies
            .Where(v => cardSaleIds.Contains(v.CardSaleId) && v.ReverseStatus == 0 && v.TenantId == tenantId)
            .OrderBy(v => v.VerifyTime).ThenBy(v => v.Id)
            .Select(v => new { v.Id, v.CardSaleId, v.VerifyTimes })
            .ToListAsync();

        // 每条核销记录截至自身的累计有效核销次数（前缀累计，排除已冲正记录，与冲正恢复剩余次数的口径一致）
        var consumedByVerify = new Dictionary<long, int>();
        foreach (var group in verifies.GroupBy(v => v.CardSaleId))
        {
            var cumulative = 0;
            foreach (var v in group)
            {
                cumulative += v.VerifyTimes;
                consumedByVerify[v.Id] = cumulative;
            }
        }

        var customerIds = sales.Values.Select(s => s.CustomerId).Distinct().ToList();
        var customers = await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id) && c.TenantId == tenantId)
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToDictionaryAsync(c => c.Id);

        var cardIds = sales.Values.Select(s => s.CardId).Distinct().ToList();
        var cards = await _dbContext.TreatmentCards
            .Where(c => cardIds.Contains(c.Id) && c.TenantId == tenantId)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id);

        var productIds = dtos.SelectMany(d => d.Items.Select(i => i.ProductId)).Distinct().ToList();
        var productNames = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId)
            .Select(p => new { p.Id, Name = p.Master.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        foreach (var dto in dtos)
        {
            if (sales.TryGetValue(dto.CardSaleId, out var sale))
            {
                // 剩余次数 = 项目卡总次数 - 截至该条核销记录的累计有效核销次数（下限 0，兜底历史脏数据）
                dto.RemainingCount = Math.Max(0, saleItemTotals.GetValueOrDefault(dto.CardSaleId) - consumedByVerify.GetValueOrDefault(dto.Id));
                if (customers.TryGetValue(sale.CustomerId, out var customer))
                {
                    dto.CustomerName = customer.Name;
                    dto.Phone = customer.Phone;
                }
                if (cards.TryGetValue(sale.CardId, out var card))
                    dto.CardName = card.Name;
            }
            dto.VerifyItem = string.Join("、", dto.Items
                .Select(i => productNames.GetValueOrDefault(i.ProductId, string.Empty))
                .Where(n => !string.IsNullOrWhiteSpace(n)));
            foreach (var item in dto.Items)
                item.ProductName = productNames.GetValueOrDefault(item.ProductId);
        }
    }

    /// <summary>
    /// 根据ID获取项目卡核销记录详情
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardVerifyDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardVerifyDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.TreatmentCardVerifies
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId && v.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<TreatmentCardVerifyDto?>.Fail("项目卡核销记录不存在", 404);
        var dto = entity.Adapt<TreatmentCardVerifyDto>();
        // 复用列表的批量补全（客户名称/手机号/卡名称/剩余次数/核销项目/明细项目名称），详情展示字段与列表口径一致
        await FillVerifyDisplayInfoAsync(new List<TreatmentCardVerifyDto> { dto }, tenantId);
        return ApiResponseDto<TreatmentCardVerifyDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建项目卡核销记录（核销入口，事务包裹）
    /// 支持一次操作核销多个项目（一次到店做多种护理）：
    /// 1) 校验所有项目在项目卡明细中且次数合法
    /// 2) 按各项目折算单价计算金额，最后一项应用兜底（消除分摊精度误差）
    /// 3) 创建关联订单（OrderType=3，Status=2 已完成，核销不收款）
    /// 4) 创建主单 + N 条明细
    /// 5) 扣减剩余次数、更新累计消费金额
    /// 6) 记录消费（核销不发积分）
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> CreateAsync(TreatmentCardVerifyCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var now = DateTime.Now;

        // 阶段4.1：统一 StoreId 写入规则
        // 核销门店严格使用当前登录用户所属门店，禁止通过 dto.StoreId 传入
        // 跨店核销的合法性通过 IsCrossStore 标记体现（核销门店 ≠ 发卡门店）
        // 防止前端伪造 StoreId 越权（P-TC-02：跨租户核销拒绝）
        if (!_currentUser.StoreId.HasValue || _currentUser.StoreId.Value <= 0)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("无法确定当前门店", 401);

        var orderStoreId = _currentUser.StoreId.Value;
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == orderStoreId && s.TenantId == tenantId && !s.IsDeleted);
        if (store == null)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("核销门店不属于当前租户", 403);
        var orderStoreCode = store.Code;

        // 查询项目卡销售记录（含项目明细）
        var sale = await _dbContext.TreatmentCardSales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == dto.CardSaleId && s.TenantId == tenantId && !s.IsDeleted);
        if (sale == null)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("项目卡销售记录不存在", 404);

        // 验证项目卡状态
        if (sale.Status != 1)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("项目卡已用完或已过期，无法核销", 400);
        if (sale.RemainingTimes <= 0)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("项目卡剩余次数为0", 400);
        if (sale.ExpiryDate < now)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("项目卡已过期，无法核销", 400);

        // 规则2：跨店核销开关校验
        // 若本次核销为跨店（核销门店 ≠ 发卡门店），需检查租户是否允许跨店核销
        var isCrossStore = orderStoreId != sale.StoreId;
        if (isCrossStore)
        {
            var tenantSetting = await _dbContext.StoreTenantSettings
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && !s.IsDeleted);
            // 默认允许跨店核销（配置不存在或 AllowCrossStoreVerify=true 时允许）
            if (tenantSetting != null && !tenantSetting.AllowCrossStoreVerify)
            {
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail("当前租户已关闭跨店核销功能，请在发卡门店核销", 403);
            }

            // 规则9：跨店核销客户身份验证（手机号尾号核对）
            var crossStoreCustomer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == tenantId);
            if (crossStoreCustomer == null)
            {
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail("客户信息不存在，无法进行跨店核销", 400);
            }

            if (string.IsNullOrWhiteSpace(crossStoreCustomer.Phone))
            {
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail("客户未登记手机号，无法进行跨店核销身份核验", 400);
            }

            if (string.IsNullOrWhiteSpace(dto.CustomerPhoneTail))
            {
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail("跨店核销需提供客户手机号尾号（后4位）进行身份核验", 400);
            }

            var actualPhoneTail = crossStoreCustomer.Phone.Length >= 4
                ? crossStoreCustomer.Phone[^4..]
                : crossStoreCustomer.Phone;
            if (!string.Equals(actualPhoneTail, dto.CustomerPhoneTail, StringComparison.Ordinal))
            {
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail("客户手机号尾号不匹配，身份核验失败", 403);
            }
        }

        // 校验 Items：每个项目必须在 sale.Items 中，且 VerifyTimes >= 1
        // 核销项目本质是服务商品（纯补录模式），服务日期+开始时间必填，用于资源占用检测与技师统计归集
        var saleItemDict = sale.Items.ToDictionary(si => si.ProductId, si => si);

        // 聚合该项目已核销次数与金额（排除已冲正 ReverseStatus=0 的记录），
        // 用于单项目次数校验（核销次数不超过该项目剩余次数）与项目维度最后一次核销兜底（B2 小数处理规则）
        var consumedByProduct = (await _dbContext.TreatmentCardVerifyItems
                .Where(vi => vi.Verify!.CardSaleId == sale.Id && vi.Verify!.ReverseStatus == 0)
                .GroupBy(vi => vi.ProductId)
                .Select(g => new { ProductId = g.Key, Times = g.Sum(vi => vi.VerifyTimes), Amount = g.Sum(vi => vi.SubAmount) })
                .ToListAsync())
            .ToDictionary(x => x.ProductId);

        var inputTotalTimes = 0;
        foreach (var item in dto.Items)
        {
            if (item.VerifyTimes < 1)
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"项目 #{item.ProductId} 核销次数必须 >= 1", 400);
            if (!saleItemDict.ContainsKey(item.ProductId))
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"项目 #{item.ProductId} 不在项目卡项目明细中", 400);
            if (!item.ServiceStartTime.HasValue)
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"项目 #{item.ProductId} 未录入服务开始时间", 400);
            // 有结束时间时必须晚于开始时间（无结束时间时后端按服务时长推算）
            if (item.ServiceEndTime.HasValue && item.ServiceEndTime.Value <= item.ServiceStartTime.Value)
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"项目 #{item.ProductId} 服务结束时间必须晚于开始时间", 400);
            // 单项目次数校验：核销次数不能超过该项目在项目卡中的剩余可核销次数（购买次数 - 已核销次数）
            var itemRemainingTimes = saleItemDict[item.ProductId].Quantity - (consumedByProduct.TryGetValue(item.ProductId, out var cd) ? cd.Times : 0);
            if (item.VerifyTimes > itemRemainingTimes)
                return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"项目 #{item.ProductId} 核销次数 {item.VerifyTimes} 超过该项目剩余次数 {itemRemainingTimes}", 400);
            inputTotalTimes += item.VerifyTimes;
        }
        if (inputTotalTimes > sale.RemainingTimes)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"本次核销总次数 {inputTotalTimes} 超过项目卡剩余次数 {sale.RemainingTimes}", 400);

        // 计算每项 SubAmount（B2 小数处理规则：系统内部以"分"为单位整数计算，避免浮点误差）：
        // - 非该项目最后一次核销：subAmount = 折算单价(分) × 次数 / 100
        // - 该项目最后一次核销（本次次数耗尽该项目剩余）：subAmount = 分摊总价值 - 该项目已核销累计金额，
        //   确保每个项目的核销金额总和严格等于其分摊总价值
        var computedItems = new List<(TreatmentCardVerifyItemInput Input, TreatmentCardSaleItem SaleItem, decimal SubAmount)>();
        foreach (var input in dto.Items)
        {
            var saleItem = saleItemDict[input.ProductId];
            var itemRemainingTimes = saleItem.Quantity - (consumedByProduct.TryGetValue(input.ProductId, out var cd) ? cd.Times : 0);
            decimal subAmount;
            if (itemRemainingTimes >= 1 && input.VerifyTimes >= itemRemainingTimes)
            {
                // 该项目最后一次核销：兜底
                var allocatedTotalFen = (long)Math.Round(saleItem.AllocatedTotalPrice * 100m, MidpointRounding.AwayFromZero);
                var consumedFen = (long)Math.Round((consumedByProduct.TryGetValue(input.ProductId, out var ca) ? ca.Amount : 0m) * 100m, MidpointRounding.AwayFromZero);
                subAmount = Math.Max(0, allocatedTotalFen - consumedFen) / 100m;
            }
            else
            {
                // 分整数运算：折算单价(分) × 次数
                var unitFen = (long)Math.Round(saleItem.AllocatedUnitPrice * 100m, MidpointRounding.AwayFromZero);
                subAmount = (unitFen * input.VerifyTimes) / 100m;
            }
            computedItems.Add((input, saleItem, subAmount));
        }

        // 构造核销明细实体集合（一次性构建，主表冗余字段从明细聚合得出，确保数据一致）
        // 阶段4.1：明细 StoreId/StoreCode 跟随父单据（核销门店）
        var verifyItems = computedItems.Select(ci => new TreatmentCardVerifyItem
        {
            ProductId = ci.Input.ProductId,
            VerifyTimes = ci.Input.VerifyTimes,
            AllocatedUnitPrice = ci.SaleItem.AllocatedUnitPrice,
            SubAmount = ci.SubAmount,
            // 核销项目按服务商品录入的资源/服务时间（技师/房间/设备可空，未选不占用不归集）
            TechnicianId = ci.Input.TechnicianId,
            TechnicianSource = ci.Input.TechnicianSource,
            RoomId = ci.Input.RoomId,
            EquipmentId = ci.Input.EquipmentId,
            ServiceStartTime = ci.Input.ServiceStartTime,
            ServiceEndTime = ci.Input.ServiceEndTime,
            // 核销项目明细备注（快速开单服务内容弹窗录入）
            Remark = ci.Input.Remark,
            StoreId = orderStoreId,
            StoreCode = orderStoreCode,
            TenantId = tenantId,
            TenantCode = tenantCode,
            CreatedTime = now
        }).ToList();

        // 主表冗余汇总字段 = 明细的聚合（保持单一数据源：明细）
        var totalVerifyAmount = verifyItems.Sum(i => i.SubAmount);
        var totalRequestedTimes = verifyItems.Sum(i => i.VerifyTimes);

        // 查询核销项目商品信息（订单明细冗余存储商品名/编码快照，供订单详情展示；类型用于库存/BOM 扣减分流）
        // 项目卡销售明细仅存 ProductId，商品名/编码/类型需从商品主档（Product.Master）现查
        var verifyProductIds = computedItems.Select(ci => ci.Input.ProductId).Distinct().ToList();
        var productInfoMap = (await _dbContext.Products
                .Where(p => verifyProductIds.Contains(p.Id))
                .Select(p => new { p.Id, Name = p.Master.Name, Code = p.Master.Code, Type = p.Master.Type })
                .ToListAsync())
            .ToDictionary(m => m.Id);

        // 核销前资源冲突检测：每个核销项目（选了技师/房间/设备且已录服务时间）占用检测
        // 与 OrderAppService.CreateAsync 一致：先检测后开事务，冲突则拒绝核销（400）
        // 核销项目本质是服务商品，参与"同一时段冲突检测"，与预约/有效订单互检
        var conflictProductIds = computedItems.Select(ci => ci.Input.ProductId).Distinct().ToList();
        var conflictProductMasterMap = await _dbContext.Products
            .Where(p => conflictProductIds.Contains(p.Id))
            .Select(p => new { p.Id, p.MasterId })
            .ToDictionaryAsync(p => p.Id, p => p.MasterId);
        var conflictMasterIds = conflictProductMasterMap.Values.Distinct().ToList();
        var conflictDurationsByMaster = await _dbContext.ServiceProducts
            .Where(sp => conflictMasterIds.Contains(sp.MasterId))
            .ToDictionaryAsync(sp => sp.MasterId, sp => sp.Duration ?? 0);

        var verifyConflicts = new List<string>();
        var verifyConflictedResources = new HashSet<string>();
        foreach (var ci in computedItems)
        {
            var input = ci.Input;
            // 技师/房间/设备三项资源全空时无占用维度，跳过冲突检测
            if (!input.TechnicianId.HasValue && !input.RoomId.HasValue && !input.EquipmentId.HasValue)
                continue;
            // 服务开始时间已必填（前面校验）；结束时间为空时按 开始时间 + 服务时长 推算
            if (!input.ServiceStartTime.HasValue)
                continue; // 防御：理论不会到达（已校验必填）
            var conflictStart = input.ServiceStartTime.Value;
            var conflictEnd = input.ServiceEndTime
                ?? (conflictProductMasterMap.TryGetValue(input.ProductId, out var masterId)
                    && conflictDurationsByMaster.TryGetValue(masterId, out var vDuration) && vDuration > 0
                    ? conflictStart.AddMinutes(vDuration)
                    : conflictStart);

            var result = await _resourceConflictCheckService.CheckAsync(
                tenantId, orderStoreId, input.TechnicianId, input.RoomId, input.EquipmentId,
                conflictStart, conflictEnd);

            if (result.HasAnyConflict)
            {
                if (result.TechnicianConflict)
                {
                    verifyConflicts.Add($"项目技师在该时段已有安排（{result.TechnicianConflictInfo}）");
                    verifyConflictedResources.Add("技师");
                }
                if (result.RoomConflict)
                {
                    verifyConflicts.Add($"项目房间在该时段已有安排（{result.RoomConflictInfo}）");
                    verifyConflictedResources.Add("房间");
                }
                if (result.EquipmentConflict)
                {
                    verifyConflicts.Add($"项目设备在该时段已有安排（{result.EquipmentConflictInfo}）");
                    verifyConflictedResources.Add("设备");
                }
            }
        }
        if (verifyConflicts.Any())
        {
            var advice = $"请更换{string.Join("、", verifyConflictedResources)}或调整服务时间";
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail($"{string.Join("；", verifyConflicts.Distinct())}，{advice}", 400);
        }

        // 混合结算（PosCheckoutAppService）调用时复用其外部事务，本方法不自开/不提交/不回滚；
        // 独立核销时自开事务（原行为不变）
        var ownsTransaction = _dbContext.Database.CurrentTransaction == null;
        await using var transaction = ownsTransaction
            ? await _dbContext.Database.BeginTransactionAsync()
            : null;
        try
        {
            // 创建关联订单（OrderType=3 项目卡核销，Status=2 已完成，核销不涉及支付）
            // 订单含多个 OrderItem，每个核销项目对应一条
            // 阶段4.1：订单 StoreId/StoreCode 与核销单保持一致（核销门店）
            var order = new Order
            {
                OrderNo = $"TCV-{now:yyyyMMddHHmmssfff}",
                CustomerId = sale.CustomerId,
                OrderType = 3,
                Status = 2,
                ProductAmount = totalVerifyAmount,
                PaidAmount = 0,
                Points = 0,
                OrderTime = now,
                CompleteTime = now,
                StoreId = orderStoreId,
                StoreCode = orderStoreCode,
                OperatorId = dto.OperatorId,
                Remark = $"项目卡核销",
                CheckoutSessionNo = dto.CheckoutSessionNo,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now,
                OrderItems = verifyItems.Select(vi => new OrderItem
                {
                    ProductId = vi.ProductId,
                    // 订单明细冗余存储商品名/编码快照，供订单详情展示（与商品主档一致）
                    ProductName = productInfoMap.GetValueOrDefault(vi.ProductId)?.Name ?? string.Empty,
                    ProductCode = productInfoMap.GetValueOrDefault(vi.ProductId)?.Code ?? string.Empty,
                    Quantity = vi.VerifyTimes,
                    Price = vi.SubAmount / Math.Max(1, vi.VerifyTimes),
                    DiscountedAmount = vi.SubAmount,
                    // 从核销明细复制技师/房间/设备/服务时间到 OrderItem，保证技师统计归集与占用检测口径一致
                    TechnicianId = vi.TechnicianId,
                    TechnicianSource = vi.TechnicianSource,
                    RoomId = vi.RoomId,
                    EquipmentId = vi.EquipmentId,
                    ServiceStartTime = vi.ServiceStartTime,
                    ServiceEndTime = vi.ServiceEndTime,
                    // 核销项目备注透传给核销关联订单明细，与订单侧服务备注口径一致
                    Remark = vi.Remark,
                    StoreId = orderStoreId,
                    StoreCode = orderStoreCode,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    CreatedTime = now
                }).ToList()
            };
            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            // P-TC-01: 按 Product.Type 联动扣减库存/BOM 耗材
            // 核销为权责发生制转营收，库存/BOM 扣减为实物消耗，两者在同一事务内完成
            // 扣减失败抛异常触发事务回滚（项目卡次数不变化）
            // - Type=1（零售）：扣减实物商品库存（FEFO 近效期优先）
            // - Type=2（服务）：扣减 BOM 耗材库存（FEFO 近效期优先）
            // - 其他类型（3=耗材/4=样品/5=赠品）不通过核销扣减
            var productTypeDict = productInfoMap.ToDictionary(kv => kv.Key, kv => kv.Value.Type);
            // dto.Items 与 verifyItems 按索引一一对应（computedItems 派生），透传输入以取店员选择的耗材效期
            await DeductInventoryAndBomAsync(order, verifyItems, productTypeDict, dto.Items, now);

            // 创建核销主单（冗余字段从明细聚合得出）
            // 阶段4.2：IsCrossStore 跨店标记 = 核销门店 ≠ 发卡门店（持久化存储便于报表过滤，规则1）
            var verify = new TreatmentCardVerifyEntity
            {
                StoreId = orderStoreId,
                StoreCode = orderStoreCode,
                CardSaleId = dto.CardSaleId,
                VerifyAmount = totalVerifyAmount,
                OrderId = order.Id,
                VerifyTimes = totalRequestedTimes,
                VerifyTime = now,
                OperatorId = dto.OperatorId ?? _currentUser.UserId,
                // 姓名快照只在操作人就是当前登录用户时可信；代录他人时取不到对方姓名，留空由前端显示占位符
                OperatorName = (dto.OperatorId ?? _currentUser.UserId) == _currentUser.UserId
                    ? _currentUser.RealName ?? _currentUser.UserName
                    : null,
                IsCrossStore = orderStoreId != sale.StoreId,
                Remark = dto.Remark,
                CheckoutSessionNo = dto.CheckoutSessionNo,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now,
                Items = verifyItems
            };
            _dbContext.TreatmentCardVerifies.Add(verify);

            // 扣减剩余次数，更新累计消费金额
            sale.RemainingTimes -= totalRequestedTimes;
            sale.TotalConsumedAmount += totalVerifyAmount;
            sale.UpdatedTime = now;
            if (sale.RemainingTimes <= 0)
            {
                sale.Status = 2; // 已用完
            }

            // 记录消费记录（核销不发积分）
            _dbContext.ConsumeLogs.Add(new ConsumeLog
            {
                CustomerId = sale.CustomerId,
                OrderId = order.Id,
                Amount = totalVerifyAmount,
                Points = 0,
                ConsumeTime = now,
                Remark = $"项目卡核销-{order.OrderNo}",
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now
            });

            // 记录商品销售统计（ProductType=5 项目卡核销，独立类型，不计入销售排行）
            // 与项目卡购买（ProductType=4）区分，避免排行重复计算
            // P-DS-05: 核销为权责发生制转营收，不应出现在商品/服务销售 TOP 排行中
            // 核销金额供首页"营收构成"饼图消费（ProductType=5 按类型聚合），故仍须落库
            // 唯一索引已含 ProductType，核销(5)与销售(1/2)同商品同天可共存，不会 23505
            var productIds = computedItems.Select(ci => ci.Input.ProductId).Distinct().ToList();
            var productNames = await _dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, Name = p.Master.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var statDate = now.Date;
            foreach (var ci in computedItems)
            {
                var productName = productNames.GetValueOrDefault(ci.Input.ProductId, string.Empty);
                var existingStat = await _dbContext.ProductSalesStats
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId
                        && s.StoreId == orderStoreId
                        && s.ProductId == ci.Input.ProductId
                        && s.ProductType == 5
                        && s.StatDate == statDate);

                if (existingStat != null)
                {
                    existingStat.SalesCount += ci.Input.VerifyTimes;
                    existingStat.SalesAmount += ci.SubAmount;
                    existingStat.UpdatedTime = now;
                }
                else
                {
                    _dbContext.ProductSalesStats.Add(new ProductSalesStat
                    {
                        StatDate = statDate,
                        StatMonth = $"{now:yyyy-MM}",
                        ProductId = ci.Input.ProductId,
                        ProductName = productName,
                        ProductType = 5,
                        SalesCount = ci.Input.VerifyTimes,
                        SalesAmount = ci.SubAmount,
                        TenantId = tenantId,
                        TenantCode = tenantCode,
                        StoreId = orderStoreId,
                        StoreCode = orderStoreCode,
                        CreatedTime = now
                    });
                }
            }

            // 更新客户最后消费时间
            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == sale.CustomerId && c.TenantId == tenantId);
            if (customer != null)
            {
                customer.LastConsumeTime = now;
                customer.UpdatedTime = now;
            }

            // 阶段6：跨店核销审计日志（文档 6.1 节）
            // 记录操作门店、操作员、IP、客户身份核验记录、卡销售单信息
            // 审计日志在事务内写入，事务回滚则同步回滚
            await _auditService.LogAsync(new CrossStoreOperationLog
            {
                OperationType = "CrossStoreVerify",
                OperatorId = dto.OperatorId ?? _currentUser.UserId ?? 0,
                OperatorName = _currentUser.RealName,
                OperationTime = now,
                CustomerId = sale.CustomerId,
                CustomerName = customer?.Name,
                CustomerPhoneTail = customer?.Phone?.Length >= 4
                    ? customer.Phone[^4..]
                    : customer?.Phone,
                HomeStoreId = sale.StoreId,
                IsCrossStore = isCrossStore,
                RelatedEntityId = verify.Id,
                RelatedEntitySnapshot = $"{{\"CardSaleId\":{dto.CardSaleId},\"VerifyAmount\":{totalVerifyAmount:F2},\"VerifyTimes\":{totalRequestedTimes},\"RemainingTimes\":{sale.RemainingTimes}}}",
                Remark = $"项目卡核销-{order.OrderNo}",
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = orderStoreId,
                StoreCode = orderStoreCode
            });

            await _dbContext.SaveChangesAsync();

            // 技师统计归集：核销项目本质是服务商品，按核销明细涉及的 (技师, 服务日期) 重算归集
            // StatDate = 该项目服务时间日期（为空回退订单下单日）；与核销同事务，失败则整体回滚
            await RecalculateTechnicianStatsForVerifyItemsAsync(
                tenantId, orderStoreId, order.OrderTime, verifyItems);

            if (ownsTransaction)
                await transaction!.CommitAsync();
            return ApiResponseDto<TreatmentCardVerifyDto>.Ok(verify.Adapt<TreatmentCardVerifyDto>(), "核销成功");
        }
        catch
        {
            if (ownsTransaction)
                await transaction!.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新项目卡核销记录
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardVerifyDto>> UpdateAsync(TreatmentCardVerifyUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.TreatmentCardVerifies
            .FirstOrDefaultAsync(v => v.Id == dto.Id && v.TenantId == tenantId && v.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<TreatmentCardVerifyDto>.Fail("项目卡核销记录不存在", 404);

        // StoreId/StoreCode 为核销门店永久归属，禁止修改（文档 5.4 节）
        entity.CardSaleId = dto.CardSaleId;
        entity.OperatorId = dto.OperatorId ?? _currentUser.UserId;
        // 操作人变更时同步刷新姓名快照，避免 ID 与姓名错配
        entity.OperatorName = entity.OperatorId == _currentUser.UserId
            ? _currentUser.RealName ?? _currentUser.UserName
            : null;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardVerifyDto>.Ok(entity.Adapt<TreatmentCardVerifyDto>(), "更新成功");
    }

    /// <summary>
    /// 核销冲正（规则7）
    /// 通过 ReverseStatus 状态机实现，不物理删除核销记录
    /// 冲正时：恢复项目卡剩余次数、冲减累计消费金额、取消关联订单、冲减商品销售统计
    /// 冲正金额冲减原核销门店服务业绩
    /// 注：库存/BOM 实物消耗的冲回需通过独立的库存调整单处理（此处不涉及）
    /// </summary>
    public async Task<ApiResponseDto> ReverseAsync(TreatmentCardVerifyReverseDto dto)
    {
        // 审计日志语义化：标记业务动作类型
        _auditLogContext.CustomOperationType = "核销冲正";

        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var storeId = _currentUser.StoreId ?? 0;
        var now = DateTime.Now;

        var verify = await _dbContext.TreatmentCardVerifies
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.Id == dto.Id && v.TenantId == tenantId && v.StoreId == storeId);
        if (verify == null)
            return ApiResponseDto.Fail("项目卡核销记录不存在", 404);

        // 状态机校验：仅正常状态（ReverseStatus=0）可冲正
        if (verify.ReverseStatus != 0)
            return ApiResponseDto.Fail("该核销记录已冲正，不可重复冲正", 400);

        // 加载关联的项目卡销售记录
        var sale = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == verify.CardSaleId && s.TenantId == tenantId && !s.IsDeleted);
        if (sale == null)
            return ApiResponseDto.Fail("关联的项目卡销售记录不存在", 404);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            // 1. 更新核销记录冲正状态
            verify.ReverseStatus = 1; // 已冲正
            verify.Remark = string.IsNullOrWhiteSpace(verify.Remark)
                ? $"已冲正：{dto.Remark}"
                : $"{verify.Remark} | 已冲正：{dto.Remark}";
            verify.UpdatedTime = now;

            // 2. 恢复项目卡剩余次数和累计消费金额
            sale.RemainingTimes += verify.VerifyTimes;
            sale.TotalConsumedAmount = Math.Max(0m, sale.TotalConsumedAmount - verify.VerifyAmount);
            sale.UpdatedTime = now;
            // 若卡因本次核销被标记为已用完（Status=2），冲正后恢复为有效（Status=1）
            if (sale.Status == 2 && sale.RemainingTimes > 0)
            {
                sale.Status = 1;
            }

            // 3. 取消关联订单（Status=4 已取消）
            if (verify.OrderId.HasValue)
            {
                var order = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.Id == verify.OrderId.Value && o.TenantId == tenantId);
                if (order != null && order.Status == 2)
                {
                    order.Status = 4; // 已取消
                    order.UpdatedTime = now;
                }
            }

            // 4. 冲减商品销售统计（ProductType=5 项目卡核销）
            // 冲正金额冲减原核销门店（verify.StoreId）的服务业绩，保持营收构成口径对称
            var statDate = verify.VerifyTime.Date;
            var productIds = verify.Items.Select(i => i.ProductId).Distinct().ToList();
            var stats = await _dbContext.ProductSalesStats
                .Where(s => s.TenantId == tenantId
                    && s.StoreId == verify.StoreId
                    && s.ProductType == 5
                    && s.StatDate == statDate
                    && productIds.Contains(s.ProductId))
                .ToListAsync();

            foreach (var stat in stats)
            {
                var item = verify.Items.FirstOrDefault(i => i.ProductId == stat.ProductId);
                if (item != null)
                {
                    stat.SalesCount = Math.Max(0, stat.SalesCount - item.VerifyTimes);
                    stat.SalesAmount = Math.Max(0m, stat.SalesAmount - item.SubAmount);
                    stat.UpdatedTime = now;
                }
            }

            // 5. 写入冲正审计日志
            await _auditService.LogAsync(new CrossStoreOperationLog
            {
                OperationType = "VerifyReverse",
                OperatorId = _currentUser.UserId ?? 0,
                OperatorName = _currentUser.RealName,
                OperationTime = now,
                CustomerId = sale.CustomerId,
                HomeStoreId = sale.StoreId,
                IsCrossStore = verify.IsCrossStore,
                RelatedEntityId = verify.Id,
                RelatedEntitySnapshot = $"{{\"ReversedVerifyAmount\":{verify.VerifyAmount:F2},\"ReversedVerifyTimes\":{verify.VerifyTimes}}}",
                Remark = $"核销冲正-{dto.Remark}",
                TenantId = tenantId,
                TenantCode = tenantCode,
                StoreId = verify.StoreId,
                StoreCode = verify.StoreCode
            });

            // 注：库存/BOM 实物消耗的冲回需通过独立的库存调整单处理
            // （实物已消耗的耗材无法自动回补，需仓库单独盘点调整）

            await _dbContext.SaveChangesAsync();

            // 技师统计回退：冲正后核销订单（Status→4）不再满足归集条件，按核销明细涉及的 (技师, 服务日期) 重算剔除
            // 需先 SaveChanges 将订单 Status=4 落库，重算的 Status==2 过滤才看不到该订单（SQL 层查询，非内存跟踪）
            // StatDate = 项目服务时间日期（为空回退核销时间）
            await RecalculateTechnicianStatsForVerifyItemsAsync(tenantId, verify.StoreId, verify.VerifyTime, verify.Items);

            await transaction.CommitAsync();
            return ApiResponseDto.Success(null, "核销冲正成功");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 按核销明细涉及的 (技师, 服务日期) 去重后逐一重算技师统计（核销创建归集 / 冲正回退共用，天然幂等）
    /// 仅商家技师（TechnicianSource==1）参与归集；服务时间日期为空回退 fallbackTime 日期
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID（核销门店）</param>
    /// <param name="fallbackTime">回退时间（服务时间为空时的统计归属日期基准，核销场景=订单下单日/核销时间）</param>
    /// <param name="verifyItems">核销明细列表</param>
    private async Task RecalculateTechnicianStatsForVerifyItemsAsync(
        long tenantId, long storeId, DateTime fallbackTime, List<TreatmentCardVerifyItem> verifyItems)
    {
        var keys = verifyItems
            .Where(vi => vi.TechnicianId.HasValue && vi.TechnicianSource == 1)
            .Select(vi => new
            {
                TechnicianId = vi.TechnicianId!.Value,
                StatDate = (vi.ServiceStartTime ?? fallbackTime).Date
            })
            .Distinct()
            .ToList();

        foreach (var key in keys)
        {
            await _technicianStatisticAppService.RecalculateTechnicianStatisticAsync(
                tenantId, storeId, key.TechnicianId, key.StatDate);
        }
    }

    /// <summary>
    /// P-TC-01: 按 Product.Type 联动扣减库存/BOM 耗材
    /// 核销为权责发生制转营收，库存/BOM 扣减为实物消耗，两者在同一事务内完成
    /// - Type=1（零售）：扣减实物商品库存（FEFO 近效期优先）
    /// - Type=2（服务）：扣减 BOM 耗材库存（指定效期或 FEFO）
    /// - 其他类型（3=耗材/4=样品/5=赠品）不通过核销扣减
    /// </summary>
    /// <param name="order">核销关联订单（含 OrderItems，与 verifyItems 按索引一一对应）</param>
    /// <param name="verifyItems">核销项目明细</param>
    /// <param name="productTypeDict">ProductId -> Product.Type 映射</param>
    /// <param name="inputItems">核销项目输入（与 verifyItems 按索引一一对应，含店员选择的耗材效期）</param>
    /// <param name="now">统一时间戳</param>
    private async Task DeductInventoryAndBomAsync(
        Order order,
        List<TreatmentCardVerifyItem> verifyItems,
        Dictionary<long, int> productTypeDict,
        List<TreatmentCardVerifyItemInput> inputItems,
        DateTime now)
    {
        for (var i = 0; i < verifyItems.Count; i++)
        {
            var verifyItem = verifyItems[i];
            var orderItem = order.OrderItems[i];
            var productType = productTypeDict.GetValueOrDefault(verifyItem.ProductId, 0);

            switch (productType)
            {
                case 1: // 零售商品：扣减实物库存（FEFO）
                    await DeductRetailInventoryForVerifyAsync(order, orderItem, verifyItem.VerifyTimes, now);
                    break;
                case 2: // 服务项目：扣减 BOM 耗材（店员指定效期或 FEFO）
                    await DeductServiceBomForVerifyAsync(order, orderItem, verifyItem.VerifyTimes, inputItems[i].ConsumableExpiries, now);
                    break;
                // 其他类型（3=耗材/4=样品/5=赠品）不通过核销扣减
            }
        }
    }

    /// <summary>
    /// 零售商品核销出库：按 FEFO（近效期优先）自动扣减批次库存
    /// 库存不足抛异常，由 CreateAsync 捕获触发事务回滚（项目卡次数不变化）
    /// </summary>
    private async Task DeductRetailInventoryForVerifyAsync(
        Order order, OrderItem orderItem, int needQty, DateTime now)
    {
        var remaining = (decimal)needQty;

        var batches = await _dbContext.InventoryBatches
            .Where(b => b.ProductId == orderItem.ProductId
                && b.Status == 1
                && b.Quantity > 0
                && b.TenantId == order.TenantId
                && b.StoreId == order.StoreId)
            .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)  // null 批次排末尾
            .ThenBy(b => b.ExpirationDate)                    // FEFO: 近效期优先
            .ThenBy(b => b.PurchaseDate)                      // 同效期 FIFO
            .ThenBy(b => b.CreatedTime)                       // null 批次按 CreatedTime 升序，兜底稳定排序
            .ToListAsync();

        foreach (var batch in batches)
        {
            if (remaining <= 0) break;

            var deduct = Math.Min(batch.Quantity, remaining);
            var beforeQty = batch.Quantity;

            // 扣减批次
            batch.Quantity -= deduct;
            batch.UpdatedTime = now;
            if (batch.Quantity <= 0) batch.Status = 2;

            // 更新库存汇总
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(inv => inv.ProductId == orderItem.ProductId
                    && inv.TenantId == order.TenantId
                    && inv.StoreId == order.StoreId);
            if (inventory != null)
            {
                inventory.Quantity -= deduct;
                inventory.UpdatedTime = now;
            }

            // 记录库存流水（Type=2 出库）
            _dbContext.InventoryLogs.Add(new InventoryLog
            {
                ProductId = orderItem.ProductId,
                Type = 2,
                SourceType = InventoryLogSourceTypes.TreatmentCardOutbound,
                UnitPrice = batch.UnitPrice,
                Quantity = -deduct,
                BeforeQuantity = beforeQty,
                AfterQuantity = batch.Quantity,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                RelatedId = order.Id,
                Remark = order.OrderNo,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            // 记录订单明细批次扣减（效期追溯和统计的数据源）
            _dbContext.OrderItemBatches.Add(new OrderItemBatch
            {
                OrderItemId = orderItem.Id,
                OrderId = order.Id,
                ProductId = orderItem.ProductId,
                BatchId = batch.Id,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                UnitPrice = batch.UnitPrice,
                Quantity = deduct,
                CostAmount = deduct * batch.UnitPrice,
                RefundedQuantity = 0m,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            remaining -= deduct;
        }

        if (remaining > 0)
            throw new InvalidOperationException($"商品(ID:{orderItem.ProductId})库存不足，还需 {remaining}");
    }

    /// <summary>
    /// 服务项目核销出库：按 BOM 计算耗材需求扣减批次库存。
    /// 店员核销加购时选择了绑定耗材效期（TreatmentCardVerifyItemInput.ConsumableExpiries）时按指定效期扣减，
    /// 不足部分自动按 FEFO 补足（核销项无 AllowAutoFillBeyondSelection 透传，加购时前端已校验库存，此兜底仅应对并发竞争）；
    /// 未选择（未绑定耗材或默认自动推荐）时按 FEFO（近效期优先）自动扣减。
    /// 全部批次扣减后仍不足则抛异常，由 CreateAsync 捕获触发事务回滚。
    /// </summary>
    private async Task DeductServiceBomForVerifyAsync(
        Order order, OrderItem orderItem, int verifyTimes,
        List<ConsumableExpiryInput>? consumableExpiries, DateTime now)
    {
        // 查找服务项目的 BOM：BOM 服务端关联服务项目档案，而核销项 ProductId 是门店商品档案，
        // 经商品主档桥接（商品档案.MasterId == 服务项目档案.MasterId）定位对应服务项目档案
        var serviceProductIds = await _dbContext.Products
            .Where(p => p.Id == orderItem.ProductId)
            .Join(_dbContext.ServiceProducts, p => p.MasterId, sp => sp.MasterId, (p, sp) => sp.Id)
            .Distinct()
            .ToListAsync();
        var boms = serviceProductIds.Count == 0
            ? new List<ServiceBom>()
            : await _dbContext.ServiceBoms
                .Where(b => serviceProductIds.Contains(b.ServiceProductId) && b.TenantId == order.TenantId)
                .ToListAsync();

        if (!boms.Any()) return;

        // 一次性加载本次涉及耗材的商品名（库存不足提示用）
        var consumableIds = boms.Select(b => b.ConsumableProductId).Distinct().ToList();
        var consumableNames = await _dbContext.Products
            .Where(p => consumableIds.Contains(p.Id))
            .Select(p => new { p.Id, Name = p.Master != null ? p.Master.Name : string.Empty })
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        foreach (var bom in boms)
        {
            // 需要扣减的数量 = BOM 单次消耗量 × 核销次数
            var needQty = bom.Quantity * verifyTimes;

            // 店员为该耗材选择的效期（未绑定耗材/默认自动推荐时为空，走 FEFO）
            var expiry = consumableExpiries?.FirstOrDefault(e => e.ProductId == bom.ConsumableProductId);

            await DeductConsumableForVerifyAsync(
                order, orderItem, bom.ConsumableProductId,
                consumableNames.GetValueOrDefault(bom.ConsumableProductId, $"耗材(ID:{bom.ConsumableProductId})"),
                needQty, expiry?.ExpirationDates, now);
        }
    }

    /// <summary>
    /// 核销耗材批次扣减：指定效期按选择顺序扣减（同效期 FIFO），不足按 FEFO 自动补足；未指定效期按 FEFO 扣减。
    /// 扣减明细写入 OrderItemBatch（ProductId=耗材ID，便于按耗材统计效期消耗），全部扣减后仍不足抛异常触发事务回滚。
    /// </summary>
    private async Task DeductConsumableForVerifyAsync(
        Order order, OrderItem orderItem, long consumableProductId, string consumableName,
        decimal needQty, List<DateTime?>? expirationDates, DateTime now)
    {
        var remaining = needQty;
        // 记录已扣减的效期日期（自动补足时排除）；null 表示"无效期限制"批次
        var processedExpirationDates = new List<DateTime?>();

        // 1. 店员选择了效期 -> 按选择顺序扣减
        if (expirationDates != null && expirationDates.Any())
        {
            foreach (var expirationDate in expirationDates)
            {
                if (remaining <= 0) break;

                List<InventoryBatch> batches;
                if (expirationDate.HasValue)
                {
                    batches = await _dbContext.InventoryBatches
                        .Where(b => b.ProductId == consumableProductId
                            && b.ExpirationDate == expirationDate.Value
                            && b.Status == 1
                            && b.Quantity > 0
                            && b.TenantId == order.TenantId
                            && b.StoreId == order.StoreId)
                        .OrderBy(b => b.PurchaseDate) // 同效期 FIFO
                        .ToListAsync();
                }
                else
                {
                    batches = await _dbContext.InventoryBatches
                        .Where(b => b.ProductId == consumableProductId
                            && !b.ExpirationDate.HasValue
                            && b.Status == 1
                            && b.Quantity > 0
                            && b.TenantId == order.TenantId
                            && b.StoreId == order.StoreId)
                        .OrderBy(b => b.CreatedTime) // 无效期批次按 CreatedTime 升序
                        .ToListAsync();
                }

                remaining = await DeductFromConsumableForVerifyAsync(order, orderItem, consumableProductId, batches, remaining, now);
                processedExpirationDates.Add(expirationDate);
            }
        }

        // 2. 未选效期 OR 指定效期不足 -> FEFO 自动补足（排除已扣减效期）
        if (remaining > 0)
        {
            var fefoQuery = _dbContext.InventoryBatches
                .Where(b => b.ProductId == consumableProductId
                    && b.Status == 1
                    && b.Quantity > 0
                    && b.TenantId == order.TenantId
                    && b.StoreId == order.StoreId);

            if (processedExpirationDates.Any())
            {
                var processedDates = processedExpirationDates
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value.Date)
                    .ToList();
                var hasProcessedNull = processedExpirationDates.Any(d => !d.HasValue);

                if (processedDates.Any())
                {
                    fefoQuery = fefoQuery.Where(b => !b.ExpirationDate.HasValue
                        || !processedDates.Contains(b.ExpirationDate.Value.Date));
                }
                if (hasProcessedNull)
                {
                    fefoQuery = fefoQuery.Where(b => b.ExpirationDate.HasValue);
                }
            }

            var fefoBatches = await fefoQuery
                .OrderBy(b => b.ExpirationDate.HasValue ? 0 : 1)  // null 批次排末尾
                .ThenBy(b => b.ExpirationDate)                    // FEFO: 近效期优先
                .ThenBy(b => b.PurchaseDate)                      // 同效期 FIFO
                .ThenBy(b => b.CreatedTime)                       // null 批次按 CreatedTime 升序，兜底稳定排序
                .ToListAsync();

            remaining = await DeductFromConsumableForVerifyAsync(order, orderItem, consumableProductId, fefoBatches, remaining, now);
        }

        // 3. 仍有剩余 = 库存不足
        if (remaining > 0)
            throw new InvalidOperationException($"耗材 {consumableName} 库存不足，还需 {remaining}");
    }

    /// <summary>
    /// 从指定耗材批次列表中逐批扣减核销耗材库存，更新 Inventory 汇总表，记 InventoryLog（TreatmentCardOutbound）与 OrderItemBatch。
    /// 返回未满足的剩余数量（0 表示全部扣减完成）。
    /// </summary>
    private async Task<decimal> DeductFromConsumableForVerifyAsync(
        Order order, OrderItem orderItem, long consumableProductId,
        List<InventoryBatch> batches, decimal needQty, DateTime now)
    {
        var remaining = needQty;
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;
            var deduct = Math.Min(batch.Quantity, remaining);

            var beforeQty = batch.Quantity;
            batch.Quantity -= deduct;
            batch.UpdatedTime = now;
            if (batch.Quantity <= 0) batch.Status = 2;

            // 更新库存汇总
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(inv => inv.ProductId == consumableProductId
                    && inv.TenantId == order.TenantId
                    && inv.StoreId == order.StoreId);
            if (inventory != null)
            {
                inventory.Quantity -= deduct;
                inventory.UpdatedTime = now;
            }

            // 记录流水
            _dbContext.InventoryLogs.Add(new InventoryLog
            {
                ProductId = consumableProductId,
                Type = 2,
                SourceType = InventoryLogSourceTypes.TreatmentCardOutbound,
                UnitPrice = batch.UnitPrice,
                Quantity = -deduct,
                BeforeQuantity = beforeQty,
                AfterQuantity = batch.Quantity,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                RelatedId = order.Id,
                Remark = order.OrderNo,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.RealName ?? _currentUser.UserName,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            // 记录订单明细批次扣减（ProductId 记录耗材ID，便于按耗材统计效期消耗）
            _dbContext.OrderItemBatches.Add(new OrderItemBatch
            {
                OrderItemId = orderItem.Id,
                OrderId = order.Id,
                ProductId = consumableProductId,
                BatchId = batch.Id,
                BatchNo = batch.BatchNo,
                ExpirationDate = batch.ExpirationDate,
                UnitPrice = batch.UnitPrice,
                Quantity = deduct,
                CostAmount = deduct * batch.UnitPrice,
                RefundedQuantity = 0m,
                TenantId = order.TenantId,
                TenantCode = order.TenantCode,
                StoreId = order.StoreId,
                StoreCode = order.StoreCode,
                CreatedTime = now
            });

            remaining -= deduct;
        }
        return remaining;
    }
}
