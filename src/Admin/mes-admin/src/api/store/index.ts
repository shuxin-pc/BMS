// 门店管理 - API服务
import type {
  Store,
  StoreQuery,
  StoreCreate,
  StoreUpdate,
  ApiResponse,
  PagedResponse,
  TenantUser,
  AvailableUser,
  StoreTenantSetting,
  StoreTenantSettingUpdate
} from './types'
import { handleUnauthorized } from '../shared/auth'

// 导出类型供外部使用
export type {
  Store,
  StoreQuery,
  StoreCreate,
  StoreUpdate,
  ApiResponse,
  PagedResponse,
  TenantUser,
  AvailableUser,
  StoreTenantSetting,
  StoreTenantSettingUpdate
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
      // 401 未授权：token 失效或用户被禁用，跳转登录页
      if (response.status === 401) {
        handleUnauthorized(errorMessage)
      }
      throw new Error(errorMessage || '请求失败')
    }
    return result.data
  } catch {
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

// ==================== 门店用户授权分配 ====================

/**
 * 获取门店已分配的用户列表
 * @param storeId 门店ID
 * @returns 已分配用户列表
 */
export async function getStoreAssignedUsers(storeId: string): Promise<TenantUser[]> {
  return request<TenantUser[]>(`${API_BASE}/stores/${storeId}/users`)
}

/**
 * 获取门店可分配用户列表（本租户有效用户 + 标记是否已分配）
 * @param storeId 门店ID
 * @returns 可分配用户列表（含 assigned 标记）
 */
export async function getStoreAvailableUsers(storeId: string): Promise<AvailableUser[]> {
  return request<AvailableUser[]>(`${API_BASE}/stores/${storeId}/available-users`)
}

/**
 * 全量替换门店的用户分配（diff 计算：新增/删除）
 * @param storeId 门店ID
 * @param userIds 最终选中的用户ID列表
 */
export async function assignStoreUsers(storeId: string, userIds: string[]): Promise<void> {
  return request<void>(`${API_BASE}/stores/${storeId}/users`, {
    method: 'POST',
    body: JSON.stringify({ userIds })
  })
}

// ==================== 租户门店设置 ====================

/**
 * 获取当前租户的门店设置（跨店核销等租户级开关）
 * 后端不存在记录时返回默认值（AllowCrossStoreVerify=true），不自动落库
 */
export async function getStoreTenantSetting(): Promise<StoreTenantSetting> {
  return request<StoreTenantSetting>(`${API_BASE}/store-tenant-settings`)
}

/**
 * 更新当前租户的门店设置（不存在时自动创建）
 * @param data 开关配置
 */
export async function updateStoreTenantSetting(data: StoreTenantSettingUpdate): Promise<StoreTenantSetting> {
  return request<StoreTenantSetting>(`${API_BASE}/store-tenant-settings`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}
