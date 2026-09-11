namespace Bms.Store.Domain.Entities;

/// <summary>
/// 项目卡核销项目明细
/// 一次核销可包含多个项目（一次到店做多种护理），每个项目独立计算金额
/// </summary>
public class TreatmentCardVerifyItem : StoreBusinessEntityBase
{
    /// <summary>
    /// 核销主单ID
    /// </summary>
    public long VerifyId { get; set; }

    /// <summary>
    /// 核销的商品ID（服务项目，对应 TreatmentCardSaleItem.ProductId）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 本项核销次数（默认 1，至少 1）
    /// </summary>
    public int VerifyTimes { get; set; } = 1;

    /// <summary>
    /// 折算单价（核销时从 TreatmentCardSaleItem.AllocatedUnitPrice 复制并锁定）
    /// </summary>
    public decimal AllocatedUnitPrice { get; set; }

    /// <summary>
    /// 本项核销金额 = AllocatedUnitPrice × VerifyTimes
    /// 最后一次核销时此项会应用兜底逻辑（剩余金额全部分摊）
    /// </summary>
    public decimal SubAmount { get; set; }

    /// <summary>
    /// 技师ID（核销项目本质是服务商品，按服务商品录入技师，可空=未选不占用不归集）
    /// </summary>
    public long? TechnicianId { get; set; }

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师），由前端按技师选择同步，仅商家技师参与技师统计归集
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
    /// 服务开始时间（核销项目真实服务开始时间，必填，用于资源占用检测与技师统计归集）
    /// </summary>
    public DateTime? ServiceStartTime { get; set; }

    /// <summary>
    /// 服务结束时间（开始时间 + 服务时长自动计算，可空）
    /// </summary>
    public DateTime? ServiceEndTime { get; set; }

    /// <summary>
    /// 备注（核销项目明细级备注，快速开单服务内容弹窗录入）
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 导航属性：核销主单
    /// </summary>
    public TreatmentCardVerify? Verify { get; set; }

    /// <summary>
    /// 导航属性：商品
    /// </summary>
    public Product? Product { get; set; }
}
