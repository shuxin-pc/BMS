using Bms.System.Domain.Entities;

namespace Bms.System.Domain.IRepositories;

/// <summary>
/// 消息发送记录仓储接口
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// 根据ID获取消息
    /// </summary>
    Task<Message?> GetByIdAsync(long id);

    /// <summary>
    /// 管理端分页查询消息发送记录
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="tenantId">租户ID（null 表示查询所有租户，仅平台管理员可用）</param>
    /// <param name="category">分类筛选</param>
    /// <param name="sourceType">来源类型筛选</param>
    /// <param name="isRecalled">撤回状态筛选（null 表示全部）</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    Task<List<Message>> GetSentListAsync(
        int pageIndex,
        int pageSize,
        long? tenantId,
        int? category = null,
        int? sourceType = null,
        bool? isRecalled = null,
        DateTime? startTime = null,
        DateTime? endTime = null);

    /// <summary>
    /// 管理端查询消息发送记录总数
    /// </summary>
    Task<int> GetSentCountAsync(
        long? tenantId,
        int? category = null,
        int? sourceType = null,
        bool? isRecalled = null,
        DateTime? startTime = null,
        DateTime? endTime = null);

    /// <summary>
    /// 新增消息
    /// </summary>
    Task<Message> AddAsync(Message message);

    /// <summary>
    /// 更新消息（如撤回时设置 IsRecalled）
    /// </summary>
    Task UpdateAsync(Message message);
}
