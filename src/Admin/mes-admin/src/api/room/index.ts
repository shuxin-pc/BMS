// 房间/床位资源管理 - API 服务
// 对接后端 RoomsController（路由 /api/store/rooms）
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Room,
  RoomQuery,
  RoomCreate,
  RoomUpdate
} from './types'

// 导出类型供外部使用
export type {
  Room,
  RoomQuery,
  RoomCreate,
  RoomUpdate,
  PagedResponse
}

// ==================== API 方法 ====================

/**
 * 获取房间/床位分页列表
 * 对接后端：GET /api/store/rooms
 * @param query 查询参数
 * @returns 分页房间列表
 */
export async function getRoomList(query?: RoomQuery): Promise<PagedResponse<Room>> {
  const qs = buildQuery({
    name: query?.name,
    code: query?.code,
    roomType: query?.roomType,
    status: query?.status,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Room>>(`/rooms${qs}`)
}

/**
 * 获取房间详情
 * 对接后端：GET /api/store/rooms/{id}
 * @param id 房间ID
 * @returns 房间详情
 */
export async function getRoom(id: number): Promise<Room> {
  return request<Room>(`/rooms/${id}`)
}

/**
 * 创建房间
 * 对接后端：POST /api/store/rooms
 * @param data 房间信息
 * @returns 创建后的房间信息
 */
export async function createRoom(data: RoomCreate): Promise<Room> {
  return request<Room>('/rooms', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新房间
 * 对接后端：PUT /api/store/rooms/{id}
 * @param data 房间信息
 * @returns 更新后的房间信息
 */
export async function updateRoom(data: RoomUpdate): Promise<Room> {
  return request<Room>(`/rooms/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除房间
 * 对接后端：DELETE /api/store/rooms/{id}
 * @param id 房间ID
 */
export async function deleteRoom(id: number): Promise<void> {
  await request<void>(`/rooms/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除房间
 * 对接后端：POST /api/store/rooms/batch
 * @param ids 房间ID列表
 */
export async function deleteRooms(ids: number[]): Promise<void> {
  await request<void>('/rooms/batch', {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}
