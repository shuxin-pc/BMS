// ==========================================
// 仪器设备管理类型定义
// ==========================================

/**
 * 设备状态
 * - 1: 正常
 * - 2: 维修中
 * - 3: 已停用
 */
export type EquipmentStatus = 1 | 2 | 3

/**
 * 保养类型
 * - 1: 日常保养
 * - 2: 定期保养
 * - 3: 维修
 */
export type MaintenanceType = 1 | 2 | 3

/**
 * 设备台账信息
 */
export interface Equipment {
  /** 设备ID */
  id: number
  /** 所属设备类型 ID */
  equipmentTypeId: number
  /** 所属设备类型名称（展示用） */
  equipmentTypeName?: string
  /** 设备名称 */
  name: string
  /** 设备编码（资产编号） */
  code: string
  /** 型号 */
  model?: string
  /** 厂商 */
  manufacturer?: string
  /** 购买日期 */
  purchaseDate?: string
  /** 购买价格 */
  purchasePrice?: number
  /** 设备状态：1-正常，2-维修中，3-已停用 */
  status: EquipmentStatus
  /** 存放位置 */
  location?: string
  /** 上次保养日期 */
  lastMaintenanceDate?: string
  /** 下次保养日期 */
  nextMaintenanceDate?: string
  /** 保养周期（天），null=不定期 */
  maintenanceCycleDays?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 设备保养记录
 */
export interface EquipmentMaintenance {
  /** 保养记录ID */
  id: number
  /** 关联设备ID */
  equipmentId: number
  /** 关联设备名称（展示用） */
  equipmentName?: string
  /** 保养类型：1-日常保养，2-定期保养，3-维修 */
  maintenanceType: MaintenanceType
  /** 保养日期 */
  maintenanceDate: string
  /** 保养费用 */
  cost?: number
  /** 操作人 */
  operator?: string
  /** 保养结果 */
  result?: string
  /** 下次保养日期 */
  nextMaintenanceDate?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
}

/**
 * 设备查询参数
 */
export interface EquipmentQuery {
  /** 设备名称（模糊匹配） */
  name?: string
  /** 设备编码（模糊匹配） */
  code?: string
  /** 状态筛选 */
  status?: EquipmentStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 保养记录查询参数
 */
export interface MaintenanceQuery {
  /** 设备ID */
  equipmentId?: number
  /** 保养类型 */
  maintenanceType?: MaintenanceType
  /** 保养日期范围-开始 */
  startDate?: string
  /** 保养日期范围-结束 */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建设备请求
 */
export interface EquipmentCreate {
  /** 所属设备类型 ID（必填） */
  equipmentTypeId: number
  name: string
  code: string
  model?: string
  manufacturer?: string
  purchaseDate?: string
  purchasePrice?: number
  status: EquipmentStatus
  location?: string
  lastMaintenanceDate?: string
  nextMaintenanceDate?: string
  /** 保养周期（天），不填=不定期 */
  maintenanceCycleDays?: number
  remark?: string
}

/**
 * 更新设备请求
 */
export interface EquipmentUpdate extends EquipmentCreate {
  id: number
}

/**
 * 创建保养记录请求
 */
export interface MaintenanceCreate {
  equipmentId: number
  maintenanceType: MaintenanceType
  maintenanceDate: string
  cost?: number
  operator?: string
  result?: string
  nextMaintenanceDate?: string
  remark?: string
}

/**
 * 更新保养记录请求
 */
export interface MaintenanceUpdate extends MaintenanceCreate {
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
