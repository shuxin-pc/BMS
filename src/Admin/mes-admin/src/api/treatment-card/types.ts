// ==========================================
// 疗程卡管理类型定义
// 对齐后端 TreatmentCards DTO（TreatmentCardDto / TreatmentCardSaleDto / TreatmentCardVerifyDto）
// ==========================================

/**
 * 疗程卡状态（前端保留，后端用 isEnabled bool）
 * - 1: 启用
 * - 2: 停用
 */
export type TreatmentCardStatus = number

/**
 * 疗程卡销售状态
 * - 1: 有效
 * - 2: 已用完
 * - 3: 已过期
 */
export type SaleStatus = number

/**
 * 预警级别
 * - 1: 即将到期
 * - 2: 已到期
 */
export type ExpiryAlertLevel = number

/**
 * 支付方式（后端 DTO 无此字段，保留供前端使用）
 */
export type PaymentMethod = 'cash' | 'wechat' | 'alipay' | 'card' | 'balance'

/**
 * 疗程卡项目明细输入（创建/更新时传入）
 * 对齐后端 CourseCardItemCreateDto
 * 后端根据各项目原价 × 次数的比例分摊卡价，计算并锁定折算单价
 */
export interface CourseCardItemInput {
  /** 商品ID（服务项目） */
  productId: number
  /** 该项目在疗程卡中的次数 */
  quantity: number
  /** 项目原价（用于折算计算） */
  originalPrice: number
}

/**
 * 疗程卡项目明细输出
 * 对齐后端 CourseCardItemDto
 */
export interface CourseCardItem {
  /** 明细ID */
  id: number
  /** 疗程卡ID */
  courseCardId: number
  /** 商品ID */
  productId: number
  /** 该项目在疗程卡中的次数 */
  quantity: number
  /** 项目原价 */
  originalPrice: number
  /** 折算单价（按卡价比例分摊并锁定，核销时按此单价计入营收） */
  allocatedUnitPrice: number
  /** 分摊总价值（折算单价 × 数量） */
  allocatedTotalPrice: number
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 后端不返回 */
  productName?: string
}

/**
 * 疗程卡配置（模板）
 * 对齐后端 TreatmentCardDto
 */
export interface TreatmentCardConfig {
  /** 卡ID */
  id: number
  /** 归属门店ID（可空，疗程卡在租户内跨店通用） */
  storeId?: number
  /** 归属门店编码 */
  storeCode?: string
  /** 卡名称 */
  name: string
  /** 卡编码 */
  code: string
  /** 包含项目描述 */
  serviceItems?: string
  /** 总次数（各项目次数之和） */
  totalTimes: number
  /** 单价（疗程卡总售价） */
  price: number
  /** 有效期（天） */
  validityDays: number
  /** 是否启用 */
  isEnabled: boolean
  /** 备注 */
  remark?: string
  /** 包含的项目明细列表 */
  items: CourseCardItem[]
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 使用 price 替代 */
  cardPrice?: number
  /** @deprecated 使用 totalTimes 替代 */
  totalCount?: number
  /** @deprecated 使用 isEnabled 替代 */
  status?: TreatmentCardStatus
  /** @deprecated 使用 remark 或 serviceItems 替代 */
  description?: string
}

/**
 * 疗程卡配置查询参数
 * 对齐后端 TreatmentCardQueryDto
 */
export interface TreatmentCardConfigQuery {
  /** 卡名称（模糊匹配） */
  name?: string
  /** 卡编码 */
  code?: string
  /** 是否启用 */
  isEnabled?: boolean
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** @deprecated 使用 isEnabled 替代 */
  status?: TreatmentCardStatus
}

/**
 * 创建疗程卡配置请求
 * 对齐后端 TreatmentCardCreateDto
 */
export interface TreatmentCardConfigCreate {
  /** 归属门店ID */
  storeId?: number
  /** 归属门店编码 */
  storeCode?: string
  /** 卡名称 */
  name: string
  /** 卡编码 */
  code: string
  /** 包含项目描述 */
  serviceItems?: string
  /** 总次数（各项目次数之和） */
  totalTimes: number
  /** 单价（疗程卡总售价） */
  price: number
  /** 有效期（天） */
  validityDays: number
  /** 是否启用 */
  isEnabled: boolean
  /** 备注 */
  remark?: string
  /** 包含的项目明细列表 */
  items: CourseCardItemInput[]

  // ---- 以下字段后端不支持，保留为可选供前端页面使用 ----
  /** @deprecated 使用 price 替代 */
  cardPrice?: number
  /** @deprecated 使用 isEnabled 替代 */
  status?: TreatmentCardStatus
  /** @deprecated 使用 remark 或 serviceItems 替代 */
  description?: string
}

/**
 * 更新疗程卡配置请求
 * 对齐后端 TreatmentCardUpdateDto
 */
export interface TreatmentCardConfigUpdate extends TreatmentCardConfigCreate {
  id: number
}

/**
 * 疗程卡销售项目明细
 * 对齐后端 TreatmentCardSaleItemDto
 */
export interface TreatmentCardSaleItem {
  /** 明细ID */
  id: number
  /** 疗程卡销售记录ID */
  saleId: number
  /** 商品ID（服务项目） */
  productId: number
  /** 该项目在疗程卡中的次数 */
  quantity: number
  /** 项目原价 */
  originalPrice: number
  /** 折算单价（购买时锁定） */
  allocatedUnitPrice: number
  /** 分摊总价值（折算单价 × 数量） */
  allocatedTotalPrice: number
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 疗程卡销售记录
 * 对齐后端 TreatmentCardSaleDto
 */
export interface TreatmentCardSale {
  /** 销售ID */
  id: number
  /** 购买门店ID */
  storeId?: number
  /** 购买门店编码 */
  storeCode?: string
  /** 疗程卡ID */
  cardId: number
  /** 客户ID */
  customerId: number
  /** 购买日期 */
  purchaseDate: string
  /** 购买金额 */
  amount: number
  /** 累计已消费金额 */
  totalConsumedAmount: number
  /** 剩余次数 */
  remainingTimes: number
  /** 有效期至 */
  expiryDate: string
  /** 状态（1:有效 2:已用完 3:已过期） */
  status: SaleStatus
  /** 备注 */
  remark?: string
  /** 疗程卡包含的项目明细（含购买时锁定的折算单价） */
  items: TreatmentCardSaleItem[]
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 后端无此字段 */
  saleNo?: string
  /** @deprecated 后端不返回，需前端 join */
  customerName?: string
  /** @deprecated 后端不返回 */
  phone?: string
  /** @deprecated 后端不返回 */
  cardName?: string
  /** @deprecated 使用 cardId 替代 */
  cardConfigId?: number
  /** @deprecated 后端无此字段 */
  totalCount?: number
  /** @deprecated 使用 remainingTimes 替代 */
  remainingCount?: number
  /** @deprecated 使用 amount 替代 */
  actualAmount?: number
  /** @deprecated 后端无此字段 */
  paymentMethod?: PaymentMethod
  /** @deprecated 使用 purchaseDate 替代 */
  saleTime?: string
  /** @deprecated 使用 storeCode 替代 */
  storeName?: string
}

/**
 * 疗程卡销售查询参数
 * 对齐后端 TreatmentCardSaleQueryDto
 */
export interface TreatmentCardSaleQuery {
  /** 客户ID */
  customerId?: number
  /** 疗程卡ID */
  cardId?: number
  /** 状态筛选（1:有效 2:已用完 3:已过期） */
  status?: SaleStatus
  /** 客户名称（模糊匹配，join Customer 表查询） */
  customerName?: string
  /** 手机号（模糊匹配，join Customer 表查询） */
  phone?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** @deprecated 后端用 cardId */
  cardName?: string
}

/**
 * 疗程卡核销记录
 * 对齐后端 TreatmentCardVerifyDto
 */
export interface TreatmentCardVerify {
  /** 核销ID */
  id: number
  /** 核销门店ID */
  storeId?: number
  /** 核销门店编码 */
  storeCode?: string
  /** 疗程卡销售ID */
  cardSaleId: number
  /** 本次核销总金额（冗余字段，等于 Items.Sum(SubAmount)） */
  verifyAmount: number
  /** 关联订单ID */
  orderId?: number
  /** 本次核销总次数（冗余字段，等于 Items.Sum(VerifyTimes)） */
  verifyTimes: number
  /** 核销时间 */
  verifyTime: string
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 后端无此字段 */
  verifyNo?: string
  /** @deprecated 后端不返回 */
  customerName?: string
  /** @deprecated 后端不返回 */
  phone?: string
  /** @deprecated 后端不返回 */
  cardName?: string
  /** @deprecated 使用 cardSaleId 替代 */
  saleId?: number
  /** @deprecated 后端不返回（改用 items[].productId 关联查询） */
  verifyItem?: string
  /** @deprecated 后端无此字段 */
  remainingCount?: number
  /** @deprecated 使用 operatorId 替代 */
  operatorName?: string
  /** 核销项目明细列表（一次核销可包含多个项目） */
  items?: TreatmentCardVerifyItem[]
}

/**
 * 疗程卡核销项目明细（输出，对齐后端 TreatmentCardVerifyItemDto）
 */
export interface TreatmentCardVerifyItem {
  /** 明细ID */
  id: number
  /** 核销主单ID */
  verifyId: number
  /** 核销的项目ID（服务项目） */
  productId: number
  /** 本项核销次数 */
  verifyTimes: number
  /** 折算单价（核销时锁定） */
  allocatedUnitPrice: number
  /** 本项核销金额 = AllocatedUnitPrice × VerifyTimes（最后一项含兜底） */
  subAmount: number
  /** 创建时间 */
  createdAt: string
}

/**
 * 疗程卡核销查询参数
 * 对齐后端 TreatmentCardVerifyQueryDto
 */
export interface TreatmentCardVerifyQuery {
  /** 疗程卡销售记录ID */
  cardSaleId?: number
  /** 核销项目ID */
  verifyProductId?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** @deprecated 后端用 cardSaleId */
  customerName?: string
  /** @deprecated 后端不支持 */
  phone?: string
}

/**
 * 核销项目输入（对齐后端 TreatmentCardVerifyItemInput）
 * productId 设为可选：表单新增行时允许暂未选择，提交前由校验把关
 */
export interface TreatmentCardVerifyItemInput {
  /** 核销的商品ID（服务项目） */
  productId?: number
  /** 本项核销次数（默认 1，至少 1） */
  verifyTimes: number
}

/**
 * 核销请求（对齐后端 TreatmentCardVerifyCreateDto）
 * 核销金额、关联订单、核销时间由后端自动计算/创建，前端只需指定疗程卡和核销项目列表
 * 一次操作可包含多个项目（一次到店做多种护理）
 */
export interface TreatmentCardVerifyRequest {
  /** 核销门店ID（疗程卡跨店通用） */
  storeId?: number
  /** 核销门店编码 */
  storeCode?: string
  /** 疗程卡销售记录ID */
  cardSaleId: number
  /** 核销项目列表（至少 1 项） */
  items: TreatmentCardVerifyItemInput[]
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
}

/**
 * 疗程卡到期提醒
 * 对齐后端 TreatmentCardExpiryDto（GET /treatmentCardSales/expiries）
 */
export interface TreatmentCardExpiry {
  /** 记录ID（同销售记录ID） */
  id: number
  /** 客户名称 */
  customerName: string
  /** 手机号 */
  phone: string
  /** 卡名称 */
  cardName: string
  /** 销售记录ID */
  saleId: number
  /** 购买日期 */
  purchaseDate: string
  /** 到期日期 */
  expiryDate: string
  /** 剩余天数（负数表示已过期） */
  remainingDays: number
  /** 剩余次数 */
  remainingTimes: number
  /** 预警级别：1-即将到期，2-已到期 */
  alertLevel: ExpiryAlertLevel
  /** 状态：1-有效，2-已用完，3-已过期 */
  status: SaleStatus
}

/**
 * 疗程卡到期提醒查询参数
 * 对齐后端 TreatmentCardExpiryQueryDto
 */
export interface TreatmentCardExpiryQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 预警级别筛选（1:即将到期 2:已到期） */
  alertLevel?: ExpiryAlertLevel
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

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
 * 通用API响应
 */
export interface ApiResponse<T> {
  code: number
  message?: string
  data: T
}
