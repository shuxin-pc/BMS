using System;
using System.Collections.Generic;

namespace Bms.Store.Application.Dtos.Resources;

/// <summary>
/// 资源可用性查询请求参数
/// </summary>
public class ResourceAvailabilityQueryDto
{
    /// <summary>
    /// 占用开始时间（含）
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 占用结束时间（不含）
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 门店ID（可空，null=当前租户全部门店）
    /// </summary>
    public long? StoreId { get; set; }

    /// <summary>
    /// 房间类型过滤（1:房间 2:床位，null=不过滤）
    /// 当 <see cref="ServiceProductId"/> 已传且对应服务项目有 RequiredRoomType 时，以服务项目的 RequiredRoomType 为准
    /// </summary>
    public int? RoomType { get; set; }

    /// <summary>
    /// 服务项目ID（可空，传入后按其 RequiredRoomType 自动过滤房间）
    /// 优先级高于 <see cref="RoomType"/>；若服务项目未配置 RequiredRoomType 则回退使用 <see cref="RoomType"/>
    /// </summary>
    public long? ServiceProductId { get; set; }
}

/// <summary>
/// 资源可用性查询结果
/// 用于前端技师/房间/设备下拉列表标红
/// </summary>
public class ResourceAvailabilityDto
{
    public List<TechnicianAvailabilityItem> Technicians { get; set; } = new();
    public List<RoomAvailabilityItem> Rooms { get; set; } = new();
    public List<EquipmentAvailabilityItem> Equipments { get; set; } = new();
}

/// <summary>
/// 技师可用性条目
/// </summary>
public class TechnicianAvailabilityItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Source { get; set; }

    /// <summary>
    /// 是否在指定时段被占用（true=标红）
    /// </summary>
    public bool IsOccupied { get; set; }

    /// <summary>
    /// 占用来源："appointment" 或 "order"
    /// </summary>
    public string? ConflictSource { get; set; }

    /// <summary>
    /// 冲突描述（占用单号 + 时段）
    /// </summary>
    public string? ConflictInfo { get; set; }
}

/// <summary>
/// 房间/床位可用性条目
/// </summary>
public class RoomAvailabilityItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RoomType { get; set; }
    public int Status { get; set; }

    public bool IsOccupied { get; set; }
    public string? ConflictSource { get; set; }
    public string? ConflictInfo { get; set; }
}

/// <summary>
/// 设备可用性条目
/// </summary>
public class EquipmentAvailabilityItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }

    public bool IsOccupied { get; set; }
    public string? ConflictSource { get; set; }
    public string? ConflictInfo { get; set; }
}
