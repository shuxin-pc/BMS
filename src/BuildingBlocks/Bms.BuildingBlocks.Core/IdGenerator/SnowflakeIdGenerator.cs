namespace Bms.BuildingBlocks.Core.IdGenerator;

/// <summary>
/// 雪花ID生成器实现
/// 基于Twitter Snowflake算法
/// 格式：1位符号位 + 41位时间戳 + 10位工作节点ID + 12位序列号
/// </summary>
public class SnowflakeIdGenerator : ISnowflakeIdGenerator
{
    // 基准时间：2025-01-01 00:00:00 UTC
    private const long Twepoch = 1735689600000L;

    // 各部分位数
    private const int WorkerIdBits = 10;
    private const int SequenceBits = 12;

    // 最大值
    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    private const long MaxSequence = -1L ^ (-1L << SequenceBits);

    // 位移
    private const int WorkerIdShift = SequenceBits;
    private const int TimestampShift = SequenceBits + WorkerIdBits;

    // 锁对象
    private readonly object _lock = new();

    private long _workerId;
    private long _sequence = 0L;
    private long _lastTimestamp = -1L;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="workerId">工作节点ID (0-1023)</param>
    public SnowflakeIdGenerator(long workerId)
    {
        if (workerId < 0 || workerId > MaxWorkerId)
        {
            throw new ArgumentException($"Worker ID must be between 0 and {MaxWorkerId}", nameof(workerId));
        }

        _workerId = workerId;
    }

    /// <summary>
    /// 生成唯一ID
    /// </summary>
    /// <returns>雪花ID</returns>
    public long NewId()
    {
        lock (_lock)
        {
            var timestamp = GetCurrentTimestamp();

            // 时钟回拨检测
            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException($"Clock moved backwards. Refusing to generate ID for {_lastTimestamp - timestamp} milliseconds.");
            }

            // 同一毫秒内，序列号递增
            if (_lastTimestamp == timestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;

                // 序列号溢出，等待下一毫秒
                if (_sequence == 0)
                {
                    timestamp = GetNextTimestamp(_lastTimestamp);
                }
            }
            else
            {
                // 不同毫秒内，序列号重置为0
                _sequence = 0L;
            }

            _lastTimestamp = timestamp;

            // 组合各部分生成ID
            return ((timestamp - Twepoch) << TimestampShift)
                   | (_workerId << WorkerIdShift)
                   | _sequence;
        }
    }

    /// <summary>
    /// 获取当前时间戳（毫秒）
    /// </summary>
    private long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 等待下一毫秒
    /// </summary>
    private long GetNextTimestamp(long lastTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }
        return timestamp;
    }
}
