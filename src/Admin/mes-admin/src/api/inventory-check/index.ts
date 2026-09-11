// 库存盘点 - API 服务
import { request, buildQuery } from '@/api/shared/storeRequest'
import type {
  InventoryCheck,
  InventoryCheckQuery,
  InventoryCheckCreate,
  InventoryCheckProductOption,
  InventoryCheckBatchOption,
  InventoryCheckBatchLookup,
  InventoryCheckBatch,
  SubmitCheckRequest,
  CreateAndSubmitRequest,
  BatchDeductItem,
  GainBatchItem,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  InventoryCheck,
  InventoryCheckQuery,
  InventoryCheckCreate,
  InventoryCheckProductOption,
  InventoryCheckBatchOption,
  InventoryCheckBatchLookup,
  InventoryCheckBatch,
  SubmitCheckRequest,
  CreateAndSubmitRequest,
  BatchDeductItem,
  GainBatchItem,
  PagedResponse
}

/**
 * 获取盘点记录分页列表
 * @param query 查询参数
 * @returns 分页盘点记录列表
 */
export async function getInventoryCheckList(query?: InventoryCheckQuery): Promise<PagedResponse<InventoryCheck>> {
  const qs = buildQuery({
    productName: query?.productName,
    status: query?.status,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return await request<PagedResponse<InventoryCheck>>(`/inventoryChecks${qs}`)
}

/**
 * 获取盘点记录详情（含商品名称/编码与批次明细）
 * @param id 盘点记录ID
 * @returns 盘点记录详情
 */
export async function getInventoryCheckById(id: number): Promise<InventoryCheck> {
  return await request<InventoryCheck>(`/inventoryChecks/${id}`)
}

/**
 * 创建盘点记录（草稿状态，不调整库存）
 * @param data 创建请求
 * @returns 创建后的盘点记录
 */
export async function createInventoryCheck(data: InventoryCheckCreate): Promise<InventoryCheck> {
  return await request<InventoryCheck>('/inventoryChecks', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 提交盘点单：录入实际数量，计算差异，自动调整库存并写入流水
 * @param id 盘点单ID
 * @param data 提交请求（实际数量、备注）
 * @returns 提交后的盘点记录
 */
export async function submitInventoryCheck(id: number, data: SubmitCheckRequest): Promise<InventoryCheck> {
  return await request<InventoryCheck>(`/inventoryChecks/${id}/submit`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 获取商品选项列表（含当前库存和成本价，用于盘点新增）
 * @returns 商品选项数组
 */
export async function getProductOptionsForCheck(): Promise<InventoryCheckProductOption[]> {
  return await request<InventoryCheckProductOption[]>('/inventoryChecks/product-options')
}

/**
 * 盘盈批次选项查询：按商品+门店列出可累加的目标批次（含已用完/已过期），供盘盈弹窗选择
 * @param productId 商品ID
 * @param params expirationDate（按过期日期精确匹配）与 noExpiry（匹配无有效期批次）二选一
 * @returns 批次列表（含生产日期/保质期/过期日期/状态）
 */
export async function getBatchOptionsForCheck(
  productId: number,
  params: { expirationDate?: string; noExpiry?: boolean }
): Promise<InventoryCheckBatchLookup[]> {
  const qs = buildQuery({ productId, expirationDate: params.expirationDate, noExpiry: params.noExpiry })
  return await request<InventoryCheckBatchLookup[]>(`/inventoryChecks/batch-options${qs}`)
}

/**
 * 创建并提交盘点单（原子操作）：事务内完成创建+提交，不产生草稿残留
 * 替代原有的 Create + Submit 串联调用，避免中间失败产生孤儿草稿
 * @param data 创建并提交请求
 * @returns 盘点完成的记录
 */
export async function createAndSubmitInventoryCheck(data: CreateAndSubmitRequest): Promise<InventoryCheck> {
  return await request<InventoryCheck>('/inventoryChecks/create-and-submit', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 查询当日该商品是否已有非取消状态的盘点记录（用于软约束提示）
 * @param productId 商品ID
 * @returns true=当日已盘点，false=未盘点
 */
export async function checkProductCheckedToday(productId: number): Promise<boolean> {
  const qs = buildQuery({ productId })
  return await request<boolean>(`/inventoryChecks/today-check${qs}`)
}
