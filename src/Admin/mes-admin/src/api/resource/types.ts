// 资源可用性 DTO（与后端 Bms.Store.Application.Dtos.Resources 对齐）

/**
 * 技师可用性条目
 */
export interface TechnicianAvailabilityItem {
  id: number
  name: string
  source: number
  /** true=标红（该时段已被占用，仍可选） */
  isOccupied: boolean
  /** 占用来源：'appointment' / 'order' */
  conflictSource?: string
  /** 冲突描述（占用单号 + 时段） */
  conflictInfo?: string
}

/**
 * 房间/床位可用性条目
 */
export interface RoomAvailabilityItem {
  id: number
  name: string
  roomType: number
  status: number
  isOccupied: boolean
  conflictSource?: string
  conflictInfo?: string
}

/**
 * 设备可用性条目
 */
export interface EquipmentAvailabilityItem {
  id: number
  name: string
  status: number
  isOccupied: boolean
  conflictSource?: string
  conflictInfo?: string
}

/**
 * 资源可用性查询结果
 */
export interface ResourceAvailabilityDto {
  technicians: TechnicianAvailabilityItem[]
  rooms: RoomAvailabilityItem[]
  equipments: EquipmentAvailabilityItem[]
}
