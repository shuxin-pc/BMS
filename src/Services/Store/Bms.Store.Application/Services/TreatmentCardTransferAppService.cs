using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.TreatmentCardTransfers;
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

    public TreatmentCardTransferAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TreatmentCardTransferCreateDto> createValidator,
        IValidator<TreatmentCardTransferUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取疗程卡转让记录分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>> GetPagedListAsync(TreatmentCardTransferQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TreatmentCardTransferDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.TreatmentCardTransfers
            .Where(s => s.TenantId == tenantId);

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
            return ApiResponseDto<TreatmentCardTransferDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
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
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        if (dto.FromCustomerId == dto.ToCustomerId)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("不能转让给原客户本人", 400);

        var tenantId = _currentUser.TenantId.Value;

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

        var entity = dto.Adapt<TreatmentCardTransferEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;
        entity.Status = 1; // 创建即完成转让

        _dbContext.TreatmentCardTransfers.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardTransferDto>.Ok(entity.Adapt<TreatmentCardTransferDto>(), "创建成功");
    }

    /// <summary>
    /// 更新疗程卡转让记录
    /// </summary>
    public async Task<ApiResponseDto<TreatmentCardTransferDto>> UpdateAsync(TreatmentCardTransferUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<TreatmentCardTransferDto>.Fail("疗程卡转让记录不存在", 404);

        entity.StoreId = dto.StoreId;
        entity.StoreCode = dto.StoreCode;
        entity.CardSaleId = dto.CardSaleId;
        entity.FromCustomerId = dto.FromCustomerId;
        entity.ToCustomerId = dto.ToCustomerId;
        entity.TransferDate = dto.TransferDate;
        entity.TransferFee = dto.TransferFee;
        entity.OperatorId = dto.OperatorId;
        entity.Status = dto.Status;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TreatmentCardTransferDto>.Ok(entity.Adapt<TreatmentCardTransferDto>(), "更新成功");
    }

    /// <summary>
    /// 删除疗程卡转让记录
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.TreatmentCardTransfers
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("疗程卡转让记录不存在", 404);

        _dbContext.TreatmentCardTransfers.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除疗程卡转让记录
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.TreatmentCardTransfers
            .Where(s => ids.Contains(s.Id) && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.TreatmentCardTransfers.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }
}
