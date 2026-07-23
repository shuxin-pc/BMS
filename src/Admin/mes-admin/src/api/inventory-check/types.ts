// ==========================================
// 库存盘点类型定义
// 对应后端实体 InventoryCheck
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
 * 库存盘点记录
 * 对应后端实体 InventoryCheck
 */
export interface InventoryCheck {
  /** 盘点记录ID */
  id: number
  /** 商品ID */
  productId: number
  /** 商品名称（冗余字段，便于展示） */
  productName: string
  /** 商品编码（冗余字段，便于展示） */
  productCode?: string
  /** 盘点前账面库存 */
  beforeQuantity: number
  /** 实际盘点数量 */
  actualQuantity: number
  /** 差异数量（实际 - 账面，正数为盘盈，负数为盘亏） */
  diffQuantity: number
  /** 差异金额（差异数量 * 成本价，正数为盘盈金额，负数为盘亏金额） */
  diffAmount?: number
  /** 商品成本价（冗余字段，用于计算差异金额） */
  unitCost?: number
  /** 盘点时间 */
  checkTime: string
  /** 操作员ID */
  operatorId?: number
  /** 操作员名称（冗余字段，便于展示） */
  operatorName?: string
  /** 备注 */
  remark?: string
}

/**
 * 盘点查询参数
 */
export interface InventoryCheckQuery {
  /** 商品名称（模糊匹配） */
  productName?: string
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
 * 创建盘点请求
 */
export interface InventoryCheckCreate {
  /** 商品ID */
  productId: number
  /** 实际盘点数量 */
  actualQuantity: number
  /** 备注 */
  remark?: string
}
