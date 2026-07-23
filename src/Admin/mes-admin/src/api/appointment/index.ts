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
  AppointmentStatusUpdate,
  TomorrowReminder,
  TomorrowReminderQuery,
  ApiResponse,
  TechnicianSource,
  RoomOption
} from './types'

// 导出类型供外部使用
export type {
  Appointment,
  AppointmentQuery,
  AppointmentCreate,
  AppointmentStatusUpdate,
  TomorrowReminder,
  TomorrowReminderQuery,
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
    status: query?.status,
    appointmentDateStart: query?.appointmentDateStart,
    appointmentDateEnd: query?.appointmentDateEnd,
    technicianSource: query?.technicianSource,
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
export async function getAppointment(id: number): Promise<Appointment> {
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
 * 更新预约状态
 * 对接后端：PUT /api/store/appointments/{id}
 * @param data 状态更新信息
 */
export async function updateAppointmentStatus(data: AppointmentStatusUpdate): Promise<void> {
  await request<unknown>(`/appointments/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify({ status: data.status })
  })
}

// ==================== 明日提醒 ====================

/**
 * 获取明日提醒分页列表
 * 对接后端：GET /api/store/appointments/tomorrowReminders
 * @param query 查询参数
 * @returns 分页明日提醒列表
 */
export async function getTomorrowReminders(query?: TomorrowReminderQuery): Promise<PagedResponse<TomorrowReminder>> {
  const qs = buildQuery({
    customerName: query?.customerName,
    phone: query?.phone,
    remindStatus: query?.remindStatus,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TomorrowReminder>>(`/appointments/tomorrowReminders${qs}`)
}

/**
 * 发送提醒（短信/微信）
 * 对接后端：POST /api/store/appointments/{id}/reminder
 * 注意：后端仅记录提醒状态，不区分渠道；channel 参数保留供前端 UI 使用
 * @param id 预约ID
 * @param channel 提醒渠道（sms/wechat，前端 UI 用，不传后端）
 */
export async function sendReminder(id: number, _channel: 'sms' | 'wechat'): Promise<void> {
  await request<unknown>(`/appointments/${id}/reminder`, { method: 'POST' })
}

/**
 * 确认预约
 * 对接后端：PUT /api/store/appointments/{id}/confirm
 * @param id 预约ID
 */
export async function confirmTomorrowAppointment(id: number): Promise<void> {
  await request<unknown>(`/appointments/${id}/confirm`, { method: 'PUT' })
}
