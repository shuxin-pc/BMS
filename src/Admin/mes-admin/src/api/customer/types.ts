// ==========================================
// 客户档案管理类型定义
// 对齐后端 Bms.Store.Domain.Entities.Customer
// ==========================================

/**
 * 性别
 * - 0: 未知
 * - 1: 男
 * - 2: 女
 */
export type Gender = number

/**
 * 客户等级
 */
export interface CustomerLevel {
  /** 等级ID */
  id: number
  /** 等级名称 */
  name: string
  /** 等级编码 */
  code: string
  /** 折扣率（如 0.9 表示 9 折） */
  discountRate: number
  /** 排序 */
  sort: number
  /** 备注 */
  remark?: string
}

/**
 * 客户信息
 */
export interface Customer {
  /** 客户ID */
  id: number
  /** 客户姓名 */
  name: string
  /** 手机号 */
  phone: string
  /** 性别：0-未知，1-男，2-女 */
  gender: Gender
  /** 生日 */
  birthday?: string
  /** 客户等级ID */
  levelId?: number
  /** 客户等级名称（导航属性） */
  levelName?: string
  /** 累计积分 */
  totalPoints: number
  /** 当前余额 */
  balance: number
  /** 累计消费金额 */
  totalConsume: number
  /** 最后消费时间 */
  lastConsumeTime?: string
  /** 地址 */
  address?: string
  /** 客户标签（JSON格式或逗号分隔） */
  tags?: string
  /** 授权状态：0-未授权，1-已授权，2-已撤回 */
  authorizationStatus: AuthorizationStatus
  /** 授权时间 */
  authorizationTime?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 客户授权状态
 * - 0: 未授权
 * - 1: 已授权
 * - 2: 已撤回
 */
export type AuthorizationStatus = 0 | 1 | 2

/**
 * 客户查询参数
 */
export interface CustomerQuery {
  /** 姓名（模糊匹配） */
  name?: string
  /** 手机号（模糊匹配） */
  phone?: string
  /** 等级ID */
  levelId?: number
  /** 性别 */
  gender?: Gender
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建客户请求
 */
export interface CustomerCreate {
  name: string
  phone: string
  gender: Gender
  birthday?: string
  levelId?: number
  address?: string
  tags?: string
  authorizationStatus?: AuthorizationStatus
  authorizationTime?: string
  remark?: string
}

/**
 * 更新客户请求
 */
export interface CustomerUpdate extends CustomerCreate {
  id: number
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

// ==========================================
// 客户等级扩展（Mock 增删改）
// ==========================================

/**
 * 创建客户等级请求
 */
export interface CustomerLevelCreate {
  /** 等级名称 */
  name: string
  /** 等级编码 */
  code: string
  /** 折扣率（如 0.9 表示 9 折） */
  discountRate: number
  /** 排序 */
  sort: number
  /** 备注 */
  remark?: string
}

/**
 * 更新客户等级请求
 */
export interface CustomerLevelUpdate extends CustomerLevelCreate {
  /** 等级ID */
  id: number
}

// ==========================================
// 积分管理类型定义（Mock）
// ==========================================

/**
 * 积分规则配置
 */
export interface PointsRule {
  /** 规则ID */
  id: number
  /** 规则名称 */
  name: string
  /** 消费1元获得积分数 */
  pointsPerYuan: number
  /** 积分抵扣比例（1积分=N元） */
  pointsToYuan: number
  /** 生日双倍积分 */
  birthdayDouble: boolean
  /** 单笔最低获取积分门槛 */
  minPointsThreshold: number
  /** 生效状态：1-启用，2-停用 */
  status: number
  /** 备注 */
  remark?: string
  /** 更新时间 */
  updatedAt: string
}

/**
 * 积分变动类型
 * - 1: 消费获取
 * - 2: 兑换扣减
 * - 3: 活动赠送
 * - 4: 退款扣减
 */
export type PointsChangeType = 1 | 2 | 3 | 4

/**
 * 积分流水记录
 */
export interface PointsRecord {
  /** 流水ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称 */
  customerName: string
  /** 手机号 */
  phone: string
  /** 积分变动（+/-） */
  changePoints: number
  /** 变动前积分 */
  beforePoints: number
  /** 变动后积分 */
  afterPoints: number
  /** 变动类型：1-消费获取，2-兑换扣减，3-活动赠送，4-退款扣减 */
  changeType: PointsChangeType
  /** 变动时间 */
  changeTime: string
  /** 关联订单号 */
  orderNo?: string
  /** 备注 */
  remark?: string
}

/**
 * 积分流水查询参数
 */
export interface PointsRecordQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 变动类型 */
  changeType?: PointsChangeType
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

// ==========================================
// 消费记录类型定义（Mock）
// ==========================================

/**
 * 消费记录
 */
export interface ConsumeRecord {
  /** 记录ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称 */
  customerName: string
  /** 手机号 */
  phone: string
  /** 订单号 */
  orderNo: string
  /** 消费金额 */
  amount: number
  /** 消费项目摘要 */
  projectName: string
  /** 支付方式：1-现金，2-微信，3-支付宝，4-储值，5-组合 */
  paymentMethod: number
  /** 消费时间 */
  consumeTime: string
  /** 门店名称 */
  storeName: string
}

/**
 * 消费记录查询参数
 */
export interface ConsumeRecordQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 手机号（模糊匹配） */
  phone?: string
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

// ==========================================
// 客户关怀类型定义（Mock）
// ==========================================

/**
 * 关怀状态
 * - 1: 待关怀
 * - 2: 已关怀
 */
export type CareStatus = number

/**
 * 感谢方式
 * - 1: 短信
 * - 2: 微信
 * - 3: 电话
 */
export type ThankMethod = number

/**
 * 生日提醒记录
 */
export interface BirthdayReminder {
  /** 记录ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称 */
  customerName: string
  /** 手机号 */
  phone: string
  /** 生日（MM-dd） */
  birthday: string
  /** 距离生日天数 */
  daysToBirthday: number
  /** 关怀状态：1-待关怀，2-已关怀 */
  careStatus: CareStatus
  /** 关怀时间 */
  careTime?: string
}

/**
 * 消费感谢记录
 */
export interface ConsumeThankRecord {
  /** 记录ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称 */
  customerName: string
  /** 手机号 */
  phone: string
  /** 最近消费金额 */
  lastAmount: number
  /** 最近消费时间 */
  lastConsumeTime: string
  /** 感谢状态：1-待感谢，2-已感谢 */
  thankStatus: CareStatus
  /** 感谢方式：1-短信，2-微信，3-电话 */
  thankMethod?: ThankMethod
  /** 感谢时间 */
  thankTime?: string
}

/**
 * 生日提醒查询参数
 */
export interface BirthdayReminderQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 关怀状态 */
  careStatus?: CareStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 消费感谢查询参数
 */
export interface ConsumeThankQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 感谢状态 */
  thankStatus?: CareStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

// ==========================================
// 客户消费统计类型定义
// ==========================================

/**
 * 客户消费统计
 */
export interface CustomerConsumptionStat {
  /** 客户ID */
  customerId: number
  /** 消费频次（已完成订单数） */
  orderCount: number
  /** 累计消费金额 */
  totalConsumption: number
  /** 客单价 */
  averageOrderValue: number
  /** 最近消费时间 */
  lastConsumeTime?: string
  /** 消费偏好（按商品类型分组） */
  preferences: ConsumptionPreferenceItem[]
}

/**
 * 消费偏好项
 */
export interface ConsumptionPreferenceItem {
  /** 商品类型（1:实物 2:服务 4:疗程卡） */
  productType: number
  /** 商品类型名称 */
  productTypeName: string
  /** 该类型消费金额 */
  amount: number
  /** 金额占比（0-1） */
  percentage: number
}
