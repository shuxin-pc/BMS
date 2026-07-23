// ==========================================
// 子系统管理类型定义
// ==========================================

/**
 * 子系统状态
 * - 0: 禁用
 * - 1: 启用
 */
export type SubsystemStatus = number

/**
 * 当前用户信息（用于 /auth/me 接口）
 */
export interface CurrentUser {
  id: number
  userName: string
  realName?: string
  email?: string
  phone?: string
  avatar?: string
  tenantId: number
  tenantCode?: string
  roles: string[]
  roleIds: number[]
  permissions: string[]
  /** 当前用户最高角色等级（数字越小权限越大，无角色时为 100） */
  maxRoleLevel?: number
}

/**
 * 子系统信息
 */
export interface Subsystem {
  /** 子系统ID */
  id: number
  /** 子系统编码（唯一） */
  code: string
  /** 子系统名称 */
  name: string
  /** 子系统图标 */
  icon?: string
  /** 子系统描述 */
  description?: string
  /** 排序号 */
  sort?: number
  /** 状态：0-禁用，1-启用 */
  status: SubsystemStatus
  /** 创建时间 */
  createdTime: string
  /** 更新时间 */
  updatedTime?: string
}

/**
 * 子系统查询参数
 */
export interface SubsystemQuery {
  /** 子系统名称（模糊匹配） */
  name?: string
  /** 子系统编码（模糊匹配） */
  code?: string
  /** 状态筛选 */
  status?: SubsystemStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建子系统请求
 */
export interface SubsystemCreate {
  /** 子系统编码（唯一） */
  code: string
  /** 子系统名称 */
  name: string
  /** 子系统图标 */
  icon?: string
  /** 子系统描述 */
  description?: string
  /** 排序号 */
  sort?: number
  /** 状态：0-禁用，1-启用 */
  status?: SubsystemStatus
}

/**
 * 更新子系统请求
 */
export interface SubsystemUpdate {
  /** 子系统ID */
  id: number
  /** 子系统名称 */
  name: string
  /** 子系统图标 */
  icon?: string
  /** 子系统描述 */
  description?: string
  /** 排序号 */
  sort?: number
  /** 状态：0-禁用，1-启用 */
  status?: SubsystemStatus
}

/**
 * 子系统菜单分配请求
 */
export interface SubsystemMenuAssign {
  /** 菜单ID列表 */
  menuIds: number[]
}

/**
 * 租户子系统分配请求
 */
export interface TenantSubsystemAssign {
  /** 子系统ID列表 */
  subsystemIds: number[]
  /** 后端期望的字段名 */
  SubsystemIds?: number[]
}

/**
 * 角色菜单权限分配请求
 */
export interface RoleMenuAssign {
  /** 菜单ID列表 */
  menuIds: number[]
}

/**
 * 按子系统分组的角色菜单权限
 */
export interface RoleMenuGrouped {
  /** 子系统ID */
  subsystemId: number
  /** 子系统编码 */
  subsystemCode: string
  /** 子系统名称 */
  subsystemName: string
  /** 子系统图标 */
  subsystemIcon?: string
  /** 菜单树 */
  menus: Menu[]
  /** 已选中的菜单ID列表 */
  selectedMenuIds: number[]
}

// ==========================================
// 系统管理 - 类型定义
// ==========================================

/**
 * 用户状态
 * - 0: 禁用
 * - 1: 启用
 */
export type UserStatus = number

/**
 * 用户信息
 */
export interface User {
  /** 用户ID */
  id: number
  /** 用户名（登录账号） */
  userName: string
  /** 真实姓名 */
  realName: string
  /** 邮箱 */
  email: string
  /** 手机号 */
  phone: string
  /** 头像URL */
  avatar?: string
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 组织ID */
  organizationId?: number
  /** 组织名称 */
  organizationName?: string
  /** 租户ID */
  tenantId?: number | string
  /** 租户名称 */
  tenantName?: string
  /** 角色列表 */
  roles?: Role[]
  /** 角色ID列表 */
  roleIds?: number[]
  /** 角色名称（用于列表显示，多个角色用逗号分隔） */
  roleNames?: string
  /** 创建时间 */
  createdTime: string
  /** 更新时间 */
  updatedTime?: string
  /** 最后登录时间 */
  lastLoginTime?: string
  /** 最后登录IP */
  lastLoginIp?: string
}

/**
 * 用户查询参数
 */
export interface UserQuery {
  /** 用户名（模糊匹配） */
  username?: string
  /** 真实姓名（模糊匹配） */
  realName?: string
  /** 状态筛选 */
  status?: UserStatus
  /** 组织ID筛选（单个，包含子组织） */
  organizationId?: number
  /** 组织ID列表筛选（部门及以下/自定义模式） */
  organizationIds?: number[]
  /** 租户ID筛选 */
  tenantId?: number | string
  /** 角色ID筛选 */
  roleId?: number
  /** 页码 */
  pageIndex: number
  /** 每页条数 */
  pageSize: number
}

/**
 * 创建用户请求
 */
export interface UserCreate {
  /** 用户名（登录账号，创建后不可修改） */
  userName: string
  /** 密码（首次创建时必填） */
  password: string
  /** 真实姓名 */
  realName: string
  /** 邮箱 */
  email: string
  /** 手机号 */
  phone: string
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 组织ID */
  organizationId?: number
  /** 租户ID（仅超级管理员可设置） */
  tenantId?: string
  /** 角色ID列表 */
  roleIds: number[]
}

/**
 * 更新用户请求
 */
export interface UserUpdate {
  /** 用户ID */
  id: number
  /** 用户名（不可修改） */
  userName: string
  /** 真实姓名 */
  realName: string
  /** 邮箱 */
  email: string
  /** 手机号 */
  phone: string
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 组织ID */
  organizationId?: number
  /** 租户ID（仅超级管理员可设置） */
  tenantId?: string
  /** 角色ID列表 */
  roleIds: number[]
}

/**
 * 数据权限范围
 * - 1: 全部数据（查看角色同租户所有数据）
 * - 2: 部门及以下（查看本部门及下属部门）
 * - 3: 仅本人（只看自己创建的）
 * - 4: 自定义（指定具体组织ID）
 */
export type DataScope = number

/**
 * 角色信息
 */
export interface Role {
  /** 角色ID */
  id: number
  /** 角色编码（唯一） */
  code: string
  /** 角色名称 */
  name: string
  /** 角色描述 */
  description?: string
  /** 租户ID */
  tenantId?: number | string
  /** 数据权限范围类型 */
  dataScopeType: DataScope
  /** 自定义数据权限的组织ID列表（后端返回逗号分隔字符串如 "1,2,3"） */
  customOrganizationIds?: string | number[]
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 角色等级（2-99，数字越小权限越大；0/1 为系统保留角色） */
  level: number
  /** 权限列表 */
  permissions?: Permission[]
  /** 创建时间 */
  createdTime: string
  /** 更新时间 */
  updatedTime?: string
}

/**
 * 角色查询参数
 */
export interface RoleQuery {
  /** 角色名称（模糊匹配） */
  name?: string
  /** 角色编码（模糊匹配） */
  code?: string
  /** 状态筛选 */
  status?: UserStatus
  /** 租户ID筛选 */
  tenantId?: number | string
  /** 页码 */
  pageIndex: number
  /** 每页条数 */
  pageSize: number
}

/**
 * 创建角色请求
 */
export interface RoleCreate {
  /** 角色编码（唯一） */
  code: string
  /** 角色名称 */
  name: string
  /** 角色描述 */
  description?: string
  /** 数据权限范围类型 */
  dataScopeType: DataScope
  /** 自定义数据权限的组织ID列表 */
  customOrganizationIds?: string[]
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 角色等级（2-99，数字越小权限越大；0/1 为系统保留角色） */
  level: number
  /** 权限ID列表 */
  permissionIds: number[]
}

/**
 * 更新角色请求
 */
export interface RoleUpdate {
  /** 角色ID */
  id: number
  /** 角色编码（不可修改） */
  code: string
  /** 角色名称 */
  name: string
  /** 角色描述 */
  description?: string
  /** 数据权限范围类型 */
  dataScopeType: DataScope
  /** 自定义数据权限的组织ID列表 */
  customOrganizationIds?: string[]
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 角色等级（2-99，数字越小权限越大；0/1 为系统保留角色） */
  level: number
  /** 权限ID列表 */
  permissionIds: number[]
}

// ==========================================
// 菜单相关类型定义
// ==========================================

/**
 * 菜单类型
 * - 0: 目录
 * - 1: 菜单
 * - 2: 按钮
 */
export type MenuType = number

/**
 * 菜单信息
 */
export interface Menu {
  /** 菜单ID */
  id: number
  /** 父菜单ID */
  parentId?: number | null
  /** 菜单名称 */
  name: string
  /** 路由路径 */
  path?: string
  /** 组件路径 */
  component?: string
  /** 菜单权限编码 */
  code: string
  /** 菜单图标 */
  icon?: string
  /** 菜单类型：0-目录，1-菜单，2-按钮 */
  type: MenuType
  /** 排序号 */
  sort?: number
  /** 状态：0-禁用，1-启用 */
  status?: UserStatus
  /** 是否显示 */
  isVisible?: number | boolean
  /** 是否缓存 */
  isCache?: number | boolean
  /** 是否固定在标签栏 */
  isAffix?: number
  /** 是否总是显示（目录类型） */
  isAlwaysShow?: boolean
  /** 权限编码（按钮类型使用） */
  permissionCode?: string
  /** 权限字符串（按钮类型使用） */
  permission?: string
  /** 子菜单列表 */
  children?: Menu[]
  /** 创建时间 */
  createdAt?: string
  updatedAt?: string
}

/**
 * 菜单查询参数
 */
export interface MenuQuery {
  /** 菜单名称（模糊匹配） */
  name?: string
  /** 菜单类型筛选 */
  type?: MenuType
}

/**
 * 创建菜单请求
 */
export interface MenuCreate {
  /** 父菜单ID（顶级菜单为0） */
  parentId: number
  /** 菜单名称 */
  name: string
  /** 路由路径 */
  path: string
  /** 组件路径（菜单类型必填） */
  component?: string
  /** 菜单权限编码（唯一） */
  code: string
  /** 菜单图标 */
  icon?: string
  /** 菜单类型 */
  type: MenuType
  /** 排序号 */
  sort: number
  /** 是否显示：0-隐藏，1-显示 */
  isVisible: number
  /** 是否缓存：0-不缓存，1-缓存 */
  isCache: number
  /** 是否固定在标签栏：0-不固定，1-固定 */
  isAffix: number
  /** 权限字符串（按钮类型使用） */
  permission?: string
}

/**
 * 更新菜单请求
 */
export interface MenuUpdate extends MenuCreate {
  /** 菜单ID */
  id: number
}

// ==========================================
// 权限相关类型定义
// ==========================================

/**
 * 权限信息
 */
/**
 * 权限信息
 */
export interface Permission {
  /** 权限ID */
  id: number
  /** 权限编码（唯一） */
  code: string
  /** 权限名称 */
  name: string
  /** 权限类型 */
  type: number
  /** 权限描述 */
  description?: string
  /** 创建时间 */
  createdAt: string
}

// ==========================================
// 统一响应类型
// ==========================================

/**
 * API统一响应结构
 */
export interface ApiResponse<T = any> {
  /** 状态码：200-成功，其他-失败 */
  code: number
  /** 响应消息 */
  message: string
  /** 响应数据 */
  data: T
}

/**
 * 分页响应结构
 */
export interface PagedResponse<T> {
  /** 数据列表 */
  list: T[]
  /** 总记录数 */
  total: number
  /** 当前页码 */
  pageIndex: number
  /** 每页条数 */
  pageSize: number
}

// ==========================================
// 组织相关类型定义
// ==========================================

/**
 * 组织类型
 */
export type OrganizationType = 'company' | 'department' | 'group'

/**
 * 组织信息
 */
export interface Organization {
  /** 组织ID */
  id: number
  /** 父组织ID（顶级组织为0） */
  parentId: number
  /** 组织名称 */
  name: string
  /** 组织编码（唯一） */
  code: string
  /** 组织类型：company-公司，department-部门，group-小组 */
  type: OrganizationType
  /** 负责人姓名 */
  managerName?: string
  /** 负责人ID */
  managerId?: number
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 排序号 */
  sort: number
  /** 租户ID */
  tenantId?: number
  /** 子组织列表（树形结构） */
  children?: Organization[]
  /** 创建时间 */
  createdTime: string
  /** 更新时间 */
  updatedTime?: string
  /** 是否匹配搜索条件 */
  isMatched?: boolean
}

/**
 * 创建组织请求
 */
export interface OrganizationCreate {
  /** 父组织ID（顶级组织为0） */
  parentId: number
  /** 组织名称 */
  name: string
  /** 组织编码（唯一） */
  code: string
  /** 组织类型 */
  type: OrganizationType
  /** 负责人ID */
  managerId?: number
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 排序号 */
  sort: number
  /** 租户ID */
  tenantId: number
}

/**
 * 更新组织请求
 */
export interface OrganizationUpdate {
  /** 组织ID */
  id: number
  /** 父组织ID（undefined/null表示顶级组织） */
  parentId?: number
  /** 组织名称 */
  name: string
  /** 组织编码（不可修改） */
  code: string
  /** 组织类型 */
  type: OrganizationType
  /** 负责人ID */
  managerId?: number
  /** 状态：0-禁用，1-启用 */
  status: UserStatus
  /** 排序号 */
  sort: number
}

// ==========================================
// 租户相关类型定义
// ==========================================

/**
 * 租户隔离级别
 * - 1: Row（行级隔离）- 共享数据库、共享Schema，通过租户ID区分
 * - 2: Schema（Schema级隔离）- 共享数据库、独立Schema
 * - 3: Database（数据库级隔离）- 独立数据库
 */
export type TenantIsolationLevel = 1 | 2 | 3

/**
 * 租户信息（列表和详情使用）
 */
export interface Tenant {
  /** 租户ID */
  id: number
  /** 租户名称 */
  name: string
  /** 租户编码（唯一） */
  code: string
  /** 联系人 */
  contactName: string
  /** 联系电话 */
  contactPhone: string
  /** 联系人邮箱 */
  contactEmail: string
  /** 状态：1-启用，0-禁用 */
  status: number
  /** 隔离级别 */
  isolationLevel: TenantIsolationLevel
  /** 数据库连接字符串（数据库级隔离使用） */
  connectionString?: string
  /** Schema名称（Schema级隔离使用） */
  schemaName?: string
  /** 是否启用（与status等效，用于兼容前端） */
  isEnabled?: boolean
  /** 到期时间 */
  expireTime?: string
  /** 允许访问的子系统（逗号分隔） */
  allowedSubsystems?: string
  /** 允许访问的子系统列表 */
  allowedSubsystemList?: string[]
  /** 用户数量（详情统计） */
  userCount?: number
  /** 组织数量（详情统计） */
  orgCount?: number
  /** 数据占用（详情统计） */
  dataSize?: number
  /** 配置项数（详情统计） */
  configCount?: number
  /** 创建时间 */
  createdAt: string
  /** 备注 */
  remark?: string
}

/**
 * 创建租户请求
 */
export interface TenantCreate {
  /** 租户编码（唯一） */
  code: string
  /** 租户名称 */
  name: string
  /** 联系人 */
  contactName: string
  /** 联系电话 */
  contactPhone: string
  /** 联系人邮箱 */
  contactEmail: string
  /** 状态：1-启用，0-禁用 */
  status: number
  /** 隔离级别 */
  isolationLevel: TenantIsolationLevel
  /** 数据库连接字符串（数据库级隔离使用） */
  connectionString?: string
  /** Schema名称（Schema级隔离使用，默认使用租户编码小写） */
  schemaName?: string
  /** 是否启用 */
  isEnabled?: boolean
  /** 到期时间 */
  expireTime?: string
  /** 允许访问的子系统（逗号分隔，空表示所有子系统） */
  allowedSubsystems?: string
  /** 备注 */
  remark?: string
}

/**
 * 更新租户请求
 */
export interface TenantUpdate {
  /** 租户ID */
  id: number
  /** 租户名称 */
  name: string
  /** 联系人 */
  contactName: string
  /** 联系电话 */
  contactPhone: string
  /** 联系人邮箱 */
  contactEmail: string
  /** 状态：1-启用，0-禁用 */
  status: number
  /** 隔离级别 */
  isolationLevel: TenantIsolationLevel
  /** 数据库连接字符串（数据库级隔离使用） */
  connectionString?: string
  /** Schema名称（Schema级隔离使用） */
  schemaName?: string
  /** 是否启用 */
  isEnabled?: boolean
  /** 到期时间 */
  expireTime?: string
  /** 允许访问的子系统（逗号分隔，空表示所有子系统） */
  allowedSubsystems?: string
  /** 备注 */
  remark?: string
}

// 审计日志相关
export interface AuditLog {
  id: number
  tenantId?: number
  userId?: number
  userName?: string
  realName?: string
  operationType: string
  operationContent?: string
  requestPath?: string
  requestMethod?: string
  requestIp?: string
  userAgent?: string
  responseStatus?: number
  duration?: number
  errorMessage?: string
  /** 实体变更内容 */
  entityChanges?: string
  createdTime: string
}

// 系统配置相关
export interface SystemConfig {
  id: number
  tenantId?: number
  tenantCode?: string
  configKey: string
  configValue: string
  configGroup: string
  description?: string
  isPublic: boolean
  isEditable: boolean
  isSystem: boolean
  sort: number
  createdAt: string
}

export interface SystemConfigCreate {
  configKey: string
  configValue: string
  configGroup: string
  description?: string
  isPublic: boolean
  isEditable: boolean
  sort: number
}

export interface SystemConfigUpdate {
  id: number
  configKey: string
  configValue: string
  configGroup: string
  description?: string
  isPublic: boolean
  isEditable: boolean
  sort: number
}
