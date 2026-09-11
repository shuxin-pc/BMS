using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 挂单号生成器
/// 挂单号由后端自动生成，格式：PK{yyyyMMdd}{序号}，如 PK20260823001
/// 序号为该日期下同租户+门店内的递增序号，3 位数补零
/// </summary>
public static class ParkedOrderNoGenerator
{
    /// <summary>
    /// 构造挂单事务级顾问锁键
    /// 按 (租户, 门店, 挂单日期) 维度串行化并发请求，确保 ParkNo 的"查max+1"生成模式在并发下不产生重复值
    /// 必须用确定性哈希：HashCode.Combine 内部使用随机种子，相同输入每次调用值不同，会使顾问锁形同虚设
    /// </summary>
    public static long BuildLockKey(long tenantId, long storeId, DateTime heldDate)
    {
        var dateInt = int.Parse(heldDate.ToString("yyyyMMdd"));
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
    /// 生成挂单号
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="heldDate">挂单日期</param>
    /// <returns>挂单号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime heldDate)
    {
        var dateStr = heldDate.ToString("yyyyMMdd");
        var prefix = $"PK{dateStr}";
        var maxParkNo = await dbContext.ParkedOrders
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId
                        && p.ParkNo.StartsWith(prefix))
            .OrderByDescending(p => p.ParkNo)
            .Select(p => p.ParkNo)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxParkNo != null && maxParkNo.Length >= prefix.Length + 3)
        {
            if (int.TryParse(maxParkNo.Substring(prefix.Length, 3), out var maxSeq))
                nextSeq = maxSeq + 1;
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
