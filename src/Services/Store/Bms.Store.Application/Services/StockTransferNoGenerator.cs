using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 调拨单号生成器
/// 调拨单号由后端自动生成，格式：TF{yyyyMMdd}{序号}，如 TF20260718001
/// </summary>
public static class StockTransferNoGenerator
{
    /// <summary>
    /// 构造调拨流程的事务级顾问锁键
    /// 按 (租户, 门店, 调拨日期) 维度串行化并发请求，确保 TransferNo 的"查max+1"生成模式在并发下不产生重复值
    /// 必须用确定性哈希：HashCode.Combine 内部使用随机种子，相同输入每次调用值不同，会使顾问锁形同虚设
    /// </summary>
    public static long BuildLockKey(long tenantId, long storeId, DateTime transferDate)
    {
        var dateInt = int.Parse(transferDate.ToString("yyyyMMdd"));
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
    /// 生成调拨单号
    /// 格式：TF{yyyyMMdd}{序号}，如 TF20260718001
    /// 序号为该调拨日期下同租户+门店内的递增序号，3 位数补零
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="transferDate">调拨日期</param>
    /// <returns>调拨单号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime transferDate)
    {
        var dateStr = transferDate.ToString("yyyyMMdd");
        var prefix = $"TF{dateStr}";
        var maxTransferNo = await dbContext.StockTransfers
            .Where(t => t.TenantId == tenantId && t.StoreId == storeId
                        && t.TransferNo.StartsWith(prefix))
            .OrderByDescending(t => t.TransferNo)
            .Select(t => t.TransferNo)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxTransferNo != null && maxTransferNo.Length >= prefix.Length + 3)
        {
            if (int.TryParse(maxTransferNo.Substring(prefix.Length, 3), out var maxSeq))
                nextSeq = maxSeq + 1;
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
