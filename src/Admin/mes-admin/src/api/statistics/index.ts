// 统计看板 - API 服务
// 对接后端 DashboardController：/api/store/dashboard/*
import { request, buildQuery } from '../shared/storeRequest'
import type {
  DailyStat,
  ProductSalesStat,
  DashboardSummary,
  DashboardAlert,
  TrendQuery,
  TopProductsQuery,
  ProductType,
  ApiResponse
} from './types'

// 导出类型供外部使用
export type {
  DailyStat,
  ProductSalesStat,
  DashboardSummary,
  DashboardAlert,
  TrendQuery,
  TopProductsQuery,
  ProductType,
  ApiResponse
}

// ==================== API 方法 ====================

/**
 * 获取首页看板核心指标（今日数据，实时聚合）
 * 对接后端：GET /api/store/dashboard/summary
 * @returns 今日核心指标
 */
export async function getDashboardSummary(): Promise<DashboardSummary> {
  return request<DashboardSummary>('/dashboard/summary')
}

/**
 * 获取月度营收趋势数据
 * 对接后端：GET /api/store/dashboard/monthly-trend
 * @param query 查询参数（年月）
 * @returns 每日统计列表
 */
export async function getMonthlyTrend(query: TrendQuery): Promise<DailyStat[]> {
  const path = `/dashboard/monthly-trend${buildQuery({ year: query.year, month: query.month })}`
  const list = await request<DailyStat[]>(path)
  // 后端 StatDate 为 DateTime 格式，截取为 yyyy-MM-dd 保持与前端一致
  return list.map(item => ({
    ...item,
    statDate: item.statDate?.substring(0, 10) ?? ''
  }))
}

/**
 * 获取热门商品 TOP N
 * 对接后端：GET /api/store/dashboard/top-products
 * @param query 查询参数
 * @returns 热门商品列表
 */
export async function getTopProducts(query: TopProductsQuery): Promise<ProductSalesStat[]> {
  const search = new URLSearchParams()
  search.append('year', String(query.year))
  search.append('month', String(query.month))
  if (query.top) search.append('top', String(query.top))
  if (query.sortBy) search.append('sortBy', query.sortBy)
  if (query.productTypes) {
    query.productTypes.forEach(t => search.append('productTypes', String(t)))
  }
  return request<ProductSalesStat[]>(`/dashboard/top-products?${search.toString()}`)
}

/**
 * 获取营收构成（饼图数据）
 * 对接后端：GET /api/store/dashboard/revenue-composition
 * @param query 查询参数（年月，默认当前月）
 * @returns 营收构成列表
 */
export async function getRevenueComposition(
  query?: TrendQuery
): Promise<{ name: string; value: number }[]> {
  const now = new Date()
  const year = query?.year ?? now.getFullYear()
  const month = query?.month ?? now.getMonth() + 1
  const path = `/dashboard/revenue-composition${buildQuery({ year, month })}`
  return request<{ name: string; value: number }[]>(path)
}

/**
 * 获取首页预警提醒（库存预警/批次临期/项目卡到期/客户生日聚合）
 * 对接后端：GET /api/store/dashboard/alerts
 * @returns 预警提醒列表（按紧急度排序，最多 10 条）
 */
export async function getDashboardAlerts(): Promise<DashboardAlert[]> {
  return request<DashboardAlert[]>('/dashboard/alerts')
}
