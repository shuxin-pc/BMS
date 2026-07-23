// ==========================================
// 会员储值管理类型定义
// 对齐后端 StoredValues DTO（StoredValueAccountDto / StoredValueLogDto / StoredValueRuleDto）
// ==========================================

/**
 * 账户状态（前端保留类型别名，后端 DTO 无此字段）
 */
export type AccountStatus = number

/**
 * 储值账户
 * 对齐后端 StoredValueAccountDto
 */
export interface MemberAccount {
  /** 账户ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 当前余额（总余额 = 实收余额 + 赠送余额） */
  balance: number
  /** 实收余额 */
  realBalance: number
  /** 赠送余额 */
  giftBalance: number
  /** 累计充值金额 */
  totalRecharge: number
  /** 累计赠送金额 */
  totalGift: number
  /** 累计消费金额 */
  totalConsume: number
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** 客户名称（后端不返回，需前端 join） */
  customerName?: string
  /** 手机号（后端不返回） */
  phone?: string
  /** 账户状态（后端 DTO 无此字段） */
  status?: AccountStatus
  /** 开卡时间（后端用 createdAt） */
  openTime?: string
  /** 备注（后端 DTO 无此字段） */
  remark?: string
}

/**
 * 储值账户查询参数
 * 对齐后端 StoredValueAccountQueryDto（仅支持 customerId + 分页）
 */
export interface MemberAccountQuery {
  /** 客户ID */
  customerId?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** 客户名称（后端不支持） */
  customerName?: string
  /** 手机号（后端不支持） */
  phone?: string
  /** 账户状态（后端不支持） */
  status?: AccountStatus
}

/**
 * 充值请求（前端业务参数，由 index.ts 转换为后端 StoredValueLogCreateDto）
 */
export interface RechargeRequest {
  /** 账户ID */
  accountId: number
  /** 充值金额（实收） */
  amount: number
  /** 赠送金额 */
  bonusAmount: number
  /** 支付方式：1-现金，2-微信，3-支付宝 */
  paymentMethod: number
  /** 备注 */
  remark?: string
}

/**
 * 储值规则
 * 对齐后端 StoredValueRuleDto
 */
export interface RechargeRule {
  /** 规则ID */
  id: number
  /** 规则名称 */
  name: string
  /** 规则编码 */
  code: string
  /** 充值金额 */
  amount: number
  /** 赠送金额 */
  giftAmount: number
  /** 赠送比例 */
  giftRate?: number
  /** 是否启用 */
  isEnabled: boolean
  /** 排序 */
  sort: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 使用 amount 替代 */
  rechargeAmount?: number
  /** @deprecated 使用 giftAmount 替代 */
  bonusAmount?: number
  /** @deprecated 使用 giftRate 替代 */
  bonusRate?: number
  /** @deprecated 使用 isEnabled 替代 */
  status?: number
  /** @deprecated 后端无此字段 */
  startDate?: string
  /** @deprecated 后端无此字段 */
  endDate?: string
}

/**
 * 储值规则查询参数
 * 对齐后端 StoredValueRuleQueryDto
 */
export interface RechargeRuleQuery {
  /** 规则名称（模糊匹配） */
  name?: string
  /** 是否启用 */
  isEnabled?: boolean
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** @deprecated 使用 isEnabled 替代 */
  status?: number
}

/**
 * 创建储值规则请求
 * 对齐后端 StoredValueRuleCreateDto
 */
export interface RechargeRuleCreate {
  /** 规则名称 */
  name: string
  /** 规则编码 */
  code: string
  /** 充值金额 */
  amount: number
  /** 赠送金额 */
  giftAmount: number
  /** 赠送比例 */
  giftRate?: number
  /** 是否启用 */
  isEnabled: boolean
  /** 排序 */
  sort: number
  /** 备注 */
  remark?: string
}

/**
 * 更新储值规则请求
 * 对齐后端 StoredValueRuleUpdateDto
 */
export interface RechargeRuleUpdate extends RechargeRuleCreate {
  id: number
}

/**
 * 流水类型
 * - 1: 充值
 * - 2: 消费
 * - 3: 退款
 * - 4: 调整
 */
export type TransactionType = number

/**
 * 储值流水
 * 对齐后端 StoredValueLogDto
 */
export interface MemberTransaction {
  /** 流水ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 流水类型：1-充值，2-消费，3-退款，4-调整 */
  type: TransactionType
  /** 金额变化 */
  amount: number
  /** 实收金额 */
  realAmount: number
  /** 赠送金额 */
  giftAmount: number
  /** 变动前余额 */
  beforeBalance: number
  /** 变动后余额 */
  afterBalance: number
  /** 实收余额变动 */
  realBalanceChange: number
  /** 赠送余额变动 */
  giftBalanceChange: number
  /** 变动前实收余额 */
  beforeRealBalance: number
  /** 变动后实收余额 */
  afterRealBalance: number
  /** 变动前赠送余额 */
  beforeGiftBalance: number
  /** 变动后赠送余额 */
  afterGiftBalance: number
  /** 关联订单ID */
  orderId?: number
  /** 支付方式（1:现金 2:支付宝 3:微信 4:银行卡） */
  payMethod?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 后端无此字段 */
  transactionNo?: string
  /** @deprecated 后端不返回 */
  customerName?: string
  /** @deprecated 后端不返回 */
  phone?: string
  /** @deprecated 使用 payMethod 替代 */
  paymentMethod?: number
  /** @deprecated 使用 createdAt 替代 */
  operationTime?: string
}

/**
 * 储值流水查询参数
 * 对齐后端 StoredValueLogQueryDto
 */
export interface MemberTransactionQuery {
  /** 客户ID */
  customerId?: number
  /** 流水类型 */
  type?: TransactionType
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** @deprecated 后端用 customerId */
  customerName?: string
  /** @deprecated 后端不支持 */
  startDate?: string
  /** @deprecated 后端不支持 */
  endDate?: string
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
 * 储值现金流统计（G7.3）
 * 对齐后端 StoredValueCashFlowDto
 */
export interface StoredValueCashFlow {
  /** 查询起始日期 */
  startDate?: string
  /** 查询结束日期 */
  endDate?: string
  /** 新增储值金额（实收） */
  totalRecharge: number
  /** 新增赠送金额 */
  totalGift: number
  /** 储值消费金额 */
  totalConsume: number
  /** 储值退款金额 */
  totalRefund: number
  /** 沉淀资金（期末储值余额） */
  totalBalance: number
  /** 期末实收余额 */
  totalRealBalance: number
  /** 期末赠送余额 */
  totalGiftBalance: number
}

/**
 * 通用API响应
 */
export interface ApiResponse<T> {
  code: number
  message?: string
  data: T
}
