using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Entities;
using CustomerEntity = Bms.Store.Domain.Entities.Customer;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户档案应用服务实现
/// </summary>
public class CustomerAppService : ICustomerAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CustomerCreateDto> _createValidator;
    private readonly IValidator<CustomerUpdateDto> _updateValidator;

    public CustomerAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<CustomerCreateDto> createValidator,
        IValidator<CustomerUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取客户分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<CustomerDto>>> GetPagedListAsync(CustomerQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<CustomerDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Customers
            .Where(c => !c.IsDeleted && c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(c => c.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Phone))
            queryable = queryable.Where(c => c.Phone == query.Phone);
        if (query.LevelId.HasValue)
            queryable = queryable.Where(c => c.LevelId == query.LevelId.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(c => c.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<CustomerDto>
        {
            List = items.Adapt<List<CustomerDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<CustomerDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取客户详情
    /// </summary>
    public async Task<ApiResponseDto<CustomerDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerDto?>.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto<CustomerDto?>.Fail("客户不存在", 404);
        return ApiResponseDto<CustomerDto?>.Ok(entity.Adapt<CustomerDto>());
    }

    /// <summary>
    /// 创建客户
    /// </summary>
    public async Task<ApiResponseDto<CustomerDto>> CreateAsync(CustomerCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerDto>.Fail("无法确定当前租户", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var phoneExists = await _dbContext.Customers
            .AnyAsync(c => c.Phone == dto.Phone && c.TenantId == tenantId && !c.IsDeleted);
        if (phoneExists)
            return ApiResponseDto<CustomerDto>.Fail($"手机号 {dto.Phone} 已存在", 400);

        var entity = dto.Adapt<CustomerEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.Customers.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerDto>.Ok(entity.Adapt<CustomerDto>(), "创建成功");
    }

    /// <summary>
    /// 更新客户
    /// </summary>
    public async Task<ApiResponseDto<CustomerDto>> UpdateAsync(CustomerUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerDto>.Fail("无法确定当前租户", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<CustomerDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted && c.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<CustomerDto>.Fail("客户不存在", 404);

        if (entity.Phone != dto.Phone)
        {
            var phoneExists = await _dbContext.Customers
                .AnyAsync(c => c.Phone == dto.Phone && c.TenantId == tenantId && !c.IsDeleted && c.Id != dto.Id);
            if (phoneExists)
                return ApiResponseDto<CustomerDto>.Fail($"手机号 {dto.Phone} 已存在", 400);
        }

        entity.Name = dto.Name;
        entity.Phone = dto.Phone;
        entity.Gender = dto.Gender;
        entity.Birthday = dto.Birthday;
        entity.LevelId = dto.LevelId;
        entity.Address = dto.Address;
        entity.Tags = dto.Tags;
        entity.AuthorizationStatus = dto.AuthorizationStatus;
        entity.AuthorizationTime = dto.AuthorizationTime;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<CustomerDto>.Ok(entity.Adapt<CustomerDto>(), "更新成功");
    }

    /// <summary>
    /// 删除客户（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var entity = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("客户不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除客户（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.Customers
            .Where(c => ids.Contains(c.Id) && !c.IsDeleted && c.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 获取客户消费统计（消费频次、客单价、消费偏好）
    /// 偏好维度：按订单类型、商品类型、商品分类、时段、技师分组
    /// 仅统计近 6 个月已完成订单，避免历史数据干扰
    /// </summary>
    public async Task<ApiResponseDto<CustomerConsumptionStatDto>> GetConsumptionStatAsync(long customerId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<CustomerConsumptionStatDto>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted && c.TenantId == tenantId);
        if (customer == null)
            return ApiResponseDto<CustomerConsumptionStatDto>.Fail("客户不存在", 404);

        // 仅统计近 6 个月已完成订单（Status=2 已完成，排除退款/取消）
        var sixMonthsAgo = DateTime.Now.AddMonths(-6);
        var orders = await _dbContext.Orders
            .Where(o => o.CustomerId == customerId && o.TenantId == tenantId && o.Status == 2 && o.OrderTime >= sixMonthsAgo)
            .ToListAsync();

        var orderCount = orders.Count;
        var totalConsumption = orders.Sum(o => o.PaidAmount);
        var averageOrderValue = orderCount > 0 ? totalConsumption / orderCount : 0m;
        var lastConsumeTime = orders.Max(o => (DateTime?)o.OrderTime);

        // 按订单类型分组统计消费偏好（保留现有逻辑，兼容前端契约）
        // OrderType 映射：1:零售->实物商品 2:服务->服务项目 3:疗程卡核销->疗程卡
        var typeNames = new Dictionary<int, (int ProductType, string Name)>
        {
            { 1, (1, "实物商品") },
            { 2, (2, "服务项目") },
            { 3, (4, "疗程卡") }
        };

        var preferences = orders
            .GroupBy(o => o.OrderType)
            .Select(g => new ConsumptionPreferenceItem
            {
                ProductType = typeNames.TryGetValue(g.Key, out var mapping) ? mapping.ProductType : 0,
                ProductTypeName = typeNames.TryGetValue(g.Key, out var m) ? m.Name : $"类型{g.Key}",
                Amount = g.Sum(o => o.PaidAmount),
                Percentage = totalConsumption > 0 ? g.Sum(o => o.PaidAmount) / totalConsumption : 0m
            })
            .OrderByDescending(p => p.Amount)
            .ToList();

        // 联表查询订单明细 + 商品 + 分类，用于多维度偏好统计
        // 技师信息单独查询后内存关联（OrderItem 无 Technician 导航属性）
        var orderIds = orders.Select(o => o.Id).ToList();
        var orderItems = await _dbContext.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .Include(oi => oi.Product).ThenInclude(p => p != null ? p.Category : null)
            .Include(oi => oi.Order)
            .ToListAsync();

        // 查询相关技师（去重）
        var technicianIds = orderItems
            .Where(oi => oi.TechnicianId.HasValue)
            .Select(oi => oi.TechnicianId!.Value)
            .Distinct()
            .ToList();
        var technicians = technicianIds.Count > 0
            ? await _dbContext.Technicians
                .Where(t => technicianIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name)
            : new Dictionary<long, string>();

        // 构造内存中间结构：每条明细一行
        var itemRows = orderItems.Select(oi => new
        {
            OrderTime = oi.Order?.OrderTime ?? DateTime.MinValue,
            Amount = oi.DiscountedAmount,
            ProductTypeId = oi.Product?.Type ?? 0,
            CategoryName = oi.Product?.Category?.Name ?? string.Empty,
            TechnicianName = oi.TechnicianId.HasValue && technicians.TryGetValue(oi.TechnicianId.Value, out var tName) ? tName : string.Empty
        }).ToList();

        // 按订单数计算占比的基数
        var totalItemCount = itemRows.Count;
        decimal itemPercentage(int count) => totalItemCount > 0 ? (decimal)count / totalItemCount : 0m;

        // 按商品类型分组（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品）
        var productTypeNames = new Dictionary<int, string>
        {
            { 1, "实物商品" }, { 2, "服务商品" }, { 3, "耗材" }, { 4, "样品" }, { 5, "赠品" }
        };
        var preferencesByProductType = itemRows
            .GroupBy(x => x.ProductTypeId)
            .Select(g => new ConsumePreferenceItem
            {
                Key = productTypeNames.TryGetValue(g.Key, out var n) ? n : $"类型{g.Key}",
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                Percentage = itemPercentage(g.Count())
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // 按商品分类分组
        var preferencesByCategory = itemRows
            .Where(x => !string.IsNullOrEmpty(x.CategoryName))
            .GroupBy(x => x.CategoryName)
            .Select(g => new ConsumePreferenceItem
            {
                Key = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                Percentage = itemPercentage(g.Count())
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // 按时段分组（上午06-12, 下午12-18, 晚上18-24, 凌晨00-06）
        string GetTimeSlot(DateTime time)
        {
            var hour = time.Hour;
            if (hour >= 6 && hour < 12) return "上午";
            if (hour >= 12 && hour < 18) return "下午";
            if (hour >= 18) return "晚上";
            return "凌晨";
        }
        var preferencesByTimeSlot = itemRows
            .Where(x => x.OrderTime != DateTime.MinValue)
            .GroupBy(x => GetTimeSlot(x.OrderTime))
            .Select(g => new ConsumePreferenceItem
            {
                Key = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                Percentage = itemPercentage(g.Count())
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        // 按技师分组
        var preferencesByTechnician = itemRows
            .Where(x => !string.IsNullOrEmpty(x.TechnicianName))
            .GroupBy(x => x.TechnicianName)
            .Select(g => new ConsumePreferenceItem
            {
                Key = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                Percentage = itemPercentage(g.Count())
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        return ApiResponseDto<CustomerConsumptionStatDto>.Ok(new CustomerConsumptionStatDto
        {
            CustomerId = customerId,
            OrderCount = orderCount,
            TotalConsumption = totalConsumption,
            AverageOrderValue = Math.Round(averageOrderValue, 2),
            LastConsumeTime = lastConsumeTime,
            Preferences = preferences,
            PreferencesByProductType = preferencesByProductType,
            PreferencesByCategory = preferencesByCategory,
            PreferencesByTimeSlot = preferencesByTimeSlot,
            PreferencesByTechnician = preferencesByTechnician
        });
    }

    /// <summary>
    /// 永久删除客户档案（物理删除）
    /// 物理删除客户档案及关联的个人信息（美容档案、身体数据、对比照片、偏好、积分流水、消费记录、储值账户、疗程卡记录）
    /// 订单业务数据脱敏保留（CustomerId 置空，断开与客户的关联）
    /// 前置条件：无未完成订单、无未核销疗程卡、无储值余额
    /// 审计日志永久保留，满足《个人信息保护法》第 47 条合规要求
    /// </summary>
    public async Task<ApiResponseDto> PermanentlyDeleteAsync(long id, CustomerPermanentDeleteDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto.Fail("客户不存在", 404);

        // 二次确认码校验：手机号后4位
        if (string.IsNullOrEmpty(entity.Phone) || entity.Phone.Length < 4)
            return ApiResponseDto.Fail("客户手机号无效，无法进行二次确认", 400);
        var phoneLast4 = entity.Phone.Length >= 4 ? entity.Phone[^4..] : entity.Phone;
        if (!string.Equals(phoneLast4, dto.ConfirmCode, StringComparison.Ordinal))
            return ApiResponseDto.Fail("二次确认码不匹配（应为客户手机号后4位）", 400);

        // 前置条件1：无未完成订单（Status=1 进行中）
        var hasPendingOrders = await _dbContext.Orders
            .AnyAsync(o => o.CustomerId == id && o.TenantId == tenantId && o.Status == 1);
        if (hasPendingOrders)
            return ApiResponseDto.Fail("客户存在未完成订单，无法永久删除", 400);

        // 前置条件2：无未核销疗程卡（Status=1 有效 且 RemainingTimes > 0）
        var hasActiveTreatmentCards = await _dbContext.TreatmentCardSales
            .AnyAsync(t => t.CustomerId == id && t.TenantId == tenantId && t.Status == 1 && t.RemainingTimes > 0);
        if (hasActiveTreatmentCards)
            return ApiResponseDto.Fail("客户存在未核销的疗程卡，无法永久删除", 400);

        // 前置条件3：无储值余额
        var storedValueAccount = await _dbContext.StoredValueAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == id && a.TenantId == tenantId);
        if (storedValueAccount != null && storedValueAccount.Balance > 0)
            return ApiResponseDto.Fail("客户存在储值余额，无法永久删除", 400);

        // 脱敏订单：断开与客户的关联（订单业务数据保留，但 CustomerId 置空）
        // Order.CustomerId 是可空 long?，置空后订单依然存在但无法关联到具体客户
        var orders = await _dbContext.Orders
            .Where(o => o.CustomerId == id && o.TenantId == tenantId)
            .ToListAsync();
        foreach (var order in orders)
        {
            order.CustomerId = null;
            order.UpdatedTime = DateTime.Now;
        }

        // 物理删除客户关联的个人信息
        // 包含方案要求的三个表 + 其他以 CustomerId 为外键的个人敏感信息表
        // 这些表 CustomerId 均为非空外键且 OnDelete(Restrict)，必须物理删除才能删除 Customer
        var beautyProfiles = await _dbContext.CustomerBeautyProfiles
            .Where(p => p.CustomerId == id && p.TenantId == tenantId).ToListAsync();
        var bodyDataRecords = await _dbContext.BodyDataRecords
            .Where(r => r.CustomerId == id && r.TenantId == tenantId).ToListAsync();
        var comparisonPhotos = await _dbContext.ServiceComparisonPhotos
            .Where(p => p.CustomerId == id && p.TenantId == tenantId).ToListAsync();
        var preferences = await _dbContext.CustomerPreferences
            .Where(p => p.CustomerId == id && p.TenantId == tenantId).ToListAsync();
        var pointsLogs = await _dbContext.CustomerPointsLogs
            .Where(l => l.CustomerId == id && l.TenantId == tenantId).ToListAsync();
        var consumeLogs = await _dbContext.ConsumeLogs
            .Where(l => l.CustomerId == id && l.TenantId == tenantId).ToListAsync();
        var treatmentCardSales = await _dbContext.TreatmentCardSales
            .Where(t => t.CustomerId == id && t.TenantId == tenantId).ToListAsync();

        if (beautyProfiles.Count > 0) _dbContext.CustomerBeautyProfiles.RemoveRange(beautyProfiles);
        if (bodyDataRecords.Count > 0) _dbContext.BodyDataRecords.RemoveRange(bodyDataRecords);
        if (comparisonPhotos.Count > 0) _dbContext.ServiceComparisonPhotos.RemoveRange(comparisonPhotos);
        if (preferences.Count > 0) _dbContext.CustomerPreferences.RemoveRange(preferences);
        if (pointsLogs.Count > 0) _dbContext.CustomerPointsLogs.RemoveRange(pointsLogs);
        if (consumeLogs.Count > 0) _dbContext.ConsumeLogs.RemoveRange(consumeLogs);
        if (treatmentCardSales.Count > 0) _dbContext.TreatmentCardSales.RemoveRange(treatmentCardSales);
        if (storedValueAccount != null) _dbContext.StoredValueAccounts.Remove(storedValueAccount);

        // 写入删除审计日志（永久保留，不物理删除）
        var deleteLog = new CustomerDeleteLog
        {
            OriginalCustomerId = id,
            OriginalName = entity.Name,
            OriginalPhone = entity.Phone,
            OperatorId = _currentUser.UserId,
            DeleteTime = DateTime.Now,
            Reason = dto.Reason,
            TenantId = tenantId,
            TenantCode = entity.TenantCode,
            CreatedTime = DateTime.Now
        };
        _dbContext.CustomerDeleteLogs.Add(deleteLog);

        // 物理删除客户档案
        _dbContext.Customers.Remove(entity);

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "客户档案已永久删除");
    }
}
