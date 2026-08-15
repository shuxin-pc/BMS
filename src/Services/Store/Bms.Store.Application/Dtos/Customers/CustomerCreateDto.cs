namespace Bms.Store.Application.Dtos.Customers;

/// <summary>
/// 创建客户档案输入 DTO
/// </summary>
public class CustomerCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Gender { get; set; }
    public DateTime? Birthday { get; set; }
    public long? LevelId { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// 已有标签 ID 列表（允许创建客户时关联已有标签）
    /// </summary>
    public List<long>? TagIds { get; set; }

    /// <summary>
    /// 新建标签名称列表（允许创建客户时直接输入新标签名，后端自动创建标签并关联）
    /// </summary>
    public List<string>? NewTagNames { get; set; }

    public int AuthorizationStatus { get; set; }
    public DateTime? AuthorizationTime { get; set; }
    public string? Remark { get; set; }
}
