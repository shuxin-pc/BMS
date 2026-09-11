namespace Bms.BuildingBlocks.Core.Interceptors;

/// <summary>
/// 审计日志实体中文名映射
/// 键为实体类型名（GetType().Name），值为审计日志"操作内容"中展示的中文业务名，
/// 中文名与系统菜单/页面术语保持一致。未命中映射时回退显示英文类型名（新增实体漏配不影响写入）
/// </summary>
public static class AuditLogEntityNames
{
    private static readonly Dictionary<string, string> EntityNames = new(StringComparer.Ordinal)
    {
        // ===== System 服务实体 =====
        ["User"] = "用户",
        ["Role"] = "角色",
        ["Menu"] = "菜单",
        ["UserRole"] = "用户角色",
        ["Organization"] = "组织",
        ["SystemConfig"] = "系统配置",
        ["DataPermission"] = "数据权限",
        ["Tenant"] = "租户",
        ["Subsystem"] = "子系统",
        ["SubsystemMenu"] = "子系统菜单",
        ["TenantSubsystem"] = "租户子系统",
        ["RoleMenuAuth"] = "角色菜单授权",
        ["Message"] = "消息",
        ["MessageRecipient"] = "消息接收人",

        // ===== Store 服务实体 =====
        ["Activity"] = "营销活动",
        ["Appointment"] = "预约",
        ["BodyDataRecord"] = "身体数据记录",
        ["ConsumeLog"] = "消费记录",
        ["CourseCardItem"] = "次卡项目",
        ["CrossStoreOperationLog"] = "跨店操作日志",
        ["Customer"] = "顾客",
        ["CustomerBeautyProfile"] = "顾客美丽档案",
        ["CustomerCareLog"] = "顾客关怀记录",
        ["CustomerDeleteLog"] = "顾客删除记录",
        ["CustomerLevel"] = "顾客等级",
        ["CustomerPointsLog"] = "顾客积分记录",
        ["CustomerPreference"] = "顾客偏好",
        ["CustomerTag"] = "顾客标签",
        ["CustomerTagLink"] = "顾客标签关联",
        ["DailySettlement"] = "日结单",
        ["DailyStat"] = "日统计",
        ["Equipment"] = "设备",
        ["EquipmentMaintenance"] = "设备保养记录",
        ["EquipmentMaintenanceReminder"] = "设备保养提醒",
        ["EquipmentType"] = "设备类型",
        ["Inventory"] = "库存",
        ["InventoryAlert"] = "库存预警",
        ["InventoryBatch"] = "库存批次",
        ["InventoryCheck"] = "库存盘点",
        ["InventoryCheckBatch"] = "盘点批次",
        ["InventoryLog"] = "库存流水",
        ["MonthlyStat"] = "月统计",
        ["Order"] = "订单",
        ["OrderItem"] = "订单明细",
        ["OrderItemBatch"] = "订单明细批次",
        ["ParkedOrder"] = "挂单",
        ["PointsExchange"] = "积分兑换",
        ["PointsRule"] = "积分规则",
        ["PriceChangeLog"] = "价格变更记录",
        ["Product"] = "商品",
        ["ProductCategory"] = "商品分类",
        ["ProductMaster"] = "商品主档",
        ["ProductSalesStat"] = "商品销售统计",
        ["ProductSupplier"] = "商品供应商",
        ["PurchaseOrder"] = "采购订单",
        ["PurchaseOrderItem"] = "采购订单明细",
        ["PurchaseReturn"] = "采购退货",
        ["PurchaseReturnItem"] = "采购退货明细",
        ["Room"] = "房间",
        ["SampleGiftReceive"] = "样品领用记录",
        ["ServiceBom"] = "服务BOM",
        ["ServiceComparisonPhoto"] = "服务对比照",
        ["ServiceComparisonPhotoItem"] = "服务对比照明细",
        ["ServiceProduct"] = "服务项目",
        ["ServiceProductEquipment"] = "服务项目设备",
        ["ServiceProductSkill"] = "服务项目技能",
        ["ServiceReaction"] = "服务反应记录",
        ["SkillCategory"] = "技能分类",
        ["StockTransfer"] = "库存调拨",
        ["StockTransferItem"] = "调拨明细",
        ["Store"] = "门店",
        ["StoredValueAccount"] = "储值账户",
        ["StoredValueLog"] = "储值流水",
        ["StoredValueRule"] = "储值规则",
        ["StoreReminderSetting"] = "门店提醒设置",
        ["StoreTenantSetting"] = "门店租户设置",
        ["Supplier"] = "供应商",
        ["Technician"] = "技师",
        ["TechnicianSkill"] = "技师技能",
        ["TechnicianStatistic"] = "技师统计",
        ["TreatmentCard"] = "项目卡",
        ["TreatmentCardSale"] = "项目卡销售",
        ["TreatmentCardSaleItem"] = "项目卡销售明细",
        ["TreatmentCardTransfer"] = "项目卡划拨",
        ["TreatmentCardVerify"] = "项目卡核销",
        ["TreatmentCardVerifyItem"] = "项目卡核销明细",
        ["UserStore"] = "用户门店",
    };

    /// <summary>
    /// 获取实体的中文业务名，未命中映射时回退英文类型名
    /// </summary>
    public static string GetDisplayName(string entityTypeName)
    {
        return EntityNames.TryGetValue(entityTypeName, out var name) ? name : entityTypeName;
    }
}
