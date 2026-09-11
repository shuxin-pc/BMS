using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 预约号生成器
/// 预约号由后端自动生成，格式：AP{yyyyMMdd}{序号}，如 AP20260815001
/// </summary>
public static class AppointmentNoGenerator
{
    /// <summary>
    /// 构造预约事务级顾问锁键
    /// 按 (租户, 门店, 预约日期) 维度串行化并发请求，确保 AppointmentNo 的"查max+1"生成模式在并发下不产生重复值
    /// 必须用确定性哈希：HashCode.Combine 内部使用随机种子，相同输入每次调用值不同，会使顾问锁形同虚设
    /// </summary>
    public static long BuildLockKey(long tenantId, long storeId, DateTime startTime)
    {
        var dateInt = int.Parse(startTime.ToString("yyyyMMdd"));
        unchecked
        {
            var key = tenantId;
            key = key * 31 + storeId;
            key = key * 31 + dateInt;
            // 去除符号位保证非负（unchecked 允许乘法溢出按 long 截断，确定性不受影响）
            return key & long.MaxValue;
        }
    }

    /// <summary>
    /// 生成预约号
    /// 格式：AP{yyyyMMdd}{序号}，如 AP20260815001
    /// 序号为该预约日期下同租户+门店内的递增序号，3 位数补零
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="storeId">门店ID</param>
    /// <param name="startTime">预约开始时间（一体格式，仅取日期部分生成编号）</param>
    /// <returns>预约号</returns>
    public static async Task<string> GenerateAsync(
        StoreDbContext dbContext, long tenantId, long storeId, DateTime startTime)
    {
        var dateStr = startTime.ToString("yyyyMMdd");
        var prefix = $"AP{dateStr}";
        var maxAppointmentNo = await dbContext.Appointments
            .Where(a => a.TenantId == tenantId && a.StoreId == storeId
                        && a.AppointmentNo.StartsWith(prefix))
            .OrderByDescending(a => a.AppointmentNo)
            .Select(a => a.AppointmentNo)
            .FirstOrDefaultAsync();

        var nextSeq = 1;
        if (maxAppointmentNo != null && maxAppointmentNo.Length >= prefix.Length + 3)
        {
            if (int.TryParse(maxAppointmentNo.Substring(prefix.Length, 3), out var maxSeq))
                nextSeq = maxSeq + 1;
        }

        return $"{prefix}{nextSeq:D3}";
    }
}
