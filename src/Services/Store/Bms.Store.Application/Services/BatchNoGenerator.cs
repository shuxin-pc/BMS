using Microsoft.EntityFrameworkCore;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 批次号生成器
/// 统一封装各业务场景的 BatchNo 生成与唯一性校验逻辑
/// </summary>
public static class BatchNoGenerator
{
    /// <summary>
    /// 为采购订单明细生成批次号
    /// 格式：PO-{采购单号}-{行号:D2}，如 PO-20260718-01
    /// 若生成的 BatchNo 已存在，则追加 -01、-02... 序号后缀直至唯一
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="purchaseOrderNo">采购单号（须为 8 位日期格式 YYYYMMDD）</param>
    /// <param name="lineNumber">行号（从 1 开始）</param>
    /// <returns>全局唯一的批次号</returns>
    public static async Task<string> GenerateForPurchaseOrderAsync(
        StoreDbContext dbContext, long tenantId, string purchaseOrderNo, int lineNumber)
    {
        // 行号格式化为 2 位数（D2），符合需求示例 PO-20260718-01
        var baseBatchNo = $"PO-{purchaseOrderNo}-{lineNumber:D2}";

        // 唯一性校验：冲突时追加序号后缀（-01、-02...）
        var suffix = 0;
        string finalBatchNo;
        do
        {
            finalBatchNo = suffix == 0 ? baseBatchNo : $"{baseBatchNo}-{suffix:D2}";
            suffix++;
        }
        while (await dbContext.InventoryBatches.AnyAsync(b => b.TenantId == tenantId && b.BatchNo == finalBatchNo));

        return finalBatchNo;
    }
}
