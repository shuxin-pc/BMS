// 采购管理 - API服务
import type {
  PurchaseOrder,
  PurchaseOrderStatus,
  PurchaseType,
  PurchaseOrderQuery,
  PurchaseOrderItem,
  PurchaseOrderCreate,
  PurchaseOrderItemCreate,
  SupplierPurchaseSummary,
  SupplierPurchaseSummaryProduct,
  SupplierPurchaseSummaryQuery,
  PurchaseReturn,
  PurchaseReturnQuery,
  PurchaseReturnCreate,
  PurchaseReturnUpdate,
  PurchaseReturnItem,
  PurchaseReturnItemCreate,
  PagedResponse
} from './types'
import { request, buildQuery } from '@/api/shared/storeRequest'

// 导出类型供外部使用
export type {
  PurchaseOrder,
  PurchaseOrderStatus,
  PurchaseType,
  PurchaseOrderQuery,
  PurchaseOrderItem,
  PurchaseOrderCreate,
  PurchaseOrderItemCreate,
  SupplierPurchaseSummary,
  SupplierPurchaseSummaryProduct,
  SupplierPurchaseSummaryQuery,
  PurchaseReturn,
  PurchaseReturnQuery,
  PurchaseReturnCreate,
  PurchaseReturnUpdate,
  PurchaseReturnItem,
  PurchaseReturnItemCreate,
  PagedResponse
}

// ==================== 采购单 API ====================

/**
 * 获取采购单分页列表
 * 对接后端：GET /api/store/purchaseOrders
 * @param query 查询参数（供应商、商品、采购类型、单号、采购日期范围）
 * @returns 分页采购单列表（含明细）
 */
export async function getPurchaseOrders(query?: PurchaseOrderQuery): Promise<PagedResponse<PurchaseOrder>> {
  const qs = buildQuery({
    orderNo: query?.orderNo,
    supplierId: query?.supplierId,
    productId: query?.productId,
    purchaseType: query?.purchaseType,
    orderDateStart: query?.orderDateStart,
    orderDateEnd: query?.orderDateEnd,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<PurchaseOrder>>(`/purchaseOrders${qs}`)
}

/**
 * 获取采购单详情（含明细列表）
 * 对接后端：GET /api/store/purchaseOrders/{id}
 * @param id 采购单ID
 * @returns 采购单详情（含明细）
 */
export async function getPurchaseOrder(id: number): Promise<PurchaseOrder> {
  return request<PurchaseOrder>(`/purchaseOrders/${id}`)
}

/**
 * 创建采购订单（创建即入库，后端自动生成单号并联动库存）
 * 对接后端：POST /api/store/purchaseOrders
 * @param data 采购订单数据（不传 OrderNo、Status）
 * @returns 创建后的采购订单
 */
export async function createPurchaseOrder(data: PurchaseOrderCreate): Promise<PurchaseOrder> {
  return request<PurchaseOrder>('/purchaseOrders', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 获取供应商采购统计（后端聚合，支持筛选条件）
 * 对接后端：GET /api/store/suppliers/purchase-summary
 * @param query 筛选条件（复用采购订单页面 searchForm）
 */
export async function getSupplierPurchaseSummary(query?: SupplierPurchaseSummaryQuery): Promise<SupplierPurchaseSummary[]> {
  const qs = buildQuery({
    supplierId: query?.supplierId,
    productId: query?.productId,
    orderNo: query?.orderNo,
    purchaseType: query?.purchaseType,
    orderDateStart: query?.orderDateStart,
    orderDateEnd: query?.orderDateEnd
  })
  return request<SupplierPurchaseSummary[]>(`/suppliers/purchase-summary${qs}`)
}

// ==================== 采购退货 API ====================

/**
 * 获取采购退货分页列表（包含明细）
 * @param query 查询参数（退货单号、供应商）
 * @returns 分页退货列表
 */
export async function getPurchaseReturns(query?: PurchaseReturnQuery): Promise<PagedResponse<PurchaseReturn>> {
  const qs = buildQuery({
    returnNo: query?.returnNo,
    supplierId: query?.supplierId,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<PurchaseReturn>>(`/purchaseReturns${qs}`)
}

/**
 * 根据ID获取采购退货详情（包含明细）
 * @param id 退货单ID
 * @returns 退货详情
 */
export async function getPurchaseReturnById(id: number): Promise<PurchaseReturn> {
  return request<PurchaseReturn>(`/purchaseReturns/${id}`)
}

/**
 * 创建采购退货（支持一次退回多种商品）
 * @param data 退货信息（含明细列表）
 * @returns 创建后的退货记录
 */
export async function createPurchaseReturn(data: PurchaseReturnCreate): Promise<PurchaseReturn> {
  return request<PurchaseReturn>('/purchaseReturns', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新采购退货
 * @param data 退货信息（含退货单ID，body 传 id 供后端模型绑定后通过 FluentValidation 自动验证）
 * @returns 更新后的退货记录
 */
export async function updatePurchaseReturn(data: PurchaseReturnUpdate): Promise<PurchaseReturn> {
  return request<PurchaseReturn>(`/purchaseReturns/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除采购退货
 * @param id 退货单ID
 */
export async function deletePurchaseReturn(id: number): Promise<void> {
  await request<void>(`/purchaseReturns/${id}`, { method: 'DELETE' })
}

/**
 * 批量删除采购退货
 * @param ids 退货单ID列表
 */
export async function batchDeletePurchaseReturns(ids: number[]): Promise<void> {
  await request<void>('/purchaseReturns/batch', {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}
