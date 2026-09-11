using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 客户服务反应记录应用服务实现
/// 按 TenantId + StoreId 隔离，JOIN Customer/Order 返回展示字段
/// </summary>
public class ServiceReactionAppService : IServiceReactionAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ServiceReactionCreateDto> _createValidator;
    private readonly IValidator<ServiceReactionUpdateDto> _updateValidator;

    public ServiceReactionAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ServiceReactionCreateDto> createValidator,
        IValidator<ServiceReactionUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取服务反应记录分页列表
    /// 支持按客户姓名、反应日期范围、严重程度过滤
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ServiceReactionDto>>> GetPagedListAsync(ServiceReactionQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ServiceReactionDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = from p in _dbContext.ServiceReactions
                        join c in _dbContext.Customers on p.CustomerId equals c.Id
                        where p.TenantId == tenantId && p.StoreId == storeId && !c.IsDeleted
                        select new { p, c };

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.p.CustomerId == query.CustomerId.Value);
        // 客户名称/手机号合并关键字查询：命中姓名或手机号其一即满足（对齐预约列表）
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            queryable = queryable.Where(x => x.c.Name.Contains(query.Keyword) || x.c.Phone.Contains(query.Keyword));
        if (query.StartDate.HasValue)
            queryable = queryable.Where(x => x.p.ReactionDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(x => x.p.ReactionDate <= query.EndDate.Value);
        if (query.Severity.HasValue)
            queryable = queryable.Where(x => x.p.Severity == query.Severity.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.p.ReactionDate)
            .ThenByDescending(x => x.p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ServiceReactionDto
            {
                Id = x.p.Id,
                CustomerId = x.p.CustomerId,
                CustomerName = x.c.Name,
                CustomerPhone = x.c.Phone,
                OrderId = x.p.OrderId,
                ProductId = x.p.ProductId,
                ServiceItem = x.p.ServiceItem,
                ReactionDate = x.p.ReactionDate,
                Reaction = x.p.Reaction,
                Severity = x.p.Severity,
                Remark = x.p.Remark,
                CreatedAt = x.p.CreatedTime,
                UpdatedAt = x.p.UpdatedTime
            })
            .ToListAsync();

        // 订单号单独查询后填充，避免 LEFT JOIN 带来 NULL 行干扰分页
        var orderIds = items.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
        if (orderIds.Any())
        {
            var orderDict = await _dbContext.Orders
                .Where(o => orderIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.OrderNo);
            foreach (var item in items)
            {
                if (item.OrderId.HasValue && orderDict.TryGetValue(item.OrderId.Value, out var orderNo))
                    item.OrderNo = orderNo;
            }
        }

        await FillProductNamesAsync(items);

        var result = new PagedResponseDto<ServiceReactionDto>
        {
            List = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ServiceReactionDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取服务反应记录详情
    /// </summary>
    public async Task<ApiResponseDto<ServiceReactionDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceReactionDto?>.Fail("登录状态异常，请重新登录", 401);

        var dto = await GetDtoByIdAsync(id);
        if (dto == null)
            return ApiResponseDto<ServiceReactionDto?>.Fail("服务反应记录不存在", 404);
        return ApiResponseDto<ServiceReactionDto?>.Ok(dto);
    }

    /// <summary>
    /// 创建服务反应记录
    /// </summary>
    public async Task<ApiResponseDto<ServiceReactionDto>> CreateAsync(ServiceReactionCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceReactionDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceReactionDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        if (dto.OrderId.HasValue)
        {
            var orderExists = await _dbContext.Orders
                .AnyAsync(o => o.Id == dto.OrderId.Value && o.TenantId == tenantId && o.StoreId == storeId);
            if (!orderExists)
                return ApiResponseDto<ServiceReactionDto>.Fail("关联订单不存在", 400);
        }

        var entity = dto.Adapt<ServiceReaction>();

        // 选中服务项目后以商品主档名称覆盖 ServiceItem，固化为当时的名称快照
        if (dto.ProductId.HasValue)
        {
            var productName = await GetServiceProductNameAsync(dto.ProductId.Value, tenantId, storeId);
            if (productName == null)
                return ApiResponseDto<ServiceReactionDto>.Fail("服务项目不存在或不是服务类商品", 400);
            entity.ServiceItem = productName;
        }

        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.ServiceReactions.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 保存后重新查询以填充客户姓名、手机号、订单号
        var result = await GetDtoByIdAsync(entity.Id);
        return ApiResponseDto<ServiceReactionDto>.Ok(result!, "创建成功");
    }

    /// <summary>
    /// 更新服务反应记录
    /// </summary>
    public async Task<ApiResponseDto<ServiceReactionDto>> UpdateAsync(ServiceReactionUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceReactionDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceReactionDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.ServiceReactions
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<ServiceReactionDto>.Fail("服务反应记录不存在", 404);

        if (dto.OrderId.HasValue)
        {
            var orderExists = await _dbContext.Orders
                .AnyAsync(o => o.Id == dto.OrderId.Value && o.TenantId == tenantId && o.StoreId == storeId);
            if (!orderExists)
                return ApiResponseDto<ServiceReactionDto>.Fail("关联订单不存在", 400);
        }

        // 未选服务项目时沿用传入的文字描述，兼容早期手工录入的历史数据
        var serviceItem = dto.ServiceItem;
        if (dto.ProductId.HasValue)
        {
            var productName = await GetServiceProductNameAsync(dto.ProductId.Value, tenantId, storeId);
            if (productName == null)
                return ApiResponseDto<ServiceReactionDto>.Fail("服务项目不存在或不是服务类商品", 400);
            serviceItem = productName;
        }

        entity.CustomerId = dto.CustomerId;
        entity.OrderId = dto.OrderId;
        entity.ProductId = dto.ProductId;
        entity.ServiceItem = serviceItem;
        entity.ReactionDate = dto.ReactionDate;
        entity.Reaction = dto.Reaction;
        entity.Severity = dto.Severity;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();

        // 保存后重新查询以填充客户姓名、手机号、订单号
        var result = await GetDtoByIdAsync(entity.Id);
        return ApiResponseDto<ServiceReactionDto>.Ok(result!, "更新成功");
    }

    /// <summary>
    /// 删除服务反应记录（物理删除，非核心表无软删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.ServiceReactions
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto.Fail("服务反应记录不存在", 404);

        _dbContext.ServiceReactions.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除服务反应记录（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entities = await _dbContext.ServiceReactions
            .Where(p => ids.Contains(p.Id) && p.TenantId == tenantId && p.StoreId == storeId)
            .ToListAsync();

        _dbContext.ServiceReactions.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 通过 JOIN Customer 查询完整 DTO（含客户姓名、手机号、订单号）
    /// </summary>
    private async Task<ServiceReactionDto?> GetDtoByIdAsync(long id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var dto = await (from p in _dbContext.ServiceReactions
                         join c in _dbContext.Customers on p.CustomerId equals c.Id
                         where p.Id == id && p.TenantId == tenantId && p.StoreId == storeId && !c.IsDeleted
                         select new ServiceReactionDto
                         {
                             Id = p.Id,
                             CustomerId = p.CustomerId,
                             CustomerName = c.Name,
                             CustomerPhone = c.Phone,
                             OrderId = p.OrderId,
                             ProductId = p.ProductId,
                             ServiceItem = p.ServiceItem,
                             ReactionDate = p.ReactionDate,
                             Reaction = p.Reaction,
                             Severity = p.Severity,
                             Remark = p.Remark,
                             CreatedAt = p.CreatedTime,
                             UpdatedAt = p.UpdatedTime
                         }).FirstOrDefaultAsync();

        if (dto?.OrderId.HasValue == true)
        {
            var orderNo = await _dbContext.Orders
                .Where(o => o.Id == dto.OrderId.Value)
                .Select(o => o.OrderNo)
                .FirstOrDefaultAsync();
            dto.OrderNo = orderNo;
        }

        if (dto != null)
            await FillProductNamesAsync(new List<ServiceReactionDto> { dto });

        return dto;
    }

    /// <summary>
    /// 批量填充服务项目商品当前名称
    /// 单独查询而非 LEFT JOIN，与订单号填充方式保持一致，避免 NULL 行干扰分页
    /// </summary>
    private async Task FillProductNamesAsync(List<ServiceReactionDto> items)
    {
        var productIds = items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToList();
        if (!productIds.Any())
            return;

        var nameDict = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .Join(_dbContext.ProductMasters,
                p => p.MasterId,
                m => m.Id,
                (p, m) => new { p.Id, m.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        foreach (var item in items)
        {
            if (item.ProductId.HasValue && nameDict.TryGetValue(item.ProductId.Value, out var name))
                item.ProductName = name;
        }
    }

    /// <summary>
    /// 校验服务项目商品归属当前门店且为服务类商品，返回其主档名称
    /// </summary>
    /// <returns>主档名称；商品不存在、不属于本店或非服务类商品时返回 null</returns>
    private async Task<string?> GetServiceProductNameAsync(long productId, long tenantId, long storeId)
    {
        return await _dbContext.Products
            .Where(p => p.Id == productId && !p.IsDeleted && p.TenantId == tenantId && p.StoreId == storeId)
            .Join(_dbContext.ProductMasters.Where(m => m.Type == 2),
                p => p.MasterId,
                m => m.Id,
                (p, m) => m.Name)
            .FirstOrDefaultAsync();
    }
}
