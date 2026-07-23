using Microsoft.EntityFrameworkCore;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Infrastructure.Repositories;

/// <summary>
/// 消息接收记录仓储实现
/// </summary>
public class MessageRecipientRepository : IMessageRecipientRepository
{
    private readonly SystemDbContext _context;

    public MessageRecipientRepository(SystemDbContext context)
    {
        _context = context;
    }

    public async Task<MessageRecipient?> GetByIdAsync(long id)
    {
        return await _context.Set<MessageRecipient>().FindAsync(id);
    }

    public async Task<List<MessageRecipient>> GetInboxAsync(
        long userId,
        long tenantId,
        int pageIndex,
        int pageSize,
        int? category = null,
        bool? isRead = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var query = BuildInboxQuery(userId, tenantId, category, isRead, startTime, endTime);

        return await query
            .OrderByDescending(r => r.CreatedTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetInboxCountAsync(
        long userId,
        long tenantId,
        int? category = null,
        bool? isRead = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        var query = BuildInboxQuery(userId, tenantId, category, isRead, startTime, endTime);
        return await query.CountAsync();
    }

    public async Task<int> GetUnreadCountAsync(long userId, long tenantId)
    {
        return await _context.Set<MessageRecipient>()
            .Where(r => r.UserId == userId
                        && r.TenantId == tenantId
                        && !r.IsRead
                        && !r.IsDeleted)
            .CountAsync();
    }

    public async Task BatchInsertAsync(List<MessageRecipient> recipients)
    {
        if (recipients.Count == 0) return;
        await _context.Set<MessageRecipient>().AddRangeAsync(recipients);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(long recipientId, long userId)
    {
        var recipient = await _context.Set<MessageRecipient>()
            .FirstOrDefaultAsync(r => r.Id == recipientId && r.UserId == userId && !r.IsDeleted);

        if (recipient != null && !recipient.IsRead)
        {
            recipient.IsRead = true;
            recipient.ReadTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task BatchMarkAsReadAsync(List<long> recipientIds, long userId)
    {
        if (recipientIds == null || recipientIds.Count == 0) return;

        var recipients = await _context.Set<MessageRecipient>()
            .Where(r => recipientIds.Contains(r.Id) && r.UserId == userId && !r.IsDeleted && !r.IsRead)
            .ToListAsync();

        foreach (var r in recipients)
        {
            r.IsRead = true;
            r.ReadTime = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(long userId, long tenantId)
    {
        var recipients = await _context.Set<MessageRecipient>()
            .Where(r => r.UserId == userId && r.TenantId == tenantId && !r.IsRead && !r.IsDeleted)
            .ToListAsync();

        foreach (var r in recipients)
        {
            r.IsRead = true;
            r.ReadTime = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(long recipientId, long userId)
    {
        var recipient = await _context.Set<MessageRecipient>()
            .FirstOrDefaultAsync(r => r.Id == recipientId && r.UserId == userId && !r.IsDeleted);

        if (recipient != null)
        {
            recipient.IsDeleted = true;
            recipient.DeletedTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task BatchSoftDeleteAsync(List<long> recipientIds, long userId)
    {
        if (recipientIds == null || recipientIds.Count == 0) return;

        var recipients = await _context.Set<MessageRecipient>()
            .Where(r => recipientIds.Contains(r.Id) && r.UserId == userId && !r.IsDeleted)
            .ToListAsync();

        foreach (var r in recipients)
        {
            r.IsDeleted = true;
            r.DeletedTime = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteByMessageIdAsync(long messageId)
    {
        // 撤回时跨租户逻辑删除所有关联接收记录，不限制 userId
        var recipients = await _context.Set<MessageRecipient>()
            .Where(r => r.MessageId == messageId && !r.IsDeleted)
            .ToListAsync();

        foreach (var r in recipients)
        {
            r.IsDeleted = true;
            r.DeletedTime = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<MessageReadStatsData> GetReadStatsAsync(long messageId, int unreadLimit = 50)
    {
        // 排除用户侧逻辑删除与撤回标记的接收记录，统计已读/未读分布
        var recipients = await _context.Set<MessageRecipient>()
            .Where(r => r.MessageId == messageId && !r.IsDeleted)
            .Select(r => new { r.UserId, r.IsRead })
            .ToListAsync();

        var totalCount = recipients.Count;
        var readCount = recipients.Count(r => r.IsRead);
        var unreadCount = totalCount - readCount;

        // 未读名单 JOIN User 表取真实姓名，按 UserId 升序保证确定性，取前 unreadLimit 条
        var unreadList = await (from r in _context.Set<MessageRecipient>()
                                join u in _context.Set<User>() on r.UserId equals u.Id
                                where r.MessageId == messageId && !r.IsDeleted && !r.IsRead
                                orderby r.UserId ascending
                                select new UnreadUserItem(u.Id, u.RealName))
                                .Take(unreadLimit)
                                .ToListAsync();

        return new MessageReadStatsData(totalCount, readCount, unreadCount, unreadList);
    }

    private IQueryable<MessageRecipient> BuildInboxQuery(
        long userId,
        long tenantId,
        int? category,
        bool? isRead,
        DateTime? startTime,
        DateTime? endTime)
    {
        var query = _context.Set<MessageRecipient>()
            .Where(r => r.UserId == userId && r.TenantId == tenantId && !r.IsDeleted);

        // 按分类筛选需要 JOIN Message 表
        if (category.HasValue)
        {
            query = from r in query
                    join m in _context.Set<Message>() on r.MessageId equals m.Id
                    where (int)m.Category == category.Value
                    select r;
        }

        if (isRead.HasValue)
        {
            query = query.Where(r => r.IsRead == isRead.Value);
        }

        if (startTime.HasValue)
        {
            query = query.Where(r => r.CreatedTime >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            var endDateValue = endTime.Value;
            if (endDateValue.Hour == 0 && endDateValue.Minute == 0 && endDateValue.Second == 0)
            {
                endDateValue = endDateValue.AddDays(1).AddTicks(-1);
            }
            query = query.Where(r => r.CreatedTime <= endDateValue);
        }

        return query;
    }
}
