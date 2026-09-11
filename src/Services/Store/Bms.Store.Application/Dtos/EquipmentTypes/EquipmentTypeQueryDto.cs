using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.EquipmentTypes;

/// <summary>
/// 设备类型分页查询参数
/// </summary>
public class EquipmentTypeQueryDto : PagedRequestDto
{
    /// <summary>
    /// 类型名称（模糊匹配）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 类型编码（模糊匹配）
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 启用状态筛选
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// 父级类型ID筛选（按分类查看其下类型）
    /// </summary>
    public long? ParentId { get; set; }
}
