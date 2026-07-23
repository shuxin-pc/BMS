namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 批量删除消息参数
/// </summary>
public class MessageBatchDeleteDto
{
    /// <summary>
    /// 接收记录ID列表（MessageRecipient.Id）
    /// </summary>
    public List<long> Ids { get; set; } = new();
}
