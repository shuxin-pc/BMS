// 商家技师管理类型定义
// 对齐后端 TechnicianDto / TechnicianQueryDto

/**
 * 技师状态
 * - 0: 禁用
 * - 1: 启用
 */
export type TechnicianStatus = 0 | 1

/**
 * 技师来源
 * - 1: 商家技师
 * - 2: 平台技师
 */
export type TechnicianSource = 1 | 2

/**
 * 商家技师信息
 */
export interface Technician {
  id: number
  name: string
  phone: string
  /** 性别：0-未知，1-男，2-女 */
  gender: number
  /** 技能分类ID列表 */
  skillCategoryIds?: number[]
  /** 技能分类名称列表 */
  skillCategoryNames?: string[]
  /** 头像URL */
  avatarUrl?: string
  /** 状态：0-禁用，1-启用 */
  status: TechnicianStatus
  /** 技师来源：1-商家技师，2-平台技师 */
  source: TechnicianSource
  remark?: string
  createdAt: string
  updatedAt?: string
}

/**
 * 技师查询参数
 */
export interface TechnicianQuery {
  name?: string
  phone?: string
  status?: TechnicianStatus
  source?: TechnicianSource
  pageIndex?: number
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
