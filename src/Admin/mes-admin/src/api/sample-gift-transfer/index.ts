// 样品赠品调拨 - API 服务
// 对接后端 SampleGiftTransfersController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import { getAuthorizedStores } from '../store'
import type {
  SampleGiftTransfer,
  SampleGiftTransferItem,
  SampleGiftTransferQuery,
  SampleGiftTransferCreate,
  SampleGiftTransferItemCreate,
  SampleGiftTransferStatus,
  SampleGiftProductType,
  SampleGiftTransferProductOption,
  SampleGiftTransferBatchOption
} from './types'

// 导出类型供外部使用
export type {
  SampleGiftTransfer,
  SampleGiftTransferItem,
  SampleGiftTransferQuery,
  SampleGiftTransferCreate,
  SampleGiftTransferItemCreate,
  SampleGiftTransferStatus,
  SampleGiftProductType,
  PagedResponse,
  SampleGiftTransferProductOption,
  SampleGiftTransferBatchOption
}

// ==================== API 函数 ====================

/**
 * 获取样品赠品调拨单分页列表
 * 对接后端：GET /api/store/samplegifttransfers
 * @param query 查询参数
 * @returns 分页调拨单列表
 */
export async function getSampleGiftTransferList(query?: SampleGiftTransferQuery): Promise<PagedResponse<SampleGiftTransfer>> {
  const qs = buildQuery({
    transferNo: query?.transferNo,
    status: query?.status,
    fromStoreId: query?.fromStoreId,
    toStoreId: query?.toStoreId,
    productType: query?.productType,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<SampleGiftTransfer>>(`/samplegifttransfers${qs}`)
}

/**
 * 获取样品赠品调拨单详情（含明细列表）
 * 对接后端：GET /api/store/samplegifttransfers/{id}
 * @param id 调拨单ID
 * @returns 调拨单详情
 */
export async function getSampleGiftTransferDetail(id: string): Promise<SampleGiftTransfer> {
  return request<SampleGiftTransfer>(`/samplegifttransfers/${id}`)
}

/**
 * 创建样品赠品调拨单（待调出状态，不调整库存）
 * 对接后端：POST /api/store/samplegifttransfers
 * 调拨单号由后端自动生成
 * @param data 创建请求
 * @returns 创建后的调拨单
 */
export async function createSampleGiftTransfer(data: SampleGiftTransferCreate): Promise<SampleGiftTransfer> {
  return request<SampleGiftTransfer>('/samplegifttransfers', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 执行调拨（事务内调出门店扣减库存+调入门店增加库存，状态转为已调入）
 * 对接后端：POST /api/store/samplegifttransfers/{id}/execute
 * @param id 调拨单ID
 */
export async function executeSampleGiftTransfer(id: string): Promise<void> {
  await request<void>(`/samplegifttransfers/${id}/execute`, { method: 'POST' })
}

/**
 * 取消调拨单（仅待调出状态可取消，已调入需走反向调拨单）
 * 对接后端：POST /api/store/samplegifttransfers/{id}/cancel
 * @param id 调拨单ID
 */
export async function cancelSampleGiftTransfer(id: string): Promise<void> {
  await request<void>(`/samplegifttransfers/${id}/cancel`, { method: 'POST' })
}

/**
 * 获取门店选项列表（用于下拉选择）
 * 复用门店授权接口，仅返回当前用户授权的启用门店
 * @returns 门店选项数组
 */
export async function getStoreOptions(): Promise<{ id: string; code: string; name: string }[]> {
  const stores = await getAuthorizedStores()
  return stores.map(s => ({ id: s.id, code: s.code, name: s.name }))
}

/**
 * 获取调出门店的样品/赠品商品选项（仅返回 Type∈{4,5} 且 Stock > 0 的商品）
 * 对接后端：GET /api/store/samplegifttransfers/from-store-products?fromStoreId=xxx
 * @param fromStoreId 调出门店ID
 * @returns 商品选项数组（含库存量与商品类型）
 */
export async function getFromStoreProducts(fromStoreId: string): Promise<SampleGiftTransferProductOption[]> {
  const qs = buildQuery({ fromStoreId })
  return request<SampleGiftTransferProductOption[]>(`/samplegifttransfers/from-store-products${qs}`)
}

/**
 * 获取调出门店指定商品的在库批次列表（按过期日期升序，FEFO）
 * 对接后端：GET /api/store/samplegifttransfers/from-store-products/{productId}/batches?fromStoreId=xxx
 * @param fromStoreId 调出门店ID
 * @param productId 商品ID
 * @returns 批次选项数组
 */
export async function getFromStoreProductBatches(fromStoreId: string, productId: number): Promise<SampleGiftTransferBatchOption[]> {
  const qs = buildQuery({ fromStoreId })
  return request<SampleGiftTransferBatchOption[]>(`/samplegifttransfers/from-store-products/${productId}/batches${qs}`)
}

/**
 * 调拨单状态标签映射
 */
export const sampleGiftTransferStatusMap: Record<SampleGiftTransferStatus, string> = {
  1: '待调出',
  3: '已调入',
  4: '已取消'
}

/**
 * 调拨单状态对应的 tag 类型
 */
export const sampleGiftTransferStatusTagType: Record<SampleGiftTransferStatus, 'warning' | 'success' | 'info'> = {
  1: 'warning',
  3: 'success',
  4: 'info'
}

/**
 * 商品类型标签映射
 */
export const productTypeMap: Record<SampleGiftProductType, string> = {
  4: '样品',
  5: '赠品'
}

/**
 * 商品类型对应的 tag 类型
 */
export const productTypeTagType: Record<SampleGiftProductType, 'primary' | 'success'> = {
  4: 'primary',
  5: 'success'
}
