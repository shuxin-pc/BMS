// ==========================================
// 入库/出库操作类型定义
// 对应后端实体 InventoryLog
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
 * 库存操作类型
 * - 1: 入库
 * - 2: 出库
 * - 3: 盘点
 * - 4: 调拨
 */
export type InventoryOpType = 1 | 2 | 3 | 4

/**
 * 入库来源类型
 * - 1: 采购入库
 * - 2: 退货入库
 * - 3: 盘点入库
 * - 4: 调拨入库
 */
export type InboundSourceType = 1 | 2 | 3 | 4

/**
 * 库存操作日志（入库/出库记录）
 * 对应后端实体 InventoryLog
 */
export interface InventoryLog {
  /** 记录ID */
  id: number
  /** 商品ID */
  productId: number
  /** 商品名称（冗余字段，便于展示） */
  productName: string
  /** 商品编码（冗余字段，便于展示） */
  productCode?: string
  /** 库存操作类型：1-入库 2-出库 3-盘点 4-调拨 */
  type: InventoryOpType
  /** 来源类型（入库 1-4 或出库 3/6/10/11 等） */
  sourceType?: InboundSourceType | OutboundSourceType
  /** 供应商ID（采购入库时有值） */
  supplierId?: number
  /** 供应商名称（冗余字段，便于展示） */
  supplierName?: string
  /** 单价 */
  unitPrice?: number
  /** 数量变化（正数增加，负数减少） */
  quantity: number
  /** 操作前库存 */
  beforeQuantity: number
  /** 操作后库存 */
  afterQuantity: number
  /** 批次号 */
  batchNo?: string
  /** 过期日期 */
  expirationDate?: string
  /** 关联单据ID */
  relatedId?: number
  /** 备注 */
  remark?: string
  /** 操作时间 */
  createdAt: string
  /** 操作人名称（冗余字段，便于展示） */
  operatorName?: string
}

/**
 * 库存日志查询参数
 */
export interface InventoryLogQuery {
  /** 商品名称（模糊匹配） */
  productName?: string
  /** 操作类型 */
  type?: InventoryOpType
  /** 来源类型（入库或出库） */
  sourceType?: InboundSourceType | OutboundSourceType
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
 * 入库请求
 */
export interface InboundRequest {
  /** 商品ID */
  productId: number
  /** 入库来源类型：1-采购入库 2-退货入库 3-盘点入库 4-调拨入库 */
  sourceType: InboundSourceType
  /** 供应商ID（采购入库时必填） */
  supplierId?: number
  /** 单价 */
  unitPrice?: number
  /** 入库数量（正数） */
  quantity: number
  /** 批次号 */
  batchNo?: string
  /** 生产日期 */
  productionDate?: string
  /** 保质期天数 */
  shelfLifeDays?: number
  /** 过期日期（录入生产日期+保质期天数时系统自动计算） */
  expirationDate?: string
  /** 备注 */
  remark?: string
}

/**
 * 出库来源类型（前端暴露的常用手动来源）
 * - 3: 盘点盘亏
 * - 6: 其他
 * - 10: 样品领用
 * - 11: 赠品活动
 */
export type OutboundSourceType = 3 | 6 | 10 | 11

/**
 * 批次扣减明细（手动模式）
 */
export interface BatchOutboundItem {
  /** 批次ID */
  batchId: number
  /** 该批次扣减数量（正数） */
  quantity: number
}

/**
 * 出库请求（支持 FEFO 自动 / 手动指定两种模式，quantity 与 batchItems 二选一）
 */
export interface OutboundRequest {
  /** 商品ID */
  productId: number
  /** 出库来源类型 */
  sourceType: OutboundSourceType
  /** FEFO 模式：总出库量（与 batchItems 二选一） */
  quantity?: number
  /** 手动模式：批次扣减明细（与 quantity 二选一） */
  batchItems?: BatchOutboundItem[]
  /** 单价（仅记录到流水，不覆盖批次原值） */
  unitPrice?: number
  /** 备注 */
  remark?: string
}

/**
 * 出库结果汇总 DTO
 */
export interface OutboundResult {
  /** 商品ID */
  productId: number
  /** 商品名称 */
  productName?: string
  /** 商品编码 */
  productCode?: string
  /** 出库来源类型 */
  sourceType: OutboundSourceType
  /** 总出库数量（正数） */
  totalQuantity: number
  /** 操作前库存 */
  beforeQuantity: number
  /** 操作后库存 */
  afterQuantity: number
  /** 操作人 */
  operatorName?: string
  /** 批次扣减明细列表 */
  batchDetails: InventoryLog[]
}

/**
 * 库存批次选项（用于手动模式展示在库批次列表）
 */
export interface InventoryBatchOption {
  /** 批次ID */
  id: number
  /** 商品ID */
  productId: number
  /** 批次号 */
  batchNo: string
  /** 当前批次库存数量 */
  quantity: number
  /** 单价 */
  unitPrice: number
  /** 生产日期 */
  productionDate?: string
  /** 保质期天数 */
  shelfLifeDays?: number
  /** 过期日期 */
  expirationDate?: string
  /** 采购日期 */
  purchaseDate?: string
  /** 状态 */
  status: number
  /** 创建时间 */
  createdAt: string
}
