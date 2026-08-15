using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;
using TechnicianEntity = Bms.Store.Domain.Entities.Technician;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 商家技师应用服务实现
/// </summary>
public class TechnicianAppService : ITechnicianAppService
{
    /// <summary>
    /// 平台租户ID（平台技师归属于此租户，商家门店可只读选择平台技师）
    /// </summary>
    private const long PlatformTenantId = 1;

    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TechnicianCreateDto> _createValidator;
    private readonly IValidator<TechnicianUpdateDto> _updateValidator;
    private readonly ITechnicianPermissionService _permissionService;
    private readonly TechnicianSkillMatchService _skillMatchService;

    public TechnicianAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TechnicianCreateDto> createValidator,
        IValidator<TechnicianUpdateDto> updateValidator,
        ITechnicianPermissionService permissionService,
        TechnicianSkillMatchService skillMatchService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _permissionService = permissionService;
        _skillMatchService = skillMatchService;
    }

    /// <summary>
    /// 获取商家技师分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TechnicianDto>>> GetPagedListAsync(TechnicianQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TechnicianDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        // 商家门店可查询本租户技师 + 平台技师（只读选择）；平台租户仅查自己的技师
        var queryable = _dbContext.Technicians
            .Include(t => t.TechnicianSkills).ThenInclude(ts => ts.SkillCategory)
            .Where(t => !t.IsDeleted && (t.TenantId == tenantId || t.TenantId == PlatformTenantId));

        if (!string.IsNullOrWhiteSpace(query.Name))
            queryable = queryable.Where(t => t.Name.Contains(query.Name));
        if (!string.IsNullOrWhiteSpace(query.Phone))
            queryable = queryable.Where(t => t.Phone.Contains(query.Phone));
        if (query.Status.HasValue)
            queryable = queryable.Where(t => t.Status == query.Status.Value);
        if (query.Source.HasValue)
            queryable = queryable.Where(t => t.Source == query.Source.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(t => t.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<TechnicianDto>
        {
            List = items.Adapt<List<TechnicianDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TechnicianDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取商家技师详情
    /// </summary>
    public async Task<ApiResponseDto<TechnicianDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianDto?>.Fail("登录状态异常，请重新登录", 401);

        // 商家门店可查询本租户技师 + 平台技师（只读选择）
        var entity = await _dbContext.Technicians
            .Include(t => t.TechnicianSkills).ThenInclude(ts => ts.SkillCategory)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted
                && (t.TenantId == _currentUser.TenantId.Value || t.TenantId == PlatformTenantId));
        if (entity == null)
            return ApiResponseDto<TechnicianDto?>.Fail("技师不存在", 404);
        return ApiResponseDto<TechnicianDto?>.Ok(entity.Adapt<TechnicianDto>());
    }

    /// <summary>
    /// 创建商家技师
    /// </summary>
    public async Task<ApiResponseDto<TechnicianDto>> CreateAsync(TechnicianCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TechnicianDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var phoneExists = await _dbContext.Technicians
            .AnyAsync(t => t.Phone == dto.Phone && t.TenantId == tenantId && !t.IsDeleted);
        if (phoneExists)
            return ApiResponseDto<TechnicianDto>.Fail($"手机号 {dto.Phone} 已存在", 400);

        // 校验技能分类归属本租户（外键约束保证标签值在本租户分类范围内）
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            var validCategoryIds = await _dbContext.SkillCategories
                .Where(c => !c.IsDeleted && c.TenantId == tenantId
                    && dto.SkillCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();
            var invalidIds = dto.SkillCategoryIds.Except(validCategoryIds).ToList();
            if (invalidIds.Count > 0)
                return ApiResponseDto<TechnicianDto>.Fail($"技能分类ID {string.Join(",", invalidIds)} 不属于当前租户", 400);
        }

        var entity = dto.Adapt<TechnicianEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        // 强制根据当前租户设置 Source，忽略 DTO 传入（DTO 已移除 Source 字段）
        // 平台租户创建的为平台技师(2)，商家租户创建的为商家技师(1)
        entity.Source = _permissionService.IsPlatformTenant(tenantId) ? 2 : 1;
        entity.CreatedTime = DateTime.Now;

        // 级联创建技能标签关联（Distinct 防重复分类触发唯一约束）
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            foreach (var categoryId in dto.SkillCategoryIds.Distinct())
            {
                entity.TechnicianSkills.Add(new TechnicianSkill
                {
                    SkillCategoryId = categoryId,
                    TenantId = tenantId,
                    TenantCode = _currentUser.TenantCode ?? string.Empty,
                    CreatedTime = DateTime.Now
                });
            }
        }

        _dbContext.Technicians.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TechnicianDto>.Ok(entity.Adapt<TechnicianDto>(), "创建成功");
    }

    /// <summary>
    /// 更新商家技师
    /// </summary>
    public async Task<ApiResponseDto<TechnicianDto>> UpdateAsync(TechnicianUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TechnicianDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        // TenantId 隔离已保证商家租户无法操作平台技师（平台技师 TenantId=PlatformTenantId=1）
        // 此处无需追加 Source 过滤，TenantId 校验为最终防线
        var entity = await _dbContext.Technicians
            .Include(t => t.TechnicianSkills)
            .FirstOrDefaultAsync(t => t.Id == dto.Id && !t.IsDeleted && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<TechnicianDto>.Fail("技师不存在", 404);

        // 手机号变更时检查唯一性
        if (entity.Phone != dto.Phone)
        {
            var phoneExists = await _dbContext.Technicians
                .AnyAsync(t => t.Phone == dto.Phone && t.TenantId == tenantId && !t.IsDeleted && t.Id != dto.Id);
            if (phoneExists)
                return ApiResponseDto<TechnicianDto>.Fail($"手机号 {dto.Phone} 已存在", 400);
        }

        // 校验技能分类归属本租户（外键约束保证标签值在本租户分类范围内）
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            var validCategoryIds = await _dbContext.SkillCategories
                .Where(c => !c.IsDeleted && c.TenantId == tenantId
                    && dto.SkillCategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();
            var invalidIds = dto.SkillCategoryIds.Except(validCategoryIds).ToList();
            if (invalidIds.Count > 0)
                return ApiResponseDto<TechnicianDto>.Fail($"技能分类ID {string.Join(",", invalidIds)} 不属于当前租户", 400);
        }

        entity.Name = dto.Name;
        entity.Phone = dto.Phone;
        entity.Gender = dto.Gender;
        entity.AvatarUrl = dto.AvatarUrl;
        entity.Status = dto.Status;
        // Source 字段一旦创建不可变更（UpdateDto 已移除 Source 字段）
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        // 更新技能标签关联（先删旧再建新；关联表为物理删除，Distinct 防重复分类触发唯一约束）
        _dbContext.TechnicianSkills.RemoveRange(entity.TechnicianSkills);
        if (dto.SkillCategoryIds != null && dto.SkillCategoryIds.Count > 0)
        {
            foreach (var categoryId in dto.SkillCategoryIds.Distinct())
            {
                entity.TechnicianSkills.Add(new TechnicianSkill
                {
                    SkillCategoryId = categoryId,
                    TenantId = tenantId,
                    TenantCode = _currentUser.TenantCode ?? string.Empty,
                    CreatedTime = DateTime.Now
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TechnicianDto>.Ok(entity.Adapt<TechnicianDto>(), "更新成功");
    }

    /// <summary>
    /// 删除商家技师（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        // TenantId 隔离已保证商家租户无法操作平台技师（平台技师 TenantId=PlatformTenantId=1）
        // 此处无需追加 Source 过滤，TenantId 校验为最终防线
        var entity = await _dbContext.Technicians
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted && t.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto.Fail("技师不存在", 404);

        entity.IsDeleted = true;
        entity.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除商家技师（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        // TenantId 隔离已保证商家租户无法操作平台技师（平台技师 TenantId=PlatformTenantId=1）
        // 批量删除同样无需追加 Source 过滤，TenantId 校验为最终防线
        var entities = await _dbContext.Technicians
            .Where(t => ids.Contains(t.Id) && !t.IsDeleted && t.TenantId == tenantId)
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedTime = DateTime.Now;
        }
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 按服务项目查询可用技师（预约时过滤技师下拉 + 服务项目页展示可服务技师）
    /// 技能匹配：服务项目适用技能与技师技能标签沿技能分类树展开求交集，
    /// 仅选择父级分类时不遗漏拥有子级技能的技师。
    /// - serviceProductId/masterId 均为空：返回指定来源全部启用技师
    /// - 服务项目未配置技能：返回全部启用技师
    /// - 平台技师技能标签在平台租户语境、无法与当前门店技能分类对齐，保持全部可选
    /// </summary>
    /// <param name="serviceProductId">服务项目子表ID（预约页语境）</param>
    /// <param name="masterId">商品主档ID（服务项目页语境，自动反查租户内 ServiceProduct）</param>
    /// <param name="source">技师来源（1:商家 2:平台，可选）</param>
    public async Task<ApiResponseDto<List<TechnicianDto>>> GetAvailableByServiceAsync(long? serviceProductId, long? masterId, int? source)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<TechnicianDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        // 服务项目页语境：通过商品主档ID反查租户内 ServiceProduct（租户内 MasterId 唯一，1:1）
        if (masterId.HasValue)
        {
            serviceProductId = await _dbContext.ServiceProducts
                .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.MasterId == masterId.Value)
                .Select(s => (long?)s.Id)
                .FirstOrDefaultAsync();
        }

        // 候选技师：当前租户自有技师 + 平台技师（只读选择），仅筛选在岗状态
        var queryable = _dbContext.Technicians
            .Include(t => t.TechnicianSkills).ThenInclude(ts => ts.SkillCategory)
            .Where(t => !t.IsDeleted && t.Status == 1
                && (t.TenantId == tenantId || t.TenantId == PlatformTenantId));
        if (source.HasValue)
            queryable = queryable.Where(t => t.Source == source.Value);

        var technicians = await queryable.OrderBy(t => t.Name).ToListAsync();
        if (technicians.Count == 0)
            return ApiResponseDto<List<TechnicianDto>>.Ok(new List<TechnicianDto>());

        // 服务项目适用技能（当前门店语境配置）
        var serviceSkillIds = new List<long>();
        if (serviceProductId.HasValue)
        {
            serviceSkillIds = await _dbContext.ServiceProductSkills
                .Where(s => s.ServiceProductId == serviceProductId.Value && s.StoreId == storeId)
                .Select(s => s.SkillCategoryId)
                .ToListAsync();
        }

        // 未指定服务项目 或 服务项目未限定技能 → 全部返回
        if (!serviceProductId.HasValue || serviceSkillIds.Count == 0)
            return ApiResponseDto<List<TechnicianDto>>.Ok(technicians.Adapt<List<TechnicianDto>>());

        // 技能匹配过滤：平台技师技能无法对齐当前门店技能分类，保持全部可选
        await _skillMatchService.BuildExpandedMapAsync(tenantId);
        var matched = technicians
            .Where(t => t.Source == 2 || _skillMatchService.IsMatch(
                serviceSkillIds,
                t.TechnicianSkills.Select(ts => ts.SkillCategoryId)))
            .ToList();
        return ApiResponseDto<List<TechnicianDto>>.Ok(matched.Adapt<List<TechnicianDto>>());
    }

    /// <summary>
    /// 查询技师可服务的服务项目列表（技师页展示擅长项目）
    /// - 技师无技能标签：仅返回未限定技能的服务项目
    /// - 平台技师技能标签无法与当前门店技能分类对齐，按无技能标签处理
    /// </summary>
    public async Task<ApiResponseDto<List<TechnicianServiceItemDto>>> GetServiceProductsByTechnicianAsync(long technicianId)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<List<TechnicianServiceItemDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        // 校验技师归属（当前租户自有技师或平台技师）
        var technician = await _dbContext.Technicians
            .Include(t => t.TechnicianSkills)
            .FirstOrDefaultAsync(t => t.Id == technicianId && !t.IsDeleted
                && (t.TenantId == tenantId || t.TenantId == PlatformTenantId));
        if (technician == null)
            return ApiResponseDto<List<TechnicianServiceItemDto>>.Fail("技师不存在", 404);

        // 平台技师技能标签无法对齐当前门店技能分类，按无技能标签处理（仅可服务未限定技能的项目）
        var techSkillIds = technician.Source == 2
            ? new List<long>()
            : technician.TechnicianSkills.Select(ts => ts.SkillCategoryId).ToList();

        var services = await _dbContext.ServiceProducts
            .Where(s => !s.IsDeleted && s.TenantId == tenantId)
            .ToListAsync();
        if (services.Count == 0)
            return ApiResponseDto<List<TechnicianServiceItemDto>>.Ok(new List<TechnicianServiceItemDto>());

        // 当前门店为各服务项目配置的适用技能（按 StoreId 分组）
        var serviceIds = services.Select(s => s.Id).ToList();
        var skillRelations = await _dbContext.ServiceProductSkills
            .Where(r => serviceIds.Contains(r.ServiceProductId) && r.StoreId == storeId)
            .Select(r => new { r.ServiceProductId, r.SkillCategoryId })
            .ToListAsync();
        var skillByService = skillRelations.GroupBy(r => r.ServiceProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.SkillCategoryId).ToList());

        await _skillMatchService.BuildExpandedMapAsync(tenantId);

        // 批量填充商品主档名称与技能分类名称
        var masterIds = services.Select(s => s.MasterId).ToList();
        var masterNameMap = await _dbContext.ProductMasters
            .Where(m => masterIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        var allCategoryIds = skillRelations.Select(r => r.SkillCategoryId).Distinct().ToList();
        var categoryNameMap = allCategoryIds.Count > 0
            ? await _dbContext.SkillCategories
                .Where(c => allCategoryIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToDictionaryAsync(c => c.Id, c => c.Name)
            : new Dictionary<long, string>();

        var result = new List<TechnicianServiceItemDto>();
        foreach (var s in services)
        {
            var serviceSkillIds = skillByService.TryGetValue(s.Id, out var list) ? list : new List<long>();
            if (!_skillMatchService.IsMatch(serviceSkillIds, techSkillIds))
                continue;

            result.Add(new TechnicianServiceItemDto
            {
                ServiceProductId = s.Id,
                ProductId = s.MasterId,
                Name = masterNameMap.GetValueOrDefault(s.MasterId) ?? string.Empty,
                Duration = s.Duration,
                SkillCategoryIds = serviceSkillIds,
                SkillCategoryNames = serviceSkillIds
                    .Where(id => categoryNameMap.ContainsKey(id))
                    .Select(id => categoryNameMap[id])
                    .ToList()
            });
        }
        return ApiResponseDto<List<TechnicianServiceItemDto>>.Ok(result);
    }
}
