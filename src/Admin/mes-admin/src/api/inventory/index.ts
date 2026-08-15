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
  InventoryAlertScanResult,
  ExpiryInfo,
  ExpiryQuery,
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
  InventoryAlertScanResult,
  ExpiryInfo,
  ExpiryQuery,
  ProductExpiryOption,
  InventoryBatch,
  InventoryBatchQuery,
  PagedResponse
}

// ==================== 库存管理 ====================

/**
 * 获取库存分页列表
 * 对接后端：GET /api/store/inventories
 * @param query 查询参数（商品ID、分类ID、商品名称、商品类型、库存状态、分页）
 * @returns 分页库存列表
 */
export async function getInventoryList(query?: InventoryQuery): Promise<PagedResponse<Inventory>> {
  const qs = buildQuery({
    productId: query?.productId,
    categoryId: query?.categoryId,
    productName: query?.productName,
    productType: query?.productType,
    inventoryStatus: query?.inventoryStatus,
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
    productName: query?.productName,
    alertType: query?.alertType,
    isProcessed: query?.isProcessed === undefined ? undefined : String(query.isProcessed),
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<InventoryAlert>>(`/inventoryAlerts${qs}`)
}

/**
 * 手动触发库存预警扫描（低库存/效期/积压）
 * 对接后端：POST /api/store/inventoryAlerts/scan
 * @returns 扫描结果（各类型新生成预警数量）
 */
export async function scanInventoryAlerts(): Promise<InventoryAlertScanResult> {
  return request<InventoryAlertScanResult>(`/inventoryAlerts/scan`, {
    method: 'POST'
  })
}

// ==================== 效期管理 ====================
// 对接后端 InventoryBatchesController 的 expiries 接口

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
