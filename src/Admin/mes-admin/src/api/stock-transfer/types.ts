// ==========================================
// 库存调拨类型定义
// 对应后端实体 StockTransfer + StockTransferItem
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
 * 调拨单状态
 * - 1: 待调出
 * - 2: 已调出
 * - 3: 已调入
 * - 4: 已取消
 */
export type TransferStatus = 1 | 2 | 3 | 4

/**
 * 调拨单明细项
 * 对应后端实体 StockTransferItem
 */
export interface StockTransferItem {
  /** 明细ID */
  id: number
  /** 调拨单ID */
  stockTransferId: number
  /** 商品ID */
  productId: number
  /** 商品名称（冗余字段，便于展示） */
  productName: string
  /** 商品编码（冗余字段，便于展示） */
  productCode?: string
  /** 调拨数量 */
  quantity: number
  /** 商品单位（冗余字段，便于展示） */
  unit?: string
  /** 批次号 */
  batchNo?: string
  /** 备注 */
  remark?: string
}

/**
 * 调拨单
 * 对应后端实体 StockTransfer
 */
export interface StockTransfer {
  /** 调拨单ID */
  id: number
  /** 调拨单号 */
  transferNo: string
  /** 调出门店ID */
  fromStoreId: number
  /** 调出门店编码 */
  fromStoreCode?: string
  /** 调出门店名称（冗余字段，便于展示） */
  fromStoreName?: string
  /** 调入门店ID */
  toStoreId: number
  /** 调入门店编码 */
  toStoreCode?: string
  /** 调入门店名称（冗余字段，便于展示） */
  toStoreName?: string
  /** 调拨日期 */
  transferDate: string
  /** 状态：1-待调出 2-已调出 3-已调入 4-已取消 */
  status: TransferStatus
  /** 操作员ID */
  operatorId?: number
  /** 操作员名称（冗余字段，便于展示） */
  operatorName?: string
  /** 备注 */
  remark?: string
  /** 调拨明细列表 */
  items: StockTransferItem[]
  /** 创建时间 */
  createdAt: string
}

/**
 * 调拨单查询参数
 */
export interface StockTransferQuery {
  /** 调拨单号（模糊匹配） */
  transferNo?: string
  /** 状态筛选 */
  status?: TransferStatus
  /** 调出门店ID */
  fromStoreId?: number
  /** 调入门店ID */
  toStoreId?: number
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 调拨明细创建请求
 */
export interface TransferItemCreate {
  /** 商品ID */
  productId: number
  /** 调拨数量 */
  quantity: number
  /** 批次号 */
  batchNo?: string
  /** 备注 */
  remark?: string
}

/**
 * 创建调拨单请求
 */
export interface StockTransferCreate {
  /** 调出门店ID */
  fromStoreId: number
  /** 调入门店ID */
  toStoreId: number
  /** 调拨日期 */
  transferDate: string
  /** 备注 */
  remark?: string
  /** 调拨明细列表 */
  items: TransferItemCreate[]
}
