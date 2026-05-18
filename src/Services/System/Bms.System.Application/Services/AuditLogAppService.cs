using Mapster;
using Bms.System.Application.Dtos;
using Bms.System.Application.Dtos.AuditLogs;
using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

public class AuditLogAppService : IAuditLogAppService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogAppService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponseDto<AuditLogDto?>> GetByIdAsync(long id)
    {
        var auditLog = await _auditLogRepository.GetByIdAsync(id);
        if (auditLog == null)
        {
            return ApiResponseDto<AuditLogDto?>.Fail("审计日志不存在", 404);
        }
        return ApiResponseDto<AuditLogDto?>.Success(auditLog.Adapt<AuditLogDto>());
    }

    public async Task<ApiResponseDto<PagedResponseDto<AuditLogDto>>> GetPagedListAsync(AuditLogQueryDto query)
    {
        var auditLogs = await _auditLogRepository.GetPagedListAsync(
            query.PageIndex,
            query.PageSize,
            query.UserName,
            query.OperationType,
            query.StartDate,
            query.EndDate,
            query.ResponseStatus,
            query.TenantId
        );

        var totalCount = await _auditLogRepository.GetCountAsync(
            query.UserName,
            query.OperationType,
            query.StartDate,
            query.EndDate,
            query.ResponseStatus,
            query.TenantId
        );

        var items = auditLogs.Select(a => a.Adapt<AuditLogDto>()).ToList();

        return ApiResponseDto<PagedResponseDto<AuditLogDto>>.Success(new PagedResponseDto<AuditLogDto>
        {
            List = items,
            Total = totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        });
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        await _auditLogRepository.DeleteAsync(id);
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> DeleteExpiredAsync(DateTime beforeDate)
    {
        var deletedCount = await _auditLogRepository.DeleteExpiredAsync(beforeDate);
        return ApiResponseDto.Success(deletedCount, $"已清理 {deletedCount} 条历史日志");
    }

    public async Task<ApiResponseDto> CreateAsync(CreateAuditLogDto dto)
    {
        var auditLog = new AuditLog
        {
            TenantId = dto.TenantId,
            UserId = dto.UserId,
            UserName = dto.UserName,
            RealName = dto.RealName,
            OperationType = dto.OperationType,
            OperationContent = $"{dto.OperationType} operation",
            RequestPath = dto.RequestPath,
            RequestMethod = "POST",
            RequestIp = dto.RequestIp,
            UserAgent = dto.UserAgent,
            ResponseStatus = dto.ResponseStatus,
            Duration = 0,
            EntityChanges = null,
            CreatedTime = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
        return ApiResponseDto.Success(null, "审计日志创建成功");
    }
}
