using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.PriceChangeLogs;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 价格变更记录应用服务实现（只读查询，价格变更日志由 ProductAppService/ProductMasterAppService 自动写入）
/// </summary>
public class PriceChangeLogAppService : IPriceChangeLogAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public PriceChangeLogAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取价格变更记录分页列表
    /// 查询时 Include Product.Master 导航属性，内存中填充显示字段
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>> GetPagedListAsync(PriceChangeLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.PriceChangeLogs
            .Include(p => p.Product).ThenInclude(p => p.Master!)
            .Where(p => p.TenantId == tenantId);

        if (query.ProductId.HasValue)
            queryable = queryable.Where(p => p.ProductId == query.ProductId.Value);
        if (!string.IsNullOrWhiteSpace(query.ProductName))
            queryable = queryable.Where(p => p.Product != null && p.Product.Master != null && p.Product.Master.Name.Contains(query.ProductName));
        if (query.StartDate.HasValue)
            queryable = queryable.Where(p => p.ChangeTime >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            // EndDate 含当日：过滤条件为 ChangeTime <= EndDate 当天 23:59:59
            queryable = queryable.Where(p => p.ChangeTime <= query.EndDate.Value.Date.AddDays(1).AddTicks(-1));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.ChangeTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtoList = items.Select(p => new PriceChangeLogDto
        {
            Id = p.Id,
            ProductId = p.ProductId,
            OldPrice = p.OldPrice,
            NewPrice = p.NewPrice,
            ChangeTime = p.ChangeTime,
            OperatorId = p.OperatorId,
            OperatorName = p.OperatorName,
            Remark = p.Remark,
            CreatedAt = p.CreatedTime,
            UpdatedAt = p.UpdatedTime,
            ProductName = p.Product?.Master?.Name,
            ProductCode = p.Product?.Master?.Code
        }).ToList();

        var result = new PagedResponseDto<PriceChangeLogDto>
        {
            List = dtoList,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<PriceChangeLogDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取价格变更记录详情
    /// </summary>
    public async Task<ApiResponseDto<PriceChangeLogDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PriceChangeLogDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.PriceChangeLogs
            .Include(p => p.Product).ThenInclude(p => p.Master!)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<PriceChangeLogDto?>.Fail("价格变更记录不存在", 404);

        var dto = new PriceChangeLogDto
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            OldPrice = entity.OldPrice,
            NewPrice = entity.NewPrice,
            ChangeTime = entity.ChangeTime,
            OperatorId = entity.OperatorId,
            OperatorName = entity.OperatorName,
            Remark = entity.Remark,
            CreatedAt = entity.CreatedTime,
            UpdatedAt = entity.UpdatedTime,
            ProductName = entity.Product?.Master?.Name,
            ProductCode = entity.Product?.Master?.Code
        };
        return ApiResponseDto<PriceChangeLogDto?>.Ok(dto);
    }
}
