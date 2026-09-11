using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 订单号生成器
/// 订单号由后端自动生成，格式：SO{yyyyMMdd}{序号}，如 SO20260819001
/// </summary>
public static class OrderNoGenerator
{
    /// <summary>
    /// 构造订单事务级顾问锁键
    /// 按 (租户, 门店, 下单日期) 维度串行化并发请求，确保 OrderNo 的"查max+1"生成模式在并发下不产生重复值
    /// 必须用确定性哈希：HashCode.Combine 内部使用随机种子，相同输入每次调用值不同，会使顾问锁形同虚设
    /// </summary>
    public static long BuildLockKey(long tenantId, long storeId, DateTime orderDate)
    {
        var dateInt = int.Parse(orderDate.ToString("yyyyMMdd"));
        unchecked
        {
            var key = tenantId;
            key = key * 31 + storeId;
            key = key * 31 + dateInt;
            // 去除符号位保证非负（unchecked 允许乘法溢出按 long 截断，确定性不受影响）
            return key & long.MaxValue;
        }
    }

    /// <summary>
    /// 生成订单号
    /// 格式：SO{yyyyMMdd}{序号}，如 SO20260819001
    /// 序号为该下单日期下同租户+门店内的递增序号，3 位数补零
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="orderDate">下单日期</param>
    /// <returns>订单号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime orderDate)
    {
        var dateStr = orderDate.ToString("yyyyMMdd");
        var prefix = $"SO{dateStr}";
        var maxOrderNo = await dbContext.Orders
            .Where(o => o.TenantId == tenantId && o.StoreId == storeId
                        && o.OrderNo.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNo)
            .Select(o => o.OrderNo)
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
