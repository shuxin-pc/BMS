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
