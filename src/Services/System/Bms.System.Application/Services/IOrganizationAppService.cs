using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.Organizations;

namespace Bms.System.Application.Services;

public interface IOrganizationAppService
{
    Task<List<OrganizationDto>> GetTreeListAsync(OrganizationQueryDto? query, bool isSuperAdmin = true, long? tenantId = null);
    Task<List<OrganizationDto>> GetListAsync(OrganizationQueryDto query, bool isSuperAdmin = true, long? tenantId = null);
    Task<OrganizationDto?> GetByIdAsync(long id);
    Task<List<OrganizationDto>> GetChildrenAsync(long? parentId);
    Task<OrganizationDto> CreateAsync(OrganizationCreateDto dto);
    Task<OrganizationDto> UpdateAsync(OrganizationUpdateDto dto);
    Task DeleteAsync(long id);
}
