namespace Bms.BuildingBlocks.Core.IdGenerator;

/// <summary>
/// 雪花ID生成器接口
/// </summary>
public interface ISnowflakeIdGenerator
{
    /// <summary>
    /// 生成唯一ID
    /// </summary>
    /// <returns>雪花ID</returns>
    long NewId();
}
