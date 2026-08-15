// ==========================================
// 价格变更记录类型定义
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
 * 价格变更记录
 */
export interface PriceChangeLog {
  /** 记录ID */
  id: number
  /** 商品ID */
  productId: number
  /** 商品名称（关联 Master 查询填充） */
  productName?: string
  /** 商品编码（关联 Master 查询填充） */
  productCode?: string
  /** 原价格 */
  oldPrice: number
  /** 新价格 */
  newPrice: number
  /** 变更时间 */
  changeTime: string
  /** 操作员ID */
  operatorId?: number
  /** 操作员名称 */
  operatorName?: string
  /** 备注 */
  remark?: string
}

/**
 * 价格变更记录查询参数
 */
export interface PriceChangeLogQuery {
  /** 商品ID */
  productId?: number
  /** 商品名称（模糊匹配） */
  productName?: string
  /** 变更开始日期 */
  startDate?: string
  /** 变更结束日期 */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}
