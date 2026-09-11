namespace Bms.Store.Domain.Entities;

/// <summary>
/// 预约状态常量
/// 状态值与 Appointment.Status 字段保持一致，集中管理避免魔法数字
/// 创建预约即置为已预约(1)，预约均由门店人员线下与客户确认后录入，无"待确认"状态
/// </summary>
public static class AppointmentStatus
{
    /// <summary>已预约（门店确认后，等待客户到店，创建预约的初始状态）</summary>
    public const int Confirmed = 1;

    /// <summary>已到店（客户到店，等待服务开始）</summary>
    public const int Arrived = 2;

    /// <summary>已完成（服务完成，终态）</summary>
    public const int Completed = 3;

    /// <summary>已取消（门店或客户主动取消，终态）</summary>
    public const int Cancelled = 4;

    /// <summary>爽约（超过预约时段未到店，由定时任务自动标记，终态）</summary>
    public const int NoShow = 5;

    /// <summary>
    /// 判定状态值是否为合法的预约状态
    /// </summary>
    /// <param name="status">状态值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int status) => status >= Confirmed && status <= NoShow;
}
