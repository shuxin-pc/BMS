using Mapster;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Technicians;
using TechnicianStatisticEntity = Bms.Store.Domain.Entities.TechnicianStatistic;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技师统计应用服务实现
/// </summary>
public class TechnicianStatisticAppService : ITechnicianStatisticAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<TechnicianStatisticCreateDto> _createValidator;
    private readonly IValidator<TechnicianStatisticUpdateDto> _updateValidator;

    public TechnicianStatisticAppService(
        StoreDbContext dbContext,
        ICurrentUser currentUser,
        IValidator<TechnicianStatisticCreateDto> createValidator,
        IValidator<TechnicianStatisticUpdateDto> updateValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// 获取技师统计分页列表
    /// 仅返回商家技师（Source=1）的统计记录，平台技师（Source=2）线下结算不纳入门店报表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<TechnicianStatisticDto>>> GetPagedListAsync(TechnicianStatisticQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<TechnicianStatisticDto>>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;
        // join Technician 过滤 Source=1（商家技师），隐藏平台技师统计记录
        var queryable = from s in _dbContext.TechnicianStatistics
                        join t in _dbContext.Technicians on s.TechnicianId equals t.Id
                        where s.TenantId == tenantId && !t.IsDeleted && t.Source == 1
                        select s;

        if (query.TechnicianId.HasValue)
            queryable = queryable.Where(s => s.TechnicianId == query.TechnicianId.Value);
        if (query.StartDate.HasValue)
            queryable = queryable.Where(s => s.StatDate >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            queryable = queryable.Where(s => s.StatDate <= query.EndDate.Value);

        var total = await queryable.CountAsync();
        var items = await queryable
            .OrderByDescending(s => s.StatDate)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResponseDto<TechnicianStatisticDto>
        {
            List = items.Adapt<List<TechnicianStatisticDto>>(),
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<PagedResponseDto<TechnicianStatisticDto>>.Ok(result);
    }

    /// <summary>
    /// 根据ID获取技师统计详情
    /// 仅返回商家技师（Source=1）的统计记录，平台技师（Source=2）不纳入门店报表
    /// </summary>
    public async Task<ApiResponseDto<TechnicianStatisticDto?>> GetByIdAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianStatisticDto?>.Fail("登录状态异常，请重新登录", 401);

        // join Technician 过滤 Source=1（商家技师），隐藏平台技师统计记录
        var entity = await (from s in _dbContext.TechnicianStatistics
                            join t in _dbContext.Technicians on s.TechnicianId equals t.Id
                            where s.Id == id && s.TenantId == _currentUser.TenantId.Value
                                  && !t.IsDeleted && t.Source == 1
                            select s).FirstOrDefaultAsync();
        if (entity == null)
            return ApiResponseDto<TechnicianStatisticDto?>.Fail("技师统计不存在", 404);
        return ApiResponseDto<TechnicianStatisticDto?>.Ok(entity.Adapt<TechnicianStatisticDto>());
    }

    /// <summary>
    /// 创建技师统计
    /// </summary>
    public async Task<ApiResponseDto<TechnicianStatisticDto>> CreateAsync(TechnicianStatisticCreateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianStatisticDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TechnicianStatisticDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = dto.Adapt<TechnicianStatisticEntity>();
        entity.TenantId = tenantId;
        entity.TenantCode = _currentUser.TenantCode ?? string.Empty;
        entity.CreatedTime = DateTime.Now;

        _dbContext.TechnicianStatistics.Add(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TechnicianStatisticDto>.Ok(entity.Adapt<TechnicianStatisticDto>(), "创建成功");
    }

    /// <summary>
    /// 更新技师统计
    /// </summary>
    public async Task<ApiResponseDto<TechnicianStatisticDto>> UpdateAsync(TechnicianStatisticUpdateDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianStatisticDto>.Fail("登录状态异常，请重新登录", 401);

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ApiResponseDto<TechnicianStatisticDto>.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)), 400);

        var tenantId = _currentUser.TenantId.Value;
        var entity = await _dbContext.TechnicianStatistics
            .FirstOrDefaultAsync(s => s.Id == dto.Id && s.TenantId == tenantId);
        if (entity == null)
            return ApiResponseDto<TechnicianStatisticDto>.Fail("技师统计不存在", 404);

        entity.TechnicianId = dto.TechnicianId;
        entity.StatDate = dto.StatDate;
        entity.ServiceCount = dto.ServiceCount;
        entity.ServiceMinutes = dto.ServiceMinutes;
        entity.TotalTechnicianFee = dto.TotalTechnicianFee;
        entity.ReturnCustomerCount = dto.ReturnCustomerCount;
        entity.UpdatedTime = DateTime.Now;

        await _dbContext.SaveChangesAsync();
        return ApiResponseDto<TechnicianStatisticDto>.Ok(entity.Adapt<TechnicianStatisticDto>(), "更新成功");
    }

    /// <summary>
    /// 删除技师统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> DeleteAsync(long id)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);

        var entity = await _dbContext.TechnicianStatistics
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == _currentUser.TenantId.Value);
        if (entity == null)
            return ApiResponseDto.Fail("技师统计不存在", 404);

        _dbContext.TechnicianStatistics.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, "删除成功");
    }

    /// <summary>
    /// 批量删除技师统计（物理删除）
    /// </summary>
    public async Task<ApiResponseDto> BatchDeleteAsync(List<long> ids)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto.Fail("登录状态异常，请重新登录", 401);
        if (ids == null || !ids.Any())
            return ApiResponseDto.Fail("请选择要删除的数据", 400);

        var entities = await _dbContext.TechnicianStatistics
            .Where(s => ids.Contains(s.Id) && s.TenantId == _currentUser.TenantId.Value)
            .ToListAsync();

        _dbContext.TechnicianStatistics.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
        return ApiResponseDto.Success(null, $"成功删除 {entities.Count} 条数据");
    }

    /// <summary>
    /// 获取技师业绩统计报表（从 OrderItem 实时聚合，仅商家技师）
    /// 数据来源：OrderItem 关联 Order（取 OrderTime/CustomerId）+ ServiceProduct（取 Duration）
    /// 过滤条件：TenantId + TechnicianId 非空 + TechnicianSource=1（商家技师）+ Order.Status=2或3（已完成/已退款，与日结营收口径一致）
    /// 回头客定义：统计期间内同一客户被同一技师服务 >1 次
    /// 纯平台技师门店（无自有技师）：返回 IsPurePlatformStore=true + 空列表，业绩由平台统一统计，门店端不展示
    /// 管理员可通过 query.Force=true 强制查看（仅供运维排查）
    /// </summary>
    public async Task<ApiResponseDto<TechnicianStatReportDto>> GetReportAsync(TechnicianStatisticQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<TechnicianStatReportDto>.Fail("登录状态异常，请重新登录", 401);

        var tenantId = _currentUser.TenantId.Value;

        // 0. 纯平台技师门店判断：若门店无自有技师（Source=1）且未强制查看，返回隐藏标识
        // 业绩由平台统一统计，门店端不展示（避免商家误以为门店业绩为 0）
        if (!query.Force)
        {
            var hasOwnTechnician = await _dbContext.Technicians
                .AnyAsync(t => !t.IsDeleted && t.TenantId == tenantId && t.Source == 1);
            if (!hasOwnTechnician)
            {
                return ApiResponseDto<TechnicianStatReportDto>.Ok(new TechnicianStatReportDto
                {
                    IsPurePlatformStore = true,
                    Message = "本门店为纯平台技师门店，业绩报表已隐藏",
                    Items = new List<TechnicianStatisticReportDto>(),
                    Total = 0,
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize
                });
            }
        }

        // 1. 主查询：OrderItem 关联 Order（取 OrderTime/CustomerId）+ ServiceProduct（取 Duration，左连接）
        // ServiceProduct 关联字段从 ProductId 改为 MasterId（设计文档 3.4 节）
        // 通过 Product.MasterId 桥接 OrderItem.ProductId 与 ServiceProduct.MasterId
        var baseQuery = from oi in _dbContext.OrderItems
                        join o in _dbContext.Orders on oi.OrderId equals o.Id
                        join p in _dbContext.Products on oi.ProductId equals p.Id
                        join sp in _dbContext.ServiceProducts on p.MasterId equals sp.MasterId into spGroup
                        from sp in spGroup.DefaultIfEmpty()
                        where oi.TenantId == tenantId
                              && oi.StoreId == (_currentUser.StoreId ?? 0)  // 按当前门店过滤（与归集表口径一致，多门店隔离）
                              && oi.TechnicianId.HasValue
                              && oi.TechnicianSource == 1  // 仅商家技师，平台技师线下结算不纳入
                              && o.Status == 2  // 仅已完成订单（与归集表口径一致，已退款/已取消不纳入）
                              && (!query.StartDate.HasValue || o.OrderTime >= query.StartDate.Value)
                              && (!query.EndDate.HasValue || o.OrderTime <= query.EndDate.Value)
                              && (!query.TechnicianId.HasValue || oi.TechnicianId == query.TechnicianId.Value)
                        select new { oi, o, sp };

        // 2. 按技师分组聚合主指标
        var groupedQuery = from x in baseQuery
                           group x by x.oi.TechnicianId!.Value into g
                           select new TechnicianStatisticReportDto
                           {
                               TechnicianId = g.Key,
                               ServiceCount = g.Count(),
                               ServiceMinutes = g.Sum(x => x.sp != null && x.sp.Duration.HasValue
                                   ? (int)(x.oi.Quantity * x.sp.Duration.Value)
                                   : 0),
                               TotalTechnicianFee = g.Sum(x => x.oi.TechnicianFee ?? 0),
                               TotalCustomerCount = g.Select(x => x.o.CustomerId).Distinct().Count()
                           };

        // 3. 回头客子查询：统计期间内同一技师+同一客户出现 >1 次
        // EF Core 不支持"分组后再分组"（nested group by），无法翻译为 SQL，
        // 故先在数据库按(技师Id,客户Id)分组过滤取出回头客组合，再在内存中按技师聚合计数
        var returnCustomerPairs = await (from x in baseQuery
                                         where x.o.CustomerId.HasValue
                                         group x by new { TechnicianId = x.oi.TechnicianId!.Value, CustomerId = x.o.CustomerId!.Value } into g
                                         where g.Count() > 1
                                         select new { TechnicianId = g.Key.TechnicianId, CustomerId = g.Key.CustomerId })
                                         .ToListAsync();

        // 4. 执行查询：分页取主指标
        var totalCount = await groupedQuery.CountAsync();
        var mainStats = await groupedQuery
            .OrderByDescending(s => s.ServiceCount)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // 4.1 按当前页技师ID在内存中聚合回头客数量
        var technicianIds = mainStats.Select(ms => ms.TechnicianId).ToList();
        var returnStats = returnCustomerPairs
            .Where(p => technicianIds.Contains(p.TechnicianId))
            .GroupBy(p => p.TechnicianId)
            .Select(rg => new { TechnicianId = rg.Key, ReturnCustomerCount = rg.Count() })
            .ToList();

        // 5. 关联技师姓名 + 合并回头客数据
        var technicians = await _dbContext.Technicians
            .Where(t => technicianIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        foreach (var stat in mainStats)
        {
            stat.TechnicianName = technicians.FirstOrDefault(t => t.Id == stat.TechnicianId)?.Name ?? "未知";
            stat.ReturnCustomerCount = returnStats.FirstOrDefault(rc => rc.TechnicianId == stat.TechnicianId)?.ReturnCustomerCount ?? 0;
        }

        var result = new TechnicianStatReportDto
        {
            IsPurePlatformStore = false,
            Items = mainStats,
            Total = totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
        return ApiResponseDto<TechnicianStatReportDto>.Ok(result);
    }

    /// <summary>
    /// 按"技师+日期"全量重算技师统计并 upsert（自动归集算法）
    /// 口径与 GetReportAsync 一致：同门店 + 仅商家技师(Source=1) + 仅已完成订单(Status=2) + 服务时间日期==statDate（为空回退下单日）
    /// 天然幂等：重算即回退（退款=3/取消=4/冲正=4 后订单不再满足 Status=2，重算结果自然剔除，无需区分增/减）
    /// 定位键 TenantId+StoreId+TechnicianId+StatDate 保证幂等（应用层 FirstOrDefault，不加数据库唯一索引）
    /// 无有效明细则删除该统计记录
    /// </summary>
    public async Task RecalculateTechnicianStatisticAsync(long tenantId, long storeId, long technicianId, DateTime statDate)
    {
        var statDateVal = statDate.Date;

        // 1. 查询 statDate 当天该技师的有效服务明细（口径与 GetReportAsync 对齐）
        // ServiceProduct 关联字段经 Product.MasterId 桥接（OrderItem.ProductId -> Product.MasterId -> ServiceProduct.MasterId）
        var items = await (from oi in _dbContext.OrderItems
                           join o in _dbContext.Orders on oi.OrderId equals o.Id
                           join p in _dbContext.Products on oi.ProductId equals p.Id
                           join sp in _dbContext.ServiceProducts on p.MasterId equals sp.MasterId into spGroup
                           from sp in spGroup.DefaultIfEmpty()
                           where oi.TenantId == tenantId
                                 && oi.StoreId == storeId
                                 && oi.TechnicianId == technicianId
                                 && oi.TechnicianSource == 1  // 仅商家技师
                                 && o.Status == 2             // 仅已完成（全额退款=3/取消=4/冲正=4 天然排除，部分退款仍 2 按全额计入）
                                 && (oi.ServiceStartTime != null ? oi.ServiceStartTime.Value.Date : o.OrderTime.Date) == statDateVal
                           select new { oi, o, sp }).ToListAsync();

        // 2. 计算指标
        var serviceCount = items.Count;
        var serviceMinutes = items.Sum(x => x.sp != null && x.sp.Duration.HasValue
            ? (int)(x.oi.Quantity * x.sp.Duration.Value)
            : 0);
        var totalTechnicianFee = items.Sum(x => x.oi.TechnicianFee ?? 0);
        // 回头客 = 同一天内同一技师服务同一客户出现 >1 次的客户组数
        var returnCustomerCount = items
            .Where(x => x.o.CustomerId.HasValue)
            .GroupBy(x => x.o.CustomerId!.Value)
            .Count(g => g.Count() > 1);

        // 3. upsert：定位键 TenantId+StoreId+TechnicianId+StatDate
        var existing = await _dbContext.TechnicianStatistics
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.StoreId == storeId
                && s.TechnicianId == technicianId && s.StatDate == statDateVal);

        if (serviceCount == 0)
        {
            // 无有效明细则删除该记录（重算即回退）
            if (existing != null)
            {
                _dbContext.TechnicianStatistics.Remove(existing);
                await _dbContext.SaveChangesAsync();
            }
            return;
        }

        if (existing == null)
        {
            existing = new TechnicianStatisticEntity
            {
                TenantId = tenantId,
                TechnicianId = technicianId,
                StatDate = statDateVal,
                CreatedTime = DateTime.Now
            };
            _dbContext.TechnicianStatistics.Add(existing);
        }

        existing.ServiceCount = serviceCount;
        existing.ServiceMinutes = serviceMinutes;
        existing.TotalTechnicianFee = totalTechnicianFee;
        existing.ReturnCustomerCount = returnCustomerCount;
        existing.UpdatedTime = DateTime.Now;
        await _dbContext.SaveChangesAsync();
    }
}
