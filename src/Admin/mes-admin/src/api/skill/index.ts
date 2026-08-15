// 技能分类配置 - API服务（对接后端 SkillCategoriesController）
import { request, buildQuery, type PagedResponse } from '../shared/storeRequest'
import type {
  SkillCategory,
  SkillCategoryQuery,
  SkillCategoryCreate,
  SkillCategoryUpdate
} from './types'

// 导出类型供外部使用
export type {
  SkillCategory,
  SkillCategoryQuery,
  SkillCategoryCreate,
  SkillCategoryUpdate,
  PagedResponse
}

// ==================== 工具函数 ====================

/**
 * 将后端的 parentId（null=顶级）转换为前端的 parentId（0=顶级）
 */
function normalizeParentId(parentId: number | null | undefined): number {
  return parentId ?? 0
}

/**
 * 将前端的 parentId（0=顶级）转换为后端的 parentId（null=顶级）
 */
function toBackendParentId(parentId: number): number | null {
  return parentId === 0 ? null : parentId
}

/**
 * 将平铺数据构建为树形结构
 * @param list 平铺数据
 * @param parentId 父ID（0表示顶级）
 * @returns 树形结构数据
 */
function buildTree(list: SkillCategory[], parentId = 0): SkillCategory[] {
  return list
    .filter(item => item.parentId === parentId)
    .map(item => {
      const children = buildTree(list, item.id)
      return {
        ...item,
        children: children.length > 0 ? children : undefined
      }
    })
}

/**
 * 递归过滤树形数据
 * @param nodes 树节点
 * @param keyword 关键字
 * @returns 过滤后的树
 */
function filterTree(nodes: SkillCategory[], keyword: string): SkillCategory[] {
  const result: SkillCategory[] = []
  for (const node of nodes) {
    const children = node.children ? filterTree(node.children, keyword) : []
    const matched = node.name.includes(keyword) || node.code.includes(keyword)
    if (matched || children.length > 0) {
      result.push({ ...node, children: children.length > 0 ? children : undefined })
    }
  }
  return result
}

// ==================== API 函数 ====================

/**
 * 获取技能分类树形数据
 * 后端返回分页平铺列表，前端构建为树形结构
 * @param query 查询参数
 * @returns 树形分类列表
 */
export async function getSkillCategoryTree(query?: SkillCategoryQuery): Promise<SkillCategory[]> {
  // 拉取足够大的分页以覆盖全部分类（技能分类数量有限）
  const qs = buildQuery({
    name: query?.name,
    code: query?.code,
    pageIndex: 1,
    pageSize: 1000
  })
  const paged = await request<PagedResponse<SkillCategory>>(`/skillCategories${qs}`)
  let list = paged.list.map(item => ({ ...item, parentId: normalizeParentId(item.parentId) }))

  let tree = buildTree(list)

  if (query?.name) {
    tree = filterTree(tree, query.name)
  }

  return tree
}

/**
 * 创建技能分类
 * @param data 分类信息
 */
export async function createSkillCategory(data: SkillCategoryCreate): Promise<void> {
  await request<SkillCategory>('/skillCategories', {
    method: 'POST',
    body: JSON.stringify({
      name: data.name,
      code: data.code,
      parentId: toBackendParentId(data.parentId)
    })
  })
}

/**
 * 更新技能分类
 * @param data 分类信息
 */
export async function updateSkillCategory(data: SkillCategoryUpdate): Promise<void> {
  await request<SkillCategory>(`/skillCategories/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify({
      id: data.id,
      name: data.name,
      code: data.code,
      parentId: toBackendParentId(data.parentId)
    })
  })
}

/**
 * 删除技能分类（后端为软删除，子分类需调用方自行处理或后端级联）
 * @param id 分类ID
 */
export async function deleteSkillCategory(id: number): Promise<void> {
  await request(`/skillCategories/${id}`, { method: 'DELETE' })
}
