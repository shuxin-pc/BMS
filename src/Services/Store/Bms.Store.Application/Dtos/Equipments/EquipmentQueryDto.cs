using Bms.Store.Application.Dtos;

namespace Bms.Store.Application.Dtos.Equipments;

/// <summary>
/// 设备台账分页查询参数
/// </summary>
public class EquipmentQueryDto : PagedRequestDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int? Status { get; set; }
}
