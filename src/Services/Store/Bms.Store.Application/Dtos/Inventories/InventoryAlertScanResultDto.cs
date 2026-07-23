namespace Bms.Store.Application.Dtos.Inventories;

/// <summary>
/// 库存预警扫描结果
/// </summary>
public class InventoryAlertScanResultDto
{
    /// <summary>新增低库存预警数</summary>
    public int LowStockCreated { get; set; }

    /// <summary>新增效期预警数</summary>
    public int ExpiryCreated { get; set; }

    /// <summary>新增积压预警数</summary>
    public int OverstockCreated { get; set; }

    /// <summary>更新为已过期的批次数</summary>
    public int BatchExpired { get; set; }
}
