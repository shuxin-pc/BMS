// 价格变更记录 - API 服务
// 对接后端：GET /api/store/pricechangelogs
import type {
  PriceChangeLog,
  PriceChangeLogQuery,
  PagedResponse
} from './types'
import { request } from '../shared/storeRequest'

// 导出类型供外部使用
export type {
  PriceChangeLog,
  PriceChangeLogQuery,
  PagedResponse
}

/**
 * 获取价格变更记录分页列表
 * 对接后端：GET /api/store/pricechangelogs
 * @param query 查询参数（商品名称、时间范围）
 * @returns 分页价格变更记录列表
 */
export async function getPriceChangeLogs(query?: PriceChangeLogQuery): Promise<PagedResponse<PriceChangeLog>> {
  const params = new URLSearchParams()
  if (query?.productId !== undefined) params.append('productId', String(query.productId))
  if (query?.productName) params.append('productName', query.productName)
  if (query?.startDate) params.append('startDate', query.startDate)
  if (query?.endDate) params.append('endDate', query.endDate)
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<PriceChangeLog>>(`/pricechangelogs?${params}`)
}
