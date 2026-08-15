namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 批量检查业务键是否存在结果
/// </summary>
public class BizKeyCheckResultDto
{
    /// <summary>
    /// 已存在（已发送过消息）的业务唯一键列表
    /// </summary>
    public List<string> ExistingBizKeys { get; set; } = new();
}
