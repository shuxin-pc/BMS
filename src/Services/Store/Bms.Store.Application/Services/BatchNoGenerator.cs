using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 批次号生成器
/// 统一封装批次号生成逻辑，格式：{yyyyMMdd}-{序号}，如 20260718-01
/// 序号在"同门店当天"内递增
/// </summary>
public static class BatchNoGenerator
{
    /// <summary>
    /// 生成批次号
    /// 格式：{yyyyMMdd}-{序号}，如 20260718-01
    /// 序号为"同门店当天"已存在批次数量 + 1，2 位数补零
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="purchaseDate">采购日期</param>
    /// <param name="alreadyGeneratedCount">本次请求内已生成但尚未落库的同前缀批次数，用于同一事务循环生成多个批次时避免序号重复</param>
    /// <returns>批次号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime purchaseDate, int alreadyGeneratedCount = 0)
    {
        var dateStr = purchaseDate.ToString("yyyyMMdd");
        var prefix = $"{dateStr}-";
        var existingCount = await dbContext.InventoryBatches
            .CountAsync(b => b.TenantId == tenantId
                          && b.StoreId == storeId
                          && b.BatchNo.StartsWith(prefix));
        return $"{dateStr}-{existingCount + alreadyGeneratedCount + 1:D2}";
    }
}
