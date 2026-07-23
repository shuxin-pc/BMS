// 采购管理 - API服务
import type {
  PurchaseOrder,
  PurchaseOrderStatus,
  PurchaseType,
  PurchaseOrderQuery,
  PurchaseOrderItem,
  SupplierPurchaseSummary,
  PurchaseReturn,
  PurchaseReturnQuery,
  PurchaseReturnCreate,
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
  PurchaseOrderItem,
  PurchaseOrderQuery,
  SupplierPurchaseSummary,
  PurchaseReturn,
  PurchaseReturnQuery,
  PurchaseReturnCreate,
  PurchaseReturnItem,
  PurchaseReturnItemCreate,
  PagedResponse
}

// ==================== Mock 数据 ====================

// 采购单明细 Mock 数据
const mockOrderItems: PurchaseOrderItem[] = [
  { id: 1, orderId: 1, productId: 101, productName: '深层修复洗发水', productCode: 'SP-001', quantity: 20, unitPrice: 55.00, subtotal: 1100.00 },
  { id: 2, orderId: 1, productId: 102, productName: '丝滑护发素', productCode: 'SP-002', quantity: 15, unitPrice: 35.00, subtotal: 525.00 },
  { id: 3, orderId: 2, productId: 103, productName: '植物染发剂', productCode: 'SP-003', quantity: 30, unitPrice: 90.00, subtotal: 2700.00 },
  { id: 4, orderId: 3, productId: 104, productName: '强力定型喷雾', productCode: 'SP-004', quantity: 40, unitPrice: 22.00, subtotal: 880.00 },
  { id: 5, orderId: 4, productId: 105, productName: '保湿护肤霜', productCode: 'SP-005', quantity: 10, unitPrice: 120.00, subtotal: 1200.00 },
  { id: 6, orderId: 5, productId: 101, productName: '深层修复洗发水', productCode: 'SP-001', quantity: 25, unitPrice: 52.00, subtotal: 1300.00 },
  { id: 7, orderId: 6, productId: 106, productName: '一次性毛巾', productCode: 'HC-001', quantity: 200, unitPrice: 1.50, subtotal: 300.00 },
  { id: 8, orderId: 7, productId: 107, productName: '染发碗刷套装', productCode: 'HC-002', quantity: 50, unitPrice: 8.00, subtotal: 400.00 }
]

// 采购单 Mock 数据
const mockPurchaseOrders: PurchaseOrder[] = [
  {
    id: 1,
    orderNo: 'PO20260701001',
    supplierId: 1,
    supplierName: '广州美妆实业有限公司',
    orderDate: '2026-07-01T09:30:00',
    totalAmount: 1625.00,
    status: 3,
    purchaseType: 1,
    operatorId: 1,
    operatorName: '张店长',
    remark: '月初常规补货'
  },
  {
    id: 2,
    orderNo: 'PO20260703002',
    supplierId: 2,
    supplierName: '深圳染烫材料有限公司',
    orderDate: '2026-07-03T14:00:00',
    totalAmount: 2700.00,
    status: 3,
    purchaseType: 1,
    operatorId: 1,
    operatorName: '张店长',
    remark: '染发剂库存不足，紧急补货'
  },
  {
    id: 3,
    orderNo: 'PO20260705003',
    supplierId: 3,
    supplierName: '上海造型工具批发商',
    orderDate: '2026-07-05T10:15:00',
    totalAmount: 880.00,
    status: 2,
    purchaseType: 1,
    operatorId: 2,
    operatorName: '李经理',
    remark: '定型喷雾促销备货'
  },
  {
    id: 4,
    orderNo: 'PO20260708004',
    supplierId: 4,
    supplierName: '杭州护肤品牌代理商',
    orderDate: '2026-07-08T16:30:00',
    totalAmount: 1200.00,
    status: 1,
    purchaseType: 1,
    operatorId: 1,
    operatorName: '张店长',
    remark: '高端护肤品类引入'
  },
  {
    id: 5,
    orderNo: 'PO20260710005',
    supplierId: 1,
    supplierName: '广州美妆实业有限公司',
    orderDate: '2026-07-10T09:00:00',
    totalAmount: 1300.00,
    status: 2,
    purchaseType: 1,
    operatorId: 2,
    operatorName: '李经理',
    remark: '洗发水二次补货'
  },
  {
    id: 6,
    orderNo: 'PO20260711006',
    supplierId: 5,
    supplierName: '成都美发设备贸易公司',
    orderDate: '2026-07-11T11:20:00',
    totalAmount: 300.00,
    status: 3,
    purchaseType: 2,
    operatorId: 1,
    operatorName: '张店长',
    remark: '一次性毛巾耗材采购'
  },
  {
    id: 7,
    orderNo: 'PO20260712007',
    supplierId: 2,
    supplierName: '深圳染烫材料有限公司',
    orderDate: '2026-07-12T08:45:00',
    totalAmount: 400.00,
    status: 4,
    purchaseType: 2,
    operatorId: 1,
    operatorName: '张店长',
    remark: '染发工具耗材，因质量问题取消'
  }
]

// ==================== 采购单 API ====================

/**
 * 获取采购单分页列表
 * @param query 查询参数（供应商、采购类型、状态、时间范围）
 * @returns 分页采购单列表
 */
export async function getPurchaseOrders(query?: PurchaseOrderQuery): Promise<PagedResponse<PurchaseOrder>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockPurchaseOrders]
  if (query?.supplierId !== undefined) {
    list = list.filter(item => item.supplierId === query.supplierId)
  }
  if (query?.purchaseType !== undefined) {
    list = list.filter(item => item.purchaseType === query.purchaseType)
  }
  if (query?.status !== undefined) {
    list = list.filter(item => item.status === query.status)
  }
  if (query?.startDate) {
    list = list.filter(item => item.orderDate >= query.startDate!)
  }
  if (query?.endDate) {
    list = list.filter(item => item.orderDate <= query.endDate! + 'T23:59:59')
  }
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 获取采购单详情（含明细列表）
 * @param id 采购单ID
 * @returns 采购单详情（含明细）
 */
export async function getPurchaseOrder(id: number): Promise<PurchaseOrder> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const order = mockPurchaseOrders.find(o => o.id === id)
  if (!order) {
    throw new Error('采购单不存在')
  }
  const items = mockOrderItems.filter(item => item.orderId === id)
  return { ...order, items }
}

/**
 * 获取各供应商累计采购额统计
 * @returns 供应商采购额汇总列表
 */
export async function getSupplierPurchaseSummary(): Promise<SupplierPurchaseSummary[]> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const summaryMap = new Map<number, SupplierPurchaseSummary>()
  for (const order of mockPurchaseOrders) {
    if (order.status === 4) continue // 已取消的不计入统计
    const existing = summaryMap.get(order.supplierId)
    if (existing) {
      existing.totalAmount += order.totalAmount
      existing.orderCount += 1
    } else {
      summaryMap.set(order.supplierId, {
        supplierId: order.supplierId,
        supplierName: order.supplierName,
        totalAmount: order.totalAmount,
        orderCount: 1
      })
    }
  }
  return Array.from(summaryMap.values()).sort((a, b) => b.totalAmount - a.totalAmount)
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
 * @param id 退货单ID
 * @param data 退货信息（含明细列表）
 * @returns 更新后的退货记录
 */
export async function updatePurchaseReturn(id: number, data: PurchaseReturnCreate): Promise<PurchaseReturn> {
  return request<PurchaseReturn>(`/purchaseReturns/${id}`, {
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
