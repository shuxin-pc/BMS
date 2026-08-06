// 耗材 BOM 管理 - API 服务
// 对接后端 ServiceBomsController（路由 api/store/serviceboms）
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  BomItem,
  BomQuery,
  BomCreate,
  BomUpdate
} from './types'

// 导出类型供外部使用
export type {
  BomItem,
  BomQuery,
  BomCreate,
  BomUpdate,
  PagedResponse
}

// ==================== BOM 管理 ====================

/**
 * 获取 BOM 分页列表（支持按服务项目名称/耗材名称模糊筛选）
 * 对接后端：GET /api/store/serviceboms
 * @param query 查询参数
 * @returns 分页 BOM 列表
 */
export async function getBomList(query?: BomQuery): Promise<PagedResponse<BomItem>> {
  const qs = buildQuery({
    serviceProductName: query?.serviceProductName,
    consumableProductName: query?.consumableProductName,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<BomItem>>(`/serviceboms${qs}`)
}

/**
 * 创建 BOM 项
 * 对接后端：POST /api/store/serviceboms
 * @param data 创建请求
 * @returns 创建后的 BOM 项
 */
export async function createBom(data: BomCreate): Promise<BomItem> {
  return request<BomItem>('/serviceboms', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新 BOM 项
 * 对接后端：PUT /api/store/serviceboms/{id}
 * @param data 更新请求
 * @returns 更新后的 BOM 项
 */
export async function updateBom(data: BomUpdate): Promise<BomItem> {
  return request<BomItem>(`/serviceboms/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除 BOM 项
 * 对接后端：DELETE /api/store/serviceboms/{id}
 * @param id BOM 记录ID
 */
export async function deleteBom(id: number): Promise<void> {
  return request<void>(`/serviceboms/${id}`, {
    method: 'DELETE'
  })
}

// ==================== 下拉选项 ====================

/**
 * 获取服务项目选项列表（用于下拉选择）
 * 对接后端：GET /api/store/serviceboms/service-product-options
 * @returns 服务项目选项数组
 */
export async function getServiceProductOptions(): Promise<{ id: number; name: string }[]> {
  return request<{ id: number; name: string }[]>('/serviceboms/service-product-options')
}

/**
 * 获取耗材商品选项列表（用于下拉选择）
 * 对接后端：GET /api/store/serviceboms/consumable-options
 * @returns 耗材商品选项数组
 */
export async function getConsumableOptions(): Promise<{ id: number; name: string; code: string; unit: string }[]> {
  return request<{ id: number; name: string; code: string; unit: string }[]>('/serviceboms/consumable-options')
}
