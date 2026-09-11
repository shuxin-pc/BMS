// ==========================================
// 门店档案管理类型定义
// ==========================================

/**
 * 门店状态
 * - 1: 营业
 * - 2: 歇业
 */
export type StoreStatus = number

/**
 * 门店信息
 */
export interface Store {
  /** 门店ID（雪花ID，后端以字符串形式返回避免 JS 精度丢失） */
  id: string
  /** 门店名称 */
  name: string
  /** 门店编码（业务编码，如 S001） */
  code: string
  /** 门店简称 */
  shortName?: string
  /** 联系电话 */
  phone?: string
  /** 门店地址 */
  address?: string
  /** 营业时间（如 "09:00 - 22:00"） */
  businessHours?: string
  /** 门店面积（平方米） */
  area?: number
  /** 店长姓名 */
  managerName?: string
  /** 门店状态：1-营业，2-歇业 */
  status: StoreStatus
  /** 门店logo图片URL */
  logoUrl?: string
  /** 营业执照图片URL */
  businessLicenseUrl?: string
  /** 门店描述/备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 门店查询参数
 */
export interface StoreQuery {
  /** 门店名称（模糊匹配） */
  name?: string
  /** 门店编码（模糊匹配） */
  code?: string
  /** 状态筛选 */
  status?: StoreStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建门店请求
 */
export interface StoreCreate {
  /** 门店名称 */
  name: string
  /** 门店编码 */
  code: string
  /** 门店简称 */
  shortName?: string
  /** 联系电话 */
  phone?: string
  /** 门店地址 */
  address?: string
  /** 营业时间 */
  businessHours?: string
  /** 门店面积（平方米） */
  area?: number
  /** 店长姓名 */
  managerName?: string
  /** 门店状态 */
  status: StoreStatus
  /** 门店logo图片URL */
  logoUrl?: string
  /** 营业执照图片URL */
  businessLicenseUrl?: string
  /** 门店描述/备注 */
  remark?: string
}

/**
 * 更新门店请求
 */
export interface StoreUpdate extends StoreCreate {
  /** 门店ID（雪花ID，后端以字符串形式返回避免 JS 精度丢失） */
  id: string
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

/**
 * 租户用户简略信息（用于门店授权分配）
 */
export interface TenantUser {
  /** 用户ID（雪花ID，后端以字符串形式返回避免 JS 精度丢失） */
  id: string
  /** 登录用户名 */
  userName: string
  /** 真实姓名 */
  realName: string
}

/**
 * 可分配用户信息（在租户有效用户基础上标记是否已分配给目标门店）
 */
export interface AvailableUser extends TenantUser {
  /** 是否已分配给当前门店 */
  assigned: boolean
}

// ==========================================
// 门店设置类型定义
// ==========================================

/**
 * 门店设置（每门店一条记录，不存在时后端返回默认值）
 * 跨店核销为租户级语义（后端按租户取第一条记录）
 * 各类型提醒接收角色已拆分至子表 StoreReminderSetting（通过 reminder-settings 接口读写）
 */
export interface StoreTenantSetting {
  /** 记录ID（不存在记录时后端返回 0） */
  id: string
  /** 门店ID（当前门店上下文） */
  storeId: string
  /** 是否允许跨店核销（租户级）：true-允许（默认），false-仅限发卡门店核销 */
  allowCrossStoreVerify: boolean
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 更新门店设置请求
 */
export interface StoreTenantSettingUpdate {
  /** 是否允许跨店核销 */
  allowCrossStoreVerify: boolean
}

/**
 * 门店提醒配置（对应子表 StoreReminderSetting）
 * 每门店 + 每业务类型一行，通用化承载各类提醒的接收角色
 */
export interface StoreReminderSetting {
  /** 提醒业务类型编码（Birthday=生日、Appointment=预约、TreatmentExpiry=项目卡到期） */
  reminderType: string
  /** 接收该类型提醒站内信的角色ID列表（空数组表示不发送；long 经 LongToStringConverter 序列化为字符串） */
  roleIds: string[]
}
