// 客户档案管理 - API服务
import type {
  Customer,
  CustomerQuery,
  CustomerCreate,
  CustomerUpdate,
  CustomerLevel,
  CustomerLevelCreate,
  CustomerLevelUpdate,
  PointsRule,
  PointsRecord,
  PointsRecordQuery,
  ConsumeRecord,
  ConsumeRecordQuery,
  BirthdayReminder,
  BirthdayReminderQuery,
  ConsumeThankRecord,
  ConsumeThankQuery,
  CustomerConsumptionStat,
  ApiResponse,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  Customer,
  CustomerQuery,
  CustomerCreate,
  CustomerUpdate,
  CustomerLevel,
  CustomerLevelCreate,
  CustomerLevelUpdate,
  PointsRule,
  PointsRecord,
  PointsRecordQuery,
  PointsChangeType,
  ConsumeRecord,
  ConsumeRecordQuery,
  BirthdayReminder,
  BirthdayReminderQuery,
  ConsumeThankRecord,
  ConsumeThankQuery,
  CustomerConsumptionStat,
  ApiResponse,
  PagedResponse
}

// 通过网关访问后端服务
const API_BASE = '/api/customer'

// 获取token
const getToken = () => localStorage.getItem('token')

// 通用请求方法
async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>)
  }

  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(url, {
    ...options,
    headers: headers as HeadersInit
  })

  let errorMessage = ''
  try {
    const result: ApiResponse<T> = await response.json()
    if (result.message) {
      errorMessage = result.message
    }
    if (result.code != 200) {
      throw new Error(errorMessage || '请求失败')
    }
    return result.data
  } catch (err: any) {
    if (errorMessage) {
      throw new Error(errorMessage)
    }
    throw new Error(`请求失败: ${response.status}`)
  }
}

// ==================== 客户管理 ====================

/**
 * 获取客户分页列表
 * @param query 查询参数
 * @returns 分页客户列表
 */
export async function getCustomers(query?: CustomerQuery): Promise<PagedResponse<Customer>> {
  const params = new URLSearchParams()
  if (query?.name) params.append('name', query.name)
  if (query?.phone) params.append('phone', query.phone)
  if (query?.levelId !== undefined) params.append('levelId', String(query.levelId))
  if (query?.gender !== undefined) params.append('gender', String(query.gender))
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<Customer>>(`${API_BASE}/customers?${params}`)
}

/**
 * 获取客户详情
 * @param id 客户ID
 * @returns 客户详情
 */
export async function getCustomer(id: number): Promise<Customer> {
  return request<Customer>(`${API_BASE}/customers/${id}`)
}

/**
 * 创建客户
 * @param data 客户信息
 * @returns 创建后的客户信息
 */
export async function createCustomer(data: CustomerCreate): Promise<Customer> {
  return request<Customer>(`${API_BASE}/customers`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新客户
 * @param data 客户信息
 * @returns 更新后的客户信息
 */
export async function updateCustomer(data: CustomerUpdate): Promise<Customer> {
  return request<Customer>(`${API_BASE}/customers/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除客户
 * @param id 客户ID
 */
export async function deleteCustomer(id: number): Promise<void> {
  return request<void>(`${API_BASE}/customers/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除客户
 * @param ids 客户ID列表
 */
export async function deleteCustomers(ids: number[]): Promise<void> {
  return request<void>(`${API_BASE}/customers/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

// ==================== 客户等级 ====================

/**
 * 获取客户等级列表
 * @returns 等级列表
 */
export async function getCustomerLevels(): Promise<CustomerLevel[]> {
  return request<CustomerLevel[]>(`${API_BASE}/levels`)
}

// ==================== 客户等级（Mock 增删改） ====================

// Mock 客户等级数据
const mockCustomerLevels: CustomerLevel[] = [
  { id: 1, name: '普通会员', code: 'NORMAL', discountRate: 1.0, sort: 1, remark: '默认等级' },
  { id: 2, name: '银卡会员', code: 'SILVER', discountRate: 0.95, sort: 2, remark: '银卡等级' },
  { id: 3, name: '金卡会员', code: 'GOLD', discountRate: 0.9, sort: 3, remark: '金卡等级' },
  { id: 4, name: '钻石会员', code: 'DIAMOND', discountRate: 0.85, sort: 4, remark: '钻石等级' }
]

let mockLevelIdCounter = 100

/**
 * 获取客户等级列表（Mock 版本，含分页可选）
 * @returns 等级列表
 */
export async function getCustomerLevelsMock(): Promise<CustomerLevel[]> {
  await new Promise(resolve => setTimeout(resolve, 300))
  return [...mockCustomerLevels].sort((a, b) => a.sort - b.sort)
}

/**
 * 创建客户等级（Mock）
 * @param data 等级信息
 * @returns 创建后的等级
 */
export async function createCustomerLevel(data: CustomerLevelCreate): Promise<CustomerLevel> {
  await new Promise(resolve => setTimeout(resolve, 300))
  if (mockCustomerLevels.some(item => item.code === data.code)) {
    throw new Error('等级编码已存在')
  }
  const newLevel: CustomerLevel = {
    id: ++mockLevelIdCounter,
    ...data
  }
  mockCustomerLevels.push(newLevel)
  return newLevel
}

/**
 * 更新客户等级（Mock）
 * @param data 等级信息
 * @returns 更新后的等级
 */
export async function updateCustomerLevel(data: CustomerLevelUpdate): Promise<CustomerLevel> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const index = mockCustomerLevels.findIndex(item => item.id === data.id)
  if (index === -1) throw new Error('等级不存在')
  if (mockCustomerLevels.some(item => item.code === data.code && item.id !== data.id)) {
    throw new Error('等级编码已存在')
  }
  mockCustomerLevels[index] = { ...data }
  return mockCustomerLevels[index]
}

/**
 * 删除客户等级（Mock）
 * @param id 等级ID
 */
export async function deleteCustomerLevel(id: number): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const index = mockCustomerLevels.findIndex(item => item.id === id)
  if (index === -1) throw new Error('等级不存在')
  mockCustomerLevels.splice(index, 1)
}

// ==================== 积分管理（Mock） ====================

// Mock 积分规则
const mockPointsRule: PointsRule = {
  id: 1,
  name: '默认积分规则',
  pointsPerYuan: 1,
  pointsToYuan: 0.01,
  birthdayDouble: true,
  minPointsThreshold: 0,
  status: 1,
  remark: '消费1元获得1积分，100积分抵扣1元',
  updatedAt: '2026-07-01 10:00:00'
}

// Mock 积分流水
const mockPointsRecords: PointsRecord[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张小美',
    phone: '138****8888',
    changePoints: 196,
    beforePoints: 324,
    afterPoints: 520,
    changeType: 1,
    changeTime: '2026-07-10 14:31:00',
    orderNo: 'OD20260710001',
    remark: '消费获取积分'
  },
  {
    id: 2,
    customerId: 1002,
    customerName: '李晓红',
    phone: '139****6666',
    changePoints: 398,
    beforePoints: 626,
    afterPoints: 1024,
    changeType: 1,
    changeTime: '2026-07-10 15:21:00',
    orderNo: 'OD20260710002'
  },
  {
    id: 3,
    customerId: 1003,
    customerName: '王丽华',
    phone: '137****1234',
    changePoints: -50,
    beforePoints: 500,
    afterPoints: 450,
    changeType: 2,
    changeTime: '2026-07-09 16:00:00',
    remark: '兑换礼品'
  },
  {
    id: 4,
    customerId: 1004,
    customerName: '陈芳',
    phone: '135****5678',
    changePoints: 100,
    beforePoints: 200,
    afterPoints: 300,
    changeType: 3,
    changeTime: '2026-07-08 09:30:00',
    remark: '周年庆活动赠送'
  },
  {
    id: 5,
    customerId: 1005,
    customerName: '赵敏',
    phone: '136****9999',
    changePoints: -500,
    beforePoints: 500,
    afterPoints: 0,
    changeType: 4,
    changeTime: '2026-07-07 18:00:00',
    orderNo: 'OD20260707005',
    remark: '订单退款扣减'
  }
]

// 客户积分缓存（基于 Mock 数据推导，用于积分变动计算）
const mockCustomerPointsCache: Map<number, { name: string; phone: string; points: number }> = new Map([
  [1001, { name: '张小美', phone: '138****8888', points: 520 }],
  [1002, { name: '李晓红', phone: '139****6666', points: 1024 }],
  [1003, { name: '王丽华', phone: '137****1234', points: 450 }],
  [1004, { name: '陈芳', phone: '135****5678', points: 300 }],
  [1005, { name: '赵敏', phone: '136****9999', points: 0 }]
])

// 积分流水 ID 自增计数器
let mockPointsRecordIdCounter = 100

/**
 * 获取积分规则配置（Mock）
 * @returns 积分规则
 */
export async function getPointsRule(): Promise<PointsRule> {
  await new Promise(resolve => setTimeout(resolve, 300))
  return { ...mockPointsRule }
}

/**
 * 保存积分规则配置（Mock）
 * @param data 积分规则
 * @returns 更新后的规则
 */
export async function savePointsRule(data: PointsRule): Promise<PointsRule> {
  await new Promise(resolve => setTimeout(resolve, 300))
  mockPointsRule.name = data.name
  mockPointsRule.pointsPerYuan = data.pointsPerYuan
  mockPointsRule.pointsToYuan = data.pointsToYuan
  mockPointsRule.birthdayDouble = data.birthdayDouble
  mockPointsRule.minPointsThreshold = data.minPointsThreshold
  mockPointsRule.status = data.status
  mockPointsRule.remark = data.remark
  mockPointsRule.updatedAt = new Date().toISOString().replace('T', ' ').substring(0, 19)
  return { ...mockPointsRule }
}

/**
 * 获取积分流水分页列表（Mock）
 * @param query 查询参数
 * @returns 分页积分流水
 */
export async function getPointsRecords(query?: PointsRecordQuery): Promise<PagedResponse<PointsRecord>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockPointsRecords]
  if (query?.customerName) list = list.filter(item => item.customerName.includes(query.customerName!))
  if (query?.changeType !== undefined) list = list.filter(item => item.changeType === query.changeType)
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 获取可兑换积分的客户列表（Mock）
 * @returns 客户列表（含当前积分）
 */
export async function getCustomersForPointsExchange(): Promise<Array<{
  customerId: number
  customerName: string
  phone: string
  currentPoints: number
}>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  return Array.from(mockCustomerPointsCache.entries()).map(([customerId, info]) => ({
    customerId,
    customerName: info.name,
    phone: info.phone,
    currentPoints: info.points
  }))
}

/**
 * 消费获取积分（Mock）
 * 按 PointsRule 的 pointsPerYuan 计算获得积分，低于门槛不获取
 * @param customerId 客户ID
 * @param orderAmount 消费金额（元）
 * @param orderId 关联订单号
 * @returns 生成的积分流水记录
 */
export async function earnPoints(
  customerId: number,
  orderAmount: number,
  orderId: string
): Promise<PointsRecord> {
  await new Promise(resolve => setTimeout(resolve, 300))

  const customer = mockCustomerPointsCache.get(customerId)
  if (!customer) throw new Error('客户不存在')

  const rule = { ...mockPointsRule }
  const earnedPoints = Math.floor(orderAmount * rule.pointsPerYuan)
  if (earnedPoints < rule.minPointsThreshold) {
    throw new Error(`消费积分未达门槛（最低 ${rule.minPointsThreshold} 积分）`)
  }

  const beforePoints = customer.points
  const afterPoints = beforePoints + earnedPoints

  const record: PointsRecord = {
    id: ++mockPointsRecordIdCounter,
    customerId,
    customerName: customer.name,
    phone: customer.phone,
    changePoints: earnedPoints,
    beforePoints,
    afterPoints,
    changeType: 1,
    changeTime: new Date().toISOString().replace('T', ' ').substring(0, 19),
    orderNo: orderId,
    remark: '消费获取积分'
  }

  mockPointsRecords.push(record)
  customer.points = afterPoints
  return record
}

/**
 * 积分兑换扣减（Mock）
 * 兑换时扣减积分，积分不足时抛出错误（积分永不为负）
 * @param customerId 客户ID
 * @param pointsToExchange 兑换所需积分
 * @param remark 备注
 * @returns 生成的积分流水记录
 */
export async function exchangePoints(
  customerId: number,
  pointsToExchange: number,
  remark?: string
): Promise<PointsRecord> {
  await new Promise(resolve => setTimeout(resolve, 300))

  const customer = mockCustomerPointsCache.get(customerId)
  if (!customer) throw new Error('客户不存在')

  const beforePoints = customer.points
  const afterPoints = beforePoints - pointsToExchange
  // 兑换扣减：积分不足时拒绝操作，确保积分不为负
  if (afterPoints < 0) {
    throw new Error(`积分不足，当前积分：${beforePoints}`)
  }

  const record: PointsRecord = {
    id: ++mockPointsRecordIdCounter,
    customerId,
    customerName: customer.name,
    phone: customer.phone,
    changePoints: -pointsToExchange,
    beforePoints,
    afterPoints,
    changeType: 2,
    changeTime: new Date().toISOString().replace('T', ' ').substring(0, 19),
    remark: remark || '积分兑换'
  }

  mockPointsRecords.push(record)
  customer.points = afterPoints
  return record
}

/**
 * 退款扣减积分（Mock）
 * 退款时扣减原消费获得的积分，积分最多扣到0（积分永不为负）
 * @param customerId 客户ID
 * @param orderId 关联订单号
 * @param pointsToDeduct 应扣减积分
 * @returns 生成的积分流水记录
 */
export async function refundPoints(
  customerId: number,
  orderId: string,
  pointsToDeduct: number
): Promise<PointsRecord> {
  await new Promise(resolve => setTimeout(resolve, 300))

  const customer = mockCustomerPointsCache.get(customerId)
  if (!customer) throw new Error('客户不存在')

  const beforePoints = customer.points
  // 退款扣减最多扣到0，不产生负数
  const actualDeduct = Math.min(pointsToDeduct, beforePoints)
  const afterPoints = beforePoints - actualDeduct

  const record: PointsRecord = {
    id: ++mockPointsRecordIdCounter,
    customerId,
    customerName: customer.name,
    phone: customer.phone,
    changePoints: -actualDeduct,
    beforePoints,
    afterPoints,
    changeType: 4,
    changeTime: new Date().toISOString().replace('T', ' ').substring(0, 19),
    orderNo: orderId,
    remark: `订单退款扣减积分（应扣${pointsToDeduct}，实扣${actualDeduct}）`
  }

  mockPointsRecords.push(record)
  customer.points = afterPoints
  return record
}

// ==================== 消费记录（Mock） ====================

const mockConsumeRecords: ConsumeRecord[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张小美',
    phone: '138****8888',
    orderNo: 'OD20260710001',
    amount: 196,
    projectName: '美甲服务-法式美甲、手部护理',
    paymentMethod: 2,
    consumeTime: '2026-07-10 14:30:00',
    storeName: '总店'
  },
  {
    id: 2,
    customerId: 1002,
    customerName: '李晓红',
    phone: '139****6666',
    orderNo: 'OD20260710002',
    amount: 398,
    projectName: '洗护套餐 x2',
    paymentMethod: 3,
    consumeTime: '2026-07-10 15:20:00',
    storeName: '总店'
  },
  {
    id: 3,
    customerId: 1003,
    customerName: '王丽华',
    phone: '137****1234',
    orderNo: 'OD20260709003',
    amount: 280,
    projectName: '面部护理-深层清洁',
    paymentMethod: 4,
    consumeTime: '2026-07-09 10:15:00',
    storeName: '城南分店'
  },
  {
    id: 4,
    customerId: 1004,
    customerName: '陈芳',
    phone: '135****5678',
    orderNo: 'OD20260708004',
    amount: 158,
    projectName: '美睫服务-自然款',
    paymentMethod: 1,
    consumeTime: '2026-07-08 16:45:00',
    storeName: '城南分店'
  },
  {
    id: 5,
    customerId: 1005,
    customerName: '赵敏',
    phone: '136****9999',
    orderNo: 'OD20260707005',
    amount: 500,
    projectName: '全身护理套餐',
    paymentMethod: 5,
    consumeTime: '2026-07-07 11:00:00',
    storeName: '总店'
  }
]

/**
 * 获取消费记录分页列表（Mock）
 * @param query 查询参数
 * @returns 分页消费记录
 */
export async function getConsumeRecords(query?: ConsumeRecordQuery): Promise<PagedResponse<ConsumeRecord>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockConsumeRecords]
  if (query?.customerName) list = list.filter(item => item.customerName.includes(query.customerName!))
  if (query?.phone) list = list.filter(item => item.phone.includes(query.phone!))
  if (query?.startDate) list = list.filter(item => item.consumeTime >= query.startDate!)
  if (query?.endDate) list = list.filter(item => item.consumeTime <= query.endDate! + ' 23:59:59')
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

// ==================== 客户关怀（Mock） ====================

const mockBirthdayReminders: BirthdayReminder[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张小美',
    phone: '138****8888',
    birthday: '07-14',
    daysToBirthday: 2,
    careStatus: 1
  },
  {
    id: 2,
    customerId: 1002,
    customerName: '李晓红',
    phone: '139****6666',
    birthday: '07-15',
    daysToBirthday: 3,
    careStatus: 1
  },
  {
    id: 3,
    customerId: 1003,
    customerName: '王丽华',
    phone: '137****1234',
    birthday: '07-10',
    daysToBirthday: 0,
    careStatus: 2,
    careTime: '2026-07-10 09:00:00'
  },
  {
    id: 4,
    customerId: 1004,
    customerName: '陈芳',
    phone: '135****5678',
    birthday: '07-20',
    daysToBirthday: 8,
    careStatus: 1
  },
  {
    id: 5,
    customerId: 1005,
    customerName: '赵敏',
    phone: '136****9999',
    birthday: '07-05',
    daysToBirthday: -7,
    careStatus: 2,
    careTime: '2026-07-05 10:30:00'
  }
]

const mockConsumeThanks: ConsumeThankRecord[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张小美',
    phone: '138****8888',
    lastAmount: 196,
    lastConsumeTime: '2026-07-10 14:30:00',
    thankStatus: 1
  },
  {
    id: 2,
    customerId: 1002,
    customerName: '李晓红',
    phone: '139****6666',
    lastAmount: 398,
    lastConsumeTime: '2026-07-10 15:20:00',
    thankStatus: 1
  },
  {
    id: 3,
    customerId: 1003,
    customerName: '王丽华',
    phone: '137****1234',
    lastAmount: 280,
    lastConsumeTime: '2026-07-09 10:15:00',
    thankStatus: 2,
    thankMethod: 2,
    thankTime: '2026-07-09 12:00:00'
  },
  {
    id: 4,
    customerId: 1004,
    customerName: '陈芳',
    phone: '135****5678',
    lastAmount: 158,
    lastConsumeTime: '2026-07-08 16:45:00',
    thankStatus: 2,
    thankMethod: 1,
    thankTime: '2026-07-08 18:00:00'
  },
  {
    id: 5,
    customerId: 1005,
    customerName: '赵敏',
    phone: '136****9999',
    lastAmount: 500,
    lastConsumeTime: '2026-07-07 11:00:00',
    thankStatus: 1
  }
]

/**
 * 获取生日提醒分页列表（Mock）
 * @param query 查询参数
 * @returns 分页生日提醒
 */
export async function getBirthdayReminders(query?: BirthdayReminderQuery): Promise<PagedResponse<BirthdayReminder>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockBirthdayReminders]
  if (query?.customerName) list = list.filter(item => item.customerName.includes(query.customerName!))
  if (query?.careStatus !== undefined) list = list.filter(item => item.careStatus === query.careStatus)
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 标记生日关怀完成（Mock）
 * @param id 记录ID
 */
export async function markBirthdayCared(id: number): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const item = mockBirthdayReminders.find(r => r.id === id)
  if (!item) throw new Error('记录不存在')
  item.careStatus = 2
  item.careTime = new Date().toISOString().replace('T', ' ').substring(0, 19)
}

/**
 * 获取消费感谢分页列表（Mock）
 * @param query 查询参数
 * @returns 分页消费感谢
 */
export async function getConsumeThanks(query?: ConsumeThankQuery): Promise<PagedResponse<ConsumeThankRecord>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockConsumeThanks]
  if (query?.customerName) list = list.filter(item => item.customerName.includes(query.customerName!))
  if (query?.thankStatus !== undefined) list = list.filter(item => item.thankStatus === query.thankStatus)
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 标记消费感谢完成（Mock）
 * @param id 记录ID
 * @param method 感谢方式
 */
export async function markConsumeThanked(id: number, method: number): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const item = mockConsumeThanks.find(r => r.id === id)
  if (!item) throw new Error('记录不存在')
  item.thankStatus = 2
  item.thankMethod = method
  item.thankTime = new Date().toISOString().replace('T', ' ').substring(0, 19)
}

// ==================== 客户消费统计 ====================

/**
 * 获取客户消费统计（消费频次、客单价、消费偏好）
 * @param customerId 客户ID
 * @returns 消费统计数据
 */
export async function getCustomerConsumptionStat(customerId: number): Promise<CustomerConsumptionStat> {
  return request<CustomerConsumptionStat>(`${API_BASE}/customers/${customerId}/consumption-stat`)
}
