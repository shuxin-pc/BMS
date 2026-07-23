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
            return ApiResponseDto<PagedResponseDto<BodyDataRecordDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.BodyDataRecords
            .Where(p => p.TenantId == tenantId);

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(p => p.CustomerId == query.CustomerId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(p => p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<BodyDataRecordDto>
        {
            List = items.Adapt<List<BodyDataRecordDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<BodyDataRecordDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<BodyDataRecordDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataRecordDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.BodyDataRecords
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<BodyDataRecordDto?>.Fail("身体数据记录不存在", 404);
        return ApiResponseDto<BodyDataRecordDto?>.Ok(entity.Adapt<BodyDataRecordDto>());
    }

    public async Task<ApiResponseDto<BodyDataRecordDto>> CreateAsync(BodyDataRecordCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataRecordDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<BodyDataRecordDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<BodyDataRecordEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.BodyDataRecords.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<BodyDataRecordDto>.Ok(entity.Adapt<BodyDataRecordDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<BodyDataRecordDto>> UpdateAsync(BodyDataRecordUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<BodyDataRecordDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<BodyDataRecordDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.BodyDataRecords
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
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
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.BodyDataRecords
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("身体数据记录不存在", 404);

        _dbContext.BodyDataRecords.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.BodyDataRecords
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
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
            return ApiResponseDto<BodyDataRecordDto?>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.BodyDataRecords
            .Where(p => p.CustomerId == customerId && p.TenantId == tenantId)
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
            return ApiResponseDto<BodyDataTrendDto>.Fail("无法确定当前租户", 401);

        if (startDate > endDate)
            return ApiResponseDto<BodyDataTrendDto>.Fail("起始日期不能晚于结束日期", 400);

        var tenantId = _currentUser.TenantId.Value;
        var records = await _dbContext.BodyDataRecords
            .Where(r => r.CustomerId == customerId && r.TenantId == tenantId
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
            return ApiResponseDto<BodyDataComparisonDto>.Fail("无法确定当前租户", 401);

        if (startDate > endDate)
            return ApiResponseDto<BodyDataComparisonDto>.Fail("起始日期不能晚于结束日期", 400);

        var tenantId = _currentUser.TenantId.Value;

        var startRecord = await _dbContext.BodyDataRecords
            .Where(r => r.CustomerId == customerId && r.TenantId == tenantId && r.RecordDate <= startDate)
            .OrderByDescending(r => r.RecordDate)
            .FirstOrDefaultAsync();

        var endRecord = await _dbContext.BodyDataRecords
            .Where(r => r.CustomerId == customerId && r.TenantId == tenantId && r.RecordDate <= endDate)
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
