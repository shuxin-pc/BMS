using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Messages;

namespace Bms.System.Application.Services;

/// <summary>
/// 消息应用服务接口
/// </summary>
public interface IMessageAppService
{
    // 用户端（操作自己的消息）

    /// <summary>
    /// 收件箱分页查询
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<MessageInboxDto>>> GetInboxAsync(MessageInboxQueryDto query);

    /// <summary>
    /// 获取未读消息数
    /// </summary>
    Task<ApiResponseDto<int>> GetUnreadCountAsync();

    /// <summary>
    /// 获取消息详情
    /// </summary>
    Task<ApiResponseDto<MessageInboxDto?>> GetMessageAsync(long recipientId);

    /// <summary>
    /// 标记单条消息为已读
    /// </summary>
    Task<ApiResponseDto<bool>> MarkAsReadAsync(long recipientId);

    /// <summary>
    /// 批量标记已读
    /// </summary>
    Task<ApiResponseDto<bool>> BatchMarkAsReadAsync(MessageBatchReadDto dto);

    /// <summary>
    /// 全部标记已读
    /// </summary>
    Task<ApiResponseDto<bool>> MarkAllAsReadAsync();

    /// <summary>
    /// 用户侧删除单条消息（逻辑删除）
    /// </summary>
    Task<ApiResponseDto<bool>> DeleteAsync(long recipientId);

    /// <summary>
    /// 用户侧批量删除（逻辑删除）
    /// </summary>
    Task<ApiResponseDto<bool>> BatchDeleteAsync(MessageBatchDeleteDto dto);

    // 管理端

    /// <summary>
    /// 管理端消息发送记录分页查询
    /// </summary>
    Task<ApiResponseDto<PagedResponseDto<MessageDto>>> GetSentMessagesAsync(MessageQueryDto query);

    /// <summary>
    /// 管理端消息详情
    /// </summary>
    Task<ApiResponseDto<MessageDto?>> GetSentMessageAsync(long messageId);

    /// <summary>
    /// 发送消息（手动推送）
    /// </summary>
    Task<ApiResponseDto<MessageDto?>> SendAsync(MessageSendDto dto);

    /// <summary>
    /// 撤回消息
    /// </summary>
    Task<ApiResponseDto<bool>> RecallAsync(long messageId);

    /// <summary>
    /// 获取消息已读/未读统计及未读人员名单（管理端）
    /// </summary>
    Task<ApiResponseDto<MessageReadStatsDto>> GetReadStatsAsync(long messageId);

    // 内部调用

    /// <summary>
    /// 内部服务触发消息发送（自动触发）
    /// </summary>
    Task<ApiResponseDto<bool>> NotifyAsync(InternalMessageNotifyDto dto);
}
