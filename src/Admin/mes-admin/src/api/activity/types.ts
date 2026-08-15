// ==========================================
// 活动管理模块类型定义
// 字段与后端 ActivityDto / ActivityOptionDto 对齐（camelCase）
// ==========================================

/**
 * 活动状态（前端动态计算，后端无字段）
 * - notStarted: 未开始（now < startTime）
 * - ongoing: 进行中（startTime <= now <= endTime）
 * - ended: 已结束（now > endTime）
 */
export type ActivityStatus = 'notStarted' | 'ongoing' | 'ended'

/**
 * 活动（与后端 ActivityDto 对齐）
 */
export interface Activity {
  /** 活动ID */
  id: number
  /** 活动名称 */
  name: string
  /** 开始时间 */
  startTime: string
  /** 结束时间 */
  endTime: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 活动查询参数（与后端 ActivityQueryDto 对齐）
 */
export interface ActivityQuery {
  /** 活动名称（模糊查询） */
  name?: string
  /** 活动时间区间起始日（与活动周期有重叠即命中） */
  startDate?: string
  /** 活动时间区间截止日（含当天，与活动周期有重叠即命中） */
  endDate?: string
  /** 活动状态筛选：notStarted / ongoing / ended */
  status?: ActivityStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建活动请求（与后端 ActivityCreateDto 对齐）
 */
export interface ActivityCreate {
  /** 活动名称 */
  name: string
  /** 开始时间 */
  startTime: string
  /** 结束时间（必须大于开始时间） */
  endTime: string
  /** 备注 */
  remark?: string
}

/**
 * 更新活动请求（继承 Create + id）
 */
export interface ActivityUpdate extends ActivityCreate {
  /** 活动ID */
  id: number
}

/**
 * 活动下拉选项（与后端 ActivityOptionDto 对齐，仅返回进行中活动）
 */
export interface ActivityOption {
  /** 活动ID */
  id: number
  /** 活动名称 */
  name: string
  /** 开始时间 */
  startTime: string
  /** 结束时间 */
  endTime: string
}
