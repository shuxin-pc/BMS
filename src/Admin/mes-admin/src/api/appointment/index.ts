// 预约管理 - API 服务
// 对接后端 AppointmentsController / RoomsController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Appointment,
  AppointmentQuery,
  AppointmentCreate,
  AppointmentUpdate,
  ApiResponse,
  TechnicianSource,
  RoomOption
} from './types'

// 导出类型供外部使用
export type {
  Appointment,
  AppointmentQuery,
  AppointmentCreate,
  AppointmentUpdate,
  ApiResponse,
  PagedResponse,
  TechnicianSource,
  RoomOption
}

// ==================== 预约管理 ====================

/**
 * 获取预约分页列表
 * 对接后端：GET /api/store/appointments
 * @param query 查询参数
 * @returns 分页预约列表
 */
export async function getAppointments(query?: AppointmentQuery): Promise<PagedResponse<Appointment>> {
  const qs = buildQuery({
    customerId: query?.customerId,
    keyword: query?.keyword,
    appointmentNo: query?.appointmentNo,
    status: query?.status,
    // statuses 数组由 buildQuery 展开为多个同名参数（statuses=1&statuses=2），后端 List<int> 标准绑定
    statuses: query?.statuses,
    startTimeStart: query?.startTimeStart,
    startTimeEnd: query?.startTimeEnd,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Appointment>>(`/appointments${qs}`)
}

/**
 * 获取预约详情
 * 对接后端：GET /api/store/appointments/{id}
 * @param id 预约ID
 * @returns 预约详情
 */
export async function getAppointment(id: string): Promise<Appointment> {
  // 后端 LongToStringConverter 将 long 主键序列化为字符串，必须按字符串传递，避免 Number 精度丢失
  return request<Appointment>(`/appointments/${id}`)
}

/**
 * 创建预约
 * 对接后端：POST /api/store/appointments
 * 防冲突检测（技师时间冲突、房间时间冲突）由后端处理
 * @param data 预约信息
 * @returns 创建后的预约信息
 */
export async function createAppointment(data: AppointmentCreate): Promise<Appointment> {
  return request<Appointment>('/appointments', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 获取可用房间/床位列表
 * 对接后端：GET /api/store/rooms?status=1
 * @returns 启用状态的房间/床位列表
 */
export async function getRooms(): Promise<RoomOption[]> {
  const qs = buildQuery({ status: 1, pageIndex: 1, pageSize: 1000 })
  const result = await request<PagedResponse<RoomOption>>(`/rooms${qs}`)
  return result.list
}

/**
 * 根据服务项目获取可用房间/床位列表
 * 对接后端：GET /api/store/rooms/available-by-service
 * 按服务项目 ServiceProduct.RequiredRoomType 过滤，并排除指定时段已冲突的房间
 * @param serviceProductId 服务项目商品ID
 * @param startTime 预约开始时间（ISO 字符串）
 * @param endTime 预约结束时间（ISO 字符串）
 * @param excludeAppointmentId 需排除的预约ID（更新场景）
 * @returns 可用房间/床位列表
 */
export async function getAvailableRoomsByServiceProduct(
  serviceProductId: number,
  startTime: string,
  endTime: string,
  excludeAppointmentId?: number
): Promise<RoomOption[]> {
  const qs = buildQuery({
    serviceProductId,
    startTime,
    endTime,
    excludeAppointmentId
  })
  return await request<RoomOption[]>(`/rooms/available-by-service${qs}`)
}

/**
 * 更新预约（编辑 / 状态流转均走此接口，提交完整 DTO）
 * 对接后端：PUT /api/store/appointments/{id}
 * 后端 AppointmentAppService.UpdateAsync 校验状态流转与资源冲突（排除自身）
 * @param data 预约更新信息（含 id）
 * @returns 更新后的预约信息
 */
export async function updateAppointment(data: AppointmentUpdate): Promise<Appointment> {
  const { id, ...payload } = data
  return request<Appointment>(`/appointments/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })
}
