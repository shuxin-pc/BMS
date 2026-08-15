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
/// 疗程卡转让管理应用服务实现
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
    /// 获取疗程卡转让记录分页列表
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

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<TreatmentCardTransferDto>
        {
            List = items.Adapt<List<TreatmentCardTransferDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取疗程卡转让记录详情
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardTransferDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardTransferDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto<TreatmentCardTransferDto?>.Fail("疗程卡转让记录不存在", 404);
        return ApiResponseDto<TreatmentCardTransferDto?>.Ok(entity.Adapt<TreatmentCardTransferDto>());
    }

    /// <summary>
    /// 创建疗程卡转让记录
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
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("疗程卡销售记录不存在", 404);
        if (cardSale.CustomerId != dto.FromCustomerId)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("原客户与卡实际归属不符，请刷新后重试", 400);
        if (cardSale.Status != 1)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("疗程卡已用完或已过期，不可转让", 400);

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

        // 阶段6：疗程卡转让审计日志（文档 6.1 节）
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
            Remark = $"疗程卡转让-{dto.Remark ?? string.Empty}",
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            StoreId = storeId,
            StoreCode = storeCode
        });

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardTransferDto>.Ok(entity.Adapt<TreatmentCardTransferDto>(), "创建成功");
    }

    /// <summary>
    /// 更新疗程卡转让记录
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
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("疗程卡转让记录不存在", 404);

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
    /// 删除疗程卡转让记录（物理删除，单据类无软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value && s.StoreId == (_currentUser.StoreId ?? 0));
        if (entity == null)
            return ApiResponseDto.Fail("疗程卡转让记录不存在", 404);

        _dbContext.TreatmentCardTransfers.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除疗程卡转让记录（物理删除，单据类无软删除）
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
}
