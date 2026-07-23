using Microsoft.AspNetCore.SignalR;
using Bms.System.Application.Dtos.Messages;
using Bms.System.Application.Services;
using Bms.System.Api.Hubs;

namespace Bms.System.Api.Services;

/// <summary>
/// 消息推送实现（Api 层）
/// 注入 IHubContext<MessageHub>，通过 SignalR Group 向指定用户推送消息。
/// 实现 Application 层的 IMessagePusher 抽象，保持 DDD 分层纯净。
/// </summary>
public class MessagePusher : IMessagePusher
{
    private readonly IHubContext<MessageHub> _hubContext;

    public MessagePusher(IHubContext<MessageHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushToUserAsync(long userId, MessageInboxDto payload)
    {
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveMessage", payload);
    }
}
