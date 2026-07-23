using Bms.System.Application.Dtos.Messages;

namespace Bms.System.Application.Services;

/// <summary>
/// 消息推送抽象接口
/// 定义在 Application 层以保持 DDD 分层纯净：
/// MessageAppService 通过此接口触发实时推送，具体 SignalR 实现在 Api 层。
/// </summary>
public interface IMessagePusher
{
    /// <summary>
    /// 向指定用户推送消息（通过 SignalR Group）
    /// </summary>
    /// <param name="userId">接收用户ID</param>
    /// <param name="payload">推送负载（MessageInboxDto）</param>
    Task PushToUserAsync(long userId, MessageInboxDto payload);
}
