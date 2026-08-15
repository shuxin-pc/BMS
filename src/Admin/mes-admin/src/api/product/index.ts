// 商品档案管理 - API服务
// 复用 storeRequest 的统一 request 函数，自动注入 X-Store-Id 与 Authorization
import type {
  Product,
  ProductQuery,
  ProductCreate,
  ProductUpdate,
  ProductCategory,
  ApiResponse,
  PagedResponse
} from './types'
import type { ProductSupplier } from '../supplier/types'
import { request } from '../shared/storeRequest'

// 导出类型供外部使用
export type {
  Product,
  ProductQuery,
  ProductCreate,
  ProductUpdate,
  ProductCategory,
  ApiResponse,
  PagedResponse
}

// Store 子系统下商品档案路由前缀（storeRequest 的 API_BASE 已是 /api/store）
const API_BASE = '/product'

// ==================== 商品管理 ====================

/**
 * 获取商品分页列表
 * @param query 查询参数
 * @returns 分页商品列表
 */
export async function getProducts(query?: ProductQuery): Promise<PagedResponse<Product>> {
  const params = new URLSearchParams()
  if (query?.name) params.append('name', query.name)
  if (query?.code) params.append('code', query.code)
  if (query?.categoryId !== undefined) params.append('categoryId', String(query.categoryId))
  if (query?.status !== undefined) params.append('status', String(query.status))
  if (query?.type !== undefined) params.append('type', String(query.type))
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<Product>>(`${API_BASE}/products?${params}`)
}

/**
 * 获取商品详情
 * @param id 商品ID
 * @returns 商品详情
 */
export async function getProduct(id: number): Promise<Product> {
  return request<Product>(`${API_BASE}/products/${id}`)
}

/**
 * 创建商品
 * @param data 商品信息
 * @returns 创建后的商品信息
 */
export async function createProduct(data: ProductCreate): Promise<Product> {
  return request<Product>(`${API_BASE}/products`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新商品
 * @param data 商品信息
 * @returns 更新后的商品信息
 */
export async function updateProduct(data: ProductUpdate): Promise<Product> {
  return request<Product>(`${API_BASE}/products/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除商品
 * @param id 商品ID
 */
export async function deleteProduct(id: number): Promise<void> {
  return request<void>(`${API_BASE}/products/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除商品
 * @param ids 商品ID列表
 */
export async function deleteProducts(ids: number[]): Promise<void> {
  return request<void>(`${API_BASE}/products/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

/**
 * 查询品项关联的供应商列表
 * 对接后端：GET /api/store/product/products/{productId}/suppliers
 * @param productId 商品ID
 * @returns 关联供应商列表（默认供应商排在首位）
 */
export async function getSuppliersByProduct(productId: number): Promise<ProductSupplier[]> {
  return request<ProductSupplier[]>(`${API_BASE}/products/${productId}/suppliers`)
}

// ==================== 商品分类 ====================

/**
 * 获取商品分类树
 * @returns 分类树
 */
export async function getCategoryTree(): Promise<ProductCategory[]> {
  return request<ProductCategory[]>(`${API_BASE}/categories/tree`)
}

/**
 * 创建分类
 * @param data 分类信息
 * @returns 创建后的分类
 */
export async function createCategory(data: Omit<ProductCategory, 'id' | 'children'>): Promise<ProductCategory> {
  return request<ProductCategory>(`${API_BASE}/categories`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新分类
 * @param data 分类信息
 * @returns 更新后的分类
 */
export async function updateCategory(data: ProductCategory): Promise<ProductCategory> {
  return request<ProductCategory>(`${API_BASE}/categories/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除分类
 * @param id 分类ID
 */
export async function deleteCategory(id: number): Promise<void> {
  return request<void>(`${API_BASE}/categories/${id}`, {
    method: 'DELETE'
  })
}
