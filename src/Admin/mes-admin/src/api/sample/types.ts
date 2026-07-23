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
 * 库存状态
 * - 1: 充足
 * - 2: 偏低
 * - 3: 不足
 */
export type InventoryStatus = number

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
  /** 商品描述 */
  description?: string
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
 * 创建样品请求（与后端 ProductCreateDto 对齐，type 必须为 4 或 5）
 */
export interface SampleCreate {
  name: string
  code: string
  categoryId: number
  type: SampleType
  spec?: string
  unit?: string
  price: number
  costPrice?: number
  lowStockThreshold?: number
  expiryAlertDays?: number
  overstockThreshold?: number
  imageUrl?: string
  status: SampleStatus
  description?: string
}

/**
 * 更新样品请求（继承 Create + id）
 */
export interface SampleUpdate extends SampleCreate {
  id: number
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
}

/**
 * 赠品出库记录（与后端 SampleGiftOutDto 对齐）
 */
export interface SampleOutbound {
  /** 出库记录ID */
  id: number
  /** 商品ID（type=4样品/type=5赠品） */
  productId: number
  /** 出库批次ID */
  inventoryBatchId: number
  /** 出库数量 */
  quantity: number
  /** 出库时间 */
  outTime: string
  /** 关联活动ID */
  activityId?: number
  /**
   * 关联订单ID
   * 注意：独立赠品出库入口传入的 orderId 会被后端强制设为 null（P-SG-03 修复）。
   * OrderId 仅由订单创建流程（OrderAppService.DeductSampleGiftOutAsync）在事务内写入。
   * 此字段保留是为了向前兼容，前端创建/更新时不需要传。
   */
  orderId?: number
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
 * 出库记录查询参数（与后端 SampleGiftOutQueryDto 对齐）
 */
export interface SampleOutboundQuery {
  /** 商品ID */
  productId?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建出库记录请求（与后端 SampleGiftOutCreateDto 对齐）
 */
export interface SampleOutboundCreate {
  /** 商品ID（type=4样品/type=5赠品） */
  productId: number
  /** 出库批次ID */
  inventoryBatchId: number
  /** 出库数量 */
  quantity: number
  /** 出库时间 */
  outTime: string
  /** 关联活动ID */
  activityId?: number
  /**
   * 关联订单ID
   * 注意：独立赠品出库入口传入的 orderId 会被后端强制设为 null（P-SG-03 修复）。
   * OrderId 仅由订单创建流程（OrderAppService.DeductSampleGiftOutAsync）在事务内写入。
   * 此字段保留是为了向前兼容，前端创建/更新时不需要传。
   */
  orderId?: number
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
}

/**
 * 库存查询结果
 * 字段与后端 SampleInventoryDto 对齐
 */
export interface SampleInventory {
  /** 档案ID */
  id: number
  /** 名称 */
  name: string
  /** 编码 */
  code: string
  /** 类型：4-样品，5-赠品 */
  type: SampleType
  /** 单位 */
  unit?: string
  /** 当前库存 */
  currentStock: number
  /** 预警阈值（未配置时为 null，表示不参与低库存预警） */
  alertQuantity: number | null
  /** 库存状态：1-充足，2-偏低，3-不足 */
  inventoryStatus: InventoryStatus
  /** 上次入库时间 */
  lastInboundTime?: string
}

/**
 * 库存查询参数
 */
export interface SampleInventoryQuery {
  /** 名称（模糊匹配） */
  name?: string
  /** 类型筛选 */
  type?: SampleType
  /** 库存状态筛选 */
  inventoryStatus?: InventoryStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
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
