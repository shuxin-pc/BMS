// 仪器设备管理 - API 服务
// 对接后端 EquipmentsController / EquipmentMaintenancesController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Equipment,
  EquipmentMaintenance,
  EquipmentQuery,
  MaintenanceQuery,
  EquipmentCreate,
  EquipmentUpdate,
  MaintenanceCreate,
  MaintenanceUpdate,
  EquipmentStatus
} from './types'

// 导出类型供外部使用
export type {
  Equipment,
  EquipmentMaintenance,
  EquipmentQuery,
  MaintenanceQuery,
  EquipmentCreate,
  EquipmentUpdate,
  MaintenanceCreate,
  MaintenanceUpdate,
  PagedResponse
}

// ==================== 设备台账 API ====================

/**
 * 获取设备分页列表
 * 对接后端：GET /api/store/equipments
 * @param query 查询参数
 * @returns 分页设备列表
 */
export async function getEquipmentList(query?: EquipmentQuery): Promise<PagedResponse<Equipment>> {
  const qs = buildQuery({
    name: query?.name,
    code: query?.code,
    status: query?.status,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Equipment>>(`/equipments${qs}`)
}

/**
 * 获取全部设备列表（用于下拉选择）
 * 对接后端：GET /api/store/equipments?pageIndex=1&pageSize=1000
 * @param status 可选状态筛选（如预约场景传 1 仅取正常设备）
 * @returns 设备列表
 */
export async function getAllEquipments(status?: EquipmentStatus): Promise<Equipment[]> {
  const qs = buildQuery({ status, pageIndex: 1, pageSize: 1000 })
  const result = await request<PagedResponse<Equipment>>(`/equipments${qs}`)
  return result.list
}

/**
 * 获取设备详情
 * 对接后端：GET /api/store/equipments/{id}
 * @param id 设备ID
 * @returns 设备详情
 */
export async function getEquipment(id: number): Promise<Equipment> {
  return request<Equipment>(`/equipments/${id}`)
}

/**
 * 创建设备
 * 对接后端：POST /api/store/equipments
 * @param data 设备信息
 * @returns 创建后的设备信息
 */
export async function createEquipment(data: EquipmentCreate): Promise<Equipment> {
  return request<Equipment>('/equipments', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新设备
 * 对接后端：PUT /api/store/equipments/{id}
 * @param data 设备信息
 * @returns 更新后的设备信息
 */
export async function updateEquipment(data: EquipmentUpdate): Promise<Equipment> {
  return request<Equipment>(`/equipments/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除设备
 * 对接后端：DELETE /api/store/equipments/{id}
 * @param id 设备ID
 */
export async function deleteEquipment(id: number): Promise<void> {
  await request<unknown>(`/equipments/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除设备
 * 对接后端：POST /api/store/equipments/batch
 * @param ids 设备ID列表
 */
export async function deleteEquipments(ids: number[]): Promise<void> {
  await request<unknown>('/equipments/batch', {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

/**
 * 查询即将到期保养的设备列表
 * 对接后端：GET /api/store/equipments/upcoming-maintenance?days=7
 * @param days 未来天数，默认 7
 * @returns 按到期日期升序排列的设备列表
 */
export async function getUpcomingMaintenance(days: number = 7): Promise<Equipment[]> {
  return request<Equipment[]>(`/equipments/upcoming-maintenance?days=${days}`)
}

/**
 * 按服务项目查询可用设备列表
 * 对接后端：GET /api/store/equipments/available-by-service
 * 按服务项目 ServiceProductEquipment 关联的设备类型过滤；startTime/endTime 均传入时额外排除指定时段已冲突的设备
 * @param serviceProductId 服务项目子表ID（关联 ServiceProduct.Id，预约页语境）
 * @param masterId 商品主档ID（服务项目页语境，后端自动反查租户内 ServiceProduct）
 * @param startTime 预约开始时间（ISO 字符串）；可选，不传时仅按类型过滤、不排除冲突
 * @param endTime 预约结束时间（ISO 字符串）；可选，不传时仅按类型过滤、不排除冲突
 * @param excludeAppointmentId 需排除的预约ID（更新场景，避免与自身冲突）
 * @returns 可用设备列表；服务项目未关联设备类型时返回空列表
 */
export async function getAvailableEquipmentsByService(
  serviceProductId?: number,
  masterId?: number,
  startTime?: string,
  endTime?: string,
  excludeAppointmentId?: number
): Promise<Equipment[]> {
  const qs = buildQuery({
    serviceProductId,
    masterId,
    startTime,
    endTime,
    excludeAppointmentId
  })
  return request<Equipment[]>(`/equipments/available-by-service${qs}`)
}

// ==================== 设备保养记录 API ====================

/**
 * 获取保养记录分页列表
 * 对接后端：GET /api/store/equipment-maintenances
 * @param query 查询参数
 * @returns 分页保养记录列表
 */
export async function getMaintenanceList(query?: MaintenanceQuery): Promise<PagedResponse<EquipmentMaintenance>> {
  const qs = buildQuery({
    equipmentId: query?.equipmentId,
    maintenanceType: query?.maintenanceType,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<EquipmentMaintenance>>(`/equipment-maintenances${qs}`)
}

/**
 * 创建保养记录
 * 对接后端：POST /api/store/equipment-maintenances
 * @param data 保养记录信息
 * @returns 创建后的保养记录
 */
export async function createMaintenance(data: MaintenanceCreate): Promise<EquipmentMaintenance> {
  return request<EquipmentMaintenance>('/equipment-maintenances', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新保养记录
 * 对接后端：PUT /api/store/equipment-maintenances/{id}
 * @param data 保养记录信息
 * @returns 更新后的保养记录
 */
export async function updateMaintenance(data: MaintenanceUpdate): Promise<EquipmentMaintenance> {
  return request<EquipmentMaintenance>(`/equipment-maintenances/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除保养记录
 * 对接后端：DELETE /api/store/equipment-maintenances/{id}
 * @param id 保养记录ID
 */
export async function deleteMaintenance(id: number): Promise<void> {
  await request<unknown>(`/equipment-maintenances/${id}`, {
    method: 'DELETE'
  })
}
