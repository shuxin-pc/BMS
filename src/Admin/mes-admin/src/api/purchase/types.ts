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
 * - 1: 已入库（采购单创建即入库，单据不可变）
 */
export type PurchaseOrderStatus = 1

/**
 * 采购类型
 * - 1: 零售商品采购
 * - 2: 耗材采购
 */
export type PurchaseType = 1 | 2

/**
 * 采购单明细项
 * 对应后端 PurchaseOrderItemDto
 */
export interface PurchaseOrderItem {
  /** 明细ID */
  id: number
  /** 采购单ID */
  purchaseOrderId: number
  /** 供应商ID（明细级，支持一个订单多供应商） */
  supplierId: number
  /** 商品ID */
  productId: number
  /** 采购数量 */
  quantity: number
  /** 采购单价 */
  unitPrice: number
  /** 小计金额 */
  totalPrice: number
  /** 批次号 */
  batchNo?: string
  /** 生产日期 */
  productionDate?: string
  /** 保质期天数 */
  shelfLifeDays?: number
  /** 过期日期 */
  expirationDate?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 采购单信息
 * 对应后端 PurchaseOrderDto
 */
export interface PurchaseOrder {
  /** 采购单ID */
  id: number
  /** 采购单号（后端自动生成） */
  orderNo: string
  /** 采购日期 */
  orderDate: string
  /** 采购总金额 */
  totalAmount: number
  /** 状态：1-已入库 */
  status: PurchaseOrderStatus
  /** 采购类型：1-零售商品采购，2-耗材采购 */
  purchaseType: PurchaseType
  /** 操作员ID */
  operatorId?: number
  /** 操作员姓名 */
  operatorName?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
  /** 采购明细列表 */
  items?: PurchaseOrderItem[]
}

/**
 * 采购单查询参数
 */
export interface PurchaseOrderQuery {
  /** 供应商ID（后端按明细供应商子查询过滤） */
  supplierId?: number
  /** 商品ID（后端按明细商品子查询过滤） */
  productId?: number
  /** 采购类型 */
  purchaseType?: PurchaseType
  /** 采购单号 */
  orderNo?: string
  /** 采购日期开始（含当天） */
  orderDateStart?: string
  /** 采购日期结束（含当天） */
  orderDateEnd?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建采购订单明细 DTO
 */
export interface PurchaseOrderItemCreate {
  /** 供应商ID */
  supplierId: number
  /** 商品ID */
  productId: number
  /** 采购数量 */
  quantity: number
  /** 采购单价 */
  unitPrice: number
  /** 生产日期 */
  productionDate?: string
  /** 保质期天数 */
  shelfLifeDays?: number
  /** 过期日期 */
  expirationDate?: string
  /** 备注 */
  remark?: string
}

/**
 * 创建采购订单请求（不传 OrderNo、Status，后端自动生成/固定）
 */
export interface PurchaseOrderCreate {
  /** 采购日期 */
  orderDate: string
  /** 采购类型 */
  purchaseType: PurchaseType
  /** 采购总金额（明细自动汇总） */
  totalAmount: number
  /** 备注 */
  remark?: string
  /** 明细列表 */
  items: PurchaseOrderItemCreate[]
}

/**
 * 供应商采购统计的商品明细（Top 5）
 */
export interface SupplierPurchaseSummaryProduct {
  /** 商品ID */
  productId: number
  /** 商品名称（后端填充） */
  productName: string
  /** 采购数量 */
  quantity: number
  /** 采购额 */
  totalAmount: number
  /** 涉及采购单数（去重） */
  orderCount: number
}

/**
 * 供应商累计采购额统计
 */
export interface SupplierPurchaseSummary {
  /** 供应商ID */
  supplierId: number
  /** 供应商名称（后端填充） */
  supplierName: string
  /** 累计采购额 */
  totalAmount: number
  /** 采购单数 */
  orderCount: number
  /** 采购商品数（去重） */
  productCount: number
  /** 最近采购时间（ISO 字符串） */
  lastPurchaseTime?: string
  /** Top 5 商品明细（按采购额降序） */
  topProducts: SupplierPurchaseSummaryProduct[]
}

/**
 * 供应商采购统计查询参数（复用采购订单页面的筛选条件）
 */
export interface SupplierPurchaseSummaryQuery {
  supplierId?: number
  productId?: number
  orderNo?: string
  purchaseType?: PurchaseType
  orderDateStart?: string
  orderDateEnd?: string
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
 * 退货单号由后端自动生成（PR{yyyyMMdd}{序号}），创建时不传；编辑时回填原值
 */
export interface PurchaseReturnCreate {
  /** 退货单号（创建时不传，由后端生成；编辑时回填原值） */
  returnNo?: string
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

/** 更新采购退货请求（含退货单ID，参考 system 服务 UserUpdate/RoleUpdate 做法，body 传 id 供后端模型绑定） */
export interface PurchaseReturnUpdate extends PurchaseReturnCreate {
  /** 退货单ID */
  id: number
}
