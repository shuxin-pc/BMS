using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 项目卡销售单号生成器
/// 销售单号由后端自动生成，格式：TC{yyyyMMdd}{序号}，如 TC20260821001
/// </summary>
public static class TreatmentCardSaleNoGenerator
{
    /// <summary>
    /// 构造销售事务级顾问锁键
    /// 按 (租户, 门店, 销售日期) 维度串行化并发请求，确保 SaleNo 的"查max+1"生成模式在并发下不产生重复值
    /// 必须用确定性哈希：HashCode.Combine 内部使用随机种子，相同输入每次调用值不同，会使顾问锁形同虚设
    /// </summary>
    public static long BuildLockKey(long tenantId, long storeId, DateTime saleDate)
    {
        var dateInt = int.Parse(saleDate.ToString("yyyyMMdd"));
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
    /// 生成项目卡销售单号
    /// 格式：TC{yyyyMMdd}{序号}，如 TC20260821001
    /// 序号为该销售日期下同租户+门店内的递增序号，3 位数补零
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="saleDate">销售日期</param>
    /// <returns>销售单号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime saleDate)
    {
        var dateStr = saleDate.ToString("yyyyMMdd");
        var prefix = $"TC{dateStr}";
        var maxSaleNo = await dbContext.TreatmentCardSales
            .Where(s => s.TenantId == tenantId && s.StoreId == storeId
                        && s.SaleNo != null && s.SaleNo.StartsWith(prefix))
            .OrderByDescending(s => s.SaleNo)
            .Select(s => s.SaleNo)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxSaleNo != null && maxSaleNo.Length >= prefix.Length + 3)
        {
            if (int.TryParse(maxSaleNo.Substring(prefix.Length, 3), out var maxSeq))
                nextSeq = maxSeq + 1;
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
