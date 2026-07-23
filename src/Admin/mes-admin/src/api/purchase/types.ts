// ==========================================
// 采购管理类型定义（采购记录 + 采购退货）
// ==========================================

/**
 * 通用分页响应
 */
export interface PagedResponse<T> {
  list: T[]
  total: number
  pageIndex: number
  pageSize: number
}

/**
 * 采购单状态
 * - 1: 待审核
 * - 2: 已审核
 * - 3: 已入库
 * - 4: 已取消
 */
export type PurchaseOrderStatus = 1 | 2 | 3 | 4

/**
 * 采购类型
 * - 1: 零售商品采购
 * - 2: 耗材采购
 */
export type PurchaseType = 1 | 2

/**
 * 采购单明细项
 */
export interface PurchaseOrderItem {
  /** 明细ID */
  id: number
  /** 采购单ID */
  orderId: number
  /** 商品ID */
  productId: number
  /** 商品名称 */
  productName: string
  /** 商品编码 */
  productCode: string
  /** 采购数量 */
  quantity: number
  /** 采购单价 */
  unitPrice: number
  /** 小计金额 */
  subtotal: number
}

/**
 * 采购单信息
 */
export interface PurchaseOrder {
  /** 采购单ID */
  id: number
  /** 采购单号 */
  orderNo: string
  /** 供应商ID */
  supplierId: number
  /** 供应商名称 */
  supplierName: string
  /** 采购日期 */
  orderDate: string
  /** 采购总金额 */
  totalAmount: number
  /** 状态：1-待审核，2-已审核，3-已入库，4-已取消 */
  status: PurchaseOrderStatus
  /** 采购类型：1-零售商品采购，2-耗材采购 */
  purchaseType: PurchaseType
  /** 操作员ID */
  operatorId?: number
  /** 操作员名称 */
  operatorName?: string
  /** 备注 */
  remark?: string
  /** 采购明细列表 */
  items?: PurchaseOrderItem[]
}

/**
 * 采购单查询参数
 */
export interface PurchaseOrderQuery {
  /** 供应商ID */
  supplierId?: number
  /** 采购类型 */
  purchaseType?: PurchaseType
  /** 状态 */
  status?: PurchaseOrderStatus
  /** 采购开始日期 */
  startDate?: string
  /** 采购结束日期 */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 供应商累计采购额统计
 */
export interface SupplierPurchaseSummary {
  /** 供应商ID */
  supplierId: number
  /** 供应商名称 */
  supplierName: string
  /** 累计采购额 */
  totalAmount: number
  /** 采购单数 */
  orderCount: number
}

/**
 * 采购退货明细
 */
export interface PurchaseReturnItem {
  /** 明细ID */
  id: number
  /** 退货单ID */
  purchaseReturnId: number
  /** 商品ID */
  productId: number
  /** 退货数量 */
  quantity: number
  /** 退款金额 */
  refundAmount: number
  /** 批次号 */
  batchNo?: string
  /** 备注 */
  remark?: string
}

/**
 * 采购退货明细创建 DTO
 */
export interface PurchaseReturnItemCreate {
  /** 商品ID */
  productId: number
  /** 退货数量 */
  quantity: number
  /** 退款金额 */
  refundAmount: number
  /** 批次号 */
  batchNo?: string
  /** 备注 */
  remark?: string
}

/**
 * 采购退货信息（汇总+明细结构）
 */
export interface PurchaseReturn {
  /** 退货单ID */
  id: number
  /** 退货单号 */
  returnNo: string
  /** 供应商ID */
  supplierId: number
  /** 退货总数量（明细自动汇总） */
  totalQuantity: number
  /** 退款总金额（明细自动汇总） */
  totalRefundAmount: number
  /** 退货时间 */
  returnTime: string
  /** 凭证照片URL */
  voucherImageUrl?: string
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
  /** 退货明细列表 */
  items: PurchaseReturnItem[]
}

/**
 * 采购退货查询参数
 */
export interface PurchaseReturnQuery {
  /** 退货单号 */
  returnNo?: string
  /** 供应商ID */
  supplierId?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建采购退货请求（支持一次退回多种商品）
 */
export interface PurchaseReturnCreate {
  /** 退货单号 */
  returnNo: string
  /** 供应商ID */
  supplierId: number
  /** 退货时间 */
  returnTime: string
  /** 凭证照片URL */
  voucherImageUrl?: string
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 退货明细列表 */
  items: PurchaseReturnItemCreate[]
}
