using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCardTransfers;
using Bms.Store.Domain.Entities;
using TreatmentCardTransferEntity = Bms.Store.Domain.Entities.TreatmentCardTransfer;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 项目卡转让管理应用服务实现
/// </summary>
public class TreatmentCardTransferAppService : ITreatmentCardTransferAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TreatmentCardTransferCreateDto> _createValidator;
    private readonly IValidator<TreatmentCardTransferUpdateDto> _updateValidator;
    private readonly ICrossStoreOperationAuditService _auditService;

    public TreatmentCardTransferAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TreatmentCardTransferCreateDto> createValidator,
        IValidator<TreatmentCardTransferUpdateDto> updateValidator,
        ICrossStoreOperationAuditService auditService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditService = auditService;
    }

    /// <summary>
    /// 获取项目卡转让记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>> GetPagedListAsync(TreatmentCardTransferQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = _dbContext.TreatmentCardTransfers
            .Where(s => s.TenantId == tenantId && s.StoreId == storeId);

        if (query.CardSaleId.HasValue)
            queryable = queryable.Where(s => s.CardSaleId == query.CardSaleId.Value);
        if (query.FromCustomerId.HasValue)
            queryable = queryable.Where(s => s.FromCustomerId == query.FromCustomerId.Value);
        if (query.ToCustomerId.HasValue)
            queryable = queryable.Where(s => s.ToCustomerId == query.ToCustomerId.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(s => s.Status == query.Status.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(s => s.TransferDate.Date >= query.StartDate.Value.Date);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(s => s.TransferDate.Date <= query.EndDate.Value.Date);

        // 按客户名称或手机号关键字筛选（同时匹配原客户/新客户的姓名或手机号，子查询 join Customer 表，OR 语义）
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var matchedCustomerIds = _dbContext.Customers
                .Where(c => c.Name.Contains(query.Keyword) || c.Phone.Contains(query.Keyword))
                .Select(c => c.Id);
            queryable = queryable.Where(s => matchedCustomerIds.Contains(s.FromCustomerId) || matchedCustomerIds.Contains(s.ToCustomerId));
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 批量查询卡销售、客户、门店信息填充展示字段，避免 N+1
        var list = await BuildListWithDisplayFieldsAsync(items);

        var result = new PagedResponseDto<TreatmentCardTransferDto>
        {
            List = list,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>.Ok(result);
    }

    /// <summary>
    /// 批量填充转卡记录的展示字段（卡名称、原/新客户姓名电话、操作门店名称）
    /// </summary>
    private async Task<List<TreatmentCardTransferDto>> BuildListWithDisplayFieldsAsync(List<TreatmentCardTransferEntity> items)
    {
        if (!items.Any())
            return new List<TreatmentCardTransferDto>();

        var cardSaleIds = items.Where(s => s.CardSaleId > 0).Select(s => s.CardSaleId).Distinct().ToList();
        var customerIds = items.Select(s => s.FromCustomerId)
            .Concat(items.Select(s => s.ToCustomerId))
            .Distinct()
            .ToList();
        var storeIds = items.Where(s => s.StoreId > 0).Select(s => s.StoreId).Distinct().ToList();

        // 卡销售 -> 卡名（转卡记录可能引用已被软删除的销售记录，故按 TenantId 查询不追加 IsDeleted）
        var cardSales = await _dbContext.TreatmentCardSales
            .Where(s => cardSaleIds.Contains(s.Id))
            .Select(s => new { s.Id, s.CardId })
            .ToDictionaryAsync(s => s.Id);
        var cardIds = cardSales.Values.Select(c => c.CardId).Distinct().ToList();
        var cards = await _dbContext.TreatmentCards
            .Where(c => cardIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id);

        var customers = await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToDictionaryAsync(c => c.Id);

        var stores = await _dbContext.Stores
            .Where(st => storeIds.Contains(st.Id))
            .Select(st => new { st.Id, st.Name })
            .ToDictionaryAsync(st => st.Id);

        return items.Select(s =>
        {
            var dto = s.Adapt<TreatmentCardTransferDto>();
            dto.CardName = cardSales.TryGetValue(s.CardSaleId, out var cs) && cards.TryGetValue(cs.CardId, out var card)
                ? card.Name
                : null;
            dto.FromCustomerName = customers.TryGetValue(s.FromCustomerId, out var fc) ? fc.Name : null;
            dto.FromCustomerPhone = customers.TryGetValue(s.FromCustomerId, out fc) ? fc.Phone : null;
            dto.ToCustomerName = customers.TryGetValue(s.ToCustomerId, out var tc) ? tc.Name : null;
            dto.ToCustomerPhone = customers.TryGetValue(s.ToCustomerId, out tc) ? tc.Phone : null;
            dto.StoreName = stores.TryGetValue(s.StoreId, out var st) ? st.Name : null;
            return dto;
        }).ToList();
    }

    /// <summary>
    /// 根据ID获取项目卡转让记录详情
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardTransferDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardTransferDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<TreatmentCardTransferDto?>.Fail("项目卡转让记录不存在", 404);
        return ApiResponseDto<TreatmentCardTransferDto?>.Ok(entity.Adapt<TreatmentCardTransferDto>());
    }

    /// <summary>
    /// 创建项目卡转让记录
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardTransferDto>> CreateAsync(TreatmentCardTransferCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("登录状态异常，请重新登录", 401);

        // 阶段4.1：统一 StoreId 写入规则 - 操作门店严格使用当前登录用户所属门店
        if (!_currentUser.StoreId.HasValue || _currentUser.StoreId.Value <= 0)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("无法确定当前门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (dto.FromCustomerId == dto.ToCustomerId)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("不能转让给原客户本人", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        // 查询门店编码（操作门店永久归属，禁止通过 dto.StoreId 传入）
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == storeId && s.TenantId == tenantId);
        if (store == null)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("操作门店不属于当前租户", 403);
        var storeCode = store.Code;

        // 查询卡销售记录并校验业务规则（防并发转让/前端传错）
        var cardSale = await _dbContext.TreatmentCardSales
            .FirstOrDefaultAsync(s => s.Id == dto.CardSaleId && s.TenantId == tenantId);
        if (cardSale == null)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("项目卡销售记录不存在", 404);
        if (cardSale.CustomerId != dto.FromCustomerId)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("原客户与卡实际归属不符，请刷新后重试", 400);
        if (cardSale.Status != 1)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("项目卡已用完或已过期，不可转让", 400);

        // 更新卡归属为新客户（与转让记录在同一 SaveChanges 内提交，EF Core 默认事务保证原子性）
        cardSale.CustomerId = dto.ToCustomerId;
        cardSale.UpdatedTime = DateTime.Now;

        var now = DateTime.Now;
        var entity = dto.Adapt<TreatmentCardTransferEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.StoreCode = storeCode;
        entity.CreatedTime = now;
        entity.Status = 1; // 创建即完成转让
        entity.OperatorId = dto.OperatorId ?? _currentUser.UserId;
        // 姓名快照只在操作人就是当前登录用户时可信；代录他人时取不到对方姓名，留空由前端显示占位符
        entity.OperatorName = entity.OperatorId == _currentUser.UserId
            ? _currentUser.RealName ?? _currentUser.UserName
            : null;

        _dbContext.TreatmentCardTransfers.Add(entity);

        // 阶段6：项目卡转让审计日志（文档 6.1 节）
        // 记录操作门店、操作员、IP、转出/转入客户身份核验记录
        var fromCustomer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.FromCustomerId && c.TenantId == tenantId);
        var toCustomer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.ToCustomerId && c.TenantId == tenantId);
        await _auditService.LogAsync(new CrossStoreOperationLog
        {
            OperationType = "TreatmentCardTransfer",
            OperatorId = _currentUser.UserId ?? 0,
            OperatorName = _currentUser.RealName,
            OperationTime = now,
            CustomerId = dto.FromCustomerId,
            CustomerName = fromCustomer?.Name,
            CustomerPhoneTail = fromCustomer?.Phone?.Length >= 4
                ? fromCustomer.Phone[^4..]
                : fromCustomer?.Phone,
            HomeStoreId = cardSale.StoreId,
            IsCrossStore = storeId != cardSale.StoreId,
            RelatedEntityId = entity.Id,
            FromCustomerId = dto.FromCustomerId,
            ToCustomerId = dto.ToCustomerId,
            RelatedEntitySnapshot = $"{{\"CardSaleId\":{dto.CardSaleId},\"TransferFee\":{dto.TransferFee:F2}}}",
            Remark = $"项目卡转让-{dto.Remark ?? string.Empty}",
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            StoreId = storeId,
            StoreCode = storeCode
        });

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardTransferDto>.Ok(entity.Adapt<TreatmentCardTransferDto>(), "创建成功");
    }

    /// <summary>
    /// 更新项目卡转让记录
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardTransferDto>> UpdateAsync(TreatmentCardTransferUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId && s.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("项目卡转让记录不存在", 404);

        // StoreId/StoreCode 为操作门店永久归属，禁止修改（文档 5.4 节）
        entity.CardSaleId = dto.CardSaleId;
        entity.FromCustomerId = dto.FromCustomerId;
        entity.ToCustomerId = dto.ToCustomerId;
        entity.TransferDate = dto.TransferDate;
        entity.TransferFee = dto.TransferFee;
        entity.OperatorId = dto.OperatorId ?? _currentUser.UserId;
        // 操作人变更时同步刷新姓名快照，避免 ID 与姓名错配
        entity.OperatorName = entity.OperatorId == _currentUser.UserId
            ? _currentUser.RealName ?? _currentUser.UserName
            : null;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardTransferDto>.Ok(entity.Adapt<TreatmentCardTransferDto>(), "更新成功");
    }

    /// <summary>
    /// 删除项目卡转让记录（物理删除，单据类无软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto.Fail("项目卡转让记录不存在", 404);

        _dbContext.TreatmentCardTransfers.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除项目卡转让记录（物理删除，单据类无软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.TreatmentCardTransfers
            .Where(s => ids.Contains(s.Id) && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0))
            .ToListAsync();

        _dbContext.TreatmentCardTransfers.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 获取可转让的项目卡销售记录选项（转卡弹窗选择用）
    /// 仅返回状态有效(1)且剩余次数大于 0 的卡销售记录
    /// 项目卡租户内跨店通用，故不按操作门店过滤（与 CreateAsync 校验逻辑一致）
    /// </summary>
    public async Task<ApiResponseDto<List<TreatmentCardTransferOptionDto>>> GetTransferableCardSalesAsync()
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<TreatmentCardTransferOptionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var sales = await _dbContext.TreatmentCardSales
            .Where(s => s.TenantId == tenantId && !s.IsDeleted && s.Status == 1 && s.RemainingTimes > 0)
            .OrderByDescending(s => s.CreatedTime)
            .ToListAsync();

        if (!sales.Any())
            return ApiResponseDto<List<TreatmentCardTransferOptionDto>>.Ok(new List<TreatmentCardTransferOptionDto>());

        // 批量查询客户和项目卡信息，避免 N+1
        var customerIds = sales.Select(s => s.CustomerId).Distinct().ToList();
        var cardIds = sales.Select(s => s.CardId).Distinct().ToList();

        var customers = await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.Phone })
            .ToDictionaryAsync(c => c.Id);

        var cards = await _dbContext.TreatmentCards
            .Where(c => cardIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.TotalTimes })
            .ToDictionaryAsync(c => c.Id);

        var list = sales.Select(s =>
        {
            cards.TryGetValue(s.CardId, out var card);
            customers.TryGetValue(s.CustomerId, out var customer);
            return new TreatmentCardTransferOptionDto
            {
                Id = s.Id,
                CardName = card?.Name,
                CustomerId = s.CustomerId,
                CustomerName = customer?.Name,
                CustomerPhone = customer?.Phone,
                RemainingTimes = s.RemainingTimes,
                TotalTimes = card?.TotalTimes ?? s.RemainingTimes
            };
        }).ToList();

        return ApiResponseDto<List<TreatmentCardTransferOptionDto>>.Ok(list);
    }
}
