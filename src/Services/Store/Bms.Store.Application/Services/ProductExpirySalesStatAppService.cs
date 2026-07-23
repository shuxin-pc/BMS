using Mapster;
using Microsoft.EntityFrameworkCore;
using Bms.BuildingBlocks.Abstractions.Security;
using Bms.Store.Application.Dtos;
using Bms.Store.Application.Dtos.Statistics;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 效期销售统计服务实现（B5.5）
/// </summary>
/// <remarks>
/// 数据源：OrderItemBatch 表（已扣除 RefundedQuantity）
/// 关联 Order（过滤已退款/已取消）、Product（商品信息+类型）、ProductCategory（分类名称）、OrderItem（销售单价）
/// 效期区间：基于 ExpirationDate - Order.OrderTime 的剩余天数划分 5 档
/// </remarks>
public class ProductExpirySalesStatAppService : IProductExpirySalesStatAppService
{
    private readonly StoreDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ProductExpirySalesStatAppService(StoreDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取效期销售统计报表
    /// </summary>
    public async Task<ApiResponseDto<PagedResponseDto<ProductExpirySalesStatDto>>> GetReportAsync(ProductExpirySalesQueryDto query)
    {
        if (!_currentUser.TenantId.HasValue)
            return ApiResponseDto<PagedResponseDto<ProductExpirySalesStatDto>>.Fail("无法确定当前租户", 401);

        var tenantId = _currentUser.TenantId.Value;
        var storeId = query.StoreId ?? _currentUser.StoreId;

        // 默认时间范围：本月1日至今
        var today = DateTime.Today;
        var startDate = query.StartDate ?? new DateTime(today.Year, today.Month, 1);
        var endDate = query.EndDate ?? today;

        // 查询 OrderItemBatch 关联数据
        var rawData = await (
            from oib in _dbContext.OrderItemBatches
            join o in _dbContext.Orders on oib.OrderId equals o.Id
            join p in _dbContext.Products on oib.ProductId equals p.Id
            join oi in _dbContext.OrderItems on oib.OrderItemId equals oi.Id
            join c in _dbContext.ProductCategories on p.CategoryId equals c.Id into cats
            from c in cats.DefaultIfEmpty()
            where o.TenantId == tenantId
                && o.Status != 3 // 排除已退款
                && o.Status != 4 // 排除已取消
                && oib.ExpirationDate.HasValue
                && (storeId == null || o.StoreId == storeId)
                && (query.ProductType == null || p.Type == query.ProductType.Value)
                && (query.CategoryId == null || p.CategoryId == query.CategoryId.Value)
                && o.OrderTime >= startDate
                && o.OrderTime < endDate.AddDays(1)
            select new
            {
                oib.ProductId,
                ProductName = p.Name,
                ProductCode = p.Code,
                ProductType = p.Type,
                CategoryId = (long?)p.CategoryId,
                CategoryName = c != null ? c.Name : null,
                ExpirationDate = oib.ExpirationDate.Value,
                OrderId = oib.OrderId,
                OrderTime = o.OrderTime,
                // 实际销售数量（扣除已退款）
                SalesQuantity = oib.Quantity - oib.RefundedQuantity,
                // 销售单价（来自 OrderItem.Price）
                UnitPrice = oi.Price
            }
        ).ToListAsync();

        // 过滤掉退款后数量为0的记录
        rawData = rawData.Where(x => x.SalesQuantity > 0).ToList();

        // 内存计算效期区间并分组
        var grouped = rawData
            .Select(x => new
            {
                x.ProductId,
                x.ProductName,
                x.ProductCode,
                x.ProductType,
                x.CategoryId,
                x.CategoryName,
                x.ExpirationDate,
                x.OrderId,
                x.SalesQuantity,
                x.UnitPrice,
                ExpiryBucket = CalculateExpiryBucket(x.ExpirationDate, x.OrderTime)
            })
            .Where(x => query.ExpiryBucket == null || x.ExpiryBucket == query.ExpiryBucket.Value)
            // 关键词过滤（在分桶前做也可以，这里放在分组前）
            .Where(x => string.IsNullOrEmpty(query.Keyword)
                || x.ProductName.Contains(query.Keyword)
                || x.ProductCode.Contains(query.Keyword))
            .GroupBy(x => new { x.ProductId, x.ExpiryBucket })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ExpiryBucket,
                // 取第一条的商品信息（同一商品同一区间信息一致）
                ProductName = g.First().ProductName,
                ProductCode = g.First().ProductCode,
                ProductType = g.First().ProductType,
                CategoryId = g.First().CategoryId,
                CategoryName = g.First().CategoryName,
                ExpirationDateFrom = g.Min(x => x.ExpirationDate),
                ExpirationDateTo = g.Max(x => x.ExpirationDate),
                SalesQuantity = g.Sum(x => x.SalesQuantity),
                SalesAmount = g.Sum(x => x.SalesQuantity * x.UnitPrice),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count()
            })
            .ToList();

        // 计算各商品总销售数量（用于占比）
        var productTotals = grouped
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SalesQuantity));

        // 映射到 DTO
        var dtos = grouped.Select(x => new ProductExpirySalesStatDto
        {
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            ProductCode = x.ProductCode,
            ProductType = x.ProductType,
            ProductTypeName = GetProductTypeName(x.ProductType),
            CategoryId = x.CategoryId,
            CategoryName = x.CategoryName,
            ExpiryBucket = x.ExpiryBucket,
            ExpiryBucketName = GetExpiryBucketName(x.ExpiryBucket),
            ExpirationDateFrom = x.ExpirationDateFrom,
            ExpirationDateTo = x.ExpirationDateTo,
            SalesQuantity = x.SalesQuantity,
            SalesAmount = x.SalesAmount,
            OrderCount = x.OrderCount,
            QuantityPercentage = productTotals.TryGetValue(x.ProductId, out var total) && total > 0
                ? Math.Round(x.SalesQuantity / total * 100, 2)
                : 0m
        }).ToList();

        // 排序
        dtos = query.SortBy switch
        {
            "amount" => dtos.OrderByDescending(d => d.SalesAmount).ToList(),
            "count" => dtos.OrderByDescending(d => d.OrderCount).ToList(),
            _ => dtos.OrderByDescending(d => d.SalesQuantity).ToList()
        };

        // 分页
        var total = dtos.Count;
        var paged = dtos
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var result = new PagedResponseDto<ProductExpirySalesStatDto>
        {
            List = paged,
            Total = total,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };

        return ApiResponseDto<PagedResponseDto<ProductExpirySalesStatDto>>.Ok(result);
    }

    /// <summary>
    /// 计算效期区间编号（基于批次过期日期 - 订单时间）
    /// 1=已过期, 2=7天内, 3=30天内, 4=90天内, 5=90天以上
    /// </summary>
    private static int CalculateExpiryBucket(DateTime expirationDate, DateTime orderTime)
    {
        var remainingDays = (expirationDate - orderTime).TotalDays;
        return remainingDays switch
        {
            < 0 => 1,           // 已过期
            < 7 => 2,           // 7天内
            < 30 => 3,          // 30天内
            < 90 => 4,          // 90天内
            _ => 5              // 90天以上
        };
    }

    private static string GetExpiryBucketName(int bucket) => bucket switch
    {
        1 => "已过期",
        2 => "7天内",
        3 => "30天内",
        4 => "90天内",
        5 => "90天以上",
        _ => string.Empty
    };

    private static string GetProductTypeName(int type) => type switch
    {
        1 => "实物商品",
        2 => "服务商品",
        3 => "耗材",
        4 => "样品",
        5 => "赠品",
        _ => string.Empty
    };
}
