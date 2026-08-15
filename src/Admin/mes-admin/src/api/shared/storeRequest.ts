// Store 子系统共享请求工具
// 业务 API 模块（appointment/inventory/order 等）统一使用此请求函数
// 门店档案等管理类 API 走 src/api/store/index.ts 的独立 request 函数，不经过此拦截

import { handleUnauthorized } from './auth'

// 防抖：避免连续请求触发多次跳转
let isRedirectingToNoStore = false

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
 *
 * 声明式门店校验：业务 API 调用此函数即声明需要门店上下文，
 * 未选门店时跳转 /store/no-store，由后端 [AllowWithoutStore] + StoreRequiredActionFilter 兜底。
 */
export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()
  const storeId = getCurrentStoreId()

  // 未选门店：业务 API 声明式校验，跳转提示页（hash 模式不刷新页面）
  if (!storeId) {
    if (!isRedirectingToNoStore) {
      isRedirectingToNoStore = true
      window.location.hash = '/store/no-store'
      // 短暂延时后重置，允许后续再次跳转
      setTimeout(() => { isRedirectingToNoStore = false }, 2000)
    }
    const error = new Error('您尚未被分配到门店，请联系管理员绑定门店后再访问此功能') as Error & { code?: number }
    error.code = 400
    throw error
  }

  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string>)
  }

  // FormData 的 Content-Type 必须由浏览器生成，其中包含分隔各字段的 boundary；
  // 手动写死 multipart/form-data 会丢掉 boundary，后端解析不出任何字段
  if (!(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json'
  }

  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  // 已校验 storeId 必定存在，直接注入
  headers['X-Store-Id'] = storeId

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
      // 401 未授权：token 失效或用户被禁用，跳转登录页
      if (response.status === 401) {
        handleUnauthorized(errorMessage)
      }
      const error = new Error(errorMessage || '请求失败') as Error & { code?: number }
      error.code = errorCode
      throw error
    }
    return result.data
  } catch (err) {
    // 如果已经是带 code 的错误（上方 throw 出来的），直接向上抛
    if (err instanceof Error && typeof (err as Error & { code?: number }).code === 'number') {
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
export function buildQuery(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.append(key, String(value))
    }
  }
  const str = search.toString()
  return str ? `?${str}` : ''
}
