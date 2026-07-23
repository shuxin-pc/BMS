namespace Bms.Store.Domain.Entities;

/// <summary>
/// 预约状态常量
/// 状态值与 Appointment.Status 字段保持一致，集中管理避免魔法数字
/// </summary>
public static class AppointmentStatus
{
    /// <summary>待确认（预约创建后初始状态，等待门店确认）</summary>
    public const int Pending = 1;

    /// <summary>已预约（门店确认后，等待客户到店）</summary>
    public const int Confirmed = 2;

    /// <summary>已到店（客户到店，等待服务开始）</summary>
    public const int Arrived = 3;

    /// <summary>已完成（服务完成，终态）</summary>
    public const int Completed = 4;

    /// <summary>已取消（门店或客户主动取消，终态）</summary>
    public const int Cancelled = 5;

    /// <summary>爽约（超过预约时段未到店，由定时任务自动标记，终态）</summary>
    public const int NoShow = 6;

    /// <summary>
    /// 判定状态值是否为合法的预约状态
    /// </summary>
    /// <param name="status">状态值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int status) => status >= Pending && status <= NoShow;
}
