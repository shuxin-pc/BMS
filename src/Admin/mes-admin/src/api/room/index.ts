// 房间/床位资源管理 - API服务（Mock 数据实现）
import type {
  Room,
  RoomQuery,
  RoomCreate,
  RoomUpdate,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  Room,
  RoomQuery,
  RoomCreate,
  RoomUpdate,
  PagedResponse
}

// ==================== Mock 数据 ====================

const mockRooms: Room[] = [
  {
    id: 1,
    name: 'VIP美容室',
    code: 'R-001',
    roomType: 1,
    status: 1,
    location: '2楼东区',
    remark: '高端VIP客户专用',
    createdAt: '2023-06-10 09:00:00'
  },
  {
    id: 2,
    name: '标准美容室A',
    code: 'R-002',
    roomType: 1,
    status: 1,
    location: '2楼西区',
    remark: '常规美容项目',
    createdAt: '2023-06-10 09:05:00'
  },
  {
    id: 3,
    name: '标准美容室B',
    code: 'R-003',
    roomType: 1,
    status: 1,
    location: '2楼西区',
    remark: '常规美容项目',
    createdAt: '2023-06-10 09:10:00'
  },
  {
    id: 4,
    name: '治疗室A',
    code: 'R-004',
    roomType: 1,
    status: 1,
    location: '3楼东区',
    remark: '仪器治疗专用',
    createdAt: '2023-06-15 14:00:00'
  },
  {
    id: 5,
    name: '治疗室B',
    code: 'R-005',
    roomType: 1,
    status: 0,
    location: '3楼东区',
    remark: '装修中，暂时禁用',
    createdAt: '2023-06-15 14:05:00',
    updatedAt: '2024-07-01 10:00:00'
  },
  {
    id: 6,
    name: '床位01',
    code: 'B-001',
    roomType: 2,
    status: 1,
    location: '大厅A区',
    remark: '开放式床位',
    createdAt: '2023-07-01 11:00:00'
  },
  {
    id: 7,
    name: '床位02',
    code: 'B-002',
    roomType: 2,
    status: 1,
    location: '大厅A区',
    remark: '开放式床位',
    createdAt: '2023-07-01 11:05:00'
  },
  {
    id: 8,
    name: '床位03',
    code: 'B-003',
    roomType: 2,
    status: 0,
    location: '大厅B区',
    remark: '设备维护中',
    createdAt: '2023-07-01 11:10:00',
    updatedAt: '2024-07-10 16:00:00'
  }
]

// Mock 自增 ID
let roomIdCounter = 100

// 模拟延迟
const delay = (ms: number = 300) => new Promise(resolve => setTimeout(resolve, ms))

// ==================== API 函数 ====================

/**
 * 获取房间/床位分页列表
 * @param query 查询参数
 * @returns 分页房间列表
 */
export async function getRoomList(query?: RoomQuery): Promise<PagedResponse<Room>> {
  await delay()
  let list = [...mockRooms]

  if (query?.name) {
    list = list.filter(item => item.name.includes(query.name!))
  }
  if (query?.code) {
    list = list.filter(item => item.code.includes(query.code!))
  }
  if (query?.roomType !== undefined) {
    list = list.filter(item => item.roomType === query.roomType)
  }
  if (query?.status !== undefined) {
    list = list.filter(item => item.status === query.status)
  }

  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  const pagedList = list.slice(start, start + pageSize)

  return {
    list: pagedList,
    total,
    pageIndex,
    pageSize
  }
}

/**
 * 获取房间详情
 * @param id 房间ID
 * @returns 房间详情
 */
export async function getRoom(id: number): Promise<Room> {
  await delay()
  const item = mockRooms.find(r => r.id === id)
  if (!item) throw new Error('房间不存在')
  return { ...item }
}

/**
 * 创建房间
 * @param data 房间信息
 */
export async function createRoom(data: RoomCreate): Promise<void> {
  await delay()
  const exists = mockRooms.some(r => r.code === data.code)
  if (exists) throw new Error('房间编码已存在')
  const newItem: Room = {
    ...data,
    id: ++roomIdCounter,
    createdAt: new Date().toLocaleString('zh-CN', { hour12: false })
  }
  mockRooms.unshift(newItem)
}

/**
 * 更新房间
 * @param data 房间信息
 */
export async function updateRoom(data: RoomUpdate): Promise<void> {
  await delay()
  const index = mockRooms.findIndex(r => r.id === data.id)
  if (index === -1) throw new Error('房间不存在')
  const codeConflict = mockRooms.some(r => r.id !== data.id && r.code === data.code)
  if (codeConflict) throw new Error('房间编码已存在')
  mockRooms[index] = {
    ...mockRooms[index],
    ...data,
    updatedAt: new Date().toLocaleString('zh-CN', { hour12: false })
  }
}

/**
 * 删除房间
 * @param id 房间ID
 */
export async function deleteRoom(id: number): Promise<void> {
  await delay()
  const index = mockRooms.findIndex(r => r.id === id)
  if (index === -1) throw new Error('房间不存在')
  mockRooms.splice(index, 1)
}

/**
 * 切换房间状态
 * @param id 房间ID
 * @param status 目标状态
 */
export async function toggleRoomStatus(id: number, status: 0 | 1): Promise<void> {
  await delay(200)
  const item = mockRooms.find(r => r.id === id)
  if (!item) throw new Error('房间不存在')
  item.status = status
  item.updatedAt = new Date().toLocaleString('zh-CN', { hour12: false })
}
