using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Constants;
using CustomerPointsLogEntity = Bms.Store.Domain.Entities.CustomerPointsLog;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户积分流水应用服务实现
/// </summary>
public class CustomerPointsLogAppService : ICustomerPointsLogAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CustomerPointsLogCreateDto> _createValidator;
    private readonly IValidator<CustomerPointsLogUpdateDto> _updateValidator;

    public CustomerPointsLogAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CustomerPointsLogCreateDto> createValidator,
        IValidator<CustomerPointsLogUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取客户积分流水分页列表
    /// 关联 Customer 表填充客户姓名、手机号，并支持模糊查询；
    /// 关联 Order 表填充订单号（无订单时为 null）
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<CustomerPointsLogDto>>> GetPagedListAsync(CustomerPointsLogQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CustomerPointsLogDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 左连接 Customer（软删除客户的历史流水仍需展示）和 Order（OrderId 可空）
        var queryable = from log in _dbContext.CustomerPointsLogs
                        where log.TenantId == tenantId
                        join customer in _dbContext.Customers on log.CustomerId equals customer.Id
                        join order in _dbContext.Orders on log.OrderId equals order.Id into orders
                        from order in orders.DefaultIfEmpty()
                        select new { log, customer, order };

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.log.CustomerId == query.CustomerId.Value);
        if (query.Type.HasValue)
            queryable = queryable.Where(x => x.log.Type == query.Type.Value);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(x => x.customer.Name.Contains(query.Keyword) || x.customer.Phone.Contains(query.Keyword));

        var total = await queryable.CountAsync();
        // 排序说明：CreatedTime 降序保证最新在前（充值等最早的操作位于列表底部，从下往上看即业务时间正序）；
        // 同一订单结算产生的多条积分流水（如先积分抵扣后消费获得）使用同一个 CreatedTime（同一 now），
        // 仅按时间排序键不唯一会导致同订单流水展示顺序随机，从下往上看时出现"上一条变动后积分 ≠ 下一条变动前积分"的余额断链。
        // 雪花 Id 按创建顺序递增（与业务写入顺序一致），故用 Id 降序作为稳定二级键：
        // 同一时刻内后写入的（如消费获得）排在上方，从下往上看时同订单流水恰好按业务发生顺序（充值→抵扣→获得）衔接。
        var items = await queryable
            .OrderByDescending(x => x.log.CreatedTime)
            .ThenByDescending(x => x.log.Id)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new CustomerPointsLogDto
            {
                Id = x.log.Id,
                CustomerId = x.log.CustomerId,
                CustomerName = x.customer.Name,
                Phone = x.customer.Phone,
                Type = x.log.Type,
                Points = x.log.Points,
                BeforePoints = x.log.BeforePoints,
                AfterPoints = x.log.AfterPoints,
                OrderId = x.log.OrderId,
                OrderNo = x.order != null ? x.order.OrderNo : null,
                OperatorId = x.log.OperatorId,
                ExpireDate = x.log.ExpireDate,
                Remark = x.log.Remark,
                ChangeTime = x.log.CreatedTime,
                UpdatedAt = x.log.UpdatedTime
            })
            .ToListAsync();

        var result = new PagedResponseDto<CustomerPointsLogDto>
        {
            List = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CustomerPointsLogDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取客户积分流水详情
    /// </summary>
    public async Task<ApiResponseDto<CustomerPointsLogDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerPointsLogDto?>.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.CustomerPointsLogs
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<CustomerPointsLogDto?>.Fail("积分流水不存在", 404);
        return ApiResponseDto<CustomerPointsLogDto?>.Ok(entity.Adapt<CustomerPointsLogDto>());
    }

    /// <summary>
    /// 创建客户积分流水
    /// </summary>
    public async Task<ApiResponseDto<CustomerPointsLogDto>> CreateAsync(CustomerPointsLogCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerPointsLogDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerPointsLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        // 安全防护：通用创建入口仅允许手动调整类型，其他类型必须由各自业务流程写入
        // 防止前端伪造 Type=1（消费获得）等积分流水，绕过订单流程
        if (dto.Type != CustomerPointsLogType.ManualAdjust)
            return ApiResponseDto<CustomerPointsLogDto>.Fail(
                $"不支持手动创建类型 {dto.Type} 的积分流水，请通过对应业务流程操作", 400);

        var tenantId = _currentUser.TenantId.Value;

        // 查询客户当前积分，校验积分永不为负
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.TenantId == tenantId);
        if (customer == null)
            return ApiResponseDto<CustomerPointsLogDto>.Fail("客户不存在", 404);

        // 扣减类操作校验积分不为负
        if (dto.Points < 0 && customer.TotalPoints + dto.Points < 0)
            return ApiResponseDto<CustomerPointsLogDto>.Fail("客户积分不足，无法扣减", 400);

        var entity = dto.Adapt<CustomerPointsLogEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;
        entity.BeforePoints = customer.TotalPoints;
        entity.AfterPoints = customer.TotalPoints + dto.Points;

        // P-PTS-05: 强制填充操作人（过期清零 Type=7 由后台任务直接写 DbContext，不经过本入口）
        // 此处所有手动创建的积分流水都强制使用当前登录用户作为操作人，避免前端伪造
        entity.OperatorId = _currentUser.UserId;

        // 同步更新客户总积分
        customer.TotalPoints = entity.AfterPoints;
        customer.UpdatedTime = entity.CreatedTime;

        _dbContext.CustomerPointsLogs.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerPointsLogDto>.Ok(entity.Adapt<CustomerPointsLogDto>(), "创建成功");
    }

    /// <summary>
    /// 更新客户积分流水
    /// </summary>
    public async Task<ApiResponseDto<CustomerPointsLogDto>> UpdateAsync(CustomerPointsLogUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerPointsLogDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerPointsLogDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.CustomerPointsLogs
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<CustomerPointsLogDto>.Fail("积分流水不存在", 404);

        entity.CustomerId = dto.CustomerId;
        entity.Type = dto.Type;
        entity.Points = dto.Points;
        entity.BeforePoints = dto.BeforePoints;
        entity.AfterPoints = dto.AfterPoints;
        entity.OrderId = dto.OrderId;
        // OperatorId 保留创建时记录的操作人，更新不覆盖，避免丢失原始操作人
        entity.ExpireDate = dto.ExpireDate;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerPointsLogDto>.Ok(entity.Adapt<CustomerPointsLogDto>(), "更新成功");
    }

    /// <summary>
    /// 删除客户积分流水（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.CustomerPointsLogs
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("积分流水不存在", 404);

        _dbContext.CustomerPointsLogs.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除客户积分流水（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.CustomerPointsLogs
            .Where(p => ids.Contains(p.Id) && p.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.CustomerPointsLogs.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 查询客户即将过期的积分明细（P-PTS-04 第一阶段）
    /// 仅返回尚未清零（IsExpired=false）、Points>0、且在指定天数内将过期的发放类积分记录
    /// </summary>
    public async Task<ApiResponseDto<List<ExpiringPointsDto>>> GetExpiringPointsAsync(long customerId, int days = 7)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<ExpiringPointsDto>>.Fail("登录状态异常，请重新登录", 401);

        if (days <= 0)
            return ApiResponseDto<List<ExpiringPointsDto>>.Fail("查询天数必须大于0", 400);

        var tenantId = _currentUser.TenantId.Value;
        var now = DateTime.Now;
        var deadline = now.AddDays(days);

        var logs = await _dbContext.CustomerPointsLogs
            .Where(l => l.CustomerId == customerId
                && l.TenantId == tenantId
                && l.Points > 0
                && !l.IsExpired
                && l.ExpireDate.HasValue
                && l.ExpireDate <= deadline)
            .OrderBy(l => l.ExpireDate)
            .ToListAsync();

        var result = logs.Select(l => new ExpiringPointsDto
        {
            LogId = l.Id,
            Points = l.Points,
            ExpireDate = l.ExpireDate!.Value,
            DaysRemaining = (l.ExpireDate.Value - now).Days,
            Remark = l.Remark
        }).ToList();

        return ApiResponseDto<List<ExpiringPointsDto>>.Ok(result);
    }
}
