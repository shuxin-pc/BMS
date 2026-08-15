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
  SupplierUpdate,
  ProductSupplier,
  BindProductsRequest,
  SetDefaultSupplierRequest
} from './types'

// 导出类型供外部使用
export type {
  Supplier,
  SupplierQuery,
  SupplierCreate,
  SupplierUpdate,
  ProductSupplier,
  BindProductsRequest,
  SetDefaultSupplierRequest,
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
    scope: query?.scope,
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

// ==================== 供应商-品项关联 ====================

/**
 * 批量绑定品项到供应商
 * 对接后端：POST /api/store/suppliers/bind-products
 * @param data 绑定请求（供应商ID + 品项ID列表）
 * @returns 本次绑定的关联记录列表
 */
export async function bindProducts(data: BindProductsRequest): Promise<ProductSupplier[]> {
  return request<ProductSupplier[]>('/suppliers/bind-products', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 解除品项与供应商的关联
 * 对接后端：DELETE /api/store/suppliers/products/{productId}/{supplierId}
 * @param productId 品项ID
 * @param supplierId 供应商ID
 */
export async function unbindProduct(productId: number, supplierId: number): Promise<void> {
  await request<void>(`/suppliers/products/${productId}/${supplierId}`, {
    method: 'DELETE'
  })
}

/**
 * 设置品项的默认供应商
 * 对接后端：POST /api/store/suppliers/default-supplier
 * @param data 设置请求（品项ID + 供应商ID + 可选参考价/供货周期）
 * @returns 更新后的关联记录
 */
export async function setDefaultSupplier(data: SetDefaultSupplierRequest): Promise<ProductSupplier> {
  return request<ProductSupplier>('/suppliers/default-supplier', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新品项-供应商关联的参考价与供货周期（不改变默认供应商状态）
 * 对接后端：PUT /api/store/suppliers/product-relation
 * @param data 更新请求（品项ID + 供应商ID + 参考价/供货周期）
 * @returns 更新后的关联记录
 */
export async function updateProductRelation(data: SetDefaultSupplierRequest): Promise<ProductSupplier> {
  return request<ProductSupplier>('/suppliers/product-relation', {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 查询供应商关联的品项列表
 * 对接后端：GET /api/store/suppliers/{supplierId}/products
 * @param supplierId 供应商ID
 * @returns 关联品项列表（默认供应商排在首位）
 */
export async function getProductsBySupplier(supplierId: number): Promise<ProductSupplier[]> {
  return request<ProductSupplier[]>(`/suppliers/${supplierId}/products`)
}
