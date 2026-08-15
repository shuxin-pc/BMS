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
  /** 来源类型（0-13，详见 InventoryLogSourceType） */
  sourceType?: InventoryLogSourceType
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
  /** 操作前批次库存（后端按同商品同批次流水累加计算，无 productId 查询时为 null） */
  batchBeforeQuantity?: number | null
  /** 操作后批次库存（后端按同商品同批次流水累加计算，无 productId 查询时为 null） */
  batchAfterQuantity?: number | null
  /** 操作前商品总库存（后端按同商品流水累加计算，无 productId 查询时为 null） */
  totalBeforeQuantity?: number | null
  /** 操作后商品总库存（后端按同商品流水累加计算，无 productId 查询时为 null） */
  totalAfterQuantity?: number | null
}

/**
 * 库存日志查询参数
 */
export interface InventoryLogQuery {
  /** 商品ID（按商品筛选流水时传入，后端 long 序列化为字符串，前端用 string 避免大整数精度丢失） */
  productId?: number | string
  /** 商品名称（模糊匹配） */
  productName?: string
  /** 操作类型 */
  type?: InventoryOpType
  /** 来源类型（0-13，详见 InventoryLogSourceType） */
  sourceType?: InventoryLogSourceType
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 批次号（模糊匹配） */
  batchNo?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 出库来源类型（前端暴露的常用手动来源）
 * - 3: 盘点盘亏
 * - 9: 样品领用
 * - 10: 赠品活动
 * - 11: 其他
 */
export type OutboundSourceType = 3 | 9 | 10 | 11

/**
 * 库存流水来源类型（完整枚举，对应后端 InventoryLogSourceTypes 常量）
 * - 0: 销售出库
 * - 1: 采购入库
 * - 2: 退货入库
 * - 3: 盘点调整
 * - 4: 调拨入库
 * - 5: 调拨出库
 * - 6: 采购退货出库
 * - 7: 疗程卡核销出库
 * - 8: 样品/赠品出库（历史）
 * - 9: 样品领用出库
 * - 10: 赠品活动出库
 * - 11: 其他
 * - 12: 样品赠品调拨出库
 * - 13: 样品赠品调拨入库
 */
export type InventoryLogSourceType = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13

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
