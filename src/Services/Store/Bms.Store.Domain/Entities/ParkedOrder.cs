namespace Bms.Store.Domain.Entities;

/// <summary>
/// 挂单状态常量
/// 1=挂起（待取单）；取单/取消已改为物理删除，新数据不再产生 2/3 状态，
/// 存量历史数据可能保留 2=已取走 / 3=已取消（保留不动，列表查询仅返回挂起状态）
/// </summary>
public static class ParkedOrderStatuses
{
    public const int Parked = 1;
}

/// <summary>
/// POS 挂单
/// 快速开单页暂存购物车草稿，门店内共享（同门店收银员均可取单）
/// 非核心业务表，无软删除；取单/取消通过 Status 状态流转归档
/// </summary>
public class ParkedOrder : StoreBusinessEntityBase
{
    /// <summary>
    /// 挂单号（PK{yyyyMMdd}{序号}，同租户同门店内唯一）
    /// </summary>
    public string ParkNo { get; set; } = string.Empty;

    /// <summary>
    /// 会员客户ID（挂单时购物车所选会员，冗余便于列表展示与取单恢复，散客为空）
    /// </summary>
    public long? CustomerId { get; set; }

    /// <summary>
    /// 会员客户姓名（冗余展示，散客为空）
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 挂单备注（客户称呼/电话等，选填，便于取单识别）
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 挂单人姓名（冗余展示，来自 ICurrentUser.RealName/UserName，取单列表展示识别谁挂的单）
    /// </summary>
    public string? CreatedByName { get; set; }

    /// <summary>
    /// 购物车 JSON 快照（CartItem[] 序列化字符串，取单时原样返回前端反序列化恢复）
    /// </summary>
    public string CartJson { get; set; } = string.Empty;

    /// <summary>
    /// 状态（1=挂起；取单/取消物理删除后新数据仅挂起，存量历史数据可能含 2/3）
    /// </summary>
    public int Status { get; set; } = ParkedOrderStatuses.Parked;
}
