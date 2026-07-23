// ==========================================
// 服务人员模块类型定义
// 对齐后端 Technicians DTO（TechnicianDto / TechnicianStatisticDto）
// ==========================================

/**
 * 技师状态
 * - 0: 禁用
 * - 1: 启用
 */
export type TechnicianStatus = number

/**
 * 性别
 * - 0: 未知
 * - 1: 男
 * - 2: 女
 */
export type Gender = number

/**
 * 技师来源
 * - 1: 商家技师
 * - 2: 平台技师
 */
export type TechnicianSource = 1 | 2

/**
 * 技师职级（后端 DTO 无此字段，保留供前端使用）
 * - 1: 初级
 * - 2: 中级
 * - 3: 高级
 * - 4: 总监
 */
export type TechnicianLevel = number

/**
 * 技师信息
 * 对齐后端 TechnicianDto
 */
export interface Technician {
  /** 技师ID */
  id: number
  /** 技师姓名 */
  name: string
  /** 手机号 */
  phone: string
  /** 性别（0:未知 1:男 2:女） */
  gender: Gender
  /** 技能分类ID列表 */
  skillCategoryIds?: number[]
  /** 技能分类名称列表 */
  skillCategoryNames?: string[]
  /** 头像URL */
  avatarUrl?: string
  /** 状态（0:禁用 1:启用） */
  status: TechnicianStatus
  /** 技师来源（1:商家技师 2:平台技师） */
  source: TechnicianSource
  /** 备注 */
  remark: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 后端无此字段 */
  jobNumber?: string
  /** @deprecated 使用 skillTags 替代（后端为字符串） */
  skills?: string[]
  /** @deprecated 后端无此字段 */
  serviceItems?: string
  /** @deprecated 后端无此字段 */
  level?: number
  /** @deprecated 后端无此字段 */
  hireDate?: string
}

/**
 * 技师查询参数
 * 对齐后端 TechnicianQueryDto
 */
export interface TechnicianQuery {
  /** 技师姓名（模糊匹配） */
  name?: string
  /** 手机号（模糊匹配） */
  phone?: string
  /** 状态筛选（0:禁用 1:启用） */
  status?: TechnicianStatus
  /** 技师来源（1:商家技师 2:平台技师） */
  source?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** @deprecated 后端无此筛选 */
  level?: number
}

/**
 * 创建技师请求
 * 对齐后端 TechnicianCreateDto
 * 注：source 字段由后端根据当前租户强制赋值，前端不传入
 */
export interface TechnicianCreate {
  /** 技师姓名 */
  name: string
  /** 手机号 */
  phone: string
  /** 性别（0:未知 1:男 2:女） */
  gender: Gender
  /** 技能分类ID列表 */
  skillCategoryIds?: number[]
  /** 头像URL */
  avatarUrl?: string
  /** 状态（0:禁用 1:启用） */
  status: TechnicianStatus
  /** 备注 */
  remark?: string

  // ---- 以下字段后端不支持，保留为可选供前端页面使用 ----
  /** @deprecated 后端无此字段 */
  jobNumber?: string
  /** @deprecated 使用 skillTags 替代 */
  skills?: string[]
  /** @deprecated 后端无此字段 */
  serviceItems?: string
  /** @deprecated 后端无此字段 */
  level?: number
  /** @deprecated 后端无此字段 */
  hireDate?: string
}

/**
 * 更新技师请求
 * 对齐后端 TechnicianUpdateDto
 */
export interface TechnicianUpdate extends TechnicianCreate {
  id: number
}

/**
 * 技师统计信息
 * 对齐后端 TechnicianStatisticDto
 */
export interface TechnicianStatistics {
  /** 统计记录ID */
  id: number
  /** 技师ID */
  technicianId: number
  /** 统计日期 */
  statDate: string
  /** 服务人次 */
  serviceCount: number
  /** 服务时长（分钟） */
  serviceMinutes: number
  /** 技师服务费用汇总 */
  totalTechnicianFee: number
  /** 回头客数量 */
  returnCustomerCount: number
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ---- 以下字段后端 DTO 不返回，保留为可选供前端页面使用 ----
  /** @deprecated 后端不返回 */
  name?: string
  /** @deprecated 后端不返回 */
  jobNumber?: string
  /** @deprecated 使用 serviceMinutes 替代（单位：分钟） */
  totalDuration?: number
  /** @deprecated 后端不返回 */
  totalRevenue?: number
  /** @deprecated 使用 totalTechnicianFee 替代 */
  commission?: number
  /** @deprecated 使用 returnCustomerCount 替代 */
  returnRate?: number
  /** @deprecated 后端不返回 */
  averagePrice?: number
  /** @deprecated 后端不返回 */
  rating?: number
}

/**
 * 技师统计查询参数
 * 对齐后端 TechnicianStatisticQueryDto
 */
export interface TechnicianStatisticsQuery {
  /** 技师ID */
  technicianId?: number
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
  /** 是否强制查看报表（仅管理员可设置，用于运维排查纯平台技师门店） */
  force?: boolean

  // ---- 以下参数后端不支持，保留但不会传递 ----
  /** @deprecated 后端用 technicianId */
  name?: string
}

/**
 * 技师业绩统计报表（从 OrderItem 实时聚合，仅商家技师）
 * 对齐后端 TechnicianStatisticReportDto
 */
export interface TechnicianStatisticReport {
  /** 技师ID */
  technicianId: number
  /** 技师姓名 */
  technicianName: string
  /** 服务人次 */
  serviceCount: number
  /** 服务总时长（分钟） */
  serviceMinutes: number
  /** 技师服务费用汇总 */
  totalTechnicianFee: number
  /** 总客户数（去重） */
  totalCustomerCount: number
  /** 回头客数量 */
  returnCustomerCount: number
}

/**
 * 技师业绩统计报表响应（外层包装）
 * 对齐后端 TechnicianStatReportDto
 * 纯平台技师门店（无自有技师）返回 isPurePlatformStore=true + 空列表，业绩由平台统一统计
 */
export interface TechnicianStatReport {
  /** 是否纯平台技师门店（无自有技师） */
  isPurePlatformStore: boolean
  /** 提示信息（纯平台技师门店时返回） */
  message?: string
  /** 当前页数据列表 */
  items: TechnicianStatisticReport[]
  /** 总记录数 */
  total: number
  /** 当前页码 */
  pageIndex: number
  /** 每页条数 */
  pageSize: number
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
