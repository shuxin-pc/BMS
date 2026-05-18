namespace Bms.BuildingBlocks.Core.Attributes;

/// <summary>
/// 忽略ID自动生成特性
/// 标记此特性的实体在保存时不会自动生成ID
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class IgnoreIdGenerationAttribute : Attribute
{
}
