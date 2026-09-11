namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 项目卡核销记录 DTO
/// </summary>
public class TreatmentCardVerifyDto
{
    public long Id { get; set; }

    /// <summary>
    /// 核销门店ID（记录在哪家门店核销，项目卡跨店通用）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 核销门店编码
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 项目卡销售ID
    /// </summary>
    public long CardSaleId { get; set; }

    /// <summary>
    /// 本次核销总金额（冗余字段，等于 Items.Sum(SubAmount)）
    /// </summary>
    public decimal VerifyAmount { get; set; }

    /// <summary>
    /// 关联订单ID
    /// </summary>
    public long? OrderId { get; set; }

    /// <summary>
    /// 本次核销总次数（冗余字段，等于 Items.Sum(VerifyTimes)）
    /// </summary>
    public int VerifyTimes { get; set; }

    /// <summary>
    /// 核销时间
    /// </summary>
    public DateTime VerifyTime { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 操作员姓名（核销时的姓名快照）
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 核销项目明细列表（一次核销可包含多个项目）
    /// </summary>
    public List<TreatmentCardVerifyItemDto> Items { get; set; } = new();

    /// <summary>
    /// 客户名称（列表查询 join TreatmentCardSale→Customer 补充，仿照 TreatmentCardSaleDto.CustomerName）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 客户手机号（列表查询 join TreatmentCardSale→Customer 补充，仿照 TreatmentCardSaleDto.Phone）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 项目卡名称（列表查询 join TreatmentCardSale→TreatmentCard 补充，仿照 TreatmentCardSaleDto.CardName）
    /// </summary>
    public string? CardName { get; set; }

    /// <summary>
    /// 核销项目名称（列表查询聚合 Items[].ProductId → Product.Master.Name，顿号连接；一次核销可含多个项目）
    /// </summary>
    public string? VerifyItem { get; set; }

    /// <summary>
    /// 该项目卡当前剩余次数（列表查询 join TreatmentCardSale.RemainingTimes 补充）
    /// </summary>
    public int? RemainingCount { get; set; }
}

/// <summary>
/// 项目卡核销项目明细 DTO（输出）
/// 对齐实体 TreatmentCardVerifyItem
/// </summary>
public class TreatmentCardVerifyItemDto
{
    /// <summary>
    /// 明细ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 核销主单ID
    /// </summary>
    public long VerifyId { get; set; }

    /// <summary>
    /// 核销的项目ID（服务项目）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 核销项目名称（列表/详情查询 join Product.Master 补充）
    /// </summary>
    public string? ProductName { get; set; }

    /// <summary>
    /// 本项核销次数
    /// </summary>
    public int VerifyTimes { get; set; }

    /// <summary>
    /// 折算单价（核销时锁定）
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 本项核销金额 = AllocatedUnitPrice × VerifyTimes（最后一项含兜底）
    /// </summary>
    public decimal SubAmount { get; set; }

    /// <summary>
    /// 技师ID（核销项目本质是服务商品，按服务商品录入技师，可空=未选）
    /// </summary>
    public long? TechnicianId { get; set; }

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师）
    /// </summary>
    public int? TechnicianSource { get; set; }

    /// <summary>
    /// 房间/床位ID（服务项目占用房间资源，可空）
    /// </summary>
    public long? RoomId { get; set; }

    /// <summary>
    /// 设备ID（服务项目占用设备资源，可空）
    /// </summary>
    public long? EquipmentId { get; set; }

    /// <summary>
    /// 服务开始时间（核销项目真实服务开始时间）
    /// </summary>
    public DateTime? ServiceStartTime { get; set; }

    /// <summary>
    /// 服务结束时间（开始时间 + 服务时长自动计算）
    /// </summary>
    public DateTime? ServiceEndTime { get; set; }

    /// <summary>
    /// 备注（核销项目明细级备注）
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
