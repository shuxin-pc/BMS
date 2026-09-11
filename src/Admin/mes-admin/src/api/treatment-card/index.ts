// 项目卡管理 - API服务
// 对接后端 TreatmentCardsController / TreatmentCardSalesController / TreatmentCardVerifiesController
import { request, buildQuery } from '../shared/storeRequest'
import type { PagedResponse } from '../shared/storeRequest'
import type {
  TreatmentCardConfig,
  TreatmentCardConfigQuery,
  TreatmentCardConfigCreate,
  TreatmentCardConfigUpdate,
  TreatmentCardSale,
  TreatmentCardSaleCreate,
  TreatmentCardSaleItem,
  TreatmentCardSaleQuery,
  TreatmentCardSaleRefundRequest,
  TreatmentCardSaleRefundResult,
  TreatmentCardVerify,
  TreatmentCardVerifyItem,
  TreatmentCardVerifyItemInput,
  TreatmentCardVerifyQuery,
  TreatmentCardVerifyRequest,
  TreatmentCardExpiry,
  TreatmentCardExpiryQuery,
  TreatmentCardExpiryPage,
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
  TreatmentCardSaleCreate,
  TreatmentCardSaleItem,
  TreatmentCardSaleQuery,
  TreatmentCardSaleRefundRequest,
  TreatmentCardSaleRefundResult,
  TreatmentCardVerify,
  TreatmentCardVerifyItem,
  TreatmentCardVerifyItemInput,
  TreatmentCardVerifyQuery,
  TreatmentCardVerifyRequest,
  TreatmentCardExpiry,
  TreatmentCardExpiryQuery,
  TreatmentCardExpiryPage,
  CourseCardItem,
  ApiResponse,
  PagedResponse
}

// ==================== 项目卡配置 ====================
// 对接后端 TreatmentCardsController（路由 /api/store/treatmentCards）

/**
 * 获取项目卡配置分页列表
 * @param query 查询参数
 * @returns 分页项目卡配置列表
 */
export async function getTreatmentCardConfigs(query?: TreatmentCardConfigQuery): Promise<PagedResponse<TreatmentCardConfig>> {
  const qs = buildQuery({
    name: query?.name,
    // buildQuery 不接受 boolean，需转为字符串
    isEnabled: query?.isEnabled === undefined ? undefined : (query.isEnabled ? 'true' : 'false'),
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardConfig>>(`/treatmentCards${qs}`)
}

/**
 * 获取项目卡配置详情（含项目明细 items，用于核销时选择项目）
 * @param id 项目卡配置ID
 * @returns 项目卡配置详情
 */
export async function getTreatmentCardConfig(id: number): Promise<TreatmentCardConfig> {
  return request<TreatmentCardConfig>(`/treatmentCards/${id}`)
}

/**
 * 创建项目卡配置
 * @param data 项目卡配置信息
 * @returns 创建后的项目卡配置
 */
export async function createTreatmentCardConfig(data: TreatmentCardConfigCreate): Promise<TreatmentCardConfig> {
  return request<TreatmentCardConfig>('/treatmentCards', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新项目卡配置
 * @param data 项目卡配置信息
 * @returns 更新后的项目卡配置
 */
export async function updateTreatmentCardConfig(data: TreatmentCardConfigUpdate): Promise<TreatmentCardConfig> {
  return request<TreatmentCardConfig>(`/treatmentCards/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除项目卡配置
 * @param id 项目卡配置ID
 */
export async function deleteTreatmentCardConfig(id: number): Promise<void> {
  await request(`/treatmentCards/${id}`, { method: 'DELETE' })
}

// ==================== 项目卡销售 ====================
// 对接后端 TreatmentCardSalesController（路由 /api/store/treatmentCardSales）

/**
 * 获取项目卡销售分页列表
 * @param query 查询参数
 * @returns 分页项目卡销售列表
 */
export async function getTreatmentCardSales(query?: TreatmentCardSaleQuery): Promise<PagedResponse<TreatmentCardSale>> {
  const qs = buildQuery({
    customerId: query?.customerId,
    cardId: query?.cardId,
    status: query?.status,
    keyword: query?.keyword,
    cardName: query?.cardName,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardSale>>(`/treatmentCardSales${qs}`)
}

/**
 * 获取项目卡销售详情
 * @param id 销售记录ID
 * @returns 销售详情
 */
export async function getTreatmentCardSale(id: number): Promise<TreatmentCardSale> {
  return request<TreatmentCardSale>(`/treatmentCardSales/${id}`)
}

/**
 * 创建项目卡销售记录（开卡）
 * 对接后端 POST /api/store/treatmentCardSales
 * 后端自动：校验卡可用性、按售价分摊计算项目折算单价、计算有效期、发放积分
 * @param data 创建请求（cardId、customerId、amount、items、purchaseDate）
 * @returns 创建后的销售记录
 */
export async function createTreatmentCardSale(data: TreatmentCardSaleCreate): Promise<TreatmentCardSale> {
  return request<TreatmentCardSale>('/treatmentCardSales', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 项目卡退卡（退款）
 * 对接后端 POST /api/store/treatmentCardSales/{id}/refund
 * 退卡金额 = 售价 - 已核销金额（未消费部分退还客户），仅有效(1)/已用完(2)可退，已退卡(3)不可重复退
 * 原渠道资金回退（储值退实收余额、积分按比例换算退还、现金类仅线下记录）
 * @param data 退卡请求（id 雪花ID 直接拼 URL，remark 退卡原因必填）
 * @returns 退卡结果（含原价/已消费/应退金额）
 */
export async function refundTreatmentCardSale(data: TreatmentCardSaleRefundRequest): Promise<TreatmentCardSaleRefundResult> {
  return request<TreatmentCardSaleRefundResult>(`/treatmentCardSales/${data.id}/refund`, {
    method: 'POST',
    body: JSON.stringify({
      remark: data.remark
    })
  })
}

// ==================== 项目卡核销 ====================
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
    keyword: query?.keyword,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardVerify>>(`/treatmentCardVerifies${qs}`)
}

/**
 * 获取核销记录详情（含核销项目明细 items，明细含 productName 项目名称）
 * 对接后端 GET /api/store/treatmentCardVerifies/{id}
 * 注意：id 为雪花ID，序列化为字符串，禁止 Number() 转换（会丢精度），直接拼 URL
 * @param id 核销记录ID
 * @returns 核销记录详情
 */
export async function getTreatmentCardVerify(id: number | string): Promise<TreatmentCardVerify> {
  return request<TreatmentCardVerify>(`/treatmentCardVerifies/${id}`)
}

/**
 * 查询客户的有效项目卡列表
 * 复用销售列表接口，按客户名称/手机号关键字过滤有效状态（status=1）的项目卡
 * @param keyword 客户名称或手机号关键字
 * @returns 项目卡销售列表
 */
export async function getCustomerTreatmentCards(keyword?: string): Promise<TreatmentCardSale[]> {
  const res = await getTreatmentCardSales({
    keyword,
    status: 1,
    pageSize: 1000
  })
  return res.list
}

/**
 * 核销项目卡
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

// ==================== 项目卡到期提醒 ====================

/**
 * 获取项目卡到期提醒分页列表（含全量预警级别统计）
 * 对接后端 GET /treatmentCardSales/expiries
 * @param query 查询参数
 * @returns 分页到期提醒列表及全量级别统计
 */
export async function getTreatmentCardExpiries(query?: TreatmentCardExpiryQuery): Promise<TreatmentCardExpiryPage> {
  const qs = buildQuery({
    keyword: query?.keyword,
    alertLevel: query?.alertLevel,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<TreatmentCardExpiryPage>(`/treatmentCardSales/expiries${qs}`)
}
