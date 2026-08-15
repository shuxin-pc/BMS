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
 * - 1: 待调出（草稿，可执行/取消）
 * - 3: 已调入（已完成，终态）
 * - 4: 已取消（终态）
 */
export type TransferStatus = 1 | 3 | 4

/**
 * 调拨单明细项
 * 对应后端实体 StockTransferItem
 */
export interface StockTransferItem {
  /** 明细ID */
  id: string
  /** 调拨单ID */
  stockTransferId: string
  /** 商品ID */
  productId: number
  /** 商品名称（冗余字段，便于展示） */
  productName?: string
  /** 商品编码（冗余字段，便于展示） */
  productCode?: string
  /** 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品） */
  type?: number
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
 * 注意：id/fromStoreId/toStoreId 后端以 string 形式返回（long -> string 避免精度丢失）
 */
export interface StockTransfer {
  /** 调拨单ID */
  id: string
  /** 调拨单号 */
  transferNo: string
  /** 调出门店ID */
  fromStoreId: string
  /** 调出门店编码 */
  fromStoreCode?: string
  /** 调出门店名称（冗余字段，便于展示） */
  fromStoreName?: string
  /** 调入门店ID */
  toStoreId: string
  /** 调入门店编码 */
  toStoreCode?: string
  /** 调入门店名称（冗余字段，便于展示） */
  toStoreName?: string
  /** 调拨日期 */
  transferDate: string
  /** 状态：1-待调出 2-已调出 3-已调入 4-已取消 */
  status: TransferStatus
  /** 操作员ID */
  operatorId?: string
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
  fromStoreId?: string
  /** 调入门店ID */
  toStoreId?: string
  /** 开始日期（含当日） */
  startDate?: string
  /** 结束日期（含当日） */
  endDate?: string
  /** 商品类型筛选（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品） */
  productType?: number
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
 * 调拨单号由后端自动生成，前端无需传入
 */
export interface StockTransferCreate {
  /** 调出门店ID */
  fromStoreId: string
  /** 调入门店ID */
  toStoreId: string
  /** 调拨日期 */
  transferDate: string
  /** 备注 */
  remark?: string
  /** 调拨明细列表 */
  items: TransferItemCreate[]
}

/**
 * 调拨专用商品选项（含调出门店库存量）
 * 对应后端 StockTransferProductOptionDto
 */
export interface StockTransferProductOption {
  /** 商品ID */
  id: number
  /** 商品名称 */
  name: string
  /** 商品编码 */
  code: string
  /** 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品） */
  type: number
  /** 单位 */
  unit?: string
  /** 调出门店该商品库存量 */
  stock: number
}

/**
 * 调拨专用批次选项（用于手动指定批次模式）
 * 对应后端 StockTransferBatchOptionDto
 */
export interface StockTransferBatchOption {
  /** 批次ID */
  id: number
  /** 批次号 */
  batchNo: string
  /** 当前批次库存数量 */
  quantity: number
  /** 采购单价 */
  unitPrice: number
  /** 过期日期 */
  expirationDate?: string
  /** 生产日期 */
  productionDate?: string
}
