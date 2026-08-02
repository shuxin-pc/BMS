// ==========================================
// 样品赠品调拨类型定义
// 对应后端实体 SampleGiftTransfer + SampleGiftTransferItem
// 仅支持样品(4)/赠品(5)商品的跨门店调拨
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
 * 调拨单状态（与正品调拨复用同一枚举）
 * - 1: 待调出（草稿，可执行/取消）
 * - 3: 已调入（已完成，终态）
 * - 4: 已取消（终态）
 */
export type SampleGiftTransferStatus = 1 | 3 | 4

/**
 * 商品类型筛选（仅样品/赠品调拨使用）
 * - 4: 样品
 * - 5: 赠品
 */
export type SampleGiftProductType = 4 | 5

/**
 * 调拨单明细项
 * 对应后端实体 SampleGiftTransferItem
 */
export interface SampleGiftTransferItem {
  /** 明细ID */
  id: string
  /** 调拨单ID */
  sampleGiftTransferId: string
  /** 商品ID */
  productId: number
  /** 商品名称（冗余字段，便于展示） */
  productName?: string
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
 * 样品赠品调拨单
 * 对应后端实体 SampleGiftTransfer
 * 注意：id/fromStoreId/toStoreId 后端以 string 形式返回（long -> string 避免精度丢失）
 */
export interface SampleGiftTransfer {
  /** 调拨单ID */
  id: string
  /** 调拨单号（SGT 前缀） */
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
  /** 状态：1-待调出 3-已调入 4-已取消 */
  status: SampleGiftTransferStatus
  /** 操作员ID */
  operatorId?: string
  /** 操作员名称（冗余字段，便于展示） */
  operatorName?: string
  /** 备注 */
  remark?: string
  /** 调拨明细列表 */
  items: SampleGiftTransferItem[]
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 调拨单查询参数
 */
export interface SampleGiftTransferQuery {
  /** 调拨单号（模糊匹配） */
  transferNo?: string
  /** 状态筛选 */
  status?: SampleGiftTransferStatus
  /** 调出门店ID */
  fromStoreId?: string
  /** 调入门店ID */
  toStoreId?: string
  /** 商品类型筛选：4-样品 5-赠品 */
  productType?: SampleGiftProductType
  /** 开始日期（含当日） */
  startDate?: string
  /** 结束日期（含当日） */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 调拨明细创建请求
 */
export interface SampleGiftTransferItemCreate {
  /** 商品ID */
  productId: number
  /** 调拨数量 */
  quantity: number
  /** 批次号（FEFO 模式留空，手动模式必填） */
  batchNo?: string
  /** 备注 */
  remark?: string
}

/**
 * 创建调拨单请求
 * 调拨单号由后端自动生成，前端无需传入
 */
export interface SampleGiftTransferCreate {
  /** 调出门店ID */
  fromStoreId: string
  /** 调入门店ID */
  toStoreId: string
  /** 调拨日期 */
  transferDate: string
  /** 备注 */
  remark?: string
  /** 调拨明细列表 */
  items: SampleGiftTransferItemCreate[]
}

/**
 * 调拨专用商品选项（含调出门店库存量）
 * 对应后端 SampleGiftTransferProductOptionDto
 * 仅返回 Type∈{4,5} 且 Stock > 0 的商品
 */
export interface SampleGiftTransferProductOption {
  /** 商品ID */
  id: number
  /** 商品名称 */
  name: string
  /** 商品编码 */
  code: string
  /** 单位 */
  unit?: string
  /** 调出门店该商品库存量 */
  stock: number
  /** 商品类型：4-样品 5-赠品 */
  type: SampleGiftProductType
}

/**
 * 调拨专用批次选项（用于手动指定批次模式）
 * 对应后端 SampleGiftTransferBatchOptionDto
 * 仅返回调出门店该商品在库且有库存的批次，按过期日期升序（FEFO）
 */
export interface SampleGiftTransferBatchOption {
  /** 批次ID */
  id: number
  /** 批次号 */
  batchNo: string
  /** 当前批次库存数量 */
  quantity: number
  /** 采购单价（用于成本追溯） */
  unitPrice: number
  /** 过期日期 */
  expirationDate?: string
  /** 生产日期 */
  productionDate?: string
}
