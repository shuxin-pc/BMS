using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购单号生成器
/// 采购单号由后端自动生成，格式：PO{yyyyMMdd}{序号}，如 PO20260718001
/// </summary>
public static class PurchaseOrderNoGenerator
{
    /// <summary>
    /// 构造采购流程的事务级顾问锁键
    /// 按 (租户, 门店, 采购日期) 维度串行化并发请求，确保 OrderNo/BatchNo 的"查max/count+1"生成模式在并发下不产生重复值
    /// </summary>
    public static long BuildLockKey(long tenantId, long storeId, DateTime orderDate)
    {
        var dateInt = int.Parse(orderDate.ToString("yyyyMMdd"));
        return (long)HashCode.Combine(tenantId, storeId, dateInt);
    }

    /// <summary>
    /// 生成采购单号
    /// 格式：PO{yyyyMMdd}{序号}，如 PO20260718001
    /// 序号为该采购日期下同租户+门店内的递增序号，3 位数补零
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="orderDate">采购日期</param>
    /// <returns>采购单号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime orderDate)
    {
        var dateStr = orderDate.ToString("yyyyMMdd");
        var prefix = $"PO{dateStr}";
        var maxOrderNo = await dbContext.PurchaseOrders
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId
                        && p.OrderNo.StartsWith(prefix))
            .OrderByDescending(p => p.OrderNo)
            .Select(p => p.OrderNo)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxOrderNo != null && maxOrderNo.Length >= prefix.Length + 3)
        {
            if (int.TryParse(maxOrderNo.Substring(prefix.Length, 3), out var maxSeq))
                nextSeq = maxSeq + 1;
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
