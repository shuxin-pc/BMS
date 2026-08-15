using Microsoft.EntityFrameworkCore;
using Bms.Store.Infrastructure;

namespace Bms.Store.Application.Services;

/// <summary>
/// 技师技能匹配服务
/// 技能匹配语义（树形展开求交集）：
/// - 服务项目适用技能集合与技师技能标签集合各自沿技能分类树展开为「自身 + 所有后代」，
///   两侧展开集合交集非空即匹配，保证仅选择父级分类时不遗漏拥有子级技能的技师。
/// - 服务项目未限定技能（集合为空）→ 所有技师可服务。
/// - 技师无技能标签（集合为空）→ 仅可服务未限定技能的服务项目。
/// </summary>
public class TechnicianSkillMatchService
{
    private readonly StoreDbContext _dbContext;

    /// <summary>
    /// 技能分类ID → 「自身 + 所有后代」ID 集合（按租户构建，当前请求内缓存）
    /// </summary>
    private Dictionary<long, HashSet<long>> _expandedMap = new();

    public TechnicianSkillMatchService(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 加载指定租户的技能分类树，构建「分类ID → 自身+所有后代」展开映射
    /// 每次匹配前必须先调用，保证技能树上下文一致
    /// </summary>
    /// <param name="tenantId">租户ID（技能分类为租户级共享）</param>
    public async Task BuildExpandedMapAsync(long tenantId)
    {
        var categories = await _dbContext.SkillCategories
            .Where(c => !c.IsDeleted && c.TenantId == tenantId)
            .Select(c => new { c.Id, c.ParentId })
            .ToListAsync();

        // 父分类ID → 子分类ID列表（仅含非空父ID，顶级分类不进入映射）
        var children = new Dictionary<long, List<long>>();
        foreach (var c in categories)
        {
            // 顶级分类（ParentId 为 null）跳过：Dictionary 不允许 null 作为 key，且根节点无需作为子节点挂接
            if (c.ParentId is null) continue;
            var parentId = c.ParentId.Value;
            if (!children.TryGetValue(parentId, out var list))
            {
                list = new List<long>();
                children[parentId] = list;
            }
            list.Add(c.Id);
        }

        var map = new Dictionary<long, HashSet<long>>();
        foreach (var c in categories)
        {
            var set = new HashSet<long>();
            CollectDescendants(c.Id, children, set);
            map[c.Id] = set;
        }
        _expandedMap = map;
    }

    /// <summary>
    /// 递归收集某分类及其所有后代的ID集合（set.Add 返回值防环，兼容脏数据）
    /// </summary>
    private static void CollectDescendants(long id, Dictionary<long, List<long>> children, HashSet<long> set)
    {
        if (!set.Add(id)) return;
        if (children.TryGetValue(id, out var childList))
        {
            foreach (var child in childList)
                CollectDescendants(child, children, set);
        }
    }

    /// <summary>
    /// 将一组技能分类ID展开为「自身 + 所有后代」合并集合
    /// </summary>
    /// <param name="skillCategoryIds">技能分类ID列表</param>
    /// <returns>展开后的分类ID集合</returns>
    public HashSet<long> Expand(IEnumerable<long> skillCategoryIds)
    {
        var result = new HashSet<long>();
        foreach (var id in skillCategoryIds)
        {
            if (_expandedMap.TryGetValue(id, out var set))
                result.UnionWith(set);
        }
        return result;
    }

    /// <summary>
    /// 判断技师是否可服务指定服务项目
    /// </summary>
    /// <param name="serviceSkillIds">服务项目适用技能分类ID列表（当前门店配置）</param>
    /// <param name="technicianSkillIds">技师技能标签分类ID列表</param>
    /// <returns>true=可服务</returns>
    public bool IsMatch(IEnumerable<long> serviceSkillIds, IEnumerable<long> technicianSkillIds)
    {
        var serviceIds = serviceSkillIds.ToList();
        var techIds = technicianSkillIds.ToList();
        // 服务项目未限定技能 → 所有技师可服务
        if (serviceIds.Count == 0) return true;
        // 技师无技能标签 → 仅可服务未限定技能的项目
        if (techIds.Count == 0) return false;
        var expandedService = Expand(serviceIds);
        var expandedTech = Expand(techIds);
        return expandedService.Overlaps(expandedTech);
    }
}
