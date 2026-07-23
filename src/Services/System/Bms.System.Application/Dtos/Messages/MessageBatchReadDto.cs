namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 批量标记已读参数
/// </summary>
public class MessageBatchReadDto
{
    /// <summary>
    /// 接收记录ID列表（MessageRecipient.Id）
    /// </summary>
    public List<long> Ids { get; set; } = new();
}
