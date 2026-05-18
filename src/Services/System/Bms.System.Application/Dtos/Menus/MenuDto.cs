namespace Bms.System.Application.Dtos.Menus;

public class MenuDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public int Type { get; set; }
    public int Status { get; set; }
    public string? PermissionCode { get; set; }
    public bool IsVisible { get; set; }
    public bool IsCache { get; set; }
    public bool IsAlwaysShow { get; set; }
    public List<MenuDto> Children { get; set; } = new();
}

public class MenuCreateDto
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public int Type { get; set; }
    public int Status { get; set; } = 1;
    public string? PermissionCode { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsCache { get; set; } = true;
    public bool IsAlwaysShow { get; set; }
}

public class MenuUpdateDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public int Type { get; set; }
    public int Status { get; set; }
    public string? PermissionCode { get; set; }
    public bool IsVisible { get; set; }
    public bool IsCache { get; set; }
    public bool IsAlwaysShow { get; set; }
}

public class MenuQueryDto
{
    public string? Name { get; set; }
    public int? Type { get; set; }
}