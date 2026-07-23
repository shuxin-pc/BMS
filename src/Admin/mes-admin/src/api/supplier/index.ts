// 供应商管理 - API 服务
// 对接后端 SuppliersController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Supplier,
  SupplierQuery,
  SupplierCreate,
  SupplierUpdate
} from './types'

// 导出类型供外部使用
export type {
  Supplier,
  SupplierQuery,
  SupplierCreate,
  SupplierUpdate,
  PagedResponse
}

// ==================== API 方法 ====================

/**
 * 获取供应商分页列表
 * 对接后端：GET /api/store/suppliers
 * @param query 查询参数
 * @returns 分页供应商列表
 */
export async function getSuppliers(query?: SupplierQuery): Promise<PagedResponse<Supplier>> {
  const qs = buildQuery({
    name: query?.name,
    code: query?.code,
    status: query?.status,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Supplier>>(`/suppliers${qs}`)
}

/**
 * 获取供应商详情
 * 对接后端：GET /api/store/suppliers/{id}
 * @param id 供应商ID
 * @returns 供应商详情
 */
export async function getSupplier(id: number): Promise<Supplier> {
  return request<Supplier>(`/suppliers/${id}`)
}

/**
 * 创建供应商
 * 对接后端：POST /api/store/suppliers
 * @param data 供应商信息
 * @returns 创建后的供应商信息
 */
export async function createSupplier(data: SupplierCreate): Promise<Supplier> {
  return request<Supplier>('/suppliers', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新供应商
 * 对接后端：PUT /api/store/suppliers/{id}
 * @param data 供应商信息
 * @returns 更新后的供应商信息
 */
export async function updateSupplier(data: SupplierUpdate): Promise<Supplier> {
  return request<Supplier>(`/suppliers/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除供应商
 * 对接后端：DELETE /api/store/suppliers/{id}
 * @param id 供应商ID
 */
export async function deleteSupplier(id: number): Promise<void> {
  await request<void>(`/suppliers/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除供应商
 * 对接后端：POST /api/store/suppliers/batch
 * @param ids 供应商ID列表
 */
export async function deleteSuppliers(ids: number[]): Promise<void> {
  await request<void>('/suppliers/batch', {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}
