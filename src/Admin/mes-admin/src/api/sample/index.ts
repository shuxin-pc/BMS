// 样品赠品模块 - API 服务
// 对接后端 SampleGiftsController / SampleGiftReceivesController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Sample,
  SampleQuery,
  SampleReceive,
  SampleReceiveQuery,
  SampleReceiveCreate,
  SampleReport,
  SampleReportQuery
} from './types'

// 导出类型供外部使用
export type {
  Sample,
  SampleQuery,
  SampleReceive,
  SampleReceiveQuery,
  SampleReceiveCreate,
  SampleReport,
  SampleReportQuery,
  PagedResponse
}

// ==================== 样品赠品档案 ====================

/**
 * 获取样品/赠品分页列表
 * 对接后端：GET /api/store/sampleGifts
 * @param query 查询参数
 * @returns 分页样品列表
 */
export async function getSamples(query?: SampleQuery): Promise<PagedResponse<Sample>> {
  const qs = buildQuery({
    name: query?.name,
    code: query?.code,
    type: query?.type,
    status: query?.status,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Sample>>(`/sampleGifts${qs}`)
}

/**
 * 获取全部样品/赠品下拉列表（不分页）
 * 后端无专用不分页接口，通过大 pageSize 获取启用的样品赠品
 * @returns 样品列表
 */
export async function getAllSamples(): Promise<Sample[]> {
  const qs = buildQuery({
    status: 1,
    pageIndex: 1,
    pageSize: 9999
  })
  const paged = await request<PagedResponse<Sample>>(`/sampleGifts${qs}`)
  return paged.list
}

// ==================== 样品领用 ====================

/**
 * 获取领用记录分页列表
 * 对接后端：GET /api/store/sampleGiftReceives
 * @param query 查询参数
 * @returns 分页领用记录列表
 */
export async function getSampleReceives(query?: SampleReceiveQuery): Promise<PagedResponse<SampleReceive>> {
  const qs = buildQuery({
    productId: query?.productId,
    customerId: query?.customerId,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<SampleReceive>>(`/sampleGiftReceives${qs}`)
}

/**
 * 创建领用记录
 * 对接后端：POST /api/store/sampleGiftReceives
 * @param data 领用信息
 * @returns 创建后的领用记录
 */
export async function createSampleReceive(data: SampleReceiveCreate): Promise<SampleReceive> {
  return request<SampleReceive>('/sampleGiftReceives', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// ==================== 统计报表 ====================
// 对接后端 SampleGiftsController 的 reports 接口

/**
 * 获取统计报表分页列表
 * 对接后端：GET /api/store/sampleGifts/reports
 * @param query 查询参数
 * @returns 分页统计列表
 */
export async function getSampleReports(query?: SampleReportQuery): Promise<PagedResponse<SampleReport>> {
  const qs = buildQuery({
    name: query?.name,
    type: query?.type,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<SampleReport>>(`/sampleGifts/reports${qs}`)
}
