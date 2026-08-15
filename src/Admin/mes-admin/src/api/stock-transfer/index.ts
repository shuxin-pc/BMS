// 库存调拨 - API 服务
// 对接后端 StockTransfersController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import { getAuthorizedStores } from '../store'
import { getProducts } from '../product'
import type {
  StockTransfer,
  StockTransferItem,
  StockTransferQuery,
  StockTransferCreate,
  TransferItemCreate,
  TransferStatus,
  StockTransferProductOption,
  StockTransferBatchOption
} from './types'

// 导出类型供外部使用
export type {
  StockTransfer,
  StockTransferItem,
  StockTransferQuery,
  StockTransferCreate,
  TransferItemCreate,
  TransferStatus,
  PagedResponse,
  StockTransferProductOption,
  StockTransferBatchOption
}

// ==================== API 函数 ====================

/**
 * 获取调拨单分页列表
 * 对接后端：GET /api/store/stocktransfers
 * @param query 查询参数
 * @returns 分页调拨单列表
 */
export async function getStockTransferList(query?: StockTransferQuery): Promise<PagedResponse<StockTransfer>> {
  const qs = buildQuery({
    transferNo: query?.transferNo,
    status: query?.status,
    fromStoreId: query?.fromStoreId,
    toStoreId: query?.toStoreId,
    startDate: query?.startDate,
    endDate: query?.endDate,
    productType: query?.productType,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<StockTransfer>>(`/stocktransfers${qs}`)
}

/**
 * 获取调拨单详情（含明细列表）
 * 对接后端：GET /api/store/stocktransfers/{id}
 * @param id 调拨单ID
 * @returns 调拨单详情
 */
export async function getStockTransferDetail(id: string): Promise<StockTransfer> {
  return request<StockTransfer>(`/stocktransfers/${id}`)
}

/**
 * 创建调拨单
 * 对接后端：POST /api/store/stocktransfers
 * 调拨单号由后端自动生成
 * @param data 创建请求
 * @returns 创建后的调拨单
 */
export async function createStockTransfer(data: StockTransferCreate): Promise<StockTransfer> {
  return request<StockTransfer>('/stocktransfers', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 删除调拨单（仅待调出/已取消可删除）
 * 对接后端：DELETE /api/store/stocktransfers/{id}
 * @param id 调拨单ID
 */
export async function deleteStockTransfer(id: string): Promise<void> {
  await request<void>(`/stocktransfers/${id}`, { method: 'DELETE' })
}

/**
 * 执行调拨（调出门店扣减库存 + 调入门店增加库存，状态转为已调入）
 * 对接后端：POST /api/store/stocktransfers/{id}/execute
 * @param id 调拨单ID
 */
export async function executeStockTransfer(id: string): Promise<void> {
  await request<void>(`/stocktransfers/${id}/execute`, { method: 'POST' })
}

/**
 * 取消调拨单（仅待调出状态可取消）
 * 对接后端：POST /api/store/stocktransfers/{id}/cancel
 * @param id 调拨单ID
 * @param reason 取消原因（可选）
 */
export async function cancelStockTransfer(id: string, reason?: string): Promise<void> {
  const qs = buildQuery({ reason })
  await request<void>(`/stocktransfers/${id}/cancel${qs}`, { method: 'POST' })
}

/**
 * 获取门店选项列表（用于下拉选择）
 * 复用门店授权接口，仅返回当前用户授权的启用门店
 * @returns 门店选项数组
 */
export async function getStoreOptions(): Promise<{ id: string; code: string; name: string }[]> {
  const stores = await getAuthorizedStores()
  return stores.map(s => ({ id: s.id, code: s.code, name: s.name }))
}

/**
 * 获取商品选项列表（用于下拉选择）
 * @returns 商品选项数组
 */
export async function getProductOptions(): Promise<{ id: number; name: string; code: string; unit: string }[]> {
  const res = await getProducts({ pageIndex: 1, pageSize: 200 })
  return res.list.map(p => ({ id: p.id, name: p.name, code: p.code, unit: p.unit || '' }))
}

/**
 * 获取调出门店的库存商品选项（仅返回有库存的商品，含正品/样品/赠品）
 * 对接后端：GET /api/store/stocktransfers/from-store-products?fromStoreId=xxx
 * @param fromStoreId 调出门店ID
 * @returns 商品选项数组（含库存量）
 */
export async function getFromStoreProducts(fromStoreId: string): Promise<StockTransferProductOption[]> {
  const qs = buildQuery({ fromStoreId })
  return request<StockTransferProductOption[]>(`/stocktransfers/from-store-products${qs}`)
}

/**
 * 获取调出门店指定商品的在库批次列表（按过期日期升序，FEFO）
 * 对接后端：GET /api/store/stocktransfers/from-store-products/{productId}/batches?fromStoreId=xxx
 * @param fromStoreId 调出门店ID
 * @param productId 商品ID
 * @returns 批次选项数组
 */
export async function getFromStoreProductBatches(fromStoreId: string, productId: number): Promise<StockTransferBatchOption[]> {
  const qs = buildQuery({ fromStoreId })
  return request<StockTransferBatchOption[]>(`/stocktransfers/from-store-products/${productId}/batches${qs}`)
}

/**
 * 调拨单状态标签映射
 */
export const transferStatusMap: Record<TransferStatus, string> = {
  1: '待调出',
  3: '已调入',
  4: '已取消'
}

/**
 * 调拨单状态对应的 tag 类型
 */
export const transferStatusTagType: Record<TransferStatus, 'warning' | 'success' | 'info'> = {
  1: 'warning',
  3: 'success',
  4: 'info'
}

/**
 * 商品类型标签映射
 * 1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品
 */
export const productTypeMap: Record<number, string> = {
  1: '正品',
  2: '服务',
  3: '耗材',
  4: '样品',
  5: '赠品'
}
