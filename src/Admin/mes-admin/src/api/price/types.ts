// ==========================================
// 价格管理类型定义
// 字段与后端 PriceChangeLogDto 对齐
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
 * 价格变更记录（与后端 PriceChangeLogDto 对齐）
 */
export interface PriceInfo {
  /** 记录ID */
  id: number
  /** 商品ID */
  productId: number
  /** 原价 */
  oldPrice: number
  /** 新价格 */
  newPrice: number
  /** 变更时间 */
  changeTime: string
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 价格变更记录查询参数（与后端 PriceChangeLogQueryDto 对齐）
 */
export interface PriceQuery {
  /** 商品ID */
  productId?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 调价请求（与后端 PriceChangeLogCreateDto 对齐）
 */
export interface PriceAdjust {
  /** 商品ID */
  productId: number
  /** 原价 */
  oldPrice: number
  /** 新价格 */
  newPrice: number
  /** 变更时间 */
  changeTime?: string
  /** 操作员ID */
  operatorId?: number
  /** 调价备注 */
  remark?: string
}

/**
 * 批量调价范围类型
 * 1=商品ID列表 2=商品分类 3=供应商 4=全部商品
 */
export type BatchPriceAdjustRangeType = 1 | 2 | 3 | 4

/**
 * 批量调价方式
 * 1=百分比 2=固定金额增减 3=设置为新值
 */
export type BatchPriceAdjustAdjustType = 1 | 2 | 3

/**
 * 批量调价请求（与后端 BatchPriceAdjustDto 对齐）
 * 支持按商品ID列表 / 商品分类 / 供应商 / 全部商品进行批量调价
 * 调价方式支持百分比、固定金额增减、设置为新值
 */
export interface BatchPriceAdjust {
  /** 调价范围类型：1=商品ID列表 2=商品分类 3=供应商 4=全部商品 */
  rangeType: BatchPriceAdjustRangeType
  /** 商品ID列表（rangeType=1 时使用） */
  productIds?: number[]
  /** 商品分类ID（rangeType=2 时使用） */
  productCategoryId?: number
  /** 供应商ID（rangeType=3 时使用） */
  supplierId?: number
  /** 调价方式：1=百分比 2=固定金额增减 3=设置为新值 */
  adjustType: BatchPriceAdjustAdjustType
  /**
   * 调价值
   * adjustType=1 时为百分比（如 10 表示 +10%，-10 表示 -10%）
   * adjustType=2 时为金额增减（如 5 表示 +5 元，-5 表示 -5 元）
   * adjustType=3 时为新的价格值
   */
  adjustValue: number
  /** 调价备注 */
  remark?: string
}

/**
 * 批量调价失败明细（与后端 BatchPriceAdjustFailureItem 对齐）
 */
export interface BatchPriceAdjustFailureItem {
  /** 商品ID */
  productId: number
  /** 商品名称 */
  productName?: string
  /** 失败原因 */
  reason: string
}

/**
 * 批量调价结果（与后端 BatchPriceAdjustResultDto 对齐）
 */
export interface BatchPriceAdjustResult {
  /** 成功调价数量 */
  successCount: number
  /** 失败数量 */
  failedCount: number
  /** 跳过数量（原价格与新价格一致） */
  skippedCount: number
  /** 失败明细 */
  failures: BatchPriceAdjustFailureItem[]
}
