using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 样品赠品调拨单号生成器
/// 格式：SGT{yyyyMMdd}{序号}，如 SGT20260802001
/// 序号为该调拨日期下同租户+门店内的递增序号，3 位数补零
/// </summary>
public static class SampleGiftTransferNoGenerator
{
    /// <summary>
    /// 生成样品赠品调拨单号
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
        var prefix = $"SGT{dateStr}";
        var maxTransferNo = await dbContext.SampleGiftTransfers
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
