// 系统管理 - API服务
import type {
  User, UserQuery, UserCreate, UserUpdate,
  Role, RoleQuery, RoleCreate, RoleUpdate,
  Menu, MenuQuery, MenuCreate, MenuUpdate,
  Organization, OrganizationCreate, OrganizationUpdate,
  Tenant, TenantCreate, TenantUpdate,
  AuditLog,
  SystemConfig, SystemConfigCreate, SystemConfigUpdate,
  ApiResponse, PagedResponse,
  Subsystem, SubsystemQuery, SubsystemCreate, SubsystemUpdate,
  SubsystemMenuAssign, TenantSubsystemAssign, RoleMenuAssign,
  RoleMenuGrouped,
  CurrentUser
} from './types'
import { handleUnauthorized } from '../shared/auth'

// 导出类型供外部使用
export type {
  User, UserQuery, UserCreate, UserUpdate,
  Role, RoleQuery, RoleCreate, RoleUpdate,
  Menu, MenuQuery, MenuCreate, MenuUpdate,
  Organization, OrganizationCreate, OrganizationUpdate,
  Tenant, TenantCreate, TenantUpdate,
  AuditLog,
  SystemConfig, SystemConfigCreate, SystemConfigUpdate,
  ApiResponse, PagedResponse,
  Subsystem, SubsystemQuery, SubsystemCreate, SubsystemUpdate,
  SubsystemMenuAssign, TenantSubsystemAssign, RoleMenuAssign,
  RoleMenuGrouped,
  CurrentUser
}

// 通过网关访问后端服务
const API_BASE = '/api/system'
const IDENTITY_BASE = '/api/identity'

// 获取token
const getToken = () => localStorage.getItem('token')

// 通用请求方法
async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>)
  }

  // 只有当 token 存在时才添加 Authorization header
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(url, {
    ...options,
    headers: headers as HeadersInit
  })

  // 解析响应，优先获取 JSON 格式的错误信息
  let errorMessage = ''
  try {
    const result: ApiResponse<T> = await response.json()
    // 后端返回的错误信息
    if (result.message) {
      errorMessage = result.message
    }
    // 兼容 code 为字符串或数字的情况
    if (result.code != 200) {
      // 401 未授权：token 失效或用户被禁用，跳转登录页
      if (response.status === 401) {
        handleUnauthorized(errorMessage)
      }
      // 403 时附带后端返回的路径和所需权限，便于定位是哪个接口、缺什么权限
      // path / requiredPermissions 不在 ApiResponse<T> 类型中声明，仅在后端 403 响应中存在
      const extra: string[] = []
      const errorDetail = result as unknown as { path?: string; requiredPermissions?: string[] }
      if (errorDetail.path) extra.push(`接口: ${errorDetail.path}`)
      if (errorDetail.requiredPermissions?.length) {
        extra.push(`需要权限: ${errorDetail.requiredPermissions.join(', ')}`)
      }
      throw new Error(extra.length ? `${errorMessage}（${extra.join('；')}）` : (errorMessage || '请求失败'))
    }
    return result.data
  } catch {
    // 如果已有错误信息，直接抛出
    if (errorMessage) {
      throw new Error(errorMessage)
    }
    // 处理非 JSON 响应
    throw new Error(`请求失败: ${response.status}`)
  }
}

// ==================== 用户管理 ====================

/**
 * 获取用户分页列表
 * @param query 查询参数（用户名、真实姓名、状态、组织ID、租户ID）
 * @returns 分页用户列表
 */
export async function getUsers(query: UserQuery): Promise<PagedResponse<User>> {
  const params = new URLSearchParams()
  if (query.username) params.append('username', query.username)
  if (query.realName) params.append('realName', query.realName)
  if (query.status !== undefined) params.append('status', String(query.status))
  if (query.organizationId) params.append('organizationId', String(query.organizationId))
  // organizationIds: 传递组织ID列表（部门及以下/自定义模式）
  if (query.organizationIds && query.organizationIds.length > 0) {
    query.organizationIds.forEach(id => params.append('organizationIds', String(id)))
  }
  if (query.tenantId) params.append('tenantId', String(query.tenantId))
  if (query.roleId) params.append('roleId', String(query.roleId))
  params.append('pageIndex', String(query?.pageIndex))
  params.append('pageSize', String(query?.pageSize))
  return request<PagedResponse<User>>(`${API_BASE}/users?${params}`)
}

/**
 * 获取用户列表（用于下拉选择，带租户隔离）
 * @param tenantId 租户ID（可选，用于超级管理员按租户筛选）
 * @param realName 姓名筛选（可选）
 * @returns 用户列表
 */
export async function getUserList(tenantId?: number | string, realName?: string): Promise<User[]> {
  const params = new URLSearchParams()
  if (tenantId) params.append('tenantId', String(tenantId))
  if (realName) params.append('realName', realName)
  const queryString = params.toString()
  return request<User[]>(`${API_BASE}/users/all${queryString ? '?' + queryString : ''}`)
}

/**
 * 获取用户详情
 * @param id 用户ID
 * @returns 用户详情
 */
export async function getUser(id: number): Promise<User> {
  return request<User>(`${API_BASE}/users/${id}`)
}

/**
 * 创建用户
 * @param data 用户信息（密码仅创建时需要）
 * @returns 创建后的用户信息
 */
export async function createUser(data: UserCreate): Promise<User> {
  return request<User>(`${API_BASE}/users`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新用户信息
 * @param data 用户信息
 * @returns 更新后的用户信息
 */
export async function updateUser(data: UserUpdate): Promise<User> {
  return request<User>(`${API_BASE}/users/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除用户
 * @param id 用户ID
 */
export async function deleteUser(id: number): Promise<void> {
  return request<void>(`${API_BASE}/users/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除用户
 * @param ids 用户ID数组
 */
export async function deleteUsers(ids: number[]): Promise<void> {
  return request<void>(`${API_BASE}/users/batch`, {
    method: 'DELETE',
    body: JSON.stringify({ ids })
  })
}

/**
 * 重置用户密码（管理员操作）
 * @param id 用户ID
 * @param password 新密码
 */
export async function resetPassword(id: number, password: string): Promise<void> {
  return request<void>(`${API_BASE}/users/${id}/reset-password`, {
    method: 'POST',
    body: JSON.stringify({ Id: id, NewPassword: password })
  })
}

/**
 * 修改当前用户密码
 * @param oldPassword 旧密码
 * @param newPassword 新密码
 */
export async function changePassword(oldPassword: string, newPassword: string): Promise<void> {
  return request<void>(`${API_BASE}/users/change-password`, {
    method: 'POST',
    body: JSON.stringify({ oldPassword, newPassword })
  })
}

// ==================== 角色管理 ====================

/**
 * 获取角色分页列表
 * @param query 查询参数（角色名称、编码、状态）
 * @returns 分页角色列表
 */
export async function getRoles(query: RoleQuery): Promise<PagedResponse<Role>> {
  const params = new URLSearchParams()
  if (query.name) params.append('name', query.name)
  if (query.code) params.append('code', query.code)
  if (query.status !== undefined) params.append('status', String(query.status))
  if (query.tenantId !== undefined) params.append('tenantId', String(query.tenantId))
  params.append('pageIndex', String(query?.pageIndex))
  params.append('pageSize', String(query?.pageSize))
  return request<PagedResponse<Role>>(`${API_BASE}/roles?${params}`)
}

/**
 * 获取所有角色（用于下拉选择）
 * @param tenantId 可选的租户ID，传入则返回该租户的角色
 * @returns 全部角色列表
 */
export async function getAllRoles(tenantId?: number | string): Promise<Role[]> {
  const params = new URLSearchParams()
  if (tenantId !== undefined) {
    params.append('tenantId', String(tenantId))
  }
  const queryString = params.toString()
  return request<Role[]>(`${API_BASE}/roles/all${queryString ? '?' + queryString : ''}`)
}

/**
 * 获取所有角色列表（不过滤租户，用于跨租户场景显示角色名称）
 * @returns 所有角色列表
 */
export async function getAllRolesWithoutFilter(): Promise<Role[]> {
  return request<Role[]>(`${API_BASE}/roles/all-without-filter`)
}

/**
 * 获取角色详情
 * @param id 角色ID
 * @returns 角色详情（含权限信息）
 */
export async function getRole(id: number): Promise<Role> {
  return request<Role>(`${API_BASE}/roles/${id}`)
}

/**
 * 创建角色
 * @param data 角色信息
 * @returns 创建后的角色信息
 */
export async function createRole(data: RoleCreate): Promise<Role> {
  return request<Role>(`${API_BASE}/roles`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新角色
 * @param data 角色信息
 * @returns 更新后的角色信息
 */
export async function updateRole(data: RoleUpdate): Promise<Role> {
  return request<Role>(`${API_BASE}/roles/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除角色
 * @param id 角色ID
 */
export async function deleteRole(id: number): Promise<void> {
  return request<void>(`${API_BASE}/roles/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除角色
 * @param ids 角色ID数组
 */
export async function deleteRoles(ids: number[]): Promise<void> {
  return request<void>(`${API_BASE}/roles/batch`, {
    method: 'DELETE',
    body: JSON.stringify({ ids })
  })
}

// ==================== 菜单管理 ====================

/**
 * 获取菜单树（用于权限分配）
 * @returns 完整菜单树
 */
export async function getMenuTree(): Promise<Menu[]> {
  return request<Menu[]>(`${API_BASE}/menus/tree`)
}

/**
 * 获取菜单列表
 * @param query 查询参数（菜单名称、类型）
 * @returns 菜单列表
 */
export async function getMenus(query: MenuQuery): Promise<Menu[]> {
  const params = new URLSearchParams()
  if (query.name) params.append('name', query.name)
  if (query.type !== undefined) params.append('type', String(query.type))
  return request<Menu[]>(`${API_BASE}/menus?${params}`)
}

/**
 * 获取菜单详情
 * @param id 菜单ID
 * @returns 菜单详情
 */
export async function getMenu(id: number): Promise<Menu> {
  return request<Menu>(`${API_BASE}/menus/${id}`)
}

/**
 * 创建菜单
 * @param data 菜单信息
 * @returns 创建后的菜单信息
 */
export async function createMenu(data: MenuCreate): Promise<Menu> {
  return request<Menu>(`${API_BASE}/menus`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新菜单
 * @param data 菜单信息
 * @returns 更新后的菜单信息
 */
export async function updateMenu(data: MenuUpdate): Promise<Menu> {
  return request<Menu>(`${API_BASE}/menus/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除菜单
 * @param id 菜单ID
 */
export async function deleteMenu(id: number): Promise<void> {
  return request<void>(`${API_BASE}/menus/${id}`, {
    method: 'DELETE'
  })
}

// ==================== 组织管理 ====================

/**
 * 获取组织列表（树形结构）
 * @param query 查询参数（组织名称、类型、状态）
 * @returns 组织树形列表
 */
export async function getOrganizations(query?: { name?: string; status?: number; tenantId?: number | string }): Promise<Organization[]> {
  const params = new URLSearchParams()
  if (query?.name) params.append('name', query.name)
  if (query?.status !== undefined) params.append('status', String(query.status))
  if (query?.tenantId !== undefined) params.append('tenantId', String(query.tenantId))
  return request<Organization[]>(`${API_BASE}/organizations/tree?${params}`)
}

/**
 * 获取组织列表（下拉数据专用，无需组织架构页面权限）
 * 用于用户管理、角色管理、站内信等页面的组织下拉选择
 * @param query 查询参数（组织名称、状态、租户ID）
 * @returns 组织树形列表
 */
export async function getOrganizationOptions(query?: { name?: string; status?: number; tenantId?: number | string }): Promise<Organization[]> {
  const params = new URLSearchParams()
  if (query?.name) params.append('name', query.name)
  if (query?.status !== undefined) params.append('status', String(query.status))
  if (query?.tenantId !== undefined) params.append('tenantId', String(query.tenantId))
  return request<Organization[]>(`${API_BASE}/organizations/options?${params}`)
}

/**
 * 创建组织
 * @param data 组织信息
 * @returns 创建后的组织信息
 */
export async function createOrganization(data: OrganizationCreate): Promise<Organization> {
  return request<Organization>(`${API_BASE}/organizations`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新组织
 * @param data 组织信息
 * @returns 更新后的组织信息
 */
export async function updateOrganization(data: OrganizationUpdate): Promise<Organization> {
  return request<Organization>(`${API_BASE}/organizations/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除组织
 * @param id 组织ID
 */
export async function deleteOrganization(id: number): Promise<void> {
  return request<void>(`${API_BASE}/organizations/${id}`, {
    method: 'DELETE'
  })
}

// ==================== 租户管理 ====================

/**
 * 获取租户分页列表
 * @param query 查询参数（租户名称、编码、状态）
 * @returns 分页租户列表
 */
export async function getTenants(query?: { name?: string; code?: string; status?: number; pageIndex?: number; pageSize?: number }): Promise<PagedResponse<Tenant>> {
  const params = new URLSearchParams()
  if (query?.name) params.append('name', query.name)
  if (query?.code) params.append('code', query.code)
  if (query?.status !== undefined) params.append('status', String(query.status))
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<Tenant>>(`${API_BASE}/tenants?${params}`)
}

/**
 * 获取租户详情
 * @param id 租户ID
 * @returns 租户详情（含统计信息）
 */
export async function getTenant(id: number): Promise<Tenant> {
  return request<Tenant>(`${API_BASE}/tenants/${id}`)
}

/**
 * 创建租户
 * @param data 租户信息
 * @returns 创建后的租户信息
 */
export async function createTenant(data: TenantCreate): Promise<Tenant> {
  return request<Tenant>(`${API_BASE}/tenants`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新租户
 * @param data 租户信息
 * @returns 更新后的租户信息
 */
export async function updateTenant(data: TenantUpdate): Promise<Tenant> {
  return request<Tenant>(`${API_BASE}/tenants/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

// 删除租户
export async function deleteTenant(id: number): Promise<void> {
  return request<void>(`${API_BASE}/tenants/${id}`, {
    method: 'DELETE'
  })
}

// 批量删除租户
export async function deleteTenants(ids: number[]): Promise<void> {
  return request<void>(`${API_BASE}/tenants/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

// ==================== 审计日志管理 ====================

// 获取审计日志列表
export async function getAuditLogs(query?: {
  userName?: string;
  operationType?: string;
  startDate?: string;
  endDate?: string;
  responseStatus?: string;
  tenantId?: number | string;
  pageIndex?: number;
  pageSize?: number
}): Promise<PagedResponse<AuditLog>> {
  const params = new URLSearchParams()
  if (query?.userName) params.append('userName', query.userName)
  if (query?.operationType) params.append('operationType', query.operationType)
  if (query?.startDate) params.append('startDate', query.startDate)
  if (query?.endDate) params.append('endDate', query.endDate)
  if (query?.responseStatus) params.append('responseStatus', query.responseStatus)
  if (query?.tenantId) params.append('tenantId', String(query.tenantId))
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<AuditLog>>(`${API_BASE}/audit-logs?${params}`)
}

// 获取单个审计日志详情
export async function getAuditLogById(id: number): Promise<AuditLog> {
  return request<AuditLog>(`${API_BASE}/audit-logs/${id}`)
}

// 清空过期审计日志（后端根据服务器时间计算删除日期）
export async function clearExpiredAuditLogs(): Promise<number> {
  return request<number>(`${API_BASE}/audit-logs/expired`, {
    method: 'DELETE'
  })
}

// 删除单个审计日志
export async function deleteAuditLog(id: number): Promise<void> {
  return request<void>(`${API_BASE}/audit-logs/${id}`, {
    method: 'DELETE'
  })
}

// ==================== 系统配置管理 ====================

// 获取系统配置列表
export async function getSystemConfigs(query?: { configKey?: string; configGroup?: string; tenantId?: number; pageIndex?: number; pageSize?: number }): Promise<PagedResponse<SystemConfig>> {
  const params = new URLSearchParams()
  if (query?.configKey) params.append('configKey', query.configKey)
  if (query?.configGroup) params.append('configGroup', query.configGroup)
  if (query?.tenantId) params.append('tenantId', String(query.tenantId))
  params.append('pageIndex', String(query?.pageIndex || 1))
  params.append('pageSize', String(query?.pageSize || 20))
  return request<PagedResponse<SystemConfig>>(`${API_BASE}/SystemConfigs?${params}`)
}

// 按分组获取系统配置
export async function getSystemConfigsByGroup(configGroup: string): Promise<SystemConfig[]> {
  return request<SystemConfig[]>(`${API_BASE}/SystemConfigs/group/${configGroup}`)
}

// 获取公开配置（无需登录）
export async function getPublicConfigs(): Promise<SystemConfig[]> {
  return request<SystemConfig[]>(`${API_BASE}/SystemConfigs/public`)
}

// 获取系统配置字典（用于配置应用，不受IsPublic限制）
export async function getSystemConfigDict(): Promise<Record<string, string>> {
  return request<Record<string, string>>(`${API_BASE}/SystemConfigs/system`)
}

// 创建系统配置
export async function createSystemConfig(data: SystemConfigCreate): Promise<SystemConfig> {
  return request<SystemConfig>(`${API_BASE}/SystemConfigs`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// 更新系统配置
export async function updateSystemConfig(data: SystemConfigUpdate): Promise<SystemConfig> {
  return request<SystemConfig>(`${API_BASE}/SystemConfigs/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

// 删除系统配置
export async function deleteSystemConfig(id: number): Promise<void> {
  return request<void>(`${API_BASE}/SystemConfigs/${id}`, {
    method: 'DELETE'
  })
}

// ==================== 个人中心 ====================

// 获取用户个人信息
export async function getUserProfile(): Promise<User> {
  return request<User>(`${API_BASE}/profile`)
}

// 更新个人信息
export async function updateProfile(data: UserUpdate): Promise<User> {
  return request<User>(`${API_BASE}/profile`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

// 修改个人密码
export async function changeProfilePassword(oldPassword: string, newPassword: string): Promise<void> {
  return request<void>(`${API_BASE}/profile/password`, {
    method: 'PUT',
    body: JSON.stringify({ oldPassword, newPassword })
  })
}

// 更新头像
export async function updateAvatar(avatar: string): Promise<User> {
  return request<User>(`${API_BASE}/profile/avatar`, {
    method: 'PUT',
    body: JSON.stringify({ avatar })
  })
}

// ==================== 认证相关 ====================

// 登录请求类型
export interface LoginRequest {
  username: string
  password: string
}

// 登录响应类型
export interface LoginResponse {
  access_token: string
  token_type: string
  expires_in: number
  refresh_token?: string
}

// 登录
export async function login(username: string, password: string): Promise<LoginResponse> {
  const formData = new URLSearchParams()
  formData.append('grant_type', 'password')
  formData.append('username', username)
  formData.append('password', password)

  const response = await fetch(`${IDENTITY_BASE}/connect/token`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded'
    },
    body: formData
  })

  if (!response.ok) {
    // 先读取响应文本
    const responseText = await response.text()

    // 尝试解析为 JSON
    let errorData: Record<string, unknown> = {}
    try {
      errorData = JSON.parse(responseText) as Record<string, unknown>
    } catch {
      // 响应不是 JSON，使用默认错误
    }

    // 尝试各种可能的字段名
    const msg = String(errorData.error_description || errorData.errorDescription || errorData.message || errorData.msg || '登录失败')

    throw new Error(msg)
  }

  return response.json()
}

// 获取当前用户信息
export async function getCurrentUser(): Promise<CurrentUser> {
  return request<CurrentUser>(`${API_BASE}/auth/me`)
}

// ==================== 审计日志 ====================

/**
 * 审计日志请求类型（登录/登出）
 */
export interface AuditLogRequest {
  operationType: string
  userId: number
  userName?: string
  realName?: string
  tenantId?: number
  requestIp?: string
  userAgent?: string
  responseStatus: number
  requestPath?: string
}

/**
 * 记录审计日志（登录/登出）
 * @param data 审计日志信息
 */
export async function recordAuditLog(data: AuditLogRequest): Promise<void> {
  return request<void>(`${API_BASE}/audit-logs`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// ==================== 子系统管理 ====================

/**
 * 获取子系统列表
 * @param query 查询参数（子系统名称、编码、状态）
 * @returns 子系统列表
 */
export async function getSubsystems(query?: SubsystemQuery): Promise<Subsystem[]> {
  const params = new URLSearchParams()
  if (query?.name) params.append('name', query.name)
  if (query?.code) params.append('code', query.code)
  if (query?.status !== undefined) params.append('status', String(query.status))
  return request<Subsystem[]>(`${API_BASE}/subsystems?${params}`)
}

/**
 * 获取所有启用的子系统（用于下拉选择）
 * @returns 全部启用的子系统列表
 */
export async function getAllSubsystems(): Promise<Subsystem[]> {
  return request<Subsystem[]>(`${API_BASE}/subsystems/all`)
}

/**
 * 获取所有子系统（不分启用/禁用状态，用于租户子系统分配）
 * @returns 全部子系统列表
 */
export async function getSubsystemsAll(): Promise<Subsystem[]> {
  return request<Subsystem[]>(`${API_BASE}/subsystems/list-all`)
}

/**
 * 获取子系统详情
 * @param id 子系统ID
 * @returns 子系统详情
 */
export async function getSubsystem(id: number): Promise<Subsystem> {
  return request<Subsystem>(`${API_BASE}/subsystems/${id}`)
}

/**
 * 获取子系统使用情况
 * @param id 子系统ID
 * @returns 使用该子系统的租户数量
 */
export async function getSubsystemUsage(id: number): Promise<number> {
  return request<number>(`${API_BASE}/subsystems/${id}/usage`)
}

/**
 * 创建子系统
 * @param data 子系统信息
 * @returns 创建后的子系统信息
 */
export async function createSubsystem(data: SubsystemCreate): Promise<Subsystem> {
  return request<Subsystem>(`${API_BASE}/subsystems`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新子系统
 * @param data 子系统信息
 * @returns 更新后的子系统信息
 */
export async function updateSubsystem(data: SubsystemUpdate): Promise<Subsystem> {
  return request<Subsystem>(`${API_BASE}/subsystems/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除子系统
 * @param id 子系统ID
 */
export async function deleteSubsystem(id: number): Promise<void> {
  return request<void>(`${API_BASE}/subsystems/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 获取子系统关联的菜单ID列表
 * @param id 子系统ID
 * @returns 菜单ID列表
 */
export async function getSubsystemMenus(id: number | string): Promise<number[]> {
  return request<number[]>(`${API_BASE}/subsystems/${id}/menus`)
}

/**
 * 分配菜单给子系统
 * @param id 子系统ID
 * @param data 菜单ID列表
 */
export async function assignSubsystemMenus(id: number, data: SubsystemMenuAssign): Promise<void> {
  return request<void>(`${API_BASE}/subsystems/${id}/menus`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

// ==================== 租户子系统分配 ====================

/**
 * 获取租户的子系统ID列表
 * @param tenantId 租户ID
 * @returns 子系统ID列表
 */
export async function getTenantSubsystems(tenantId: number): Promise<number[]> {
  return request<number[]>(`${API_BASE}/tenants/${tenantId}/subsystems`)
}

/**
 * 为租户分配子系统（替换式）
 * @param tenantId 租户ID
 * @param data 子系统ID列表
 */
export async function assignTenantSubsystems(tenantId: number, data: TenantSubsystemAssign): Promise<void> {
  return request<void>(`${API_BASE}/tenants/${tenantId}/subsystems`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 为租户添加单个子系统
 * @param tenantId 租户ID
 * @param subsystemId 子系统ID
 */
export async function addTenantSubsystem(tenantId: number, subsystemId: number): Promise<void> {
  return request<void>(`${API_BASE}/tenants/${tenantId}/subsystems/${subsystemId}`, {
    method: 'POST'
  })
}

/**
 * 批量为租户添加子系统
 * @param tenantId 租户ID
 * @param data 子系统ID列表
 */
export async function batchAddTenantSubsystems(tenantId: number, data: TenantSubsystemAssign): Promise<void> {
  return request<void>(`${API_BASE}/tenants/${tenantId}/subsystems/batch`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 移除租户的单个子系统
 * @param tenantId 租户ID
 * @param subsystemId 子系统ID
 */
export async function removeTenantSubsystem(tenantId: number, subsystemId: number): Promise<void> {
  return request<void>(`${API_BASE}/tenants/${tenantId}/subsystems/${subsystemId}`, {
    method: 'DELETE'
  })
}

/**
 * 批量移除租户的子系统
 * @param tenantId 租户ID
 * @param data 子系统ID列表
 */
export async function batchRemoveTenantSubsystems(tenantId: number, data: TenantSubsystemAssign): Promise<void> {
  return request<void>(`${API_BASE}/tenants/${tenantId}/subsystems`, {
    method: 'DELETE',
    body: JSON.stringify(data)
  })
}

// ==================== 角色菜单权限 ====================

/**
 * 获取角色的菜单权限ID列表
 * @param roleId 角色ID
 * @returns 菜单ID列表
 */
export async function getRoleMenuAuths(roleId: number): Promise<number[]> {
  return request<number[]>(`${API_BASE}/roles/${roleId}/menus/auth`)
}

/**
 * 获取按子系统分组的角色菜单权限
 * @param roleId 角色ID
 * @returns 按子系统分组的菜单权限
 */
export async function getRoleMenuAuthsGrouped(roleId: number): Promise<RoleMenuGrouped[]> {
  return request<RoleMenuGrouped[]>(`${API_BASE}/roles/${roleId}/menus/auth/grouped`)
}

/**
 * 为角色分配菜单权限（替换式）
 * @param roleId 角色ID
 * @param data 菜单ID列表
 */
export async function assignRoleMenuAuths(roleId: number, data: RoleMenuAssign): Promise<void> {
  return request<void>(`${API_BASE}/roles/${roleId}/menus/auth`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 移除角色的单个菜单权限
 * @param roleId 角色ID
 * @param menuId 菜单ID
 */
export async function removeRoleMenuAuth(roleId: number, menuId: number): Promise<void> {
  return request<void>(`${API_BASE}/roles/${roleId}/menus/auth/${menuId}`, {
    method: 'DELETE'
  })
}
