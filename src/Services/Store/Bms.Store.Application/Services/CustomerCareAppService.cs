using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户关怀应用服务实现
/// 生日提醒和消费感谢的待关怀/已关怀状态基于 CustomerCareLog 流水表实时计算
/// </summary>
public class CustomerCareAppService : ICustomerCareAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CustomerCareAppService(StoreDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取生日提醒分页列表
    /// 范围：未来 7 天（含今天）过生日的客户，按距生日天数升序
    /// 状态：LEFT JOIN 当年 Type=1 的关怀记录，有则"已关怀"，无则"待关怀"
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<BirthdayReminderDto>>> GetBirthdayRemindersAsync(BirthdayReminderQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<BirthdayReminderDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var today = DateTime.Today;
        var currentYear = today.Year;

        // 1. 查所有 Birthday 不为空 + 未删除 + 本租户本门店的客户
        var customers = await _dbContext.Customers
            .Where(c => c.Birthday.HasValue && !c.IsDeleted
                && c.TenantId == tenantId && c.StoreId == storeId)
            .Select(c => new { c.Id, c.Name, c.Phone, c.Birthday })
            .ToListAsync();

        // 2. 内存计算 daysToBirthday，过滤 [0, 7]
        var reminders = customers
            .Select(c =>
            {
                var birthday = c.Birthday!.Value;
                var days = CalcDaysToBirthday(birthday, today);
                return new BirthdayReminderDto
                {
                    Id = c.Id,
                    CustomerId = c.Id,
                    CustomerName = c.Name,
                    Phone = c.Phone,
                    Birthday = birthday.ToString("MM-dd"),
                    DaysToBirthday = days,
                    CareStatus = 1 // 默认待关怀
                };
            })
            .Where(r => r.DaysToBirthday >= 0 && r.DaysToBirthday <= 7)
            .ToList();

        // 3. 批量查当年 Type=1 的关怀记录
        var customerIds = reminders.Select(r => r.CustomerId).ToList();
        var careLogs = await _dbContext.CustomerCareLogs
            .Where(l => l.Type == CustomerCareLogTypes.BirthdayCare
                && l.CareYear == currentYear
                && l.TenantId == tenantId && l.StoreId == storeId
                && customerIds.Contains(l.CustomerId))
            .Select(l => new { l.CustomerId, l.CareTime, l.OperatorName })
            .ToListAsync();

        var careLogMap = careLogs.ToDictionary(l => l.CustomerId);
        foreach (var reminder in reminders)
        {
            if (careLogMap.TryGetValue(reminder.CustomerId, out var log))
            {
                reminder.CareStatus = 2;
                reminder.CareTime = log.CareTime;
                reminder.OperatorName = log.OperatorName;
            }
        }

        // 4. 按条件过滤
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            reminders = reminders
                .Where(r => (r.CustomerName?.Contains(query.Keyword) ?? false)
                    || (r.Phone?.Contains(query.Keyword) ?? false))
                .ToList();
        if (query.CareStatus.HasValue)
            reminders = reminders.Where(r => r.CareStatus == query.CareStatus.Value).ToList();

        // 5. 按 daysToBirthday 升序排序
        reminders = reminders.OrderBy(r => r.DaysToBirthday).ToList();

        // 6. 内存分页
        var total = reminders.Count;
        var paged = reminders
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new PagedResponseDto<BirthdayReminderDto>
        {
            List = paged,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<BirthdayReminderDto>>.Ok(result);
    }

    /// <summary>
    /// 标记生日关怀（幂等：同年同客户重复标记只更新时间）
    /// </summary>
    public async Task<ApiResponseDto> MarkBirthdayCaredAsync(long customerId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var currentYear = DateTime.Today.Year;
        var operatorName = string.IsNullOrWhiteSpace(_currentUser.RealName) ? _currentUser.UserName : _currentUser.RealName;

        // 校验客户存在且属于本租户本门店
        var customerExists = await _dbContext.Customers
            .AnyAsync(c => c.Id == customerId && !c.IsDeleted
                && c.TenantId == tenantId && c.StoreId == storeId);
        if (!customerExists)
            return ApiResponseDto.Fail("客户不存在", 404);

        var existing = await _dbContext.CustomerCareLogs
            .FirstOrDefaultAsync(l => l.Type == CustomerCareLogTypes.BirthdayCare
                && l.CustomerId == customerId
                && l.CareYear == currentYear
                && l.TenantId == tenantId && l.StoreId == storeId);

        if (existing != null)
        {
            existing.CareTime = DateTime.Now;
            existing.OperatorName = operatorName;
            existing.UpdatedTime = DateTime.Now;
            existing.UpdatedBy = _currentUser.UserId;
        }
        else
        {
            _dbContext.CustomerCareLogs.Add(new CustomerCareLog
            {
                CustomerId = customerId,
                Type = CustomerCareLogTypes.BirthdayCare,
                CareYear = currentYear,
                CareTime = DateTime.Now,
                OperatorName = operatorName,
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = _currentUser.StoreCode ?? string.Empty,
                CreatedTime = DateTime.Now,
                CreatedBy = _currentUser.UserId
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 并发场景下部分唯一索引冲突，记录已存在，忽略
        }
        return ApiResponseDto.Success(null, "标记成功");
    }

    /// <summary>
    /// 获取消费感谢分页列表
    /// 范围：近 7 天有已完成订单（Status=2）的客户，按客户聚合出累计消费金额与订单笔数，最近一笔订单作为感谢锚点
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ConsumeThankRecordDto>>> GetConsumeThanksAsync(ConsumeThankQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ConsumeThankRecordDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var startDate = DateTime.Today.AddDays(-7);

        // 1. 查近7天已完成订单 + Include Customer，按 OrderTime 降序
        var recentOrders = await _dbContext.Orders
            .Where(o => o.Status == 2 && o.CustomerId.HasValue
                && o.OrderTime >= startDate
                && o.TenantId == tenantId && o.StoreId == storeId)
            .OrderByDescending(o => o.OrderTime)
            .Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.PaidAmount,
                o.OrderTime,
                CustomerName = o.Customer != null ? o.Customer.Name : string.Empty,
                Phone = o.Customer != null ? o.Customer.Phone : string.Empty
            })
            .ToListAsync();

        // 2. 内存按客户分组聚合：近7天累计金额、订单笔数，以及最近一笔订单
        // 最近一笔订单的 ID 作为列表项业务ID，供标记感谢时回传（感谢记录按订单锚定）
        var groupedCustomers = recentOrders
            .GroupBy(o => o.CustomerId!.Value)
            .Select(g =>
            {
                var latest = g.OrderByDescending(o => o.OrderTime).First();
                return new
                {
                    latest.Id,
                    CustomerId = g.Key,
                    latest.CustomerName,
                    latest.Phone,
                    TotalAmount = g.Sum(o => o.PaidAmount),
                    OrderCount = g.Count(),
                    LatestOrderTime = latest.OrderTime
                };
            })
            .ToList();

        // 3. 批量查 Type=2 的感谢记录
        var orderIds = groupedCustomers.Select(o => o.Id).ToList();
        var thankLogs = await _dbContext.CustomerCareLogs
            .Where(l => l.Type == CustomerCareLogTypes.ConsumeThank
                && l.TenantId == tenantId && l.StoreId == storeId
                && orderIds.Contains(l.RefOrderId!.Value))
            .Select(l => new { l.RefOrderId, l.Method, l.CareTime, l.OperatorName })
            .ToListAsync();

        var thankLogMap = thankLogs.ToDictionary(l => l.RefOrderId!.Value, l => l);
        var records = groupedCustomers.Select(o =>
        {
            var dto = new ConsumeThankRecordDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CustomerName = o.CustomerName,
                Phone = o.Phone,
                TotalAmount = o.TotalAmount,
                OrderCount = o.OrderCount,
                LastConsumeTime = o.LatestOrderTime,
                ThankStatus = 1
            };
            if (thankLogMap.TryGetValue(o.Id, out var log))
            {
                dto.ThankStatus = 2;
                dto.ThankMethod = log.Method;
                dto.ThankTime = log.CareTime;
                dto.OperatorName = log.OperatorName;
            }
            return dto;
        }).ToList();

        // 4. 按条件过滤
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            records = records
                .Where(r => (r.CustomerName?.Contains(query.Keyword) ?? false)
                    || (r.Phone?.Contains(query.Keyword) ?? false))
                .ToList();
        if (query.ThankStatus.HasValue)
            records = records.Where(r => r.ThankStatus == query.ThankStatus.Value).ToList();

        // 5. 按最近消费时间倒序排序
        records = records.OrderByDescending(r => r.LastConsumeTime).ToList();

        // 6. 内存分页
        var total = records.Count;
        var paged = records
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new PagedResponseDto<ConsumeThankRecordDto>
        {
            List = paged,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ConsumeThankRecordDto>>.Ok(result);
    }

    /// <summary>
    /// 标记消费感谢（幂等：同订单重复标记只更新方式和时间）
    /// </summary>
    public async Task<ApiResponseDto> MarkConsumeThankedAsync(long orderId, int method)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        if (method < 1 || method > 3)
            return ApiResponseDto.Fail("感谢方式无效（1=短信 2=微信 3=电话）", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var operatorName = string.IsNullOrWhiteSpace(_currentUser.RealName) ? _currentUser.UserName : _currentUser.RealName;

        // 校验订单存在且属于本租户本门店，且为已完成状态
        var order = await _dbContext.Orders
            .Where(o => o.Id == orderId
                && o.TenantId == tenantId && o.StoreId == storeId
                && o.Status == 2 && o.CustomerId.HasValue)
            .Select(o => new { o.Id, o.CustomerId })
            .FirstOrDefaultAsync();
        if (order == null)
            return ApiResponseDto.Fail("订单不存在或不符合感谢条件", 404);

        var existing = await _dbContext.CustomerCareLogs
            .FirstOrDefaultAsync(l => l.Type == CustomerCareLogTypes.ConsumeThank
                && l.RefOrderId == orderId
                && l.TenantId == tenantId && l.StoreId == storeId);

        if (existing != null)
        {
            existing.Method = method;
            existing.CareTime = DateTime.Now;
            existing.OperatorName = operatorName;
            existing.UpdatedTime = DateTime.Now;
            existing.UpdatedBy = _currentUser.UserId;
        }
        else
        {
            _dbContext.CustomerCareLogs.Add(new CustomerCareLog
            {
                CustomerId = order.CustomerId!.Value,
                Type = CustomerCareLogTypes.ConsumeThank,
                RefOrderId = orderId,
                Method = method,
                CareTime = DateTime.Now,
                OperatorName = operatorName,
                TenantId = tenantId,
                TenantCode = _currentUser.TenantCode ?? string.Empty,
                StoreId = storeId,
                StoreCode = _currentUser.StoreCode ?? string.Empty,
                CreatedTime = DateTime.Now,
                CreatedBy = _currentUser.UserId
            });
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // 并发场景下部分唯一索引冲突，记录已存在，忽略
        }
        return ApiResponseDto.Success(null, "标记成功");
    }

    /// <summary>
    /// 计算距下次生日的天数
    /// 处理2月29日生日在非闰年的情况（按2月28日处理）
    /// </summary>
    private static int CalcDaysToBirthday(DateTime birthday, DateTime today)
    {
        var day = birthday.Day;
        if (birthday.Month == 2 && day == 29 && !DateTime.IsLeapYear(today.Year))
            day = 28;

        var nextBirthday = new DateTime(today.Year, birthday.Month, day);
        if (nextBirthday < today)
            nextBirthday = nextBirthday.AddYears(1);

        return (nextBirthday - today).Days;
    }
}
