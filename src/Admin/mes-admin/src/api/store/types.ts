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
