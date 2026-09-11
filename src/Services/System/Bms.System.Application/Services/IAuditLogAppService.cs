using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.AuditLogs;

namespace Bms.System.Application.Services;

public interface IAuditLogAppService
{
    Task<ApiResponseDto<AuditLogDto?>> GetByIdAsync(long id);
    Task<ApiResponseDto<PagedResponseDto<AuditLogDto>>> GetPagedListAsync(AuditLogQueryDto query);
    Task<ApiResponseDto> CreateAsync(CreateAuditLogDto dto);
    Task<ApiResponseDto> DeleteAsync(long id);
    Task<ApiResponseDto> DeleteExpiredAsync(DateTime beforeDate, long? tenantId = null);
}
