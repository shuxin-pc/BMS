// 库存管理 - API 服务
// 对接后端 InventoriesController / InventoryAlertsController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Inventory,
  InventoryQuery,
  InventoryAlert,
  InventoryAlertQuery,
  ExpiryInfo,
  ExpiryQuery,
  ExpiryAlert,
  ExpiryAlertQuery,
  ProductExpiryOption,
  InventoryBatch,
  InventoryBatchQuery
} from './types'

// 导出类型供外部使用
export type {
  Inventory,
  InventoryQuery,
  InventoryAlert,
  InventoryAlertQuery,
  ExpiryInfo,
  ExpiryQuery,
  ExpiryAlert,
  ExpiryAlertQuery,
  ProductExpiryOption,
  InventoryBatch,
  InventoryBatchQuery,
  PagedResponse
}

// ==================== 库存管理 ====================

/**
 * 获取库存分页列表
 * 对接后端：GET /api/store/inventories
 * @param query 查询参数（商品ID、分页）
 * @returns 分页库存列表
 */
export async function getInventoryList(query?: InventoryQuery): Promise<PagedResponse<Inventory>> {
  const qs = buildQuery({
    productId: query?.productId,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Inventory>>(`/inventories${qs}`)
}

/**
 * 获取库存预警列表
 * 对接后端：GET /api/store/inventoryAlerts
 * @param query 查询参数（商品ID、预警类型、是否已处理、分页）
 * @returns 分页预警列表
 */
export async function getInventoryAlerts(query?: InventoryAlertQuery): Promise<PagedResponse<InventoryAlert>> {
  const qs = buildQuery({
    productId: query?.productId,
    alertType: query?.alertType,
    isProcessed: query?.isProcessed === undefined ? undefined : String(query.isProcessed),
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<InventoryAlert>>(`/inventoryAlerts${qs}`)
}

// ==================== 效期管理 ====================
// 对接后端 InventoryBatchesController 的 expiries / expiryAlerts 接口

/**
 * 获取效期信息分页列表
 * 对接后端：GET /api/store/inventoryBatches/expiries
 * @param query 查询参数（商品名称、效期状态筛选）
 * @returns 分页效期列表
 */
export async function getExpiryList(query?: ExpiryQuery): Promise<PagedResponse<ExpiryInfo>> {
  const qs = buildQuery({
    productName: query?.productName,
    status: query?.status,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<ExpiryInfo>>(`/inventoryBatches/expiries${qs}`)
}

/**
 * 获取效期预警列表（即将过期或已过期的商品）
 * 对接后端：GET /api/store/inventoryBatches/expiryAlerts
 * @param query 查询参数（商品名称、分页）
 * @returns 分页预警列表
 */
export async function getExpiryAlerts(query?: ExpiryAlertQuery): Promise<PagedResponse<ExpiryAlert>> {
  const qs = buildQuery({
    productName: query?.productName,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<ExpiryAlert>>(`/inventoryBatches/expiryAlerts${qs}`)
}

/**
 * 按商品ID查询可用效期选项列表（用于 POS 效期选择）
 * 对接后端：GET /api/store/inventoryBatches/products/{productId}/expiry-options
 * @param productId 商品ID
 * @returns 可用效期选项列表（按到期日期升序，近效期优先）
 */
export async function getProductExpiryOptions(productId: number): Promise<ProductExpiryOption[]> {
  return request<ProductExpiryOption[]>(`/inventoryBatches/products/${productId}/expiry-options`)
}

// ==================== 库存批次管理 ====================
// 对接后端 InventoryBatchesController 的批次列表接口

/**
 * 获取库存批次分页列表
 * 对接后端：GET /api/store/inventoryBatches
 * @param query 查询参数（商品ID、批次号、状态、分页）
 * @returns 分页批次列表
 */
export async function getInventoryBatchList(query?: InventoryBatchQuery): Promise<PagedResponse<InventoryBatch>> {
  const qs = buildQuery({
    productId: query?.productId,
    batchNo: query?.batchNo,
    status: query?.status,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<InventoryBatch>>(`/inventoryBatches${qs}`)
}
