namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 客户档案永久删除输入 DTO
/// 用于物理删除客户档案及关联个人信息，满足《个人信息保护法》第 47 条合规要求
/// </summary>
public class CustomerPermanentDeleteDto
{
    /// <summary>
    /// 二次确认码（客户手机号后4位）
    /// 用于防止误操作，必须与客户当前手机号后4位匹配
    /// </summary>
    public string ConfirmCode { get; set; } = string.Empty;

    /// <summary>
    /// 删除原因（写入审计日志，永久保留）
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
