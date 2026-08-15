using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Activities;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 活动管理应用服务实现
/// </summary>
public class ActivityAppService : IActivityAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ActivityCreateDto> _createValidator;
    private readonly IValidator<ActivityUpdateDto> _updateValidator;

    public ActivityAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ActivityCreateDto> createValidator,
        IValidator<ActivityUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取活动分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ActivityDto>>> GetPagedListAsync(ActivityQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<PagedResponseDto<ActivityDto>>.Fail("无法确定当前门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var queryable = _dbContext.Activities
            .Where(a => !a.IsDeleted && a.TenantId == tenantId && a.StoreId == storeId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(a => a.Name.Contains(query.Name));
        // 活动时间筛选采用区间重叠语义：活动周期与查询区间只要有任意一天交集即命中，
        // 避免跨越查询区间边界的长期活动被漏掉
        if (query.StartDate.HasValue)
        {
            var rangeStart = query.StartDate.Value.Date;
            queryable = queryable.Where(a => a.EndTime >= rangeStart);
        }
        if (query.EndDate.HasValue)
        {
            // 查询结束日只精确到天，需覆盖当天全部时刻，故用次日零点做右开区间
            var rangeEndExclusive = query.EndDate.Value.Date.AddDays(1);
            queryable = queryable.Where(a => a.StartTime < rangeEndExclusive);
        }

        // 活动状态筛选：与前端 calcStatus 保持同一套规则（now 与 StartTime/EndTime 比较）
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var now = DateTime.Now;
            queryable = query.Status switch
            {
                "notStarted" => queryable.Where(a => a.StartTime > now),
                "ongoing" => queryable.Where(a => a.StartTime <= now && a.EndTime >= now),
                "ended" => queryable.Where(a => a.EndTime < now),
                _ => queryable
            };
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(a => a.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = items.Adapt<List<ActivityDto>>();
        var result = new PagedResponseDto<ActivityDto>
        {
            List = dtos,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ActivityDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取活动详情
    /// </summary>
    public async Task<ApiResponseDto<ActivityDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<ActivityDto?>.Fail("无法确定当前门店", 401);

        var entity = await _dbContext.Activities
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted
                && a.TenantId == _currentUser.TenantId.Value
                && a.StoreId == _currentUser.StoreId.Value);
        if (entity == null)
            return ApiResponseDto<ActivityDto?>.Fail("活动不存在", 404);

        return ApiResponseDto<ActivityDto?>.Ok(entity.Adapt<ActivityDto>());
    }

    /// <summary>
    /// 创建活动
    /// </summary>
    public async Task<ApiResponseDto<ActivityDto>> CreateAsync(ActivityCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<ActivityDto>.Fail("无法确定当前门店", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ActivityDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var entity = dto.Adapt<Activity>();
        // 活动按整天计：开始时间为当天零点，结束时间为当天最后一刻（包含当天，避免当天即被判定为已结束）
        entity.StartTime = dto.StartTime.Date;
        entity.EndTime = dto.EndTime.Date.AddDays(1).AddMilliseconds(-1);
        entity.TenantId = _currentUser.TenantId.Value;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = _currentUser.StoreId.Value;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.Activities.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ActivityDto>.Ok(entity.Adapt<ActivityDto>(), "创建成功");
    }

    /// <summary>
    /// 更新活动
    /// </summary>
    public async Task<ApiResponseDto<ActivityDto>> UpdateAsync(ActivityUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<ActivityDto>.Fail("无法确定当前门店", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ActivityDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.Activities
            .FirstOrDefaultAsync(a => a.Id == dto.Id && !a.IsDeleted
                && a.TenantId == tenantId && a.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<ActivityDto>.Fail("活动不存在", 404);

        entity.Name = dto.Name;
        // 活动按整天计：开始时间为当天零点，结束时间为当天最后一刻（包含当天，避免当天即被判定为已结束）
        entity.StartTime = dto.StartTime.Date;
        entity.EndTime = dto.EndTime.Date.AddDays(1).AddMilliseconds(-1);
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<ActivityDto>.Ok(entity.Adapt<ActivityDto>(), "更新成功");
    }

    /// <summary>
    /// 删除活动（软删除）
    /// 活动被样品/赠品领用、订单内赠品通过 ActivityId 关联，物理删除会导致历史记录悬空，
    /// 故采用软删除：列表不再展示，但历史关联记录仍可显示活动信息。
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto.Fail("无法确定当前门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var entity = await _dbContext.Activities
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted
                && a.TenantId == tenantId
                && a.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto.Fail("活动不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 获取进行中活动下拉选项
    /// 过滤条件：未删除且 StartTime <= now <= EndTime，按 StartTime 倒序
    /// </summary>
    public async Task<ApiResponseDto<List<ActivityOptionDto>>> GetOptionsAsync()
    {
        if (!_currentUser.TenantId.HasValue || !_currentUser.StoreId.HasValue)
            return ApiResponseDto<List<ActivityOptionDto>>.Fail("无法确定当前门店", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId.Value;
        var now = DateTime.Now;

        var options = await _dbContext.Activities
            .Where(a => !a.IsDeleted && a.TenantId == tenantId && a.StoreId == storeId
                && a.StartTime <= now && a.EndTime >= now)
            .OrderByDescending(a => a.StartTime)
            .Select(a => new ActivityOptionDto
            {
                Id = a.Id,
                Name = a.Name,
                StartTime = a.StartTime,
                EndTime = a.EndTime
            })
            .ToListAsync();

        return ApiResponseDto<List<ActivityOptionDto>>.Ok(options);
    }
}
