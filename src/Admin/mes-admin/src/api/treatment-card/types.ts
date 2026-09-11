// ==========================================
// 项目卡管理类型定义
// 对齐后端 TreatmentCards DTO（TreatmentCardDto / TreatmentCardSaleDto / TreatmentCardVerifyDto）
// ==========================================

import type { ConsumableExpiry, TechnicianSource } from '../order/types'

/**
 * 项目卡状态（前端保留，后端用 isEnabled bool）
 * - 1: 启用
 * - 2: 停用
 */
export type TreatmentCardStatus = number

/**
 * 项目卡销售状态
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
 * 项目卡项目明细输入（创建/更新时传入）
 * 对齐后端 CourseCardItemCreateDto
 * 后端根据各项目原价 × 次数的比例分摊卡价，计算并锁定折算单价
 */
export interface CourseCardItemInput {
  /** 商品ID（服务项目） */
  productId: number
  /** 该项目在项目卡中的次数 */
  quantity: number
  /** 项目原价（用于折算计算） */
  originalPrice: number
}

/**
 * 项目卡项目明细输出
 * 对齐后端 CourseCardItemDto
 */
export interface CourseCardItem {
  /** 明细ID */
  id: number
  /** 项目卡ID */
  courseCardId: number
  /** 商品ID */
  productId: number
  /** 商品编码（关联商品主档，仅展示） */
  productCode?: string
  /** 商品名称（关联商品主档，仅展示） */
  productName?: string
  /** 该项目在项目卡中的次数 */
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
}

/**
 * 项目卡配置（模板）
 * 对齐后端 TreatmentCardDto
 */
export interface TreatmentCardConfig {
  /** 卡ID */
  id: number
  /** 归属门店ID（可空，项目卡在租户内跨店通用） */
  storeId?: number
  /** 归属门店编码 */
  storeCode?: string
  /** 卡名称 */
  name: string
  /** 包含项目描述 */
  serviceItems?: string
  /** 总次数（各项目次数之和） */
  totalTimes: number
  /** 单价（项目卡总售价） */
  price: number
  /** 有效期（天） */
  validityDays: number
  /** 是否启用 */
  isEnabled: boolean
  /** 是否已有销售数据（有销售数据的卡禁止修改除启用状态外的配置，亦禁止删除） */
  hasSales?: boolean
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
 * 项目卡配置查询参数
 * 对齐后端 TreatmentCardQueryDto
 */
export interface TreatmentCardConfigQuery {
  /** 卡名称（模糊匹配） */
  name?: string
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
 * 创建项目卡配置请求
 * 对齐后端 TreatmentCardCreateDto
 */
export interface TreatmentCardConfigCreate {
  /** 归属门店ID */
  storeId?: number
  /** 归属门店编码 */
  storeCode?: string
  /** 卡名称 */
  name: string
  /** 包含项目描述 */
  serviceItems?: string
  /** 总次数（各项目次数之和） */
  totalTimes: number
  /** 单价（项目卡总售价） */
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
 * 更新项目卡配置请求
 * 对齐后端 TreatmentCardUpdateDto
 */
export interface TreatmentCardConfigUpdate extends TreatmentCardConfigCreate {
  id: number
}

/**
 * 项目卡销售项目明细
 * 对齐后端 TreatmentCardSaleItemDto
 */
export interface TreatmentCardSaleItem {
  /** 明细ID */
  id: number
  /** 项目卡销售记录ID */
  saleId: number
  /** 商品ID（服务项目） */
  productId: number
  /** 该项目在项目卡中的次数 */
  quantity: number
  /** 项目原价 */
  originalPrice: number
  /** 折算单价（购买时锁定） */
  allocatedUnitPrice: number
  /** 分摊总价值（折算单价 × 数量） */
  allocatedTotalPrice: number
  /** 该项目剩余可核销次数（= Quantity - 该卡该项目已核销次数，列表/详情接口聚合核销明细补充，排除已冲正记录；核销弹窗用于限制单一项目核销次数） */
  remainingQuantity?: number
  /** 该项目已核销累计金额（列表/详情接口聚合核销明细金额补充，排除已冲正记录；核销弹窗用于该项目最后一次核销时的兜底金额计算） */
  consumedAmount?: number
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 创建项目卡销售项目明细请求
 * 对齐后端 TreatmentCardSaleItemCreateDto（前端只传商品、次数、原价；折算单价由后端分摊计算）
 */
export interface TreatmentCardSaleItemCreate {
  /** 商品ID（服务项目） */
  productId: number
  /** 该项目在项目卡中的次数 */
  quantity: number
  /** 项目原价（用于后端折算计算） */
  originalPrice: number
}

/**
 * 创建项目卡销售记录请求
 * 对齐后端 TreatmentCardSaleCreateDto（购买门店由后端从当前登录用户读取，前端不传）
 */
export interface TreatmentCardSaleCreate {
  /** 项目卡配置ID */
  cardId: number
  /** 客户ID */
  customerId: number
  /** 购买日期 */
  purchaseDate: string
  /** 购买金额 */
  amount: number
  /** 项目卡包含的项目明细（取自项目卡配置 items） */
  items: TreatmentCardSaleItemCreate[]
  /** 备注 */
  remark?: string
  /** 购物车结算批次号（POS 购物车一次结算生成，用于订单/核销/开卡三单据聚合追溯；独立开卡为空） */
  checkoutSessionNo?: string
  /** 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值卡 6:积分抵扣 7:组合支付），POS 统一支付方式，与订单一致 */
  payMethod?: number
  /** 组合支付-类别1金额（现金/支付宝/微信/银行卡，payMethod=7 时使用） */
  cashAmount?: number
  /** 组合支付-类别1具体方式（1:现金 2:支付宝 3:微信 4:银行卡） */
  cashPayMethod?: number
  /** 组合支付-类别2储值扣款金额（payMethod=7 时使用） */
  storedValueAmount?: number
  /** 组合支付-类别3积分抵扣金额（payMethod=7 时使用） */
  pointsAmount?: number
}

/**
 * 项目卡销售记录
 * 对齐后端 TreatmentCardSaleDto
 */
export interface TreatmentCardSale {
  /** 销售ID */
  id: number
  /** 购买门店ID */
  storeId?: number
  /** 购买门店编码 */
  storeCode?: string
  /** 项目卡ID */
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
  /** 项目卡有效期（天），0 表示不限到期时间 */
  validityDays: number
  /** 状态（1:有效 2:已用完 3:已过期） */
  status: SaleStatus
  /** 备注 */
  remark?: string
  /** 销售单号（后端自动生成，格式：TC{yyyyMMdd}{序号}；历史数据为空） */
  saleNo?: string
  /** 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值卡 6:积分抵扣 7:组合支付），POS 统一支付方式 */
  payMethod?: number
  /** 组合支付-类别1金额（现金/支付宝/微信/银行卡，payMethod=7 时使用） */
  cashAmount?: number
  /** 组合支付-类别1具体方式（1:现金 2:支付宝 3:微信 4:银行卡） */
  cashPayMethod?: number
  /** 组合支付-类别2储值扣款金额（payMethod=7 时使用） */
  storedValueAmount?: number
  /** 组合支付-类别3积分抵扣金额（payMethod=7 时使用） */
  pointsAmount?: number
  /** 客户名称（列表/详情接口 join Customer 返回） */
  customerName?: string
  /** 客户手机号（列表/详情接口 join Customer 返回） */
  phone?: string
  /** 项目卡名称（列表/详情接口 join TreatmentCard 返回） */
  cardName?: string
  /** 购买总次数（= Items.Sum(Quantity)，列表/详情接口返回） */
  totalCount?: number
  /** 项目卡包含的项目明细（含购买时锁定的折算单价） */
  items: TreatmentCardSaleItem[]
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 使用 cardId 替代 */
  cardConfigId?: number
  /** @deprecated 使用 remainingTimes 替代 */
  remainingCount?: number
  /** @deprecated 使用 amount 替代 */
  actualAmount?: number
  /** @deprecated 使用 payMethod 替代 */
  paymentMethod?: PaymentMethod
  /** @deprecated 使用 purchaseDate 替代 */
  saleTime?: string
  /** @deprecated 使用 storeCode 替代 */
  storeName?: string
}

/**
 * 项目卡销售查询参数
 * 对齐后端 TreatmentCardSaleQueryDto
 */
export interface TreatmentCardSaleQuery {
  /** 客户ID */
  customerId?: number
  /** 项目卡ID */
  cardId?: number
  /** 状态筛选（1:有效 2:已用完 3:已过期） */
  status?: SaleStatus
  /** 客户名称/手机号合并关键字（命中姓名或手机号其一即满足） */
  keyword?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
  /** 卡名称（模糊匹配，join TreatmentCard 表查询） */
  cardName?: string
}

/**
 * 项目卡退卡请求
 * 对齐后端 TreatmentCardSaleRefundDto（POST /treatmentCardSales/{id}/refund）
 * 退卡金额 = 售价 - 已核销金额（未消费部分退还客户）
 * @remarks id 为雪花ID，序列化为字符串，禁止 Number() 转换（会丢精度），直接拼 URL
 */
export interface TreatmentCardSaleRefundRequest {
  /** 项目卡销售记录ID */
  id: number | string
  /** 退卡原因（必填，便于审计追溯） */
  remark: string
}

/**
 * 项目卡退卡结果
 * 对齐后端 TreatmentCardSaleRefundResultDto
 */
export interface TreatmentCardSaleRefundResult {
  /** 销售记录ID */
  saleId: number
  /** 原售价 */
  originalAmount: number
  /** 已消费金额（不冲回，服务已实际发生） */
  consumedAmount: number
  /** 应退金额（售价 - 已消费金额） */
  refundAmount: number
  /** 退卡后状态（3:已退卡） */
  status: number
}

/**
 * 项目卡核销记录
 * 对齐后端 TreatmentCardVerifyDto
 */
export interface TreatmentCardVerify {
  /** 核销ID */
  id: number
  /** 核销门店ID */
  storeId?: number
  /** 核销门店编码 */
  storeCode?: string
  /** 项目卡销售ID */
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

  // ---- 列表接口（GET /treatmentCardVerifies）join 补全的展示字段 ----
  /** @deprecated 后端无此字段，前端页面已移除核销单号列 */
  verifyNo?: string
  /** 客户名称（列表接口 join TreatmentCardSale→Customer 返回） */
  customerName?: string
  /** 客户手机号（列表接口 join TreatmentCardSale→Customer 返回） */
  phone?: string
  /** 卡名称（列表接口 join TreatmentCardSale→TreatmentCard 返回） */
  cardName?: string
  /** @deprecated 使用 cardSaleId 替代 */
  saleId?: number
  /** 核销项目名称（列表接口聚合 items[].productId → 商品名返回） */
  verifyItem?: string
  /** 该项目卡当前剩余次数（列表接口 join TreatmentCardSale.RemainingTimes 返回） */
  remainingCount?: number
  /** 操作人（后端 DTO 直接返回 OperatorName） */
  operatorName?: string
  /** 核销项目明细列表（一次核销可包含多个项目） */
  items?: TreatmentCardVerifyItem[]
}

/**
 * 项目卡核销项目明细（输出，对齐后端 TreatmentCardVerifyItemDto）
 */
export interface TreatmentCardVerifyItem {
  /** 明细ID */
  id: number
  /** 核销主单ID */
  verifyId: number
  /** 核销的项目ID（服务项目） */
  productId: number
  /** 核销项目名称（列表/详情接口 join Product.Master 返回） */
  productName?: string
  /** 本项核销次数 */
  verifyTimes: number
  /** 折算单价（核销时锁定） */
  allocatedUnitPrice: number
  /** 本项核销金额 = AllocatedUnitPrice × VerifyTimes（最后一项含兜底） */
  subAmount: number
  /** 技师ID（核销项目按服务商品录入技师，可空=未选） */
  technicianId?: number
  /** 技师来源：1-商家技师，2-平台技师 */
  technicianSource?: TechnicianSource
  /** 房间/床位ID（服务项目占用房间资源，可空） */
  roomId?: number
  /** 设备ID（服务项目占用设备资源，可空） */
  equipmentId?: number
  /** 服务开始时间（核销项目真实服务开始时间，ISO 字符串） */
  serviceStartTime?: string
  /** 服务结束时间（开始时间 + 服务时长自动计算，ISO 字符串） */
  serviceEndTime?: string
  /** 创建时间 */
  createdAt: string
}

/**
 * 项目卡核销查询参数
 * 对齐后端 TreatmentCardVerifyQueryDto
 */
export interface TreatmentCardVerifyQuery {
  /** 项目卡销售记录ID */
  cardSaleId?: number
  /** 核销项目ID */
  verifyProductId?: number
  /** 客户名称/手机号关键字（模糊匹配，OR 语义，子查询 join TreatmentCardSale→Customer） */
  keyword?: string
  /** 核销开始日期（含，YYYY-MM-DD） */
  startDate?: string
  /** 核销结束日期（含当天） */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
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
  /** 技师ID（核销项目按服务商品录入技师，可空=未选不占用不归集） */
  technicianId?: number
  /** 技师来源：1-商家技师，2-平台技师（仅商家技师参与技师统计归集） */
  technicianSource?: TechnicianSource
  /** 房间/床位ID（服务项目占用房间资源，可空） */
  roomId?: number
  /** 设备ID（服务项目占用设备资源，可空） */
  equipmentId?: number
  /** 服务开始时间（核销项目真实服务开始时间，必填，ISO 字符串） */
  serviceStartTime?: string
  /** 服务结束时间（开始时间 + 服务时长自动计算，ISO 字符串，可空） */
  serviceEndTime?: string
  /** 核销项目明细备注（快速开单服务内容弹窗录入） */
  remark?: string
  /** 服务项目绑定耗材的效期选择（对应后端 TreatmentCardVerifyItemInput.ConsumableExpiries） */
  consumableExpiries?: ConsumableExpiry[]
}

/**
 * 核销请求（对齐后端 TreatmentCardVerifyCreateDto）
 * 核销金额、关联订单、核销时间由后端自动计算/创建，前端只需指定项目卡和核销项目列表
 * 一次操作可包含多个项目（一次到店做多种护理）
 */
export interface TreatmentCardVerifyRequest {
  /** 核销门店ID（项目卡跨店通用） */
  storeId?: number
  /** 核销门店编码 */
  storeCode?: string
  /** 项目卡销售记录ID */
  cardSaleId: number
  /** 核销项目列表（至少 1 项） */
  items: TreatmentCardVerifyItemInput[]
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 购物车结算批次号（POS 购物车一次结算生成，用于订单/核销/开卡三单据聚合追溯；独立核销为空） */
  checkoutSessionNo?: string
}

/**
 * 项目卡到期提醒
 * 对齐后端 TreatmentCardExpiryDto（GET /treatmentCardSales/expiries）
 */
export interface TreatmentCardExpiry {
  /** 记录ID（同销售记录ID，雪花ID经 LongToStringConverter 序列化为字符串） */
  id: string
  /** 客户名称 */
  customerName: string
  /** 手机号 */
  phone: string
  /** 卡名称 */
  cardName: string
  /** 销售记录ID（雪花ID，序列化为字符串） */
  saleId: string
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
 * 项目卡到期提醒查询参数
 * 对齐后端 TreatmentCardExpiryQueryDto
 */
export interface TreatmentCardExpiryQuery {
  /** 客户名称或手机号关键字（模糊匹配，OR 语义） */
  keyword?: string
  /** 预警级别筛选（1:即将到期 2:已到期） */
  alertLevel?: ExpiryAlertLevel
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 项目卡到期提醒分页响应（含全量预警级别统计）
 * 对齐后端 TreatmentCardExpiryPageDto
 * 统计口径：基于当前搜索条件下 30 天内到期/已过期的全量记录，不受预警级别筛选与分页影响
 */
export interface TreatmentCardExpiryPage {
  /** 数据列表 */
  list: TreatmentCardExpiry[]
  /** 总数（受预警级别筛选影响） */
  total: number
  /** 当前页码 */
  pageIndex: number
  /** 每页条数 */
  pageSize: number
  /** 即将到期数量（全量） */
  expiringCount: number
  /** 已到期数量（全量） */
  expiredCount: number
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
