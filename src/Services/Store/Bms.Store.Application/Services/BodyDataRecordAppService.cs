using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using BodyDataRecordEntity = Bms.Store.Domain.Entities.BodyDataRecord;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

public class BodyDataRecordAppService : IBodyDataRecordAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<BodyDataRecordCreateDto> _createValidator;
    private readonly IValidator<BodyDataRecordUpdateDto> _updateValidator;

    public BodyDataRecordAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<BodyDataRecordCreateDto> createValidator,
        IValidator<BodyDataRecordUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<BodyDataRecordDto>>> GetPagedListAsync(BodyDataRecordQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<BodyDataRecordDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = from p in _dbContext.BodyDataRecords
                        join c in _dbContext.Customers on p.CustomerId equals c.Id
                        where p.TenantId == tenantId && p.StoreId == storeId && !c.IsDeleted
                        select new { p, c };

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.p.CustomerId == query.CustomerId.Value);
        // 客户名称/手机号合并关键字查询：命中姓名或手机号其一即满足（对齐预约列表）
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(x => x.c.Name.Contains(query.Keyword) || x.c.Phone.Contains(query.Keyword));
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.p.RecordDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(x => x.p.RecordDate <= query.EndDate.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.p.RecordDate)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new BodyDataRecordDto
            {
                Id = x.p.Id,
                CustomerId = x.p.CustomerId,
                CustomerName = x.c.Name,
                CustomerPhone = x.c.Phone,
                RecordDate = x.p.RecordDate,
                Weight = x.p.Weight,
                BodyFat = x.p.BodyFat,
                Bust = x.p.Bust,
                Waist = x.p.Waist,
                Hip = x.p.Hip,
                Remark = x.p.Remark,
                CreatedAt = x.p.CreatedTime,
                UpdatedAt = x.p.UpdatedTime
            })
            .ToListAsync();

        var result = new PagedResponseDto<BodyDataRecordDto>
        {
            List = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<BodyDataRecordDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<BodyDataRecordDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataRecordDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.BodyDataRecords
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<BodyDataRecordDto?>.Fail("身体数据记录不存在", 404);
        return ApiResponseDto<BodyDataRecordDto?>.Ok(entity.Adapt<BodyDataRecordDto>());
    }

    public async Task<ApiResponseDto<BodyDataRecordDto>> CreateAsync(BodyDataRecordCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataRecordDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<BodyDataRecordDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = dto.Adapt<BodyDataRecordEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.BodyDataRecords.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<BodyDataRecordDto>.Ok(entity.Adapt<BodyDataRecordDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<BodyDataRecordDto>> UpdateAsync(BodyDataRecordUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataRecordDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<BodyDataRecordDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.BodyDataRecords
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<BodyDataRecordDto>.Fail("身体数据记录不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.RecordDate = dto.RecordDate;
        entity.Weight = dto.Weight;
        entity.BodyFat = dto.BodyFat;
        entity.Bust = dto.Bust;
        entity.Waist = dto.Waist;
        entity.Hip = dto.Hip;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<BodyDataRecordDto>.Ok(entity.Adapt<BodyDataRecordDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.BodyDataRecords
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto.Fail("身体数据记录不存在", 404);

        _dbContext.BodyDataRecords.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entities = await _dbContext.BodyDataRecords
            .Where(p => ids.Contains(p.Id) && p.TenantId == tenantId && p.StoreId == storeId)
            .ToListAsync();

        _dbContext.BodyDataRecords.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 获取客户最近一次身体数据记录
    /// </summary>
    public async Task<ApiResponseDto<BodyDataRecordDto?>> GetLatestAsync(long customerId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataRecordDto?>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.BodyDataRecords
            .Where(p => p.CustomerId == customerId && p.TenantId == tenantId && p.StoreId == storeId)
            .OrderByDescending(p => p.RecordDate)
            .FirstOrDefaultAsync();

        if (entity == null)
            return ApiResponseDto<BodyDataRecordDto?>.Fail("未找到身体数据记录", 404);

        return ApiResponseDto<BodyDataRecordDto?>.Ok(entity.Adapt<BodyDataRecordDto>());
    }

    /// <summary>
    /// 按时间范围获取身体数据趋势
    /// 按 RecordDate 升序返回时间序列，并计算首末记录的变化量
    /// </summary>
    public async Task<ApiResponseDto<BodyDataTrendDto>> GetTrendAsync(long customerId, DateTime startDate, DateTime endDate)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataTrendDto>.Fail("登录状态异常，请重新登录", 401);

        if (startDate > endDate)
            return ApiResponseDto<BodyDataTrendDto>.Fail("起始日期不能晚于结束日期", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var records = await _dbContext.BodyDataRecords
            .Where(r => r.CustomerId == customerId && r.TenantId == tenantId && r.StoreId == storeId
                && r.RecordDate >= startDate && r.RecordDate <= endDate)
            .OrderBy(r => r.RecordDate)
            .ToListAsync();

        var first = records.FirstOrDefault();
        var last = records.LastOrDefault();

        var trend = new BodyDataTrendDto
        {
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            Dates = records.Select(r => r.RecordDate).ToList(),
            WeightSeries = records.Select(r => r.Weight).ToList(),
            BodyFatSeries = records.Select(r => r.BodyFat).ToList(),
            BustSeries = records.Select(r => r.Bust).ToList(),
            WaistSeries = records.Select(r => r.Waist).ToList(),
            HipSeries = records.Select(r => r.Hip).ToList(),
            WeightChange = ComputeChange(first?.Weight, last?.Weight),
            BodyFatChange = ComputeChange(first?.BodyFat, last?.BodyFat),
            BustChange = ComputeChange(first?.Bust, last?.Bust),
            WaistChange = ComputeChange(first?.Waist, last?.Waist),
            HipChange = ComputeChange(first?.Hip, last?.Hip),
            DaysCount = (endDate.Date - startDate.Date).Days + 1,
            RecordsCount = records.Count
        };

        return ApiResponseDto<BodyDataTrendDto>.Ok(trend);
    }

    /// <summary>
    /// 对比两个时间点的身体数据差异
    /// 取起始日期当天或之前最近的一条作为起点，结束日期当天或之前最近的一条作为终点
    /// </summary>
    public async Task<ApiResponseDto<BodyDataComparisonDto>> GetComparisonAsync(long customerId, DateTime startDate, DateTime endDate)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataComparisonDto>.Fail("登录状态异常，请重新登录", 401);

        if (startDate > endDate)
            return ApiResponseDto<BodyDataComparisonDto>.Fail("起始日期不能晚于结束日期", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        var startRecord = await _dbContext.BodyDataRecords
            .Where(r => r.CustomerId == customerId && r.TenantId == tenantId && r.StoreId == storeId && r.RecordDate <= startDate)
            .OrderByDescending(r => r.RecordDate)
            .FirstOrDefaultAsync();

        var endRecord = await _dbContext.BodyDataRecords
            .Where(r => r.CustomerId == customerId && r.TenantId == tenantId && r.StoreId == storeId && r.RecordDate <= endDate)
            .OrderByDescending(r => r.RecordDate)
            .FirstOrDefaultAsync();

        if (startRecord == null && endRecord == null)
            return ApiResponseDto<BodyDataComparisonDto>.Fail("未找到身体数据记录", 404);

        var comparison = new BodyDataComparisonDto
        {
            CustomerId = customerId,
            StartRecord = startRecord?.Adapt<BodyDataRecordDto>(),
            EndRecord = endRecord?.Adapt<BodyDataRecordDto>(),
            WeightChange = ComputeChange(startRecord?.Weight, endRecord?.Weight),
            BodyFatChange = ComputeChange(startRecord?.BodyFat, endRecord?.BodyFat),
            BustChange = ComputeChange(startRecord?.Bust, endRecord?.Bust),
            WaistChange = ComputeChange(startRecord?.Waist, endRecord?.Waist),
            HipChange = ComputeChange(startRecord?.Hip, endRecord?.Hip),
            DaysBetween = startRecord != null && endRecord != null
                ? (endRecord.RecordDate.Date - startRecord.RecordDate.Date).Days
                : 0
        };

        return ApiResponseDto<BodyDataComparisonDto>.Ok(comparison);
    }

    /// <summary>
    /// 计算可空 decimal 的变化量（末值-首值）；任一为空则返回 null
    /// </summary>
    private static decimal? ComputeChange(decimal? start, decimal? end)
    {
        if (!start.HasValue || !end.HasValue)
            return null;
        return end.Value - start.Value;
    }
}
