namespace Bms.Store.Domain.Constants;

/// <summary>
/// 仪器设备状态常量
/// 依据：B7.1 设备状态明确为"正常/维修中/已停用"三种
/// </summary>
public static class EquipmentStatus
{
    /// <summary>
    /// 正常（可用状态）
    /// </summary>
    public const int Normal = 1;

    /// <summary>
    /// 维修中（不可用）
    /// </summary>
    public const int Maintenance = 2;

    /// <summary>
    /// 已停用（不可用）
    /// </summary>
    public const int Stopped = 3;

    private static readonly Dictionary<int, string> Names = new()
    {
        { Normal, "正常" },
        { Maintenance, "维修中" },
        { Stopped, "已停用" }
    };

    /// <summary>
    /// 校验状态值是否合法
    /// </summary>
    /// <param name="status">状态值</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(int status) => status == Normal || status == Maintenance || status == Stopped;

    /// <summary>
    /// 获取状态中文名称
    /// </summary>
    /// <param name="status">状态值</param>
    /// <returns>状态中文名，未知状态返回"未知"</returns>
    public static string GetName(int status) => Names.TryGetValue(status, out var name) ? name : "未知";
}
