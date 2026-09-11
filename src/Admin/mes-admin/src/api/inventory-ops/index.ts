// 入库/出库操作 - API 服务
// 入库/出库相关函数均对接后端真实端点
import type {
  InventoryLog,
  InventoryLogQuery,
  OutboundRequest,
  OutboundResult,
  InventoryBatchOption,
  InventoryOpType,
  InboundSourceType,
  OutboundSourceType,
  InventoryLogSourceType,
  PagedResponse
} from './types'
import { request } from '../shared/storeRequest'

// 导出类型供外部使用
export type {
  InventoryLog,
  InventoryLogQuery,
  OutboundRequest,
  OutboundResult,
  InventoryBatchOption,
  InventoryOpType,
  InboundSourceType,
  OutboundSourceType,
  InventoryLogSourceType,
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
  if (query?.productId !== undefined) params.append('productId', String(query.productId))
  if (query?.productName) params.append('productName', query.productName)
  if (query?.type !== undefined) params.append('type', String(query.type))
  if (query?.sourceType !== undefined) params.append('sourceType', String(query.sourceType))
  if (query?.startDate) params.append('startDate', query.startDate)
  if (query?.endDate) params.append('endDate', query.endDate)
  if (query?.batchNo) params.append('batchNo', query.batchNo)
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<InventoryLog>>(`/inventorylogs?${params}`)
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
 * @returns 商品选项数组（含 Type 字段，用于按场景过滤）
 */
export async function getProductOptions(): Promise<{ id: number; name: string; code: string; unit?: string; type: number }[]> {
  return request<{ id: number; name: string; code: string; unit?: string; type: number }[]>('/product/products/options')
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
 * 库存操作类型标签映射
 */
export const inventoryOpTypeMap: Record<InventoryOpType, string> = {
  1: '入库',
  2: '出库',
  3: '盘点',
  4: '调拨'
}

/**
 * 库存流水来源类型标签映射（用于流水 Drawer 列展示与筛选）
 */
export const inventoryLogSourceTypeMap: Record<InventoryLogSourceType, string> = {
  0: '销售出库',
  1: '采购入库',
  2: '退货入库',
  3: '盘点调整',
  4: '调拨入库',
  5: '调拨出库',
  6: '采购退货出库',
  7: '项目卡核销出库',
  8: '样品领用出库',
  9: '赠品活动出库',
  10: '其他'
}

/**
 * 库存流水来源类型下拉选项（用于 Drawer 内 SourceType 筛选）
 */
export const inventoryLogSourceTypeOptions: { label: string; value: InventoryLogSourceType }[] = [
  { label: '销售出库', value: 0 },
  { label: '采购入库', value: 1 },
  { label: '退货入库', value: 2 },
  { label: '盘点调整', value: 3 },
  { label: '调拨入库', value: 4 },
  { label: '调拨出库', value: 5 },
  { label: '采购退货出库', value: 6 },
  { label: '项目卡核销出库', value: 7 },
  { label: '样品领用出库', value: 8 },
  { label: '赠品活动出库', value: 9 },
  { label: '其他', value: 10 }
]
