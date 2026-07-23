namespace Bms.System.Application.Dtos.Messages;

/// <summary>
/// 未读人员名单项
/// </summary>
public class MessageReadUserDto
{
    /// <summary>
    /// 接收用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 真实姓名（为空时由应用层填充"未实名用户"）
    /// </summary>
    public string RealName { get; set; } = string.Empty;
}
