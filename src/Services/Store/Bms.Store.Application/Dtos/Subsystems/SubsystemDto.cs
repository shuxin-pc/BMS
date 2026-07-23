namespace Bms.Store.Application.Dtos.Subsystems;

/// <summary>
/// 子系统 DTO
/// </summary>
public class SubsystemDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public int Sort { get; set; }
    public int Status { get; set; }
}

/// <summary>
/// 子系统创建 DTO
/// </summary>
public class SubsystemCreateDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public int Sort { get; set; }
    public int Status { get; set; }
}

/// <summary>
/// 子系统查询 DTO
/// </summary>
public class SubsystemQueryDto
{
    public string? Name { get; set; }
    public int? Status { get; set; }
}