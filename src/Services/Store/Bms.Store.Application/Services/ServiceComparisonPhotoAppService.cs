using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.BuildingBlocks.Storage.Abstractions;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Customers;
using ServiceComparisonPhotoEntity = Bms.Store.Domain.Entities.ServiceComparisonPhoto;
using ServiceComparisonPhotoItemEntity = Bms.Store.Domain.Entities.ServiceComparisonPhotoItem;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

public class ServiceComparisonPhotoAppService : IServiceComparisonPhotoAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<ServiceComparisonPhotoCreateDto> _createValidator;
    private readonly IValidator<ServiceComparisonPhotoUpdateDto> _updateValidator;
    private readonly IFileReferenceResolver _fileReferenceResolver;
    private readonly IFileStorageService _fileStorageService;

    public ServiceComparisonPhotoAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<ServiceComparisonPhotoCreateDto> createValidator,
        IValidator<ServiceComparisonPhotoUpdateDto> updateValidator,
        IFileReferenceResolver fileReferenceResolver,
        IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _fileReferenceResolver = fileReferenceResolver;
        _fileStorageService = fileStorageService;
    }

    public async Task<ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>> GetPagedListAsync(ServiceComparisonPhotoQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var queryable = from p in _dbContext.ServiceComparisonPhotos
                        join c in _dbContext.Customers on p.CustomerId equals c.Id
                        where p.TenantId == tenantId && p.StoreId == storeId && !c.IsDeleted
                        select new { p, c };

        if (query.CustomerId.HasValue)
            queryable = queryable.Where(x => x.p.CustomerId == query.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(query.CustomerName))
            queryable = queryable.Where(x => x.c.Name.Contains(query.CustomerName));
        if (!string.IsNullOrWhiteSpace(query.CustomerPhone))
            queryable = queryable.Where(x => x.c.Phone.Contains(query.CustomerPhone));
        if (!string.IsNullOrWhiteSpace(query.ServiceItem))
            queryable = queryable.Where(x => x.p.ServiceItem != null && x.p.ServiceItem.Contains(query.ServiceItem));

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(x => x.p.PhotoDate)
            .ThenByDescending(x => x.p.CreatedTime)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ServiceComparisonPhotoDto
            {
                Id = x.p.Id,
                CustomerId = x.p.CustomerId,
                CustomerName = x.c.Name,
                CustomerPhone = x.c.Phone,
                OrderId = x.p.OrderId,
                ProductId = x.p.ProductId,
                ServiceItem = x.p.ServiceItem,
                PhotoDate = x.p.PhotoDate,
                PhotoType = x.p.PhotoType,
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
        await FillItemsAsync(items);

        var result = new PagedResponseDto<ServiceComparisonPhotoDto>
        {
            List = items,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<ServiceComparisonPhotoDto>>.Ok(result);
    }

    public async Task<ApiResponseDto<ServiceComparisonPhotoDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceComparisonPhotoDto?>.Fail("登录状态异常，请重新登录", 401);

        var dto = await GetDtoByIdAsync(id);
        if (dto == null)
            return ApiResponseDto<ServiceComparisonPhotoDto?>.Fail("服务对比照片不存在", 404);
        return ApiResponseDto<ServiceComparisonPhotoDto?>.Ok(dto);
    }

    public async Task<ApiResponseDto<ServiceComparisonPhotoDto>> CreateAsync(ServiceComparisonPhotoCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;

        // 校验关联订单归属当前租户（选填，未选时跳过）
        if (dto.OrderId.HasValue)
        {
            var orderExists = await _dbContext.Orders
                .AnyAsync(o => o.Id == dto.OrderId.Value && o.TenantId == tenantId && o.StoreId == storeId);
            if (!orderExists)
                return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("关联订单不存在", 400);
        }

        var entity = dto.Adapt<ServiceComparisonPhotoEntity>();

        // 选中服务项目后以商品主档名称覆盖 ServiceItem，固化为当时的名称快照
        if (dto.ProductId.HasValue)
        {
            var productName = await GetServiceProductNameAsync(dto.ProductId.Value, tenantId, storeId);
            if (productName == null)
                return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("服务项目不存在或不是服务类商品", 400);
            entity.ServiceItem = productName;
        }

        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.StoreId = storeId;
        entity.StoreCode = _currentUser.StoreCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        // 明细按提交顺序编号，前端拖拽调整顺序后重新提交即可生效
        var sortOrder = 0;
        foreach (var item in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ObjectKey))
                continue;
            entity.Items.Add(BuildItemEntity(item.ObjectKey!, sortOrder++, tenantId, storeId));
        }

        _dbContext.ServiceComparisonPhotos.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 保存后重新查询以填充客户姓名、手机号、订单号
        var result = await GetDtoByIdAsync(entity.Id);
        return ApiResponseDto<ServiceComparisonPhotoDto>.Ok(result!, "创建成功");
    }

    public async Task<ApiResponseDto<ServiceComparisonPhotoDto>> UpdateAsync(ServiceComparisonPhotoUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.ServiceComparisonPhotos
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == dto.Id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("服务对比照片不存在", 404);

        // 校验关联订单归属当前租户（选填，未选时跳过）
        if (dto.OrderId.HasValue)
        {
            var orderExists = await _dbContext.Orders
                .AnyAsync(o => o.Id == dto.OrderId.Value && o.TenantId == tenantId && o.StoreId == storeId);
            if (!orderExists)
                return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("关联订单不存在", 400);
        }

        // 未选服务项目时沿用传入的文字描述，兼容早期手工录入的历史数据
        var serviceItem = dto.ServiceItem;
        if (dto.ProductId.HasValue)
        {
            var productName = await GetServiceProductNameAsync(dto.ProductId.Value, tenantId, storeId);
            if (productName == null)
                return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("服务项目不存在或不是服务类商品", 400);
            serviceItem = productName;
        }

        entity.CustomerId = dto.CustomerId;
        entity.OrderId = dto.OrderId;
        entity.ProductId = dto.ProductId;
        entity.ServiceItem = serviceItem;
        entity.PhotoDate = dto.PhotoDate;
        entity.PhotoType = dto.PhotoType;
        entity.Remark = dto.Remark;
        entity.UpdatedTime = DateTime.Now;

        // 明细走差异更新：带 Id 表示保留已有照片（仅刷新排序），不带 Id 表示新增，
        // 已有明细未出现在提交列表中则删除。不做全删重建，是为了让保留的照片沿用原 objectKey，
        // 避免前端重新上传同一张图产生重复对象
        var keepIds = dto.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        var removedItems = entity.Items.Where(i => !keepIds.Contains(i.Id)).ToList();
        if (removedItems.Any())
            _dbContext.ServiceComparisonPhotoItems.RemoveRange(removedItems);

        var existingItems = entity.Items.ToDictionary(i => i.Id);
        var sortOrder = 0;
        foreach (var item in dto.Items)
        {
            if (item.Id.HasValue)
            {
                if (!existingItems.TryGetValue(item.Id.Value, out var existing))
                    return ApiResponseDto<ServiceComparisonPhotoDto>.Fail("照片明细不存在", 400);
                existing.SortOrder = sortOrder++;
                existing.UpdatedTime = DateTime.Now;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(item.ObjectKey))
                    continue;
                entity.Items.Add(BuildItemEntity(item.ObjectKey!, sortOrder++, tenantId, storeId));
            }
        }

        await _dbContext.SaveChangesAsync();

        // 数据库提交成功后才清理对象，顺序不可颠倒：先删文件若随后事务失败，界面会出现打不开的死图
        await DeleteObjectsAsync(removedItems.Select(i => i.PhotoSource));

        // 保存后重新查询以填充客户姓名、手机号、订单号
        var result = await GetDtoByIdAsync(entity.Id);
        return ApiResponseDto<ServiceComparisonPhotoDto>.Ok(result!, "更新成功");
    }

    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entity = await _dbContext.ServiceComparisonPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId && p.StoreId == storeId);
        if (entity == null)
            return ApiResponseDto.Fail("服务对比照片不存在", 404);

        // 明细由外键级联删除，实体删掉后就查不到来源了，故先取出待清理的对象键
        var sources = await _dbContext.ServiceComparisonPhotoItems
            .Where(i => i.ServiceComparisonPhotoId == id)
            .Select(i => i.PhotoSource)
            .ToListAsync();

        _dbContext.ServiceComparisonPhotos.Remove(entity);
        await _dbContext.SaveChangesAsync();
        await DeleteObjectsAsync(sources);
        return ApiResponseDto.Success(null, "删除成功");
    }

    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var entities = await _dbContext.ServiceComparisonPhotos
            .Where(p => ids.Contains(p.Id) && p.TenantId == tenantId && p.StoreId == storeId)
            .ToListAsync();

        // 只清理确实归属本店的主记录下的明细，避免请求里夹带别店 Id 时误删对象
        var ownedIds = entities.Select(p => p.Id).ToList();
        var sources = await _dbContext.ServiceComparisonPhotoItems
            .Where(i => ownedIds.Contains(i.ServiceComparisonPhotoId))
            .Select(i => i.PhotoSource)
            .ToListAsync();

        _dbContext.ServiceComparisonPhotos.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        await DeleteObjectsAsync(sources);
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 通过 JOIN Customer 查询完整 DTO（含客户姓名、手机号、订单号）
    /// </summary>
    private async Task<ServiceComparisonPhotoDto?> GetDtoByIdAsync(long id)
    {
        var tenantId = _currentUser.TenantId!.Value;
        var storeId = _currentUser.StoreId ?? 0;
        var dto = await (from p in _dbContext.ServiceComparisonPhotos
                         join c in _dbContext.Customers on p.CustomerId equals c.Id
                         where p.Id == id && p.TenantId == tenantId && p.StoreId == storeId && !c.IsDeleted
                         select new ServiceComparisonPhotoDto
                         {
                             Id = p.Id,
                             CustomerId = p.CustomerId,
                             CustomerName = c.Name,
                             CustomerPhone = c.Phone,
                             OrderId = p.OrderId,
                             ProductId = p.ProductId,
                             ServiceItem = p.ServiceItem,
                             PhotoDate = p.PhotoDate,
                             PhotoType = p.PhotoType,
                             Remark = p.Remark,
                             CreatedAt = p.CreatedTime,
                             UpdatedAt = p.UpdatedTime
                         }).FirstOrDefaultAsync();

        if (dto != null)
        {
            if (dto.OrderId.HasValue)
            {
                var orderNo = await _dbContext.Orders
                    .Where(o => o.Id == dto.OrderId.Value)
                    .Select(o => o.OrderNo)
                    .FirstOrDefaultAsync();
                dto.OrderNo = orderNo;
            }

            await FillProductNamesAsync(new List<ServiceComparisonPhotoDto> { dto });
            await FillItemsAsync(new List<ServiceComparisonPhotoDto> { dto });
        }

        return dto;
    }

    /// <summary>
    /// 构建照片明细实体，Id 与外键由 EF 保存时自动填充
    /// </summary>
    private ServiceComparisonPhotoItemEntity BuildItemEntity(string photoSource, int sortOrder, long tenantId, long storeId)
    {
        return new ServiceComparisonPhotoItemEntity
        {
            PhotoSource = photoSource,
            SortOrder = sortOrder,
            TenantId = tenantId,
            TenantCode = _currentUser.TenantCode ?? string.Empty,
            StoreId = storeId,
            StoreCode = _currentUser.StoreCode ?? string.Empty,
            CreatedTime = DateTime.Now
        };
    }

    /// <summary>
    /// 批量填充照片明细，按 SortOrder 升序
    /// 一次性查出所有主记录的明细再分组，避免逐条查询导致的 N+1
    /// </summary>
    private async Task FillItemsAsync(List<ServiceComparisonPhotoDto> photos)
    {
        if (!photos.Any())
            return;

        var photoIds = photos.Select(x => x.Id).ToList();
        var items = await _dbContext.ServiceComparisonPhotoItems
            .Where(i => photoIds.Contains(i.ServiceComparisonPhotoId))
            .OrderBy(i => i.SortOrder)
            .Select(i => new { i.ServiceComparisonPhotoId, i.Id, i.PhotoSource, i.SortOrder })
            .ToListAsync();

        // 整页照片一次性签发，逐条签发会让列表接口的耗时随照片数线性增长
        var resolved = _fileReferenceResolver.Resolve(
            items.Select(i => i.PhotoSource),
            new PresignScope { TenantId = _currentUser.TenantId ?? 0, StoreId = _currentUser.StoreId });

        var itemGroups = items
            .GroupBy(i => i.ServiceComparisonPhotoId)
            .ToDictionary(g => g.Key, g => g.Select(i => new ServiceComparisonPhotoItemDto
            {
                Id = i.Id,
                // 越权或无法解析的来源取不到地址，此时返回空串让前端显示占位图，不因脏数据整页报错
                PhotoUrl = resolved.TryGetValue(i.PhotoSource, out var url) ? url : string.Empty,
                SortOrder = i.SortOrder
            }).ToList());

        foreach (var photo in photos)
        {
            if (itemGroups.TryGetValue(photo.Id, out var list))
                photo.Items = list;
        }
    }

    /// <summary>
    /// 清理照片对象。外部直链不在对象存储内，其前缀无法通过越权校验会被自动跳过。
    /// 删除失败仅记日志不抛异常，业务数据已提交不应因此回滚
    /// </summary>
    private Task DeleteObjectsAsync(IEnumerable<string> photoSources)
    {
        return _fileStorageService.DeleteManyAsync(
            photoSources,
            new PresignScope { TenantId = _currentUser.TenantId ?? 0, StoreId = _currentUser.StoreId });
    }

    /// <summary>
    /// 批量填充服务项目商品当前名称
    /// 单独查询而非 LEFT JOIN，与订单号填充方式保持一致，避免 NULL 行干扰分页
    /// </summary>
    private async Task FillProductNamesAsync(List<ServiceComparisonPhotoDto> items)
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
