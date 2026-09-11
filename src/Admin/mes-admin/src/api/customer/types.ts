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
  /** 等级值（1:普通会员 2:会员），创建后不可修改 */
  level: number
  /** 折扣率（如 0.9 表示 9 折） */
  discountRate: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 标签颜色（el-tag type 名）
 * - primary
 * - success
 * - warning
 * - danger
 * - info
 */
export type TagColor = 'primary' | 'success' | 'warning' | 'danger' | 'info'

/**
 * 客户标签（字典实体，门店级隔离）
 */
export interface CustomerTag {
  id: number
  /** 标签名称（同门店内唯一） */
  name: string
  /** 标签颜色 */
  color?: TagColor | string
  /** 排序 */
  sort: number
  /** 备注 */
  remark?: string
  createdAt?: string
  updatedAt?: string
}

/**
 * 客户标签简要信息（嵌套在 Customer 中返回）
 */
export interface CustomerTagBrief {
  /** 标签ID（后端 long 序列化为字符串） */
  id: string
  name: string
  color?: TagColor | string
}

/**
 * 创建客户标签请求
 */
export interface CustomerTagCreate {
  name: string
  color?: TagColor | string
  sort: number
  remark?: string
}

/**
 * 更新客户标签请求
 */
export interface CustomerTagUpdate extends CustomerTagCreate {
  id: number
}

/**
 * 客户标签查询参数
 */
export interface CustomerTagQuery {
  name?: string
  pageIndex?: number
  pageSize?: number
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
  /** 客户标签列表 */
  tags?: CustomerTagBrief[]
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
  /** 客户名称或手机号关键字（模糊匹配，OR 语义） */
  keyword?: string
  /** 等级ID */
  levelId?: number
  /** 客户标签ID（按标签筛选关联客户） */
  tagId?: number
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
  /** 已有标签 ID 列表（后端 long 序列化为字符串，故为 string[]） */
  tagIds?: string[]
  /** 新建标签名称列表（后端自动创建标签并关联） */
  newTagNames?: string[]
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
 * 客户档案永久删除输入
 * 对齐后端 Bms.Store.Application.Dtos.Customers.CustomerPermanentDeleteDto
 * 用于物理删除客户档案及关联个人信息，满足《个人信息保护法》第 47 条合规要求
 */
export interface CustomerPermanentDeleteDto {
  /** 二次确认码（客户手机号后4位） */
  confirmCode: string
  /** 删除原因（写入审计日志，永久保留） */
  reason: string
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
// 客户等级扩展
// ==========================================

/**
 * 创建客户等级请求
 */
export interface CustomerLevelCreate {
  /** 等级名称 */
  name: string
  /** 等级编码 */
  code: string
  /** 等级值（1:普通会员 2:会员），创建后不可修改 */
  level: number
  /** 折扣率（如 0.9 表示 9 折） */
  discountRate: number
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
// 积分管理类型定义
// ==========================================

/**
 * 积分规则配置（对齐后端 PointsRuleDto）
 */
export interface PointsRule {
  /** 规则ID（后端 long 序列化为字符串，前端按 number 使用） */
  id: number
  /** 消费1元获得积分数 */
  pointsRate: number
  /** 每积分可抵扣金额（小数，如 0.01 表示 100 积分=1 元） */
  deductRate: number
  /** 单笔最高抵扣金额（0=不限） */
  maxDeductAmount: number
  /** 积分有效期天数（null=永久） */
  pointsValidityDays?: number | null
  /** 生日双倍积分 */
  birthdayDouble: boolean
  /** 单笔最低消费金额门槛（null=无门槛） */
  minAmountThreshold?: number | null
  /** 生效状态：0-禁用，1-启用 */
  status: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 积分变动类型
 * - 1: 消费获取
 * - 2: 积分抵扣
 * - 3: 退款扣减
 * - 5: 充值获得
 * - 6: 项目卡购买获得
 * - 7: 过期清零
 * - 8: 手动调整
 */
export type PointsChangeType = 1 | 2 | 3 | 5 | 6 | 7 | 8

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
  points: number
  /** 变动前积分 */
  beforePoints: number
  /** 变动后积分 */
  afterPoints: number
  /** 变动类型：1-消费获取，3-退款扣减 */
  type: PointsChangeType
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
  /** 客户名称或手机号关键字（模糊匹配，OR 语义） */
  keyword?: string
  /** 变动类型 */
  changeType?: PointsChangeType
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建积分流水请求（仅用于手动调整 Type=8，其他类型由业务流程自动写入）
 */
export interface PointsLogCreate {
  /** 客户ID */
  customerId: number
  /** 积分类型（手动调整固定为 8） */
  type: 8
  /** 变动积分（正数增加，负数扣减） */
  points: number
  /** 变动前积分 */
  beforePoints: number
  /** 变动后积分 */
  afterPoints: number
  /** 原因备注（必填） */
  remark: string
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
  /** 支付方式：1-现金，2-支付宝，3-微信，4-银行卡，5-储值卡，6-积分抵扣，7-组合支付 */
  paymentMethod: number
  /** 订单状态：2-已完成，3-已退款，4-已取消 */
  status: number
  /** 消费时间 */
  consumeTime: string
}

/**
 * 消费记录查询参数
 */
export interface ConsumeRecordQuery {
  /** 客户名称或手机号关键字（模糊匹配，OR 语义） */
  keyword?: string
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
  /** 操作人姓名（已关怀时显示） */
  operatorName?: string
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
  /** 近7天累计消费金额 */
  totalAmount: number
  /** 近7天消费频次（订单笔数） */
  orderCount: number
  /** 最近消费时间 */
  lastConsumeTime: string
  /** 感谢状态：1-待感谢，2-已感谢 */
  thankStatus: CareStatus
  /** 感谢方式：1-短信，2-微信，3-电话 */
  thankMethod?: ThankMethod
  /** 感谢时间 */
  thankTime?: string
  /** 操作人姓名（已感谢时显示） */
  operatorName?: string
}

/**
 * 生日提醒查询参数
 */
export interface BirthdayReminderQuery {
  /** 客户名称或手机号关键字（模糊匹配，OR 语义） */
  keyword?: string
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
  /** 客户名称或手机号关键字（模糊匹配，OR 语义） */
  keyword?: string
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
  /** 商品类型（1:实物 2:服务 4:项目卡） */
  productType: number
  /** 商品类型名称 */
  productTypeName: string
  /** 该类型消费金额 */
  amount: number
  /** 金额占比（0-1） */
  percentage: number
}
