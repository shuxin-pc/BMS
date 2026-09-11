// 日结管理 - API服务
import { request, buildQuery, type PagedResponse } from '@/api/shared/storeRequest'
import type {
  DailySettlement,
  DailySettlementQuery,
  TodaySummary,
  SettlementValidationResult,
  ManualSummarizeRequest,
  ReverseRequest
} from './types'

// 导出类型供外部使用
export type {
  DailySettlement,
  DailySettlementQuery,
  TodaySummary,
  SettlementValidationResult,
  ManualSummarizeRequest,
  ReverseRequest,
  PagedResponse,
  SettlementStatus
} from './types'

const BASE = '/daily-settlements'

/**
 * 获取今日经营汇总（实时聚合，不落库）
 */
export async function getTodaySummary(): Promise<TodaySummary> {
  return request<TodaySummary>(`${BASE}/today-summary`)
}

/**
 * 获取日结记录分页列表
 */
export async function getDailySettlements(query?: DailySettlementQuery): Promise<PagedResponse<DailySettlement>> {
  const qs = buildQuery({
    settlementDateStart: query?.startDate,
    settlementDateEnd: query?.endDate,
    status: query?.status,
    pageIndex: query?.pageIndex ?? 1,
    pageSize: query?.pageSize ?? 20
  })
  return request<PagedResponse<DailySettlement>>(`${BASE}${qs}`)
}

/**
 * 手动汇总日结（创建待确认记录）
 */
export async function executeDailySettlement(data?: ManualSummarizeRequest): Promise<DailySettlement> {
  return request<DailySettlement>(`${BASE}/summarize`, {
    method: 'POST',
    body: JSON.stringify(data ?? {})
  })
}

/**
 * 获取日结记录详情
 */
export async function getDailySettlement(id: number): Promise<DailySettlement> {
  return request<DailySettlement>(`${BASE}/${id}`)
}

/**
 * 确认前防漏单校验
 */
export async function validateSettlement(id: number): Promise<SettlementValidationResult> {
  return request<SettlementValidationResult>(`${BASE}/${id}/validate`)
}

/**
 * 确认日结（待确认 -> 已确认），确认时可修改备注
 * @param remark 新备注（可选）：不传则保持原值，传空字符串清空备注
 */
export async function confirmSettlement(id: number, remark?: string): Promise<DailySettlement> {
  const hasRemark = remark !== undefined
  return request<DailySettlement>(`${BASE}/${id}/confirm`, {
    method: 'POST',
    ...(hasRemark ? { body: JSON.stringify({ remark }) } : {})
  })
}

/**
 * 反日结（已确认 -> 待确认）
 */
export async function reverseSettlement(id: number, data?: ReverseRequest): Promise<DailySettlement> {
  return request<DailySettlement>(`${BASE}/${id}/reverse`, {
    method: 'POST',
    body: JSON.stringify(data ?? {})
  })
}

/**
 * 重新汇总（仅待确认状态可重算）
 */
export async function recalculateSettlement(id: number): Promise<DailySettlement> {
  return request<DailySettlement>(`${BASE}/${id}/recalculate`, { method: 'POST' })
}
