namespace Bms.Store.Application.Dtos.ServiceBoms;

/// <summary>
/// 更新服务BOM输入 DTO
/// </summary>
public class ServiceBomUpdateDto : ServiceBomCreateDto
{
    public long Id { get; set; }
}
