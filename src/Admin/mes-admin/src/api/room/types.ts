// ==========================================
// 房间/床位资源管理类型定义
// ==========================================

/**
 * 房间类型
 * - 1: 房间
 * - 2: 床位
 */
export type RoomType = 1 | 2

/**
 * 房间状态
 * - 0: 禁用
 * - 1: 启用
 */
export type RoomStatus = 0 | 1

/**
 * 房间/床位信息
 */
export interface Room {
  /** 房间/床位ID */
  id: number
  /** 房间/床位名称 */
  name: string
  /** 编码 */
  code: string
  /** 类型：1-房间，2-床位 */
  roomType: RoomType
  /** 状态：0-禁用，1-启用 */
  status: RoomStatus
  /** 位置描述 */
  location?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 房间查询参数
 */
export interface RoomQuery {
  /** 名称（模糊匹配） */
  name?: string
  /** 编码（模糊匹配） */
  code?: string
  /** 类型筛选 */
  roomType?: RoomType
  /** 状态筛选 */
  status?: RoomStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建房间请求
 */
export interface RoomCreate {
  name: string
  code: string
  roomType: RoomType
  status: RoomStatus
  location?: string
  remark?: string
}

/**
 * 更新房间请求
 */
export interface RoomUpdate extends RoomCreate {
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
