import { ElMessage } from 'element-plus'

// 避免 401 响应重复跳转登录页（多个并发请求可能同时返回错误）
let isRedirecting = false

/**
 * 处理 401 未授权响应（用户被禁用或 token 失效）
 * 清除登录状态并跳转到登录页
 *
 * 使用场景：各 API 模块的 request 函数检测到 401 响应时调用
 * 后端触发场景：
 * - UserStatusCheckMiddleware 检测到用户被禁用（L2 修复）
 * - token 过期或无效
 *
 * 注意：403（无权限）不应调用此函数。403 表示用户已认证但无权访问特定接口，
 * 重新登录不会改变权限，跳转登录页会导致无限循环。
 * 403 应走正常错误抛出路径，由调用方 catch 处理。
 */
export function handleUnauthorized(message?: string): void {
  // 避免多个并发请求同时返回 401 导致重复跳转
  if (isRedirecting) return
  isRedirecting = true

  const msg = message || '登录已失效，请重新登录'

  // 显示提示（延迟跳转，让用户看到提示）
  ElMessage.error(msg)

  // 清除登录状态（与 user.ts logout 保持一致）
  localStorage.removeItem('token')
  localStorage.removeItem('tenantId')
  localStorage.removeItem('tenantCode')
  localStorage.removeItem('currentSubsystemId')
  localStorage.removeItem('currentStoreId')

  // 延迟 1.5 秒跳转，让用户看到错误提示
  setTimeout(() => {
    window.location.href = '/login'
    isRedirecting = false
  }, 1500)
}
