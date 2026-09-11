-- =====================================================================
-- 快速开单数据清空脚本（PostgreSQL）
--
-- 用途：清空「快速开单」页面产生的全部业务数据（含进货链路采购单），
--       用于重建干净的测试流程
-- 范围：全库所有门店（不按 StoreId 过滤）
-- 注意：仅适用于测试库 / 演示库，执行前请先备份数据库
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. 订单域（零售 / 耗材 / 赠品 / 服务结算）
-- ---------------------------------------------------------------------
TRUNCATE TABLE
    bms_store."OrderItemBatches",
    bms_store."OrderItems",
    bms_store."Orders",

    -- 2. 项目卡开卡
    bms_store."TreatmentCardSaleItems",
    bms_store."TreatmentCardSales",

    -- 3. 项目卡核销
    bms_store."TreatmentCardVerifyItems",
    bms_store."TreatmentCardVerifies",

    -- 4. 项目卡转卡
    bms_store."TreatmentCardTransfers",

    -- 5. 挂单
    bms_store."ParkedOrders",

    -- 6. 储值 / 积分 / 消费流水
    bms_store."StoredValueLogs",
    bms_store."StoredValueAccounts",
    bms_store."CustomerPointsLogs",
    bms_store."ConsumeLogs",

    -- 7. 统计 / 日结
    bms_store."DailySettlements",
    bms_store."DailyStats",
    bms_store."MonthlyStats",
    bms_store."ProductSalesStats",
    bms_store."TechnicianStatistics",

    -- 8. 开单/核销衍生记录（引用了订单表，需一并清空，否则外键约束报错）
    bms_store."ServiceComparisonPhotoItems",
    bms_store."ServiceComparisonPhotos",
    bms_store."ServiceReactions",

    -- 9. 采购相关（采购订单/采购退货及明细，进货单据；退货单引用了采购订单）
    bms_store."PurchaseReturnItems",
    bms_store."PurchaseReturns",
    bms_store."PurchaseOrderItems",
    bms_store."PurchaseOrders",

    -- 10. 库存相关表一并清空（商品库存需重新建档；样品领取引用了库存批次，随批次清空）
    bms_store."SampleGiftReceives",
    bms_store."InventoryAlerts",
    bms_store."InventoryLogs",
    bms_store."InventoryBatches",
    bms_store."Inventories",

    -- 11. 预约联动清理（因开单产生的预约及状态流转数据）
    bms_store."Appointments"
RESTART IDENTITY;

-- ---------------------------------------------------------------------
-- 12. 重置客户聚合数据（保留客户资料，仅归零开单产生的累计值）
--     累计积分 / 当前余额 / 累计消费 / 最后消费时间
-- ---------------------------------------------------------------------
UPDATE bms_store."Customers"
SET "TotalPoints"      = 0,
    "Balance"          = 0,
    "TotalConsume"     = 0,
    "LastConsumeTime"  = NULL;

COMMIT;

-- =====================================================================
-- 边界说明（脚本未涉及，请知悉）：
--   - 客户档案(Customers)仅重置聚合字段，资料本身保留
--   - 项目卡配置(TreatmentCards)及卡项(CourseCardItems)为卡种配置数据，保留
--   - 调拨单/库存盘点/价格变更记录等单据未清空（如需清空可另行加入）
--   - 积分规则、储值规则、商品/服务档案、技师/房间/设备等基础资料未清空
-- =====================================================================
