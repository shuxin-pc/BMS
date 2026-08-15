using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Abstractions;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Stores;
using Bms.Store.Domain.Entities;
using StoreEntity = Bms.Store.Domain.Entities.Store;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 门店管理应用服务实现
/// </summary>
public class StoreAppService : IStoreAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<StoreCreateDto> _createValidator;
    private readonly IValidator<StoreUpdateDto> _updateValidator;
    private readonly IUserQueryService _userQueryService;

    public StoreAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<StoreCreateDto> createValidator,
        IValidator<StoreUpdateDto> updateValidator,
        IUserQueryService userQueryService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _userQueryService = userQueryService;
    }

    /// <summary>
    /// 获取门店分页列表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<StoreDto>>> GetPagedListAsync(StoreQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<PagedResponseDto<StoreDto>>.Fail("登录状态异常，请重新登录", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var queryable = _dbContext.Stores
            .Where(s => !s.IsDeleted && s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            queryable = queryable.Where(s => s.Name.Contains(query.Name));
        }
        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            queryable = queryable.Where(s => s.Code.Contains(query.Code));
        }
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(s => s.Status == query.Status.Value);
        }

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<StoreDto>
        {
            List = items.Adapt<List<StoreDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<StoreDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取门店详情
    /// </summary>
    public async Task<ApiResponseDto<StoreDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<StoreDto?>.Fail("登录状态异常，请重新登录", 401);
        }

        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value);
        if (store == null)
        {
            return ApiResponseDto<StoreDto?>.Fail("门店不存在", 404);
        }
        return ApiResponseDto<StoreDto?>.Ok(store.Adapt<StoreDto>());
    }

    /// <summary>
    /// 获取当前用户授权的门店列表（仅返回启用状态的门店，用于门店切换器）
    /// super_admin/tenant_admin：返回本租户所有营业中门店（保持租户管理员默认全门店权限）
    /// 普通用户：仅返回被分配（UserStores 表）的营业中门店，未分配则返回空列表
    /// </summary>
    /// <summary>
    /// 获取当前用户授权可见的营业中门店列表。
    /// - super_admin：本租户所有营业中门店（平台管理员）
    /// - 其他用户（含 tenant_admin）：仅返回 UserStores 表中被分配的营业中门店
    ///   tenant_admin 的 UserStore 记录由门店创建时自动维护（AutoAssignTenantAdminUsersAsync）
    /// </summary>
    public async Task<ApiResponseDto<List<StoreDto>>> GetAuthorizedStoresAsync()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<List<StoreDto>>.Fail("登录状态异常，请重新登录", 401);
        }

        var tenantId = _currentUser.TenantId.Value;

        // super_admin：本租户所有营业中门店
        if (_currentUser.IsSuperAdmin)
        {
            var adminStores = await _dbContext.Stores
                .Where(s => !s.IsDeleted && s.TenantId == tenantId && s.Status == 1)
                .OrderBy(s => s.CreatedTime)
                .ToListAsync();
            return ApiResponseDto<List<StoreDto>>.Ok(adminStores.Adapt<List<StoreDto>>());
        }

        // 其他用户（含 tenant_admin）：仅返回被分配的营业中门店
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
        {
            return ApiResponseDto<List<StoreDto>>.Ok(new List<StoreDto>());
        }

        // 按 CreatedTime 升序排序：让最早创建的门店排第一位，配合前端 stores[0] 默认选中规则
        var stores = await (from s in _dbContext.Stores
                            join us in _dbContext.UserStores on s.Id equals us.StoreId
                            where !s.IsDeleted && s.TenantId == tenantId && s.Status == 1
                                  && !us.IsDeleted && us.UserId == userId.Value
                            orderby s.CreatedTime
                            select s).ToListAsync();

        return ApiResponseDto<List<StoreDto>>.Ok(stores.Adapt<List<StoreDto>>());
    }

    /// <summary>
    /// 创建门店
    /// </summary>
    public async Task<ApiResponseDto<StoreDto>> CreateAsync(StoreCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<StoreDto>.Fail("登录状态异常，请重新登录", 401);
        }

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<StoreDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var codeExists = await _dbContext.Stores
            .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted);
        if (codeExists)
        {
            return ApiResponseDto<StoreDto>.Fail($"门店编码 {dto.Code} 已存在", 400);
        }

        var store = dto.Adapt<StoreEntity>();
        store.TenantId = tenantId;
        store.TenantCode = _currentUser.TenantCode ?? string.Empty;
        store.CreatedTime = DateTime.Now;

        _dbContext.Stores.Add(store);
        await _dbContext.SaveChangesAsync();

        // 门店创建后，自动为本租户所有 tenant_admin 用户分配门店访问权限
        // tenant_admin 是租户管理员，应拥有本租户全部门店的访问权限，
        // 通过 UserStore 记录自动维护（非中间件硬编码放行），保持权限模型一致性
        await AutoAssignTenantAdminUsersAsync(store.Id, tenantId, _currentUser.TenantCode ?? string.Empty);

        return ApiResponseDto<StoreDto>.Ok(store.Adapt<StoreDto>(), "创建成功");
    }

    /// <summary>
    /// 更新门店
    /// </summary>
    public async Task<ApiResponseDto<StoreDto>> UpdateAsync(StoreUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<StoreDto>.Fail("登录状态异常，请重新登录", 401);
        }

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return ApiResponseDto<StoreDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);
        }

        var tenantId = _currentUser.TenantId.Value;
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted && s.TenantId == tenantId);
        if (store == null)
        {
            return ApiResponseDto<StoreDto>.Fail("门店不存在", 404);
        }

        // 编码变更时检查唯一性
        if (store.Code != dto.Code)
        {
            var codeExists = await _dbContext.Stores
                .AnyAsync(s => s.Code == dto.Code && s.TenantId == tenantId && !s.IsDeleted && s.Id != dto.Id);
            if (codeExists)
            {
                return ApiResponseDto<StoreDto>.Fail($"门店编码 {dto.Code} 已存在", 400);
            }
        }

        // 手动更新字段（避免覆盖 Id/TenantId 等审计字段）
        store.Name = dto.Name;
        store.Code = dto.Code;
        store.ShortName = dto.ShortName;
        store.Phone = dto.Phone;
        store.Address = dto.Address;
        store.BusinessHours = dto.BusinessHours;
        store.ManagerName = dto.ManagerName;
        store.Status = dto.Status;
        store.LogoUrl = dto.LogoUrl;
        store.BusinessLicenseUrl = dto.BusinessLicenseUrl;
        store.Remark = dto.Remark;
        store.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto<StoreDto>.Ok(store.Adapt<StoreDto>(), "更新成功");
    }

    /// <summary>
    /// 删除门店（软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var store = await _dbContext.Stores
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId);
        if (store == null)
        {
            return ApiResponseDto.Fail("门店不存在", 404);
        }

        store.IsDeleted = true;
        store.UpdatedTime = DateTime.Now;
        // 门店软删除时，同步软删除该门店的所有 UserStore 关联记录
        // 含 tenant_admin 自动分配的记录和普通用户手动分配的记录，避免悬挂关联
        await SoftDeleteUserStoresByStoreIdAsync(id, tenantId);
        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除门店（软删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        }

        if (ids == null || !ids.Any())
        {
            return ApiResponseDto.Fail("请选择要删除的门店", 400);
        }

        var stores = await _dbContext.Stores
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        foreach (var store in stores)
        {
            store.IsDeleted = true;
            store.UpdatedTime = DateTime.Now;
        }

        // 批量软删除门店时，同步软删除这些门店的所有 UserStore 关联记录
        if (stores.Any())
        {
            var storeIds = stores.Select(s => s.Id).ToList();
            var userStores = await _dbContext.UserStores
                .Where(us => storeIds.Contains(us.StoreId) && us.TenantId == _currentUser.TenantId.Value && !us.IsDeleted)
                .ToListAsync();
            var now = DateTime.Now;
            foreach (var us in userStores)
            {
                us.IsDeleted = true;
                us.UpdatedTime = now;
            }
        }

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, $"成功删除 {stores.Count} 个门店");
    }

    /// <summary>
    /// 获取门店已分配的用户列表
    /// </summary>
    public async Task<ApiResponseDto<List<TenantUserDto>>> GetAssignedUsersAsync(long storeId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<List<TenantUserDto>>.Fail("登录状态异常，请重新登录", 401);
        }

        var tenantId = _currentUser.TenantId.Value;

        // 校验门店属于当前租户（防止跨租户越权查询）
        var storeExists = await _dbContext.Stores
            .AnyAsync(s => s.Id == storeId && !s.IsDeleted && s.TenantId == tenantId);
        if (!storeExists)
        {
            return ApiResponseDto<List<TenantUserDto>>.Fail("门店不存在", 404);
        }

        var users = await _dbContext.UserStores
            .Where(us => us.StoreId == storeId && us.TenantId == tenantId && !us.IsDeleted)
            .OrderBy(us => us.UserId)
            .Select(us => new TenantUserDto
            {
                Id = us.UserId,
                UserName = us.UserName,
                RealName = us.RealName
            })
            .ToListAsync();

        return ApiResponseDto<List<TenantUserDto>>.Ok(users);
    }

    /// <summary>
    /// 获取门店可分配用户列表（本租户有效用户 + 标记是否已分配）
    /// </summary>
    public async Task<ApiResponseDto<List<AvailableUserDto>>> GetAvailableUsersAsync(long storeId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto<List<AvailableUserDto>>.Fail("登录状态异常，请重新登录", 401);
        }

        var tenantId = _currentUser.TenantId.Value;

        // 校验门店属于当前租户
        var storeExists = await _dbContext.Stores
            .AnyAsync(s => s.Id == storeId && !s.IsDeleted && s.TenantId == tenantId);
        if (!storeExists)
        {
            return ApiResponseDto<List<AvailableUserDto>>.Fail("门店不存在", 404);
        }

        // 跨服务查询本租户有效用户
        var tenantUsers = await _userQueryService.GetActiveUsersByTenantAsync(tenantId);

        // 过滤掉 tenant_admin 用户：tenant_admin 的门店分配由后端自动维护（创建门店时自动分配），
        // 不在弹窗中显示，避免手动操作破坏自动维护的一致性
        var tenantAdminUsers = await _userQueryService.GetTenantAdminUsersAsync(tenantId);
        var tenantAdminUserIds = tenantAdminUsers.Select(u => u.Id).ToHashSet();
        tenantUsers = tenantUsers.Where(u => !tenantAdminUserIds.Contains(u.Id)).ToList();

        // 查询该门店已分配的用户ID集合
        var assignedUserIds = await _dbContext.UserStores
            .Where(us => us.StoreId == storeId && us.TenantId == tenantId && !us.IsDeleted)
            .Select(us => us.UserId)
            .ToListAsync();
        var assignedSet = new HashSet<long>(assignedUserIds);

        var result = tenantUsers.Select(u => new AvailableUserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            RealName = u.RealName,
            Assigned = assignedSet.Contains(u.Id)
        }).ToList();

        return ApiResponseDto<List<AvailableUserDto>>.Ok(result);
    }

    /// <summary>
    /// 全量替换门店的用户分配（diff 计算：新增/删除）
    /// 仅 super_admin/tenant_admin 可调用
    /// </summary>
    public async Task<ApiResponseDto> AssignUsersAsync(long storeId, List<long> userIds)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        }

        var tenantId = _currentUser.TenantId.Value;
        var tenantCode = _currentUser.TenantCode ?? string.Empty;
        var operatorUserId = _currentUser.UserId ?? 0;

        // 校验门店属于当前租户
        var storeExists = await _dbContext.Stores
            .AnyAsync(s => s.Id == storeId && !s.IsDeleted && s.TenantId == tenantId);
        if (!storeExists)
        {
            return ApiResponseDto.Fail("门店不存在", 404);
        }

        // 防御性：去重 + 非空校验
        userIds = userIds?.Distinct().ToList() ?? new List<long>();

        // 待分配用户必须属于当前租户（防止跨租户分配）
        // 通过 IUserQueryService 获取本租户有效用户ID集合进行校验
        var tenantUsers = await _userQueryService.GetActiveUsersByTenantAsync(tenantId);
        var tenantUserMap = tenantUsers.ToDictionary(u => u.Id);
        var invalidUserIds = userIds.Where(id => !tenantUserMap.ContainsKey(id)).ToList();
        if (invalidUserIds.Any())
        {
            return ApiResponseDto.Fail($"以下用户不属于当前租户或已禁用：{string.Join(",", invalidUserIds)}", 400);
        }

        // 防御性校验：禁止手动分配/取消分配 tenant_admin 用户
        // tenant_admin 的门店权限由后端自动维护（创建门店时自动分配），手动操作会破坏一致性
        var tenantAdminUsers = await _userQueryService.GetTenantAdminUsersAsync(tenantId);
        var tenantAdminUserIds = tenantAdminUsers.Select(u => u.Id).ToHashSet();
        var protectedUserIds = userIds.Where(id => tenantAdminUserIds.Contains(id)).ToList();
        if (protectedUserIds.Any())
        {
            return ApiResponseDto.Fail("租户管理员的门店权限由系统自动维护，不支持手动分配", 400);
        }

        // 查询当前已分配记录（未删除）
        var existing = await _dbContext.UserStores
            .Where(us => us.StoreId == storeId && us.TenantId == tenantId && !us.IsDeleted)
            .ToListAsync();
        var existingUserIds = existing.Select(us => us.UserId).ToHashSet();

        // diff 计算
        var toAdd = userIds.Where(id => !existingUserIds.Contains(id)).ToList();
        var toRemove = existing.Where(us => !userIds.Contains(us.UserId)).ToList();

        var now = DateTime.Now;

        // 软删除取消分配的记录
        foreach (var us in toRemove)
        {
            us.IsDeleted = true;
            us.UpdatedTime = now;
            us.UpdatedBy = operatorUserId;
        }

        // 新增分配记录（冗余 UserName/RealName 便于列表展示）
        foreach (var userId in toAdd)
        {
            var tenantUser = tenantUserMap[userId];
            _dbContext.UserStores.Add(new UserStore
            {
                UserId = userId,
                StoreId = storeId,
                UserName = tenantUser.UserName,
                RealName = tenantUser.RealName,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now,
                CreatedBy = operatorUserId
            });
        }

        await _dbContext.SaveChangesAsync();

        return ApiResponseDto.Success(null, $"分配成功：新增 {toAdd.Count} 人，移除 {toRemove.Count} 人");
    }

    /// <summary>
    /// 自动为本租户所有 tenant_admin 用户分配指定门店的访问权限。
    /// 在门店创建时调用，保持"租户管理员拥有本租户全部门店访问权限"的语义，
    /// 通过 UserStore 记录维护（非中间件硬编码放行）。
    /// </summary>
    private async Task AutoAssignTenantAdminUsersAsync(long storeId, long tenantId, string tenantCode)
    {
        var tenantAdminUsers = await _userQueryService.GetTenantAdminUsersAsync(tenantId);
        if (!tenantAdminUsers.Any())
        {
            return;
        }

        var now = DateTime.Now;
        foreach (var user in tenantAdminUsers)
        {
            _dbContext.UserStores.Add(new UserStore
            {
                UserId = user.Id,
                StoreId = storeId,
                UserName = user.UserName,
                RealName = user.RealName,
                TenantId = tenantId,
                TenantCode = tenantCode,
                CreatedTime = now,
                CreatedBy = _currentUser.UserId ?? 0
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 软删除指定门店的所有 UserStore 关联记录。
    /// 在门店删除时调用，避免门店删除后 UserStore 关联悬挂。
    /// </summary>
    private async Task SoftDeleteUserStoresByStoreIdAsync(long storeId, long tenantId)
    {
        var userStores = await _dbContext.UserStores
            .Where(us => us.StoreId == storeId && us.TenantId == tenantId && !us.IsDeleted)
            .ToListAsync();

        var now = DateTime.Now;
        var operatorUserId = _currentUser.UserId ?? 0;
        foreach (var us in userStores)
        {
            us.IsDeleted = true;
            us.UpdatedTime = now;
            us.UpdatedBy = operatorUserId;
        }
    }
}
