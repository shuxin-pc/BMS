/**
 * 消息 API 服务
 */
import type {
  MessageInboxQuery,
  MessageInboxItem,
  MessageSentQuery,
  MessageSentItem,
  MessageSendDto,
  MessageReadStats,
  PagedResponse
} from './types'
import { handleUnauthorized } from '../shared/auth'

const API_BASE = '/api/system/messages'
const getToken = () => localStorage.getItem('token')

/**
 * 通用请求方法（与 api/system/index.ts 模式一致）
 */
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
    const result = await response.json()
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

/**
 * 构建查询参数
 */
function buildQuery<T extends object>(params: T): string {
  const searchParams = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.append(key, String(value))
    }
  })
  const qs = searchParams.toString()
  return qs ? `?${qs}` : ''
}

// ========================================================
// 用户端 API
// ========================================================

/**
 * 收件箱列表
 */
export function getInbox(query: MessageInboxQuery): Promise<PagedResponse<MessageInboxItem>> {
  return request<PagedResponse<MessageInboxItem>>(`${API_BASE}/inbox${buildQuery(query)}`)
}

/**
 * 未读消息数
 */
export function getUnreadCount(): Promise<number> {
  return request<number>(`${API_BASE}/unread-count`)
}

/**
 * 消息详情
 */
export function getMessage(recipientId: number): Promise<MessageInboxItem> {
  return request<MessageInboxItem>(`${API_BASE}/${recipientId}`)
}

/**
 * 标记已读
 */
export function markAsRead(recipientId: number): Promise<boolean> {
  return request<boolean>(`${API_BASE}/${recipientId}/read`, { method: 'PUT' })
}

/**
 * 批量已读
 */
export function batchMarkAsRead(ids: number[]): Promise<boolean> {
  return request<boolean>(`${API_BASE}/batch-read`, {
    method: 'PUT',
    body: JSON.stringify({ ids })
  })
}

/**
 * 全部标记已读
 */
export function markAllAsRead(): Promise<boolean> {
  return request<boolean>(`${API_BASE}/read-all`, { method: 'PUT' })
}

/**
 * 删除消息
 */
export function deleteMessage(recipientId: number): Promise<boolean> {
  return request<boolean>(`${API_BASE}/${recipientId}`, { method: 'DELETE' })
}

/**
 * 批量删除
 */
export function batchDelete(ids: number[]): Promise<boolean> {
  return request<boolean>(`${API_BASE}/batch`, {
    method: 'DELETE',
    body: JSON.stringify({ ids })
  })
}

// ========================================================
// 管理端 API
// ========================================================

/**
 * 消息发送记录列表
 */
export function getSentList(query: MessageSentQuery): Promise<PagedResponse<MessageSentItem>> {
  return request<PagedResponse<MessageSentItem>>(`${API_BASE}${buildQuery(query)}`)
}

/**
 * 发送消息
 */
export function sendMessage(dto: MessageSendDto): Promise<MessageSentItem> {
  return request<MessageSentItem>(`${API_BASE}/send`, {
    method: 'POST',
    body: JSON.stringify(dto)
  })
}

/**
 * 撤回消息
 */
export function recallMessage(messageId: number): Promise<boolean> {
  return request<boolean>(`${API_BASE}/${messageId}/recall`, { method: 'DELETE' })
}

/**
 * 消息已读/未读统计及未读人员名单
 */
export function getReadStats(messageId: number): Promise<MessageReadStats> {
  return request<MessageReadStats>(`${API_BASE}/${messageId}/read-stats`)
}
