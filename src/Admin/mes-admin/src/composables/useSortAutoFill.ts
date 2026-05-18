/**
 * 排序自动填充 Composable
 * 用于在新增表单时自动填充排序值（同层级最大序号+1）
 */
import { ref } from 'vue'

export interface SortItem {
  sort?: number | null
  parentId?: number | null
  children?: SortItem[]
  [key: string]: any
}

export interface FetchItemsOptions {
  parentId?: number | null
  group?: string | null
  tenantId?: number | string | null
}

export type FetchItemsFunc<T> = (options?: FetchItemsOptions) => T[] | Promise<T[]>

/**
 * 使用排序自动填充
 * @param fetchItems 获取完整列表数据的函数（支持异步，可直接调用 API）
 * @param getParentId 获取当前层级标识的函数（如 parentId），无层级填 null
 * @param getGroupValue 获取分组标识的函数（如 configGroup），无分组填 null
 * @param getTenantId 获取租户ID的函数，无租户概念可不传
 */
export function useSortAutoFill<T extends SortItem>(
  fetchItems: FetchItemsFunc<T>,
  getParentId: () => number | null,
  getGroupValue?: () => string | null,
  getTenantId?: () => number | string | null | undefined
) {
  // 当前计算的自动填充值
  const autoSort = ref<number>(0)

  // 加载状态
  const loading = ref(false)

  /**
   * 递归收集所有指定 parentId 下的直接子节点（扁平化）
   */
  function collectChildren(items: T[], targetParentId: number | null): T[] {
    const result: T[] = []
    for (const item of items) {
      const itemParentId = item.parentId ?? null
      // 目标为顶级（parentId=0）时，匹配 null 或 0；否则精确匹配
      const isRootMatch = targetParentId === 0
        ? (itemParentId === null || itemParentId === 0)
        : itemParentId === targetParentId
      if (isRootMatch) {
        result.push(item)
      }
      if (item.children?.length) {
        result.push(...collectChildren(item.children as T[], targetParentId))
      }
    }
    return result
  }

  /**
   * 计算并设置自动填充的排序值
   * @param excludeId 排除的 ID（如编辑时排除自身）
   */
  async function calculateAutoSort(excludeId?: number) {
    loading.value = true
    try {
      const currentParentId = getParentId()
      const currentGroup = getGroupValue?.()
      const currentTenantId = getTenantId?.()

      // 调用 fetchItems 获取数据（可以是 API 调用）
      const items = await fetchItems({ parentId: currentParentId, group: currentGroup ?? undefined, tenantId: currentTenantId })

      // 如果没有 parentId 概念（parentId 返回 null），直接使用 items 列表
      let siblings: T[]
      if (currentParentId === null) {
        siblings = [...items]
      } else {
        siblings = collectChildren(items, currentParentId)
      }

      // 按分组筛选
      if (currentGroup !== null && currentGroup !== undefined) {
        siblings = siblings.filter(item => item.configGroup === currentGroup)
      }

      // 排除自身
      const filtered = excludeId !== undefined
        ? siblings.filter(item => item.id !== excludeId)
        : siblings

      const maxSort = filtered.reduce((max, item) => Math.max(max, item.sort ?? 0), 0)
      autoSort.value = maxSort + 1
    } catch (error) {
      console.error('计算自动排序失败:', error)
      autoSort.value = 0
    } finally {
      loading.value = false
    }
  }

  return {
    autoSort,
    loading,
    calculateAutoSort
  }
}
