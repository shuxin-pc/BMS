// ==========================================
// 库存管理类型定义
// 字段与后端 InventoryDto / InventoryAlertDto 对齐（camelCase）
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
 * 商品类型（与后端 ProductMaster.Type 对齐）
 * - 1: 实物商品
 * - 2: 服务商品
 * - 3: 耗材
 * - 4: 样品
 * - 5: 赠品
 */
export type ProductType = 1 | 2 | 3 | 4 | 5

/**
 * 库存状态（与后端 InventoryAppService.CalculateInventoryStatus 对齐）
 * - 1: 充足
 * - 2: 偏低
 * - 3: 不足
 * - 4: 积压
 */
export type InventoryStatus = 1 | 2 | 3 | 4

/**
 * 库存信息（与后端 InventoryDto 对齐）
 * 以档案为主表左连接库存汇总表，档案存在即可见，无库存记录时数量为 0
 */
export interface Inventory {
  /** 档案ID（Product.Id） */
  id: number
  /** 商品ID（同 id，保留用于按商品维度调用其他接口） */
  productId: number
  /** 库存数量（无库存记录时为 0） */
  quantity: number
  /** 低库存预警阈值（来源 Product.LowStockThreshold，未配置时为 null） */
  alertQuantity: number | null
  /** 积压预警阈值（来源 Product.OverstockThreshold，未配置时为 null） */
  overstockThreshold: number | null
  /** 库存状态：1-充足，2-偏低，3-不足，4-积压 */
  inventoryStatus: InventoryStatus
  /** 创建时间 */
  createdAt: string
  /** 更新时间（优先取 Inventory.UpdatedTime，无库存记录时取 Product.UpdatedTime） */
  updatedAt?: string
  /** 商品名称（联表 ProductMaster.Name） */
  productName: string
  /** 商品编码（联表 ProductMaster.Code） */
  productCode: string
  /** 商品类型：1-实物商品，2-服务商品，3-耗材，4-样品，5-赠品 */
  productType: ProductType
  /** 商品分类名称（联表 ProductCategory.Name） */
  categoryName?: string
}

/**
 * 库存查询参数（与后端 InventoryQueryDto 对齐）
 */
export interface InventoryQuery {
  /** 商品ID */
  productId?: number
  /** 商品分类ID */
  categoryId?: number
  /** 商品名称（模糊匹配） */
  productName?: string
  /** 商品类型筛选 */
  productType?: ProductType
  /** 库存状态筛选（在内存中计算后过滤） */
  inventoryStatus?: InventoryStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 库存预警信息（与后端 InventoryAlertDto 对齐）
 */
export interface InventoryAlert {
  /** 预警记录ID */
  id: number
  /** 商品ID */
  productId: number
  /** 预警类型 */
  alertType: number
  /** 当前库存数量 */
  currentQuantity: number
  /** 预警阈值 */
  alertValue: number
  /** 过期日期 */
  expirationDate?: string
  /** 批次ID（仅效期预警有值） */
  batchId?: number
  /** 是否已处理 */
  isProcessed: boolean
  /** 处理时间 */
  processedTime?: string
  /** 处理备注 */
  processedRemark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
  /** 商品名称（联表 ProductMaster.Name） */
  productName?: string
  /** 商品编码（联表 ProductMaster.Code） */
  productCode?: string
  /** 商品分类名称（联表 ProductCategory.Name） */
  categoryName?: string
  /** 批次号（联表 InventoryBatch.BatchNo，仅效期预警有值） */
  batchNo?: string
  /** 门店名称（联表 Store.Name） */
  storeName?: string
  /** 缺口数量（低库存预警时 = alertValue - currentQuantity，其余类型为 0） */
  shortageAmount: number
}

/**
 * 库存预警查询参数（与后端 InventoryAlertQueryDto 对齐）
 */
export interface InventoryAlertQuery {
  /** 商品ID */
  productId?: number
  /** 预警类型 */
  alertType?: number
  /** 是否已处理 */
  isProcessed?: boolean
  /** 商品名称（模糊匹配） */
  productName?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 库存预警扫描结果（与后端 InventoryAlertScanResultDto 对齐）
 */
export interface InventoryAlertScanResult {
  /** 新生成低库存预警数量 */
  lowStockCreated: number
  /** 新生成效期预警数量 */
  expiryCreated: number
  /** 新生成积压预警数量 */
  overstockCreated: number
  /** 标记为已过期的批次数量 */
  batchExpired: number
}

/**
 * 商品效期状态
 * - normal: 正常
 * - expiring: 即将过期
 * - expired: 已过期
 */
export type ExpiryStatus = 'normal' | 'expiring' | 'expired'

/**
 * 效期信息
 * 字段与后端 ExpiryDto 对齐
 */
export interface ExpiryInfo {
  /** 记录ID */
  id: number
  /** 商品名称 */
  productName: string
  /** 商品编码 */
  productCode: string
  /** 批次号 */
  batchNo: string
  /** 采购日期 */
  purchaseDate?: string
  /** 过期日期 */
  expirationDate?: string
  /** 剩余天数（负数表示已过期） */
  remainingDays: number
  /** 效期状态 */
  status: ExpiryStatus
  /** 门店名称 */
  storeName?: string
}

/**
 * 效期查询参数
 */
export interface ExpiryQuery {
  /** 商品名称（模糊匹配） */
  productName?: string
  /** 效期状态筛选 */
  status?: ExpiryStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 效期预警级别
 * - expiring: 即将过期
 * - expired: 已过期
 */
export type AlertLevel = 'expiring' | 'expired'

/**
 * 商品可用效期选项（用于 POS 效期选择界面）
 * 字段与后端 ProductExpiryOptionDto 对齐
 */
export interface ProductExpiryOption {
  /** 过期日期（ISO 字符串，null 表示"无效期限制"批次） */
  expirationDate: string | null
  /** 该效期下所有在库批次的总库存数量 */
  totalQuantity: number
  /** 最早采购日期（同效期多批次时取最早） */
  earliestPurchaseDate?: string
  /** 剩余天数（负数表示已过期；null 表示"无效期限制"批次，无剩余天数概念） */
  remainingDays: number | null
  /** 是否推荐（近效期优先，列表中第一项为 true） */
  isRecommended: boolean
  /** 是否"无效期限制"批次（未填到期日期的批次） */
  isNoExpiry: boolean
}

// ==================== 库存批次 ====================

/**
 * 库存批次状态
 * - 1: 在库
 * - 2: 已用完
 * - 3: 已过期
 */
export type InventoryBatchStatus = 1 | 2 | 3

/**
 * 库存批次信息（与后端 InventoryBatchDto 对齐）
 */
export interface InventoryBatch {
  /** 批次ID */
  id: number
  /** 商品ID */
  productId: number
  /** 批次号 */
  batchNo: string
  /** 当前库存数量 */
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
  /** 状态：1-在库 2-已用完 3-已过期 */
  status: InventoryBatchStatus
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 库存批次查询参数（与后端 InventoryBatchQueryDto 对齐）
 */
export interface InventoryBatchQuery {
  /** 商品ID */
  productId?: number
  /** 批次号（模糊匹配） */
  batchNo?: string
  /** 状态：1-在库 2-已用完 3-已过期 */
  status?: InventoryBatchStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}
