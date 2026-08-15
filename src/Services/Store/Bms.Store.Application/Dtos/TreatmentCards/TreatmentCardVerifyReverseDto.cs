namespace Bms.Store.Application.Dtos.TreatmentCards;

/// <summary>
/// 疗程卡核销冲正请求 DTO
/// 冲正不物理删除核销记录，仅更新 ReverseStatus 状态（规则7）
/// 冲正金额冲减原核销门店服务业绩
/// </summary>
public class TreatmentCardVerifyReverseDto
{
    /// <summary>
    /// 核销记录ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 冲正原因（必填，便于审计追溯）
    /// </summary>
    public string Remark { get; set; } = string.Empty;
}
