using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Rooms;
using RoomEntity = Bms.Store.Domain.Entities.Room;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 房间床位应用服务实现
/// </summary>
public class RoomAppService : IRoomAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<RoomCreateDto> _createValidator;
    private readonly IValidator<RoomUpdateDto> _updateValidator;

    public RoomAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<RoomCreateDto> createValidator,
        IValidator<RoomUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ApiResponseDto<PagedResponseDto<RoomDto>>> GetPagedListAsync(RoomQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<RoomDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Rooms
            .Where(r => !r.IsDeleted && r.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(r => r.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Code))
            queryable = queryable.Where(r => r.Code.Contains(query.Code));
        if (query.RoomType.HasValue)
            queryable = queryable.Where(r => r.RoomType == query.RoomType.Value);
        if (query.Status.HasValue)
            queryable = queryable.Where(r => r.Status == query.Status.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(r => r.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<RoomDto>
        {
            List = items.Adapt<List<RoomDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<RoomDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<RoomDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<RoomDto?>.Fail("登录状态异常，请重新登录", 401);

        var room = await _dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.TenantId == _currentUser.TenantId.Value);
        if (room == null)
            return ApiResponseDto<RoomDto?>.Fail("房间不存在", 404);
        return ApiResponseDto<RoomDto?>.Ok(room.Adapt<RoomDto>());
    }

    public async Task<ApiResponseDto<RoomDto>> CreateAsync(RoomCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<RoomDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<RoomDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.Rooms
            .AnyAsync(r => r.Code == dto.Code && r.TenantId == tenantId && !r.IsDeleted);
        if (codeExists)
            return ApiResponseDto<RoomDto>.Fail($"编码 {dto.Code} 已存在", 400);

        var room = dto.Adapt<RoomEntity>();
        room.TenantId = tenantId;
        room.TenantCode = _currentUser.TenantCode ?? string.Empty;
        room.CreatedTime = DateTime.Now;

        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<RoomDto>.Ok(room.Adapt<RoomDto>(), "创建成功");
    }

    public async Task<ApiResponseDto<RoomDto>> UpdateAsync(RoomUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<RoomDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<RoomDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var room = await _dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted && r.TenantId == tenantId);
        if (room == null)
            return ApiResponseDto<RoomDto>.Fail("房间不存在", 404);

        if (room.Code != dto.Code)
        {
            var codeExists = await _dbContext.Rooms
                .AnyAsync(r => r.Code == dto.Code && r.TenantId == tenantId && !r.IsDeleted && r.Id != dto.Id);
            if (codeExists)
                return ApiResponseDto<RoomDto>.Fail($"编码 {dto.Code} 已存在", 400);
        }

        room.Name = dto.Name;
        room.Code = dto.Code;
        room.RoomType = dto.RoomType;
        room.Status = dto.Status;
        room.Location = dto.Location;
        room.Remark = dto.Remark;
        room.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<RoomDto>.Ok(room.Adapt<RoomDto>(), "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var room = await _dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted && r.TenantId == _currentUser.TenantId.Value);
        if (room == null)
            return ApiResponseDto.Fail("房间不存在", 404);

        room.IsDeleted = true;
        room.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var rooms = await _dbContext.Rooms
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted && r.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var room in rooms)
        {
            room.IsDeleted = true;
            room.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {rooms.Count} 条数据");
    }

    /// <summary>
    /// 根据服务项目查询可用房间/床位列表
    /// ① 查询服务项目 ServiceProduct.RequiredRoomType（null=不限制）
    /// ② 按 RequiredRoomType 过滤本租户启用房间
    /// ③ 排除指定时段已冲突的房间（预约与订单双向检测）
    /// </summary>
    public async Task<ApiResponseDto<List<RoomDto>>> GetAvailableByServiceProductAsync(
        long serviceProductId,
        DateTime startTime,
        DateTime endTime,
        long? excludeAppointmentId = null)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<RoomDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 查询服务项目的 RequiredRoomType
        // ServiceProduct 关联字段从 ProductId 改为 MasterId（设计文档 3.4 节）
        // 参数 serviceProductId 实际为门店商品档案 Product.Id，需通过 Product.MasterId 关联 ServiceProduct
        var serviceProduct = await (from sp in _dbContext.ServiceProducts
                                    join p in _dbContext.Products on sp.MasterId equals p.MasterId
                                    where p.Id == serviceProductId && sp.TenantId == tenantId
                                    select sp).FirstOrDefaultAsync();
        var requiredRoomType = serviceProduct?.RequiredRoomType;

        // 查询本租户启用房间（按 RequiredRoomType 过滤，null=不限制）
        var rooms = await _dbContext.Rooms
            .Where(r => !r.IsDeleted && r.TenantId == tenantId && r.Status == 1)
            .Where(r => requiredRoomType == null || r.RoomType == requiredRoomType)
            .ToListAsync();

        // 排除时段冲突的房间（预约表：状态非已完成/已取消，时段重叠）
        // 先按日期范围过滤（缩小查询集），再在内存中精确判断时段重叠
        var startDate = startTime.Date;
        var endDate = endTime.Date;
        var conflictCandidates = await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId
                && a.Status != 4 && a.Status != 5
                && a.RoomId.HasValue
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId.Value)
                && a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
            .Select(a => new { a.RoomId, a.AppointmentDate, a.AppointmentTime, a.EndTime })
            .ToListAsync();

        var conflictRoomIds = conflictCandidates
            .Where(a => a.EndTime.HasValue
                && a.AppointmentDate.Date.Add(a.AppointmentTime) < endTime
                && a.EndTime.Value > startTime)
            .Select(a => a.RoomId!.Value)
            .Distinct()
            .ToList();

        var available = rooms.Where(r => !conflictRoomIds.Contains(r.Id)).ToList();
        return ApiResponseDto<List<RoomDto>>.Ok(available.Adapt<List<RoomDto>>());
    }
}
