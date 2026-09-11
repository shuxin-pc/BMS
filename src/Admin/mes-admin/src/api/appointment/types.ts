// ==========================================
// 预约管理类型定义
// 字段与后端 AppointmentDto / RoomDto 对齐
// ==========================================

/**
 * 预约状态
 * - 1: 已预约（创建即已预约，由门店人员线下确认后录入）
 * - 2: 已到店
 * - 3: 已完成
 * - 4: 已取消（门店人员手动取消）
 * - 5: 爽约（超过预约时段未到店，系统自动更改）
 */
export type AppointmentStatus = number

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
 * - 1: 商家技师
 * - 2: 平台技师
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
  /** 预约开始时间（一体格式 ISO 字符串，含日期与时刻；服务跨日时结束时间落在次日） */
  startTime: string
  /** 结束时间（由后端根据 ServiceProduct.Duration 自动计算，跨日时日期可能为次日） */
  endTime?: string
  /** 预约状态：1-已预约，2-已到店，3-已完成，4-已取消，5-爽约 */
  status: AppointmentStatus
  /** 技师ID */
  technicianId?: number
  /** 技师名称（后端关联 Technician 表填充） */
  technicianName?: string
  /** 技师来源：1-商家技师，2-平台技师 */
  technicianSource?: TechnicianSource
  /** 房间/床位ID */
  roomId?: number
  /** 房间/床位名称（后端关联 Room 表填充） */
  roomName?: string
  /** 设备ID（某些服务项目需要特定设备） */
  equipmentId?: number
  /** 设备名称（后端关联 Equipment 表填充，用于列表/详情展示） */
  equipmentName?: string
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
  /** 客户名称或手机号关键字（模糊匹配，命中姓名或手机号其一即满足） */
  keyword?: string
  /** 预约编号（模糊匹配） */
  appointmentNo?: string
  /** 预约状态 */
  status?: AppointmentStatus
  /** 预约状态集合（IN 查询，逗号分隔传递，如 [1, 2] 表示已预约+已到店；与 status 叠加生效） */
  statuses?: number[]
  /** 预约开始日期起始（yyyy-MM-dd，按 StartTime 日期部分过滤） */
  startTimeStart?: string
  /** 预约开始日期截止（yyyy-MM-dd，按 StartTime 日期部分过滤） */
  startTimeEnd?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建预约请求（与后端 AppointmentCreateDto 对齐）
 * EndTime 由后端根据 ProductId 关联的 ServiceProduct.Duration 自动计算，前端无需传入
 * 预约号由后端 AppointmentNoGenerator 自动生成（AP{yyyyMMdd}{序号}），前端无需传入
 */
export interface AppointmentCreate {
  /** 客户ID */
  customerId?: number
  /** 客户名称 */
  customerName: string
  /** 客户手机号 */
  customerPhone: string
  /** 预约开始时间（一体格式，yyyy-MM-ddTHH:mm:ss） */
  startTime: string
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
 * 更新预约请求（与后端 AppointmentUpdateDto 对齐 = AppointmentCreateDto + Id）
 * 后端 UpdateAsync 会校验状态流转，且会把 ConfirmTime/ArrivalTime/CompleteTime 原样写回实体，
 * 因此更新时必须原样回传这三个时间戳字段，否则会被清空
 */
export interface AppointmentUpdate extends AppointmentCreate {
  /** 预约ID */
  id: number
  /** 确认时间（原样回传，避免被后端清空） */
  confirmTime?: string | null
  /** 到店时间（原样回传，避免被后端清空） */
  arrivalTime?: string | null
  /** 完成时间（原样回传，避免被后端清空） */
  completeTime?: string | null
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
