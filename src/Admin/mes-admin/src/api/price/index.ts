// 价格管理 - API 服务
// 对接后端 PriceChangeLogsController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  PriceInfo,
  PriceQuery,
  PriceAdjust,
  BatchPriceAdjust,
  BatchPriceAdjustResult
} from './types'

// 导出类型供外部使用
export type {
  PriceInfo,
  PriceQuery,
  PriceAdjust,
  BatchPriceAdjust,
  BatchPriceAdjustResult,
  PagedResponse
}

// ==================== API 方法 ====================

/**
 * 获取价格变更记录分页列表
 * 对接后端：GET /api/store/priceChangeLogs
 * @param query 查询参数
 * @returns 分页价格变更记录列表
 */
export async function getPriceList(query?: PriceQuery): Promise<PagedResponse<PriceInfo>> {
  const qs = buildQuery({
    productId: query?.productId,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<PriceInfo>>(`/priceChangeLogs${qs}`)
}

/**
 * 调整商品价格（创建价格变更记录）
 * 对接后端：POST /api/store/priceChangeLogs
 * @param data 调价请求（包含商品ID、原价、新价格和备注）
 * @returns 创建后的价格变更记录
 */
export async function adjustPrice(data: PriceAdjust): Promise<PriceInfo> {
  return request<PriceInfo>('/priceChangeLogs', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 批量调价：按范围（商品ID列表/分类/供应商/全部）批量更新商品价格并自动记录价格变更日志
 * 对接后端：POST /api/store/priceChangeLogs/batch-adjust
 * @param data 批量调价请求（含调价范围、调价方式、调价值）
 * @returns 调价结果（成功/失败/跳过数量及失败明细）
 */
export async function batchAdjustPrice(data: BatchPriceAdjust): Promise<BatchPriceAdjustResult> {
  return request<BatchPriceAdjustResult>('/priceChangeLogs/batch-adjust', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}
