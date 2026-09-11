using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购退货单号生成器
/// 退货单号由后端自动生成，格式：PR{yyyyMMdd}{序号}，如 PR20260806001
/// </summary>
public static class PurchaseReturnNoGenerator
{
    /// <summary>
    /// 构造采购退货事务级顾问锁键
    /// 按 (租户, 门店, 退货日期) 维度串行化并发请求，确保 ReturnNo 的"查max+1"生成模式在并发下不产生重复值
    /// 必须用确定性哈希：HashCode.Combine 内部使用随机种子，相同输入每次调用值不同，会使顾问锁形同虚设
    /// </summary>
    public static long BuildLockKey(long tenantId, long storeId, DateTime returnDate)
    {
        var dateInt = int.Parse(returnDate.ToString("yyyyMMdd"));
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
    /// 生成采购退货单号
    /// 格式：PR{yyyyMMdd}{序号}，如 PR20260806001
    /// 序号为该退货日期下同租户+门店内的递增序号，3 位数补零
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="returnDate">退货日期</param>
    /// <returns>退货单号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime returnDate)
    {
        var dateStr = returnDate.ToString("yyyyMMdd");
        var prefix = $"PR{dateStr}";
        var maxReturnNo = await dbContext.PurchaseReturns
            .Where(p => p.TenantId == tenantId && p.StoreId == storeId
                        && p.ReturnNo.StartsWith(prefix))
            .OrderByDescending(p => p.ReturnNo)
            .Select(p => p.ReturnNo)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxReturnNo != null && maxReturnNo.Length >= prefix.Length + 3)
        {
            if (int.TryParse(maxReturnNo.Substring(prefix.Length, 3), out var maxSeq))
                nextSeq = maxSeq + 1;
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
