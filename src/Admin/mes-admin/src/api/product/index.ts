// 商品档案管理 - API服务
import type {
  Product,
  ProductQuery,
  ProductCreate,
  ProductUpdate,
  ProductCategory,
  ApiResponse,
  PagedResponse
} from './types'

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

// 通过网关访问后端服务
const API_BASE = '/api/product'

// 获取token
const getToken = () => localStorage.getItem('token')

// 通用请求方法
async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>)
  }

  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(url, {
    ...options,
    headers: headers as HeadersInit
  })

  let errorMessage = ''
  try {
    const result: ApiResponse<T> = await response.json()
    if (result.message) {
      errorMessage = result.message
    }
    if (result.code != 200) {
      throw new Error(errorMessage || '请求失败')
    }
    return result.data
  } catch (err: any) {
    if (errorMessage) {
      throw new Error(errorMessage)
    }
    throw new Error(`请求失败: ${response.status}`)
  }
}

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
  if (query?.supplierId !== undefined) params.append('supplierId', String(query.supplierId))
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
