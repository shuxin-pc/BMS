namespace Bms.Store.Domain.Entities;

/// <summary>
/// 库存流水来源类型常量
/// 标识 InventoryLog 的业务来源，入库(Type=1)与出库(Type=2)均可设置
/// 状态值与 InventoryLog.SourceType 字段保持一致，集中管理避免魔法数字
/// </summary>
public static class InventoryLogSourceTypes
{
    /// <summary>【出库】销售出库（订单触发，必绑 OrderId，禁止手动创建）</summary>
    public const int SalesOutbound = 0;

    /// <summary>【入库】采购入库（采购订单创建即入库）</summary>
    public const int PurchaseInbound = 1;

    /// <summary>【入库】退货入库（客户订单退款退货回库，由 OrderAppService 退款流程自动产生，禁止手动创建）</summary>
    public const int ReturnInbound = 2;

    /// <summary>【入库/出库】盘点调整（盘盈入库 Type=1 / 盘亏出库 Type=2 均使用此值）</summary>
    public const int CheckAdjustment = 3;

    /// <summary>【入库】调拨入库（调入门店库存增加）</summary>
    public const int TransferInbound = 4;

    /// <summary>【出库】调拨出库（调出门店库存扣减）</summary>
    public const int TransferOutbound = 5;

    /// <summary>【出库】采购退货出库（向供应商退回商品时扣减库存）</summary>
    public const int PurchaseReturnOutbound = 6;

    /// <summary>【出库】疗程卡核销出库（核销时扣减零售商品/BOM 耗材，必绑 OrderId，禁止手动创建）</summary>
    public const int TreatmentCardOutbound = 7;

    /// <summary>【出库】样品/赠品出库（历史值：样品领用、赠品出库合并记录，P-SG-02 修复后仅用于兼容历史数据，新数据按 SourceType=9/10 细分）</summary>
    public const int SampleGiftOutbound = 8;

    /// <summary>【出库】样品领用出库（SampleGiftReceive 触发，非销售，不计入销售额与主营成本）</summary>
    public const int SampleReceiveOutbound = 9;

    /// <summary>【出库】赠品活动出库（SampleGiftOut 触发，非销售，不计入销售额与主营成本）</summary>
    public const int GiftOutbound = 10;

    /// <summary>【出库】其他（手工录入或未分类的出库来源，仅用于 Type=2 出库；入库来源已不再使用此值）</summary>
    public const int Other = 11;

    /// <summary>
    /// 判定来源类型值是否合法
    /// </summary>
    /// <param name="sourceType">来源类型值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int sourceType) =>
        sourceType >= SalesOutbound && sourceType <= Other;

    /// <summary>
    /// 判定来源类型是否属于"销售出库类"（主营成本，必绑订单）
    /// 包括 SalesOutbound 与 TreatmentCardOutbound
    /// </summary>
    public static bool IsSalesCategory(int sourceType) =>
        sourceType == SalesOutbound || sourceType == TreatmentCardOutbound;

    /// <summary>
    /// 判定来源类型是否属于"样品赠品类"（营业外支出，不计入主营成本）
    /// 包括 SampleGiftOutbound（历史合并值）、SampleReceiveOutbound、GiftOutbound
    /// </summary>
    public static bool IsSampleGiftCategory(int sourceType) =>
        sourceType == SampleGiftOutbound
        || sourceType == SampleReceiveOutbound
        || sourceType == GiftOutbound;
}
