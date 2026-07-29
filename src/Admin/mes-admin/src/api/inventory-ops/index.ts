// 入库/出库操作 - API 服务
// 入库/出库相关函数均对接后端真实端点
import type {
  InventoryLog,
  InventoryLogQuery,
  InboundRequest,
  OutboundRequest,
  OutboundResult,
  InventoryBatchOption,
  InventoryOpType,
  InboundSourceType,
  OutboundSourceType,
  PagedResponse
} from './types'
import { request } from '../shared/storeRequest'

// 导出类型供外部使用
export type {
  InventoryLog,
  InventoryLogQuery,
  InboundRequest,
  OutboundRequest,
  OutboundResult,
  InventoryBatchOption,
  InventoryOpType,
  InboundSourceType,
  OutboundSourceType,
  PagedResponse
}

// ==================== API 函数 ====================

/**
 * 获取库存操作日志分页列表
 * 对接后端：GET /api/store/inventorylogs
 * @param query 查询参数
 * @returns 分页日志列表
 */
export async function getInventoryLogList(query?: InventoryLogQuery): Promise<PagedResponse<InventoryLog>> {
  const params = new URLSearchParams()
  if (query?.productName) params.append('productName', query.productName)
  if (query?.type !== undefined) params.append('type', String(query.type))
  if (query?.sourceType !== undefined) params.append('sourceType', String(query.sourceType))
  if (query?.startDate) params.append('startDate', query.startDate)
  if (query?.endDate) params.append('endDate', query.endDate)
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<InventoryLog>>(`/inventorylogs?${params}`)
}

/**
 * 入库操作（事务内同步维护 InventoryLog + InventoryBatch + Inventory 三表）
 * 对接后端：POST /api/store/inventorylogs/inbound
 * @param data 入库请求
 * @returns 创建后的库存日志（含商品/供应商/操作人显示字段）
 */
export async function createInbound(data: InboundRequest): Promise<InventoryLog> {
  return request<InventoryLog>('/inventorylogs/inbound', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 出库操作（事务内按 FEFO 或手动指定扣减批次，写多条流水，更新汇总表）
 * 对接后端：POST /api/store/inventorylogs/outbound
 * @param data 出库请求（quantity FEFO 模式 / batchItems 手动模式二选一）
 * @returns 出库结果汇总（含批次扣减明细）
 */
export async function createOutbound(data: OutboundRequest): Promise<OutboundResult> {
  return request<OutboundResult>('/inventorylogs/outbound', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 获取商品轻量选项列表（用于下拉选择，不分页）
 * 对接后端：GET /api/store/product/products/options
 * 注意：ProductsController 路由前缀为 api/store/product/[controller]，与其他控制器不同
 * @returns 商品选项数组
 */
export async function getProductOptions(): Promise<{ id: number; name: string; code: string; unit?: string }[]> {
  return request<{ id: number; name: string; code: string; unit?: string }[]>('/product/products/options')
}

/**
 * 获取供应商轻量选项列表（用于下拉选择，不分页）
 * 对接后端：GET /api/store/suppliers/options
 * @returns 供应商选项数组
 */
export async function getSupplierOptions(): Promise<{ id: number; name: string }[]> {
  return request<{ id: number; name: string }[]>('/suppliers/options')
}

/**
 * 获取商品当前库存
 * 对接后端：GET /api/store/inventories?productId={id}&pageSize=1
 * @param productId 商品ID
 * @returns 当前库存数量（无记录返回 0）
 */
export async function getProductStock(productId: number): Promise<number> {
  const params = new URLSearchParams()
  params.append('productId', String(productId))
  params.append('pageSize', '1')
  const result = await request<PagedResponse<{ id: number; productId: number; quantity: number }> | null>(`/inventories?${params}`)
  // 无记录时后端返回空列表或 null，统一返回 0
  if (!result || !result.list || result.list.length === 0) return 0
  return result.list[0].quantity
}

/**
 * 获取商品在库批次列表（用于手动指定批次模式的批次选择）
 * 对接后端：GET /api/store/inventorybatches?productId={id}&status=1
 * @param productId 商品ID
 * @returns 在库批次列表（含批次号、过期日期、可用数量等）
 */
export async function getProductBatches(productId: number): Promise<InventoryBatchOption[]> {
  const params = new URLSearchParams()
  params.append('productId', String(productId))
  params.append('status', '1')
  params.append('pageSize', '100')
  const result = await request<PagedResponse<InventoryBatchOption> | null>(`/inventorybatches?${params}`)
  if (!result || !result.list) return []
  return result.list
}

/**
 * 入库来源类型标签映射
 */
export const inboundSourceTypeMap: Record<InboundSourceType, string> = {
  1: '采购入库',
  2: '退货入库',
  3: '盘点入库',
  4: '调拨入库'
}

/**
 * 出库来源类型标签映射
 */
export const outboundSourceTypeMap: Record<OutboundSourceType, string> = {
  3: '盘点盘亏',
  6: '其他',
  10: '样品领用',
  11: '赠品活动'
}

/**
 * 库存操作类型标签映射
 */
export const inventoryOpTypeMap: Record<InventoryOpType, string> = {
  1: '入库',
  2: '出库',
  3: '盘点',
  4: '调拨'
}
