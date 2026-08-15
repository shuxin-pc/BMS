namespace Bms.BuildingBlocks.Storage.Internal;

/// <summary>
/// 桶内对象的元信息，仅含孤儿判定所需的最小字段集
/// </summary>
/// <param name="Key">对象键</param>
/// <param name="LastModifiedUtc">最后修改时间（UTC）。用于保留期判定，须已规范化为 UTC</param>
internal sealed record StoredObject(string Key, DateTime LastModifiedUtc);
