// 疗程卡管理 - API服务
// 对接后端 TreatmentCardsController / TreatmentCardSalesController / TreatmentCardVerifiesController
import { request, buildQuery } from '../shared/storeRequest'
import type { PagedResponse } from '../shared/storeRequest'
import type {
  TreatmentCardConfig,
  TreatmentCardConfigQuery,
  TreatmentCardConfigCreate,
  TreatmentCardConfigUpdate,
  TreatmentCardSale,
  TreatmentCardSaleItem,
  TreatmentCardSaleQuery,
  TreatmentCardVerify,
  TreatmentCardVerifyItem,
  TreatmentCardVerifyItemInput,
  TreatmentCardVerifyQuery,
  TreatmentCardVerifyRequest,
  TreatmentCardExpiry,
  TreatmentCardExpiryQuery,
  CourseCardItem,
  ApiResponse
} from './types'

// 导出类型供外部使用
export type {
  TreatmentCardConfig,
  TreatmentCardConfigQuery,
  TreatmentCardConfigCreate,
  TreatmentCardConfigUpdate,
  TreatmentCardSale,
  TreatmentCardSaleItem,
  TreatmentCardSaleQuery,
  TreatmentCardVerify,
  TreatmentCardVerifyItem,
  TreatmentCardVerifyItemInput,
  TreatmentCardVerifyQuery,
  TreatmentCardVerifyRequest,
  TreatmentCardExpiry,
  TreatmentCardExpiryQuery,
  CourseCardItem,
  ApiResponse,
  PagedResponse
}

// ==================== 疗程卡配置 ====================
// 对接后端 TreatmentCardsController（路由 /api/store/treatmentCards）

/**
 * 获取疗程卡配置分页列表
 * @param query 查询参数
 * @returns 分页疗程卡配置列表
 */
export async function getTreatmentCardConfigs(query?: TreatmentCardConfigQuery): Promise<PagedResponse<TreatmentCardConfig>> {
  const qs = buildQuery({
    name: query?.name,
    code: query?.code,
    // buildQuery 不接受 boolean，需转为字符串
    isEnabled: query?.isEnabled === undefined ? undefined : (query.isEnabled ? 'true' : 'false'),
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardConfig>>(`/treatmentCards${qs}`)
}

/**
 * 获取疗程卡配置详情（含项目明细 items，用于核销时选择项目）
 * @param id 疗程卡配置ID
 * @returns 疗程卡配置详情
 */
export async function getTreatmentCardConfig(id: number): Promise<TreatmentCardConfig> {
  return request<TreatmentCardConfig>(`/treatmentCards/${id}`)
}

/**
 * 创建疗程卡配置
 * @param data 疗程卡配置信息
 * @returns 创建后的疗程卡配置
 */
export async function createTreatmentCardConfig(data: TreatmentCardConfigCreate): Promise<TreatmentCardConfig> {
  return request<TreatmentCardConfig>('/treatmentCards', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新疗程卡配置
 * @param data 疗程卡配置信息
 * @returns 更新后的疗程卡配置
 */
export async function updateTreatmentCardConfig(data: TreatmentCardConfigUpdate): Promise<TreatmentCardConfig> {
  return request<TreatmentCardConfig>(`/treatmentCards/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除疗程卡配置
 * @param id 疗程卡配置ID
 */
export async function deleteTreatmentCardConfig(id: number): Promise<void> {
  await request(`/treatmentCards/${id}`, { method: 'DELETE' })
}

// ==================== 疗程卡销售 ====================
// 对接后端 TreatmentCardSalesController（路由 /api/store/treatmentCardSales）

/**
 * 获取疗程卡销售分页列表
 * @param query 查询参数
 * @returns 分页疗程卡销售列表
 */
export async function getTreatmentCardSales(query?: TreatmentCardSaleQuery): Promise<PagedResponse<TreatmentCardSale>> {
  const qs = buildQuery({
    customerId: query?.customerId,
    cardId: query?.cardId,
    status: query?.status,
    customerName: query?.customerName,
    phone: query?.phone,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardSale>>(`/treatmentCardSales${qs}`)
}

/**
 * 获取疗程卡销售详情
 * @param id 销售记录ID
 * @returns 销售详情
 */
export async function getTreatmentCardSale(id: number): Promise<TreatmentCardSale> {
  return request<TreatmentCardSale>(`/treatmentCardSales/${id}`)
}

// ==================== 疗程卡核销 ====================
// 对接后端 TreatmentCardVerifiesController（路由 /api/store/treatmentCardVerifies）

/**
 * 获取核销记录分页列表
 * @param query 查询参数
 * @returns 分页核销记录列表
 */
export async function getTreatmentCardVerifies(query?: TreatmentCardVerifyQuery): Promise<PagedResponse<TreatmentCardVerify>> {
  const qs = buildQuery({
    cardSaleId: query?.cardSaleId,
    verifyProductId: query?.verifyProductId,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardVerify>>(`/treatmentCardVerifies${qs}`)
}

/**
 * 查询客户的有效疗程卡列表
 * 复用销售列表接口，按客户名称/手机号过滤有效状态（status=1）的疗程卡
 * @param customerName 客户名称
 * @param phone 手机号
 * @returns 疗程卡销售列表
 */
export async function getCustomerTreatmentCards(customerName?: string, phone?: string): Promise<TreatmentCardSale[]> {
  const res = await getTreatmentCardSales({
    customerName,
    phone,
    status: 1,
    pageSize: 1000
  })
  return res.list
}

/**
 * 核销疗程卡
 * 对接后端 POST /treatmentCardVerifies
 * @param data 核销请求（cardSaleId, items: TreatmentCardVerifyItemInput[], storeId, storeCode, operatorId, remark）
 *             核销金额、关联订单、核销时间由后端自动计算/创建
 * @returns 核销记录
 */
export async function verifyTreatmentCard(data: TreatmentCardVerifyRequest): Promise<TreatmentCardVerify> {
  return request<TreatmentCardVerify>('/treatmentCardVerifies', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// ==================== 疗程卡到期提醒 ====================

/**
 * 获取疗程卡到期提醒分页列表
 * 对接后端 GET /treatmentCardSales/expiries
 * @param query 查询参数
 * @returns 分页到期提醒列表
 */
export async function getTreatmentCardExpiries(query?: TreatmentCardExpiryQuery): Promise<PagedResponse<TreatmentCardExpiry>> {
  const qs = buildQuery({
    customerName: query?.customerName,
    alertLevel: query?.alertLevel,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardExpiry>>(`/treatmentCardSales/expiries${qs}`)
}
