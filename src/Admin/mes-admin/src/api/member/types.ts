// ==========================================
// 会员储值管理类型定义
// 对齐后端 StoredValues DTO（StoredValueAccountDto / StoredValueLogDto / StoredValueRuleDto）
// ==========================================

/**
 * 储值账户
 * 对齐后端 StoredValueAccountDto
 */
export interface MemberAccount {
  /** 账户ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称（客户已删除时为空） */
  customerName?: string
  /** 客户手机号（客户已删除时为空） */
  phone?: string
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
}

/**
 * 储值账户查询参数
 * 对齐后端 StoredValueAccountQueryDto
 */
export interface MemberAccountQuery {
  /** 客户ID */
  customerId?: number
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 手机号（模糊匹配） */
  phone?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 充值请求
 * 对齐后端 StoredValueRechargeDto。赠送金额由后端按储值规则计算，前端不传
 */
export interface RechargeRequest {
  /** 客户ID */
  customerId: number
  /** 充值金额（实收） */
  amount: number
  /** 支付方式：1-现金，2-支付宝，3-微信，4-银行卡 */
  payMethod?: number
  /** 备注 */
  remark?: string
}

/**
 * 充值赠送金额试算结果
 * 对齐后端 StoredValueGiftPreviewDto
 */
export interface RechargeGiftPreview {
  /** 充值金额 */
  amount: number
  /** 按储值规则计算出的赠送金额 */
  giftAmount: number
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
  /** 充值金额（同租户内唯一） */
  amount: number
  /** 赠送金额 */
  giftAmount: number
  /** 是否启用 */
  isEnabled: boolean
  /** 生效日期（含当天） */
  startDate: string
  /** 失效日期（含当天，为空表示长期有效） */
  endDate?: string
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
  /** @deprecated 使用 isEnabled 替代 */
  status?: number
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
  /** 充值金额（同租户内唯一） */
  amount: number
  /** 赠送金额 */
  giftAmount: number
  /** 是否启用 */
  isEnabled: boolean
  /** 生效日期（含当天） */
  startDate: string
  /** 失效日期（含当天，为空表示长期有效） */
  endDate?: string
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
  /** 支付方式（1:现金 2:支付宝 3:微信 4:银行卡 5:储值） */
  payMethod?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
  /** 客户姓名（后端关联 Customer 查询返回） */
  customerName?: string
  /** 客户手机号（后端关联 Customer 查询返回） */
  phone?: string
  /** 操作人ID */
  operatorId?: number
  /** 操作人姓名（写入时的姓名快照） */
  operatorName?: string
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
  /** 客户姓名（模糊匹配） */
  customerName?: string
  /** 客户手机号（模糊匹配） */
  phone?: string
  /** 开始日期 */
  startDate?: string
  /** 结束日期（含当日） */
  endDate?: string
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
