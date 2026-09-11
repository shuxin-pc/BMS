using Bms.Store.Domain.Entities;

namespace Bms.Store.Application.Services;

/// <summary>
/// 预约状态流转矩阵
/// 定义状态合法流转路径：当前状态 -> 允许的下一状态集合
/// 终态（已完成/已取消/爽约）不允许流转到任何状态
/// </summary>
public static class AppointmentStatusTransition
{
    /// <summary>
    /// 状态流转矩阵：Key=当前状态，Value=允许的下一状态集合
    /// </summary>
    private static readonly Dictionary<int, HashSet<int>> _transitions = new()
    {
        // 已预约（创建即已预约）-> 已到店 / 已完成 / 已取消 / 爽约
        { AppointmentStatus.Confirmed, new() { AppointmentStatus.Arrived, AppointmentStatus.Completed, AppointmentStatus.Cancelled, AppointmentStatus.NoShow } },
        // 已到店 -> 已完成 / 已取消
        { AppointmentStatus.Arrived, new() { AppointmentStatus.Completed, AppointmentStatus.Cancelled } },
        // 已完成（终态）
        { AppointmentStatus.Completed, new() },
        // 已取消（终态）
        { AppointmentStatus.Cancelled, new() },
        // 爽约（终态）
        { AppointmentStatus.NoShow, new() }
    };

    /// <summary>
    /// 判定状态流转是否合法
    /// </summary>
    /// <param name="from">当前状态</param>
    /// <param name="to">目标状态</param>
    /// <returns>合法流转返回 true，否则 false</returns>
    public static bool CanTransition(int from, int to)
    {
        if (from == to)
            return true; // 状态未变更，允许
        if (!_transitions.TryGetValue(from, out var allowed))
            return false;
        return allowed.Contains(to);
    }

    /// <summary>
    /// 获取指定状态允许流转的下一状态集合
    /// </summary>
    /// <param name="from">当前状态</param>
    /// <returns>允许的下一状态集合；未知状态返回空集合</returns>
    public static IReadOnlyCollection<int> GetAllowedTransitions(int from)
    {
        return _transitions.TryGetValue(from, out var allowed) ? allowed : Array.Empty<int>();
    }
}
