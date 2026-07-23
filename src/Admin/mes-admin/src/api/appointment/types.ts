// ==========================================
// 预约管理类型定义
// 字段与后端 AppointmentDto / RoomDto 对齐
// ==========================================

/**
 * 预约状态
 * - 1: 待确认
 * - 2: 已预约
 * - 3: 已到店
 * - 4: 已完成
 * - 5: 已取消（门店人员手动取消）
 * - 6: 爽约（超过预约时段未到店，系统自动更改）
 */
export type AppointmentStatus = number

/**
 * 提醒状态
 * - 1: 待提醒
 * - 2: 已提醒
 */
export type RemindStatus = number

/**
 * 客户确认状态
 * - 1: 待确认
 * - 2: 已确认
 * - 3: 需改期
 */
export type CustomerConfirmStatus = number

/**
 * 视图类型
 * - day: 日视图
 * - week: 周视图
 */
export type CalendarViewType = 'day' | 'week'

/**
 * 查看维度
 * - technician: 按技师
 * - room: 按房间
 */
export type CalendarDimension = 'technician' | 'room'

/**
 * 技师来源
 * - 1: 平台技师
 * - 2: 商家技师
 */
export type TechnicianSource = 1 | 2

/**
 * 预约信息（与后端 AppointmentDto 对齐）
 */
export interface Appointment {
  /** 预约ID */
  id: number
  /** 预约编号 */
  appointmentNo: string
  /** 客户ID */
  customerId: number
  /** 客户名称 */
  customerName: string
  /** 客户手机号 */
  customerPhone: string
  /** 预约日期（ISO 字符串） */
  appointmentDate: string
  /** 预约时间（TimeSpan 序列化为字符串） */
  appointmentTime: string
  /** 结束时间（由后端根据 ServiceProduct.Duration 自动计算） */
  endTime?: string
  /** 预约状态：1-待确认，2-已预约，3-已到店，4-已完成，5-已取消，6-爽约 */
  status: AppointmentStatus
  /** 技师ID */
  technicianId?: number
  /** 技师来源：1-平台技师，2-商家技师 */
  technicianSource?: TechnicianSource
  /** 房间/床位ID */
  roomId?: number
  /** 设备ID（某些服务项目需要特定设备） */
  equipmentId?: number
  /** 服务项目商品ID（关联 Product 主表，type=2 服务项目） */
  productId: number
  /** 服务项目商品名称（后端 Join Product 表填充，替代原 serviceItem 字符串字段） */
  productName?: string
  /** 备注 */
  remark?: string
  /** 确认时间 */
  confirmTime?: string
  /** 到店时间 */
  arrivalTime?: string
  /** 完成时间 */
  completeTime?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 预约查询参数（与后端 AppointmentQueryDto 对齐）
 */
export interface AppointmentQuery {
  /** 客户ID */
  customerId?: number
  /** 预约状态 */
  status?: AppointmentStatus
  /** 预约日期起始（yyyy-MM-dd） */
  appointmentDateStart?: string
  /** 预约日期截止（yyyy-MM-dd） */
  appointmentDateEnd?: string
  /** 技师来源筛选 */
  technicianSource?: TechnicianSource
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建预约请求（与后端 AppointmentCreateDto 对齐）
 * EndTime 由后端根据 ProductId 关联的 ServiceProduct.Duration 自动计算，前端无需传入
 */
export interface AppointmentCreate {
  /** 预约编号 */
  appointmentNo?: string
  /** 客户ID */
  customerId?: number
  /** 客户名称 */
  customerName: string
  /** 客户手机号 */
  customerPhone: string
  /** 预约日期（yyyy-MM-dd） */
  appointmentDate: string
  /** 预约时间（HH:mm） */
  appointmentTime: string
  /** 服务项目商品ID（必填，关联 Product 主表 type=2 服务项目） */
  productId: number
  /** 预约状态 */
  status?: number
  /** 技师ID */
  technicianId?: number
  /** 房间/床位ID */
  roomId?: number
  /** 设备ID（某些服务项目需要特定设备） */
  equipmentId?: number
  /** 备注 */
  remark?: string
}

/**
 * 更新预约状态请求（前端专用，用于 updateAppointmentStatus 函数）
 */
export interface AppointmentStatusUpdate {
  id: number
  status: AppointmentStatus
}

/**
 * 房间/床位选项（与后端 RoomDto 对齐）
 */
export interface RoomOption {
  /** 房间/床位ID */
  id: number
  /** 房间/床位名称 */
  name: string
  /** 房间编码 */
  code: string
  /** 类型：1-房间，2-床位 */
  roomType: number
  /** 状态：0-禁用，1-启用 */
  status: number
  /** 位置 */
  location?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 明日提醒预约信息
 * 对齐后端 TomorrowReminderDto（GET /appointments/tomorrowReminders）
 */
export interface TomorrowReminder {
  /** 提醒ID */
  id: number
  /** 预约编号 */
  appointmentNo: string
  /** 客户名称 */
  customerName: string
  /** 手机号 */
  phone: string
  /** 服务项目 */
  serviceName: string
  /** 技师名称 */
  technicianName?: string
  /** 预约时间（yyyy-MM-dd HH:mm） */
  appointmentTime: string
  /** 提醒状态：1-待提醒，2-已提醒 */
  remindStatus: RemindStatus
  /** 提醒渠道：sms-短信，wechat-微信（未提醒时为空） */
  remindChannel?: 'sms' | 'wechat'
  /** 客户确认状态：1-待确认，2-已确认，3-需改期 */
  customerConfirmStatus: CustomerConfirmStatus
  /** 备注 */
  remark?: string
}

/**
 * 明日提醒查询参数
 * 对齐后端 TomorrowReminderQueryDto
 */
export interface TomorrowReminderQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 手机号（模糊匹配） */
  phone?: string
  /** 提醒状态筛选 */
  remindStatus?: RemindStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
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

/**
 * 通用API响应
 */
export interface ApiResponse<T> {
  code: number
  message?: string
  data: T
}
