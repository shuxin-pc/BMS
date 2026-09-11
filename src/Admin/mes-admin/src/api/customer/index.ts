// 客户档案管理 - API服务
// 真实后端调用统一走 shared/storeRequest（路径前缀 /api/store，自动注入 X-Store-Id）
import type {
  Customer,
  CustomerQuery,
  CustomerCreate,
  CustomerUpdate,
  CustomerPermanentDeleteDto,
  CustomerLevel,
  CustomerLevelCreate,
  CustomerLevelUpdate,
  CustomerTag,
  CustomerTagCreate,
  CustomerTagUpdate,
  CustomerTagQuery,
  PointsRule,
  PointsRecord,
  PointsRecordQuery,
  PointsChangeType,
  PointsLogCreate,
  ConsumeRecord,
  ConsumeRecordQuery,
  BirthdayReminder,
  BirthdayReminderQuery,
  ConsumeThankRecord,
  ConsumeThankQuery,
  CustomerConsumptionStat
} from './types'
import { request, buildQuery, type ApiResponse, type PagedResponse } from '../shared/storeRequest'
import { toLocalDateTime } from '@/utils/time'

// 导出类型供外部使用
export type {
  Customer,
  CustomerQuery,
  CustomerCreate,
  CustomerUpdate,
  CustomerPermanentDeleteDto,
  CustomerLevel,
  CustomerLevelCreate,
  CustomerLevelUpdate,
  CustomerTag,
  CustomerTagCreate,
  CustomerTagUpdate,
  CustomerTagQuery,
  PointsRule,
  PointsRecord,
  PointsRecordQuery,
  PointsChangeType,
  PointsLogCreate,
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

// ==================== 客户管理 ====================

/**
 * 获取客户分页列表
 * @param query 查询参数
 * @returns 分页客户列表
 */
export async function getCustomers(query?: CustomerQuery): Promise<PagedResponse<Customer>> {
  const params = buildQuery({
    name: query?.name,
    phone: query?.phone,
    keyword: query?.keyword,
    levelId: query?.levelId,
    tagId: query?.tagId,
    gender: query?.gender,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  return request<PagedResponse<Customer>>(`/customers${params}`)
}

/**
 * 获取客户详情
 * @param id 客户ID
 * @returns 客户详情
 */
export async function getCustomer(id: number): Promise<Customer> {
  return request<Customer>(`/customers/${id}`)
}

/**
 * 创建客户
 * @param data 客户信息
 * @returns 创建后的客户信息
 */
export async function createCustomer(data: CustomerCreate): Promise<Customer> {
  return request<Customer>(`/customers`, {
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
  return request<Customer>(`/customers/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除客户
 * @param id 客户ID
 */
export async function deleteCustomer(id: number): Promise<void> {
  return request<void>(`/customers/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除客户
 * @param ids 客户ID列表
 */
export async function deleteCustomers(ids: number[]): Promise<void> {
  return request<void>(`/customers/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

/**
 * 永久删除客户档案（物理删除）
 * 物理删除客户及关联个人信息（含美容档案、身体数据、对比照片等），订单脱敏保留
 * 前置条件：无未完成订单、无未核销项目卡、无储值余额
 * 需二次确认（客户手机号后4位）
 * 依据：《个人信息保护法》第 47 条
 * @param id 客户ID
 * @param dto 二次确认码 + 删除原因
 */
export async function permanentlyDeleteCustomer(
  id: number,
  dto: CustomerPermanentDeleteDto
): Promise<void> {
  return request<void>(`/customers/${id}/permanent`, {
    method: 'DELETE',
    body: JSON.stringify(dto)
  })
}

// ==================== 客户等级 ====================

/**
 * 获取客户等级列表
 * 后端 CustomerLevelsController.GetList 为分页接口，此处拉取全量后返回 list
 * @returns 等级列表
 */
export async function getCustomerLevels(): Promise<CustomerLevel[]> {
  const result = await request<PagedResponse<CustomerLevel>>(`/customerLevels${buildQuery({ pageSize: 9999 })}`)
  return result.list
}

/**
 * 创建客户等级
 * 业务约束：系统仅支持普通会员(level=1)和会员(level=2)两个等级，同租户最多 2 条
 * @param data 等级信息
 * @returns 创建后的等级
 */
export async function createCustomerLevel(data: CustomerLevelCreate): Promise<CustomerLevel> {
  return request<CustomerLevel>(`/customerLevels`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新客户等级
 * 业务约束：等级值 level 创建后不可修改
 * @param data 等级信息
 * @returns 更新后的等级
 */
export async function updateCustomerLevel(data: CustomerLevelUpdate): Promise<CustomerLevel> {
  return request<CustomerLevel>(`/customerLevels/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除客户等级
 * 业务约束：默认等级(level=1)不可删除，有关联客户的等级不可删除
 * @param id 等级ID
 */
export async function deleteCustomerLevel(id: number): Promise<void> {
  return request<void>(`/customerLevels/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除客户等级
 * 业务约束：默认等级(level=1)不可删除，有关联客户的等级不可删除
 * @param ids 等级ID列表
 */
export async function batchDeleteCustomerLevels(ids: number[]): Promise<void> {
  return request<void>(`/customerLevels/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

// ==================== 客户标签 ====================

/**
 * 获取客户标签分页列表
 * @param query 查询参数
 * @returns 分页标签列表
 */
export async function getCustomerTags(query?: CustomerTagQuery): Promise<PagedResponse<CustomerTag>> {
  const params = buildQuery({
    name: query?.name,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  return request<PagedResponse<CustomerTag>>(`/customerTags${params}`)
}

/**
 * 获取全量客户标签列表（供客户弹窗下拉使用，不分页）
 * @returns 标签列表
 */
export async function getAllCustomerTags(): Promise<CustomerTag[]> {
  return request<CustomerTag[]>(`/customerTags/all`)
}

/**
 * 创建客户标签
 * @param data 标签信息
 * @returns 创建后的标签
 */
export async function createCustomerTag(data: CustomerTagCreate): Promise<CustomerTag> {
  return request<CustomerTag>(`/customerTags`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新客户标签
 * @param data 标签信息
 * @returns 更新后的标签
 */
export async function updateCustomerTag(data: CustomerTagUpdate): Promise<CustomerTag> {
  return request<CustomerTag>(`/customerTags/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除客户标签
 * 业务约束：有关联客户的标签不可删除
 * @param id 标签ID
 */
export async function deleteCustomerTag(id: number): Promise<void> {
  return request<void>(`/customerTags/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除客户标签
 * 业务约束：有关联客户的标签不可删除
 * @param ids 标签ID列表
 */
export async function batchDeleteCustomerTags(ids: number[]): Promise<void> {
  return request<void>(`/customerTags/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

// ==================== 积分管理 ====================

// Mock 积分规则（仅 earnPoints/refundPoints 等 Mock 函数依赖，真实页面走下方 getPointsRule/savePointsRule）
const mockPointsRule: PointsRule = {
  id: 1,
  pointsRate: 1,
  deductRate: 0.01,
  maxDeductAmount: 0,
  pointsValidityDays: null,
  birthdayDouble: true,
  minAmountThreshold: 0,
  status: 1,
  remark: '消费1元获得1积分，100积分抵扣1元'
}

// Mock 积分流水
const mockPointsRecords: PointsRecord[] = [
  {
    id: 1,
    customerId: 1001,
    customerName: '张小美',
    phone: '138****8888',
    points: 196,
    beforePoints: 324,
    afterPoints: 520,
    type: 1,
    changeTime: '2026-07-10 14:31:00',
    orderNo: 'OD20260710001',
    remark: '消费获取积分'
  },
  {
    id: 2,
    customerId: 1002,
    customerName: '李晓红',
    phone: '139****6666',
    points: 398,
    beforePoints: 626,
    afterPoints: 1024,
    type: 1,
    changeTime: '2026-07-10 15:21:00',
    orderNo: 'OD20260710002'
  },
  {
    id: 5,
    customerId: 1005,
    customerName: '赵敏',
    phone: '136****9999',
    points: -500,
    beforePoints: 500,
    afterPoints: 0,
    type: 3,
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
 * 获取当前门店的积分规则（后端按门店隔离，每店一份，取列表第一条）
 * @returns 积分规则，当前门店未配置时返回 null
 */
export async function getPointsRule(): Promise<PointsRule | null> {
  const params = buildQuery({ pageIndex: 1, pageSize: 1 })
  const res = await request<PagedResponse<PointsRule>>(`/pointsRules${params}`)
  return res.list[0] ?? null
}

/**
 * 保存积分规则（有 id 走 PUT 更新，无 id 走 POST 创建）
 * @param data 积分规则
 * @returns 保存后的规则
 */
export async function savePointsRule(data: PointsRule): Promise<PointsRule> {
  const body = JSON.stringify({
    pointsRate: data.pointsRate,
    deductRate: data.deductRate,
    maxDeductAmount: data.maxDeductAmount,
    pointsValidityDays: data.pointsValidityDays,
    birthdayDouble: data.birthdayDouble,
    minAmountThreshold: data.minAmountThreshold,
    status: data.status,
    remark: data.remark
  })
  if (data.id) {
    return request<PointsRule>(`/pointsRules/${data.id}`, {
      method: 'PUT',
      body
    })
  }
  return request<PointsRule>(`/pointsRules`, {
    method: 'POST',
    body
  })
}

/**
 * 获取积分流水分页列表
 * @param query 查询参数
 * @returns 分页积分流水
 */
export async function getPointsRecords(query?: PointsRecordQuery): Promise<PagedResponse<PointsRecord>> {
  const params = buildQuery({
    keyword: query?.keyword,
    type: query?.changeType,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  return request<PagedResponse<PointsRecord>>(`/customerPointsLogs${params}`)
}

/**
 * 消费获取积分（Mock）
 * 按 PointsRule 的 pointsRate 计算获得积分，低于门槛不获取
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
  if (orderAmount < (rule.minAmountThreshold ?? 0)) {
    throw new Error(`消费金额未达门槛（最低 ${rule.minAmountThreshold} 元）`)
  }
  const earnedPoints = Math.floor(orderAmount * rule.pointsRate)

  const beforePoints = customer.points
  const afterPoints = beforePoints + earnedPoints

  const record: PointsRecord = {
    id: ++mockPointsRecordIdCounter,
    customerId,
    customerName: customer.name,
    phone: customer.phone,
    points:earnedPoints,
    beforePoints,
    afterPoints,
    type: 1,
    changeTime: toLocalDateTime().replace('T', ' '),
    orderNo: orderId,
    remark: '消费获取积分'
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
    points:-actualDeduct,
    beforePoints,
    afterPoints,
    type: 3,
    changeTime: toLocalDateTime().replace('T', ' '),
    orderNo: orderId,
    remark: `订单退款扣减积分（应扣${pointsToDeduct}，实扣${actualDeduct}）`
  }

  mockPointsRecords.push(record)
  customer.points = afterPoints
  return record
}

// ==================== 手动调整积分 ====================

/**
 * 创建积分流水（手动调整）
 * 后端 CreateAsync 仅允许 Type=8（手动调整），其他类型由业务流程自动写入
 * @param data 积分流水数据
 */
export async function createPointsLog(data: PointsLogCreate): Promise<void> {
  await request(`/customerPointsLogs`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// ==================== 消费记录 ====================

/** 后端订单 DTO（消费记录所需字段子集，camelCase 与后端 JSON 序列化对齐） */
interface OrderDtoForConsume {
  id: number
  orderNo: string
  customerId?: number
  customerName?: string
  phone?: string
  projectSummary?: string
  paidAmount: number
  refundAmount: number
  status: number
  payMethod?: number
  orderTime: string
}

/**
 * 获取消费记录分页列表
 * 对接后端：GET /api/store/orders（按客户姓名/手机号/时间范围过滤）
 * 将 OrderDto 字段映射为 ConsumeRecord
 * @param query 查询参数
 * @returns 分页消费记录
 */
export async function getConsumeRecords(query?: ConsumeRecordQuery): Promise<PagedResponse<ConsumeRecord>> {
  const qs = buildQuery({
    keyword: query?.keyword,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  const res = await request<PagedResponse<OrderDtoForConsume>>(`/orders${qs}`)
  return {
    list: res.list.map(o => ({
      id: o.id,
      customerId: o.customerId ?? 0,
      customerName: o.customerName ?? '散客',
      phone: o.phone ?? '',
      orderNo: o.orderNo,
      // 消费金额展示净实付（实收 - 已退款），退款后金额随之减少
      amount: o.paidAmount - (o.refundAmount || 0),
      projectName: o.projectSummary ?? '',
      paymentMethod: o.payMethod ?? 0,
      status: o.status,
      consumeTime: o.orderTime
    })),
    total: res.total,
    pageIndex: res.pageIndex,
    pageSize: res.pageSize
  }
}

// ==================== 客户关怀 ====================

/**
 * 获取生日提醒分页列表
 * @param query 查询参数
 * @returns 分页生日提醒
 */
export async function getBirthdayReminders(query?: BirthdayReminderQuery): Promise<PagedResponse<BirthdayReminder>> {
  const params = buildQuery({
    keyword: query?.keyword,
    careStatus: query?.careStatus,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<BirthdayReminder>>(`/customerCares/birthdays${params}`)
}

/**
 * 标记生日关怀完成
 * @param id 客户ID
 */
export async function markBirthdayCared(id: number): Promise<void> {
  await request<void>(`/customerCares/birthdays/${id}/mark`, { method: 'POST' })
}

/**
 * 获取消费感谢分页列表
 * @param query 查询参数
 * @returns 分页消费感谢
 */
export async function getConsumeThanks(query?: ConsumeThankQuery): Promise<PagedResponse<ConsumeThankRecord>> {
  const params = buildQuery({
    keyword: query?.keyword,
    thankStatus: query?.thankStatus,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<ConsumeThankRecord>>(`/customerCares/consumeThanks${params}`)
}

/**
 * 标记消费感谢完成
 * @param id 订单ID
 * @param method 感谢方式
 */
export async function markConsumeThanked(id: number, method: number): Promise<void> {
  await request<void>(`/customerCares/consumeThanks/${id}/mark`, {
    method: 'POST',
    body: JSON.stringify({ method })
  })
}

// ==================== 客户消费统计 ====================

/**
 * 获取客户消费统计（消费频次、客单价、消费偏好）
 * @param customerId 客户ID
 * @returns 消费统计数据
 */
export async function getCustomerConsumptionStat(customerId: number): Promise<CustomerConsumptionStat> {
  return request<CustomerConsumptionStat>(`/customers/${customerId}/consumption-stat`)
}
