// POS 挂单 - API 服务
// 对接后端 ParkedOrdersController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  ParkedOrder,
  ParkedOrderQuery,
  ParkedOrderCreate
} from './types'

// 导出类型供外部使用
export type {
  ParkedOrder,
  ParkedOrderQuery,
  ParkedOrderCreate,
  PagedResponse
}

/**
 * 挂单：保存当前购物车草稿
 * 对接后端：POST /api/store/parked-orders
 * @param data 挂单信息（含购物车 JSON 快照）
 * @returns 挂单信息
 */
export async function createParkedOrder(data: ParkedOrderCreate): Promise<ParkedOrder> {
  return request<ParkedOrder>('/parked-orders', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 挂单分页列表（挂起状态）
 * 对接后端：GET /api/store/parked-orders
 * @param query 查询参数
 * @returns 分页挂单列表
 */
export async function getParkedOrders(query?: ParkedOrderQuery): Promise<PagedResponse<ParkedOrder>> {
  const qs = buildQuery({
    keyword: query?.keyword,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<ParkedOrder>>(`/parked-orders${qs}`)
}

/**
 * 取单：恢复挂单购物车（挂单置为已取走，返回完整购物车快照）
 * 对接后端：POST /api/store/parked-orders/{id}/resume
 * @param id 挂单ID（字符串，避免 Number 精度丢失）
 * @returns 挂单完整信息（含 cartJson）
 */
export async function resumeParkedOrder(id: string): Promise<ParkedOrder> {
  return request<ParkedOrder>(`/parked-orders/${id}/resume`, { method: 'POST' })
}

/**
 * 取消挂单
 * 对接后端：POST /api/store/parked-orders/{id}/cancel
 * @param id 挂单ID（字符串，避免 Number 精度丢失）
 */
export async function cancelParkedOrder(id: string): Promise<void> {
  return request<void>(`/parked-orders/${id}/cancel`, { method: 'POST' })
}
