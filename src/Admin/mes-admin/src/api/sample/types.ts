// ==========================================
// 样品赠品模块类型定义
// 样品/赠品即 Product 表中 type=4（样品）或 type=5（赠品）的记录
// 字段与后端 ProductDto / SampleGiftReceiveDto / SampleGiftOutDto 对齐（camelCase）
// ==========================================

/**
 * 样品赠品类型（与 ProductType 一致）
 * - 4: 样品
 * - 5: 赠品
 */
export type SampleType = 4 | 5

/**
 * 样品赠品状态（与 ProductStatus 一致）
 * - 1: 上架
 * - 2: 下架
 */
export type SampleStatus = 1 | 2

/**
 * 领用用途
 * - 1: 体验
 * - 2: 赠送
 * @deprecated 后端 SampleGiftReceiveDto 不包含此字段，保留供前端兼容使用
 */
export type ReceivePurpose = number

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
 * 样品/赠品档案（与后端 ProductDto 对齐，type=4或5）
 */
export interface Sample {
  /** 商品ID */
  id: number
  /** 商品名称 */
  name: string
  /** 商品编码 */
  code: string
  /** 商品分类ID */
  categoryId: number
  /** 商品分类名称 */
  categoryName?: string
  /** 商品类型：4-样品，5-赠品 */
  type: SampleType
  /** 规格 */
  spec?: string
  /** 单位 */
  unit?: string
  /** 品牌 */
  brand?: string
  /** 供应商ID */
  supplierId?: number
  /** 售价 */
  price: number
  /** 成本价 */
  costPrice?: number
  /** 上次采购价（采购入库时自动更新，分店独立采购） */
  lastPurchasePrice?: number
  /** 低库存预警阈值 */
  lowStockThreshold?: number
  /** 效期预警天数 */
  expiryAlertDays?: number
  /** 积压预警阈值 */
  overstockThreshold?: number
  /** 商品图片URL */
  imageUrl?: string
  /** 商品状态：1-上架，2-下架 */
  status: SampleStatus
  /** 分店级备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 样品查询参数（与后端 SampleGiftQueryDto 对齐）
 */
export interface SampleQuery {
  /** 商品名称（模糊匹配） */
  name?: string
  /** 商品编码（模糊匹配） */
  code?: string
  /** 类型筛选：4-样品，5-赠品 */
  type?: SampleType
  /** 状态筛选：1-上架，2-下架 */
  status?: SampleStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 样品领用记录（与后端 SampleGiftReceiveDto 对齐）
 */
export interface SampleReceive {
  /** 领用记录ID */
  id: number
  /** 商品ID（type=4样品/type=5赠品） */
  productId: number
  /** 出库批次ID */
  inventoryBatchId: number
  /** 客户ID（可选，未选择客户时为 null） */
  customerId: number | null
  /** 领用数量 */
  quantity: number
  /** 领用时间 */
  receiveTime: string
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 关联活动ID（可选） */
  activityId?: number | null
  /** 关联活动名称（显示字段，由后端填充） */
  activityName?: string | null
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 领用记录查询参数（与后端 SampleGiftReceiveQueryDto 对齐）
 */
export interface SampleReceiveQuery {
  /** 商品ID */
  productId?: number
  /** 客户ID */
  customerId?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建领用记录请求（与后端 SampleGiftReceiveCreateDto 对齐）
 */
export interface SampleReceiveCreate {
  /** 商品ID（type=4样品/type=5赠品） */
  productId: number
  /** 出库批次ID */
  inventoryBatchId: number
  /** 客户ID（可选，未选择客户时为 null） */
  customerId: number | null
  /** 领用数量 */
  quantity: number
  /** 领用时间 */
  receiveTime: string
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 关联活动ID（可选，用于活动维度归因统计） */
  activityId?: number | null
}

/**
 * 统计报表行
 * 字段与后端 SampleReportDto 对齐
 */
export interface SampleReport {
  /** 档案ID */
  id: number
  /** 名称 */
  name: string
  /** 类型：4-样品，5-赠品 */
  type: SampleType
  /** 领用数量 */
  receiveCount: number
  /** 出库数量 */
  outboundCount: number
  /** 合计发出 */
  totalIssued: number
  /** 当前库存 */
  currentStock: number
  /** 领用占比（%） */
  receiveRatio: number
}

/**
 * 统计报表查询参数
 */
export interface SampleReportQuery {
  /** 名称（模糊匹配） */
  name?: string
  /** 类型筛选 */
  type?: SampleType
  /** 开始日期（YYYY-MM-DD） */
  startDate?: string
  /** 结束日期（YYYY-MM-DD） */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}
