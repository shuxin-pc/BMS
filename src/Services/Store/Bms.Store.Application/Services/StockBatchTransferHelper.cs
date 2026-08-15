using Microsoft.EntityFrameworkCore;
using Bms.Store.Domain.Entities;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 批次扣减明细（helper 返回值，含调入方继承所需的批次属性）
/// </summary>
public record BatchDeduction(
    string BatchNo,
    decimal UnitPrice,
    DateTime? ProductionDate,
    int? ShelfLifeDays,
    DateTime? ExpirationDate,
    DateTime? PurchaseDate,
    decimal DeductQuantity);

/// <summary>
/// 调入门店收货批次信息（helper 返回值，供调用方写入库流水）
/// </summary>
public record ReceivedBatch(
    string NewBatchNo,
    decimal Quantity,
    decimal UnitPrice,
    DateTime? ExpirationDate);

/// <summary>
/// 库存批次调拨公共逻辑
/// 抽取自 StockTransferAppService.ExecuteAsync，正品调拨与样品赠品调拨共用
/// 职责边界：只处理批次级操作（扣减/合并/日志写入）；Inventory 汇总表扣减/新建、
/// BeforeQuantity/AfterQuantity 递推由 AppService 编排
/// </summary>
public static class StockBatchTransferHelper
{
    /// <summary>
    /// 扣减调出门店批次（手动指定或 FEFO 自动），返回每个批次的扣减明细
    /// FEFO 排序：ExpirationDate ASC, BatchNo ASC（BatchNo 次序消除同过期日随机顺序）
    /// 抛 InvalidOperationException 由 AppService 捕获转 ApiResponseDto.Fail
    /// </summary>
    /// <remarks>
    /// 并发策略：未加显式行锁（PostgreSQL 不支持 WITH (UPDLOCK)，FOR UPDATE 与 FromSqlInterpolated
    /// 组合有 schema/列映射复杂性）。沿用正品调拨现状，事务内串行扣减。
    /// 极端并发下的超扣风险为已知限制（设计文档 4.5 节），如需严格防护后续可对实体加 RowVersion。
    /// </remarks>
    public static async Task<List<BatchDeduction>> DeductBatchesAsync(
        StoreDbContext dbContext, long tenantId, long fromStoreId,
        long productId, string? batchNo, decimal quantity, DateTime now)
    {
        IQueryable<InventoryBatch> query;
        if (!string.IsNullOrWhiteSpace(batchNo))
        {
            // 手动模式：指定批次
            query = dbContext.InventoryBatches
                .Where(b => b.ProductId == productId
                    && b.BatchNo == batchNo
                    && b.TenantId == tenantId
                    && b.StoreId == fromStoreId
                    && b.Status == 1);
        }
        else
        {
            // FEFO 自动模式：按 ExpirationDate ASC, BatchNo ASC 排序
            // BatchNo 次序为重构增强，消除同过期日批次的随机顺序
            query = dbContext.InventoryBatches
                .Where(b => b.ProductId == productId
                    && b.TenantId == tenantId
                    && b.StoreId == fromStoreId
                    && b.Status == 1
                    && b.Quantity > 0)
                .OrderBy(b => b.ExpirationDate)
                .ThenBy(b => b.BatchNo);
        }

        var fromBatches = await query.ToListAsync();

        if (!fromBatches.Any())
        {
            throw new InvalidOperationException(!string.IsNullOrWhiteSpace(batchNo)
                ? $"调出门店商品(ID:{productId})批次 {batchNo} 不存在或已用完"
                : $"调出门店商品(ID:{productId})无可用库存批次");
        }

        var totalAvailable = fromBatches.Sum(b => b.Quantity);
        if (totalAvailable < quantity)
            throw new InvalidOperationException($"调出门店商品(ID:{productId})批次库存不足（可用 {totalAvailable}，需调拨 {quantity}）");

        var deductions = new List<BatchDeduction>();
        var remaining = quantity;
        foreach (var batch in fromBatches)
        {
            if (remaining <= 0) break;
            var deduct = Math.Min(batch.Quantity, remaining);
            batch.Quantity -= deduct;
            batch.UpdatedTime = now;
            if (batch.Quantity == 0)
                batch.Status = 2; // 已用完

            remaining -= deduct;
            deductions.Add(new BatchDeduction(
                batch.BatchNo, batch.UnitPrice, batch.ProductionDate,
                batch.ShelfLifeDays, batch.ExpirationDate, batch.PurchaseDate, deduct));
        }
        return deductions;
    }

    /// <summary>
    /// 调入门店按扣减明细新建或累加批次
    /// 批次号按 BatchNoGenerator 在调入门店重新生成（不沿用调出门店批次号），
    /// 其余属性（UnitPrice/ProductionDate/ShelfLifeDays/ExpirationDate）继承调出批次；
    /// PurchaseDate 用调拨入库日（与批次号日期前缀保持一致，符合采购入库惯例）。
    /// </summary>
    /// <param name="alreadyGeneratedCount">本次 ExecuteAsync 内已生成的批次数，跨 items 累加避免事务内 CountAsync 漏算未落库批次导致序号重复</param>
    /// <returns>每个扣减批次对应的新批次号与流水所需字段，供调用方写入库流水</returns>
    public static async Task<List<ReceivedBatch>> MergeReceiveBatchesAsync(
        StoreDbContext dbContext, long tenantId, long toStoreId,
        long productId, List<BatchDeduction> deductions, DateTime now,
        string tenantCode, int alreadyGeneratedCount)
    {
        var result = new List<ReceivedBatch>();
        for (var i = 0; i < deductions.Count; i++)
        {
            var d = deductions[i];
            // 调入门店按当天重新生成批次号，alreadyGeneratedCount + i 避免事务内未落库批次导致序号重复
            var newBatchNo = await BatchNoGenerator.GenerateAsync(
                dbContext, tenantId, toStoreId, now, alreadyGeneratedCount + i);

            var toBatch = await dbContext.InventoryBatches
                .FirstOrDefaultAsync(b => b.ProductId == productId
                    && b.BatchNo == newBatchNo
                    && b.TenantId == tenantId
                    && b.StoreId == toStoreId
                    && b.Status == 1);

            if (toBatch == null)
            {
                // 新建批次：批次号重新生成，其余属性继承调出批次；PurchaseDate 用入库日保持与批次号前缀一致
                dbContext.InventoryBatches.Add(new InventoryBatch
                {
                    ProductId = productId,
                    BatchNo = newBatchNo,
                    Quantity = d.DeductQuantity,
                    UnitPrice = d.UnitPrice,
                    ProductionDate = d.ProductionDate,
                    ShelfLifeDays = d.ShelfLifeDays,
                    ExpirationDate = d.ExpirationDate,
                    PurchaseDate = now,
                    Status = 1,
                    TenantId = tenantId,
                    TenantCode = tenantCode,
                    StoreId = toStoreId,
                    CreatedTime = now
                });
            }
            else
            {
                // 防御性兜底：极端情况下批次号撞号（如手工创建同号批次），累加到现有批次
                toBatch.Quantity += d.DeductQuantity;
                toBatch.UpdatedTime = now;
            }

            result.Add(new ReceivedBatch(newBatchNo, d.DeductQuantity, d.UnitPrice, d.ExpirationDate));
        }
        return result;
    }

    /// <summary>
    /// 写一条库存流水（出库 quantity 为负 / 入库 quantity 为正）
    /// Before/After 由 AppService 计算 Inventory 汇总递推后传入
    /// </summary>
    public static void WriteInventoryLog(
        StoreDbContext dbContext, long tenantId, string tenantCode, long storeId,
        long productId, int sourceType, decimal quantity, string batchNo,
        DateTime? expirationDate, decimal unitPrice,
        decimal beforeQty, decimal afterQty, long relatedId, string remark,
        long? operatorId, string? operatorName, DateTime now)
    {
        dbContext.InventoryLogs.Add(new InventoryLog
        {
            ProductId = productId,
            Type = quantity > 0 ? 1 : 2, // 1=入库 2=出库
            SourceType = sourceType,
            UnitPrice = unitPrice,
            Quantity = quantity,
            BeforeQuantity = beforeQty,
            AfterQuantity = afterQty,
            BatchNo = batchNo,
            ExpirationDate = expirationDate,
            RelatedId = relatedId,
            Remark = remark,
            OperatorId = operatorId,
            OperatorName = operatorName,
            TenantId = tenantId,
            TenantCode = tenantCode,
            StoreId = storeId,
            CreatedTime = now
        });
    }
}
