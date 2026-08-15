// 资源可用性 API 服务
// 对接后端 ResourcesController（路由 /api/store/resources）
import { request, buildQuery } from '../shared/storeRequest'
import type { ResourceAvailabilityDto, TechnicianAvailabilityItem, RoomAvailabilityItem, EquipmentAvailabilityItem } from './types'

export type { ResourceAvailabilityDto, TechnicianAvailabilityItem, RoomAvailabilityItem, EquipmentAvailabilityItem }

/**
 * 查询指定时段内技师/房间/设备的可用性
 * 用于前端列表标红 + 提交校验
 * @param startTime 占用开始时间（含）
 * @param endTime 占用结束时间（不含）
 * @param storeId 门店ID（可空，缺省时取当前用户门店）
 */
export async function getResourceAvailability(
  startTime: string,
  endTime: string,
  storeId?: number
): Promise<ResourceAvailabilityDto> {
  const qs = buildQuery({
    startTime,
    endTime,
    storeId
  })
  return request<ResourceAvailabilityDto>(`/resources/availability${qs}`)
}
