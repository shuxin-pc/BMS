namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 创建项目卡核销记录请求 DTO
/// 核销金额、关联订单、核销时间由后端自动计算/创建，前端只需指定项目卡和核销项目列表
/// 一次操作可包含多个核销项目（一次到店做多种护理）
/// 跨店核销时需提供客户手机号尾号用于身份核验（规则9）
/// </summary>
public class TreatmentCardVerifyCreateDto
{
    /// <summary>
    /// 核销门店ID（已废弃，统一使用当前登录用户所属门店）
    /// 保留字段仅为前向兼容，服务层忽略此值
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 核销门店编码（已废弃，统一使用当前登录用户所属门店）
    /// 保留字段仅为前向兼容，服务层忽略此值
    /// </summary>
    public string? StoreCode { get; set; }

    /// <summary>
    /// 项目卡销售ID
    /// </summary>
    public long CardSaleId { get; set; }

    /// <summary>
    /// 客户手机号尾号（后4位，跨店核销时必填，用于身份核验，规则9）
    /// 同店核销可不填
    /// </summary>
    public string? CustomerPhoneTail { get; set; }

    /// <summary>
    /// 核销项目列表（至少 1 项，每项含项目 ID 和核销次数）
    /// 所有项目必须在项目卡销售明细（TreatmentCardSaleItem）中
    /// </summary>
    public List<TreatmentCardVerifyItemInput> Items { get; set; } = new();

    /// <summary>
    /// 操作员ID
    /// </summary>
    public long? OperatorId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 购物车结算批次号（POS 购物车一次结算生成，用于订单/核销/开卡三单据聚合追溯）
    /// 非购物车结算（独立核销）不传
    /// </summary>
    public string? CheckoutSessionNo { get; set; }
}

/// <summary>
/// 核销项目输入
/// </summary>
public class TreatmentCardVerifyItemInput
{
    /// <summary>
    /// 核销的商品ID（服务项目，对应 TreatmentCardSaleItem.ProductId）
    /// </summary>
    public long ProductId { get; set; }

    /// <summary>
    /// 本项核销次数（默认 1，至少 1）
    /// </summary>
    public int VerifyTimes { get; set; } = 1;

    /// <summary>
    /// 技师ID（核销项目本质是服务商品，按服务商品录入技师，可空=未选不占用不归集）
    /// </summary>
    public long? TechnicianId { get; set; }

    /// <summary>
    /// 技师来源（1:商家技师 2:平台技师），仅商家技师参与技师统计归集
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
    /// 核销项目明细备注（快速开单服务内容弹窗录入，可空）
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 服务项目绑定耗材的效期选择（核销项目本质是服务商品）。
    /// 加购核销时店员选择绑定耗材的效期；空表示未绑定耗材或系统自动按 FEFO 扣减。
    /// 后端 DeductServiceBomForVerifyAsync 按指定效期扣减对应耗材库存。
    /// </summary>
    public List<ConsumableExpiryInput> ConsumableExpiries { get; set; } = new();
}
