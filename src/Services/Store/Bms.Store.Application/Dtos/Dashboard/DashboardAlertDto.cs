namespace Bms.Store.Application.Dtos.Dashboard;

/// <summary>
/// 首页看板预警提醒项
/// 聚合库存预警/批次临期/项目卡到期/客户生日四类来源，字段直接对齐前端展示结构
/// </summary>
public class DashboardAlertDto
{
    /// <summary>
    /// 预警级别：danger（紧急）/ warning（提醒），对齐前端样式类名
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// 预警标题（如：商品库存不足、项目卡已到期）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 预警详情描述
    /// </summary>
    public string Desc { get; set; } = string.Empty;

    /// <summary>
    /// 标签文字（如：紧急、临期、到期、关怀）
    /// </summary>
    public string TagText { get; set; } = string.Empty;

    /// <summary>
    /// 展示时间（库存预警为发生时间，其余为剩余天数文案）
    /// </summary>
    public string Time { get; set; } = string.Empty;
}
