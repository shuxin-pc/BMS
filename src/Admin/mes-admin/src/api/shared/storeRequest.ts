// Store 子系统共享请求工具
// 所有 store 子系统的 API 模块（appointment/inventory/order 等）统一使用此请求函数
// 自动注入 Authorization token 和 X-Store-Id 请求头

/** 通用 API 响应封装（与后端 ApiResponseDto 对齐） */
export interface ApiResponse<T> {
  code: number
  message?: string
  data: T
}

/** 通用分页响应（与后端 PagedResponseDto 对齐） */
export interface PagedResponse<T> {
  list: T[]
  total: number
  pageIndex: number
  pageSize: number
}

// 通过网关访问后端服务
const API_BASE = '/api/store'

// 获取 token
const getToken = () => localStorage.getItem('token')

// 获取当前选择的门店 ID（用于 X-Store-Id 请求头）
const getCurrentStoreId = () => localStorage.getItem('currentStoreId')

/**
 * 通用请求方法
 * 自动携带 Authorization 和 X-Store-Id 请求头，解包 ApiResponse 返回 data
 */
export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>)
  }

  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  // 所有 store 子系统请求自动携带 X-Store-Id（若已选择门店）
  const storeId = getCurrentStoreId()
  if (storeId) {
    headers['X-Store-Id'] = storeId
  }

  const url = path.startsWith('http') ? path : `${API_BASE}${path}`
  const response = await fetch(url, {
    ...options,
    headers: headers as HeadersInit
  })

  let errorMessage = ''
  let errorCode: number | undefined
  try {
    const result: ApiResponse<T> = await response.json()
    if (result.message) {
      errorMessage = result.message
    }
    if (result.code !== 200) {
      errorCode = result.code
      const error = new Error(errorMessage || '请求失败') as Error & { code?: number }
      error.code = errorCode
      throw error
    }
    return result.data
  } catch (err: any) {
    // 如果已经是带 code 的错误（上方 throw 出来的），直接向上抛
    if (err && typeof err.code === 'number') {
      throw err
    }
    if (errorMessage) {
      const error = new Error(errorMessage) as Error & { code?: number }
      error.code = errorCode
      throw error
    }
    throw new Error(`请求失败: ${response.status}`)
  }
}

/** 构建查询参数字符串 */
export function buildQuery(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.append(key, String(value))
    }
  }
  const str = search.toString()
  return str ? `?${str}` : ''
}
