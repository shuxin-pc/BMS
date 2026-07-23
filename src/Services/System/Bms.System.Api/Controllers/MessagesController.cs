using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Messages;
using Bms.System.Application.Services;

namespace Bms.System.Api.Controllers;

/// <summary>
/// 消息控制器（用户端 + 管理端）
/// 用户端：操作自己的收件箱（标记已读、删除等）
/// 管理端：发送消息、查看发送记录、撤回
/// </summary>
[ApiController]
[Route("api/system/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageAppService _messageService;

    public MessagesController(IMessageAppService messageService)
    {
        _messageService = messageService;
    }

    // ========================================================
    // 用户端（操作自己的消息）
    // ========================================================

    /// <summary>
    /// 收件箱列表（分页+筛选）
    /// </summary>
    [HttpGet("inbox")]
    public async Task<ApiResponseDto<PagedResponseDto<MessageInboxDto>>> GetInbox([FromQuery] MessageInboxQueryDto query)
    {
        return await _messageService.GetInboxAsync(query);
    }

    /// <summary>
    /// 未读消息数
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ApiResponseDto<int>> GetUnreadCount()
    {
        return await _messageService.GetUnreadCountAsync();
    }

    /// <summary>
    /// 消息详情
    /// </summary>
    [HttpGet("{recipientId:long}")]
    public async Task<ApiResponseDto<MessageInboxDto?>> GetMessage(long recipientId)
    {
        return await _messageService.GetMessageAsync(recipientId);
    }

    /// <summary>
    /// 标记已读
    /// </summary>
    [HttpPut("{recipientId:long}/read")]
    public async Task<ApiResponseDto<bool>> MarkAsRead(long recipientId)
    {
        return await _messageService.MarkAsReadAsync(recipientId);
    }

    /// <summary>
    /// 批量已读
    /// </summary>
    [HttpPut("batch-read")]
    public async Task<ApiResponseDto<bool>> BatchMarkAsRead([FromBody] MessageBatchReadDto dto)
    {
        return await _messageService.BatchMarkAsReadAsync(dto);
    }

    /// <summary>
    /// 全部标记已读
    /// </summary>
    [HttpPut("read-all")]
    public async Task<ApiResponseDto<bool>> MarkAllAsRead()
    {
        return await _messageService.MarkAllAsReadAsync();
    }

    /// <summary>
    /// 删除消息（逻辑删除）
    /// </summary>
    [HttpDelete("{recipientId:long}")]
    public async Task<ApiResponseDto<bool>> Delete(long recipientId)
    {
        return await _messageService.DeleteAsync(recipientId);
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    [HttpDelete("batch")]
    public async Task<ApiResponseDto<bool>> BatchDelete([FromBody] MessageBatchDeleteDto dto)
    {
        return await _messageService.BatchDeleteAsync(dto);
    }

    // ========================================================
    // 管理端（发送/管理消息）
    // ========================================================

    /// <summary>
    /// 消息发送记录列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ApiResponseDto<PagedResponseDto<MessageDto>>> GetSentList([FromQuery] MessageQueryDto query)
    {
        return await _messageService.GetSentMessagesAsync(query);
    }

    /// <summary>
    /// 发送消息（手动推送）
    /// </summary>
    [HttpPost("send")]
    public async Task<ApiResponseDto<MessageDto?>> Send([FromBody] MessageSendDto dto)
    {
        return await _messageService.SendAsync(dto);
    }

    /// <summary>
    /// 撤回消息
    /// </summary>
    [HttpDelete("{messageId:long}/recall")]
    public async Task<ApiResponseDto<bool>> Recall(long messageId)
    {
        return await _messageService.RecallAsync(messageId);
    }

    /// <summary>
    /// 消息已读/未读统计及未读人员名单
    /// </summary>
    [HttpGet("{messageId:long}/read-stats")]
    public async Task<ApiResponseDto<MessageReadStatsDto>> GetReadStats(long messageId)
    {
        return await _messageService.GetReadStatsAsync(messageId);
    }
}
