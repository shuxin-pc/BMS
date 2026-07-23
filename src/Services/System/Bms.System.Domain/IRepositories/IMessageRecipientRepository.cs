using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 消息接收记录仓储接口
/// </summary>
public interface IMessageRecipientRepository
{
    /// <summary>
    /// 根据ID获取接收记录
    /// </summary>
    Task<MessageRecipient?> GetByIdAsync(long id);

    /// <summary>
    /// 用户收件箱分页查询
    /// </summary>
    /// <param name="userId">接收用户ID</param>
    /// <param name="tenantId">租户ID（与用户租户一致）</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="category">分类筛选</param>
    /// <param name="isRead">已读筛选（null 表示全部）</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    Task<List<MessageRecipient>> GetInboxAsync(
        long userId,
        long tenantId,
        int pageIndex,
        int pageSize,
        int? category = null,
        bool? isRead = null,
        DateTime? startTime = null,
        DateTime? endTime = null);

    /// <summary>
    /// 用户收件箱总数
    /// </summary>
    Task<int> GetInboxCountAsync(
        long userId,
        long tenantId,
        int? category = null,
        bool? isRead = null,
        DateTime? startTime = null,
        DateTime? endTime = null);

    /// <summary>
    /// 获取用户未读消息数
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, long tenantId);

    /// <summary>
    /// 批量新增接收记录（消息发送时展开接收者）
    /// </summary>
    Task BatchInsertAsync(List<MessageRecipient> recipients);

    /// <summary>
    /// 标记单条消息为已读
    /// </summary>
    Task MarkAsReadAsync(long recipientId, long userId);

    /// <summary>
    /// 批量标记已读
    /// </summary>
    Task BatchMarkAsReadAsync(List<long> recipientIds, long userId);

    /// <summary>
    /// 标记用户所有未读消息为已读
    /// </summary>
    Task MarkAllAsReadAsync(long userId, long tenantId);

    /// <summary>
    /// 用户侧逻辑删除单条消息
    /// </summary>
    Task SoftDeleteAsync(long recipientId, long userId);

    /// <summary>
    /// 用户侧批量逻辑删除
    /// </summary>
    Task BatchSoftDeleteAsync(List<long> recipientIds, long userId);

    /// <summary>
    /// 按消息ID逻辑删除所有接收记录（管理端撤回时使用，跨租户操作）
    /// </summary>
    Task SoftDeleteByMessageIdAsync(long messageId);

    /// <summary>
    /// 按消息ID统计已读/未读数量，并返回前 N 条未读人员名单。
    /// 排除已逻辑删除（IsDeleted=true）的接收记录。
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <param name="unreadLimit">未读名单最大返回条数（默认 50）</param>
    Task<MessageReadStatsData> GetReadStatsAsync(long messageId, int unreadLimit = 50);
}

/// <summary>
/// 消息已读/未读统计原始数据（仓储层返回，由应用层映射为 DTO）
/// </summary>
public record MessageReadStatsData(
    int TotalCount,
    int ReadCount,
    int UnreadCount,
    List<UnreadUserItem> UnreadList);

/// <summary>
/// 未读人员名单项
/// </summary>
public record UnreadUserItem(long UserId, string RealName);
