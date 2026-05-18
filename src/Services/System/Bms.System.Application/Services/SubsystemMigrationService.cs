using Bms.System.Domain.Entities;
using Bms.System.Domain.IRepositories;

namespace Bms.System.Application.Services;

/// <summary>
/// 子系统数据迁移服务实现
/// </summary>
public class SubsystemMigrationService : ISubsystemMigrationService
{
    private readonly ISubsystemRepository _subsystemRepository;
    private readonly ITenantSubsystemRepository _tenantSubsystemRepository;
    private readonly ITenantRepository _tenantRepository;

    public SubsystemMigrationService(
        ISubsystemRepository subsystemRepository,
        ITenantSubsystemRepository tenantSubsystemRepository,
        ITenantRepository tenantRepository)
    {
        _subsystemRepository = subsystemRepository;
        _tenantSubsystemRepository = tenantSubsystemRepository;
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// 执行数据迁移：从 Tenant.AllowedSubsystems 迁移到新表
    /// </summary>
    public async Task MigrateAsync()
    {
        // 获取所有租户
        var allTenants = await _tenantRepository.GetAllTenantsAsync();
        var tenants = allTenants
            .Where(t => !t.IsDeleted && !string.IsNullOrEmpty(t.AllowedSubsystems))
            .ToList();

        if (!tenants.Any())
        {
            return;
        }

        // 解析所有子系统编码
        var allSubsystemCodes = tenants
            .Where(t => !string.IsNullOrEmpty(t.AllowedSubsystems))
            .SelectMany(t => t.AllowedSubsystems!.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(code => code.Trim())
            .Distinct()
            .ToList();

        // 获取或创建子系统
        var existingSubsystems = await _subsystemRepository.GetListAsync();
        var existingCodes = existingSubsystems.Select(s => s.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var code in allSubsystemCodes)
        {
            if (!existingCodes.Contains(code))
            {
                var subsystem = new Subsystem
                {
                    Code = code,
                    Name = GetSubsystemName(code),
                    Description = $"系统自动创建的 {code} 子系统",
                    Sort = 0,
                    Status = 1,
                    CreatedTime = DateTime.UtcNow,
                    UpdatedTime = DateTime.UtcNow
                };
                await _subsystemRepository.AddAsync(subsystem);
                existingSubsystems.Add(subsystem);
                existingCodes.Add(code);
            }
        }

        // 刷新子系统列表
        existingSubsystems = await _subsystemRepository.GetListAsync();
        var subsystemDict = existingSubsystems.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

        // 迁移租户子系统关联
        foreach (var tenant in tenants)
        {
            if (string.IsNullOrEmpty(tenant.AllowedSubsystems))
            {
                continue;
            }

            var allowedCodes = tenant.AllowedSubsystems.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(code => code.Trim())
                .ToList();

            // 获取现有的租户子系统关联
            var existingTenantSubsystems = await _tenantSubsystemRepository.GetByTenantIdAsync(tenant.Id);
            var existingSubsystemIds = existingTenantSubsystems.Select(ts => ts.SubsystemId).ToHashSet();

            // 添加新的关联
            var newTenantSubsystems = new List<TenantSubsystem>();
            foreach (var code in allowedCodes)
            {
                if (subsystemDict.TryGetValue(code, out var subsystem))
                {
                    if (!existingSubsystemIds.Contains(subsystem.Id))
                    {
                        newTenantSubsystems.Add(new TenantSubsystem
                        {
                            TenantId = tenant.Id,
                            SubsystemId = subsystem.Id,
                            CreatedTime = DateTime.UtcNow,
                            UpdatedTime = DateTime.UtcNow
                        });
                    }
                }
            }

            if (newTenantSubsystems.Any())
            {
                await _tenantSubsystemRepository.AddRangeAsync(newTenantSubsystems);
            }
        }
    }

    /// <summary>
    /// 根据编码获取子系统名称
    /// </summary>
    private string GetSubsystemName(string code)
    {
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "system", "系统管理" },
            { "production", "生产管理" },
            { "quality", "质量管理" },
            { "equipment", "设备管理" },
            { "inventory", "库存管理" },
            { "bms", "BMS系统" },
            { "bms-system", "BMS系统管理" },
            { "bms-production", "BMS生产管理" },
            { "bms-quality", "BMS质量管理" },
            { "bms-equipment", "BMS设备管理" },
            { "bms-inventory", "BMS库存管理" }
        };

        return nameMap.TryGetValue(code, out var name) ? name : code;
    }
}
