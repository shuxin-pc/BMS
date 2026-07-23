// ==========================================
// 供应商管理类型定义
// 字段与后端 SupplierDto / SupplierCreateDto / SupplierQueryDto 对齐（camelCase）
// ==========================================

/**
 * 合作状态
 * - 0: 禁用
 * - 1: 启用
 */
export type CooperationStatus = number

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
 * 供应商信息（与后端 SupplierDto 对齐）
 */
export interface Supplier {
  /** 供应商ID */
  id: number
  /** 供应商名称 */
  name: string
  /** 供应商编码 */
  code: string
  /** 联系人 */
  contact?: string
  /** 联系电话 */
  phone?: string
  /** 地址 */
  address?: string
  /** 银行账户 */
  bankAccount?: string
  /** 状态：0-禁用，1-启用 */
  status: CooperationStatus
  /** 备注 */
  remark?: string
  /** 累计采购额（只读，后端计算） */
  totalPurchaseAmount: number
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 供应商查询参数（与后端 SupplierQueryDto 对齐）
 */
export interface SupplierQuery {
  /** 供应商名称（模糊匹配） */
  name?: string
  /** 供应商编码 */
  code?: string
  /** 状态筛选：0-禁用，1-启用 */
  status?: CooperationStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建供应商请求（与后端 SupplierCreateDto 对齐）
 */
export interface SupplierCreate {
  name: string
  code: string
  contact?: string
  phone?: string
  address?: string
  bankAccount?: string
  status: CooperationStatus
  remark?: string
}

/**
 * 更新供应商请求（与后端 SupplierUpdateDto 对齐，继承 Create + id）
 */
export interface SupplierUpdate extends SupplierCreate {
  id: number
}
