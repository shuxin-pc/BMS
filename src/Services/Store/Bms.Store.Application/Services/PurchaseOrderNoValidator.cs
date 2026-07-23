using System.Globalization;

namespace Bms.Store.Application.Services;

/// <summary>
/// 采购单号格式校验器
/// 采购单号必须为 8 位有效日期格式 YYYYMMDD（如 20260718），支持手工录入但需通过格式校验
/// </summary>
public static class PurchaseOrderNoValidator
{
    /// <summary>
    /// 校验采购单号是否为 8 位有效日期格式 YYYYMMDD
    /// </summary>
    /// <param name="orderNo">采购单号</param>
    /// <returns>合法返回 true，否则 false</returns>
    public static bool IsValid(string orderNo)
    {
        if (string.IsNullOrWhiteSpace(orderNo) || orderNo.Length != 8)
            return false;

        return DateTime.TryParseExact(orderNo, "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _);
    }
}
