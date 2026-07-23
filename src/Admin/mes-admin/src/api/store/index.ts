// 门店管理 - API服务
import type {
  Store,
  StoreQuery,
  StoreCreate,
  StoreUpdate,
  ApiResponse,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  Store,
  StoreQuery,
  StoreCreate,
  StoreUpdate,
  ApiResponse,
  PagedResponse
}

// 通过网关访问后端服务
const API_BASE = '/api/store'

// 获取token
const getToken = () => localStorage.getItem('token')

// 获取当前选择的门店ID（用于 X-Store-Id 请求头）
const getCurrentStoreId = () => localStorage.getItem('currentStoreId')

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

  // 所有 /api/store/* 请求自动携带 X-Store-Id（若已选择门店）
  const storeId = getCurrentStoreId()
  if (storeId) {
    headers['X-Store-Id'] = storeId
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

// ==================== 门店管理 ====================

/**
 * 获取门店分页列表
 * @param query 查询参数
 * @returns 分页门店列表
 */
export async function getStores(query?: StoreQuery): Promise<PagedResponse<Store>> {
  const params = new URLSearchParams()
  if (query?.name) params.append('name', query.name)
  if (query?.code) params.append('code', query.code)
  if (query?.status !== undefined) params.append('status', String(query.status))
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<Store>>(`${API_BASE}/stores?${params}`)
}

/**
 * 获取门店详情
 * @param id 门店ID
 * @returns 门店详情
 */
export async function getStore(id: string): Promise<Store> {
  return request<Store>(`${API_BASE}/stores/${id}`)
}

/**
 * 创建门店
 * @param data 门店信息
 * @returns 创建后的门店信息
 */
export async function createStore(data: StoreCreate): Promise<Store> {
  return request<Store>(`${API_BASE}/stores`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新门店
 * @param data 门店信息
 * @returns 更新后的门店信息
 */
export async function updateStore(data: StoreUpdate): Promise<Store> {
  return request<Store>(`${API_BASE}/stores/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除门店
 * @param id 门店ID
 */
export async function deleteStore(id: string): Promise<void> {
  return request<void>(`${API_BASE}/stores/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除门店
 * @param ids 门店ID列表
 */
export async function deleteStores(ids: string[]): Promise<void> {
  return request<void>(`${API_BASE}/stores/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

/**
 * 获取当前用户授权的门店列表（用于门店切换器）
 * 仅返回启用状态的门店
 */
export async function getAuthorizedStores(): Promise<Store[]> {
  return request<Store[]>(`${API_BASE}/stores/authorized`)
}
