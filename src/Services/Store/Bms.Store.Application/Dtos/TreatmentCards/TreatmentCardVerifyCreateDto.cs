namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 创建疗程卡核销记录请求 DTO
/// 核销金额、关联订单、核销时间由后端自动计算/创建，前端只需指定疗程卡和核销项目列表
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
    /// 疗程卡销售ID
    /// </summary>
    public long CardSaleId { get; set; }

    /// <summary>
    /// 客户手机号尾号（后4位，跨店核销时必填，用于身份核验，规则9）
    /// 同店核销可不填
    /// </summary>
    public string? CustomerPhoneTail { get; set; }

    /// <summary>
    /// 核销项目列表（至少 1 项，每项含项目 ID 和核销次数）
    /// 所有项目必须在疗程卡销售明细（TreatmentCardSaleItem）中
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
}
