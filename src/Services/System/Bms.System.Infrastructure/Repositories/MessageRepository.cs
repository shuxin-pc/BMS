using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

/// <summary>
/// 消息发送记录仓储实现
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly SystemDbContext _context;

    public MessageRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(long id)
    {
        return await _context.Set<Message>().FindAsync(id);
    }

    public async Task<List<Message>> GetSentListAsync(
        int pageIndex,
        int pageSize,
        long? tenantId,
        int? category = null,
        int? sourceType = null,
        bool? isRecalled = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var query = BuildSentQuery(tenantId, category, sourceType, isRecalled, startTime, endTime);

        return await query
            .OrderByDescending(m => m.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetSentCountAsync(
        long? tenantId,
        int? category = null,
        int? sourceType = null,
        bool? isRecalled = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var query = BuildSentQuery(tenantId, category, sourceType, isRecalled, startTime, endTime);
        return await query.CountAsync();
    }

    public async Task<Message> AddAsync(Message message)
    {
        await _context.Set<Message>().AddAsync(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task UpdateAsync(Message message)
    {
        _context.Set<Message>().Update(message);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Message> BuildSentQuery(
        long? tenantId,
        int? category,
        int? sourceType,
        bool? isRecalled,
        DateTime? startTime,
        DateTime? endTime)
    {
        var query = _context.Set<Message>().AsQueryable();

        // 平台管理员查看所有租户，租户管理员仅查看本租户
        if (tenantId.HasValue)
        {
            query = query.Where(m => m.TenantId == tenantId.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(m => (int)m.Category == category.Value);
        }

        if (sourceType.HasValue)
        {
            query = query.Where(m => (int)m.SourceType == sourceType.Value);
        }

        if (isRecalled.HasValue)
        {
            query = query.Where(m => m.IsRecalled == isRecalled.Value);
        }

        if (startTime.HasValue)
        {
            query = query.Where(m => m.CreatedTime >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            // 结束日期当天 00:00:00 时扩展到 23:59:59
            var endDateValue = endTime.Value;
            if (endDateValue.Hour == 0 && endDateValue.Minute == 0 && endDateValue.Second == 0)
            {
                endDateValue = endDateValue.AddDays(1).AddTicks(-1);
            }
            query = query.Where(m => m.CreatedTime <= endDateValue);
        }

        return query;
    }
}
