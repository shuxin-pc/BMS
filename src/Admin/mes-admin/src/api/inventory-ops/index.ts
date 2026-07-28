// 入库/出库操作 - API 服务
// 入库相关函数对接后端真实端点；出库暂保留 Mock，下一期替换
import type {
  InventoryLog,
  InventoryLogQuery,
  InboundRequest,
  OutboundRequest,
  InventoryOpType,
  InboundSourceType,
  PagedResponse
} from './types'
import { request } from '../shared/storeRequest'

// 导出类型供外部使用
export type {
  InventoryLog,
  InventoryLogQuery,
  InboundRequest,
  OutboundRequest,
  InventoryOpType,
  InboundSourceType,
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
 * 出库操作（Mock 实现，下一期做出库时替换为 POST /inventorylogs/outbound）
 * @param data 出库请求
 * @returns 创建后的库存日志
 */
export async function createOutbound(data: OutboundRequest): Promise<InventoryLog> {
  // Mock 实现：出库功能下一期对接后端
  // 不依赖共享 Mock 状态，仅返回合成的日志对象
  return {
    id: 0,
    productId: data.productId,
    productName: '',
    type: 2,
    quantity: -data.quantity,
    beforeQuantity: 0,
    afterQuantity: 0,
    remark: data.remark,
    createdAt: new Date().toISOString(),
    operatorName: '当前用户'
  }
}

/**
 * 获取商品轻量选项列表（用于下拉选择，不分页）
 * 对接后端：GET /api/store/product/products/options
 * 注意：ProductsController 路由前缀为 api/store/product/[controller]，与其他控制器不同
 * @returns 商品选项数组
 */
export async function getProductOptions(): Promise<{ id: number; name: string; code: string; unit: string }[]> {
  return request<{ id: number; name: string; code: string; unit: string }[]>('/product/products/options')
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
