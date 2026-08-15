import { defineStore } from 'pinia'
import { getMenuTree, login, getCurrentUser } from '@/api/system'
import { getAuthorizedStores as fetchAuthorizedStores } from '@/api/store'
import type { Store } from '@/api/store/types'
import type { Menu, Subsystem } from '@/api/system/types'

export interface UserInfo {
  id: number
  userName: string
  realName: string
  avatar: string
  email: string
  phone: string
  roles: string[]
  roleIds: number[]
  permissions: string[]
  tenantId?: number | string
  tenantCode?: string
  /** 当前用户最高角色等级（数字越小权限越大，无角色时为 100） */
  maxRoleLevel?: number
}

export const useUserStore = defineStore('user', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    userInfo: <UserInfo>{
      id: 0,
      userName: '',
      realName: '',
      avatar: '',
      email: '',
      phone: '',
      roles: [],
      roleIds: [],
      permissions: [],
      tenantId: localStorage.getItem('tenantId') || 0,
      tenantCode: localStorage.getItem('tenantCode') || undefined,
      maxRoleLevel: 100
    },
    // 用户授权的子系统列表
    authorizedSubsystems: [] as Subsystem[],
    // 当前选中的子系统ID（snowflake ID 超过 JS 安全整数范围，必须用 string 存储）
    currentSubsystemId: localStorage.getItem('currentSubsystemId') || '',
    // 当前租户下授权的门店列表（仅 store 子系统使用）
    authorizedStores: [] as Store[],
    // 当前选中的门店ID（string 存储，与 currentSubsystemId 一致）
    currentStoreId: localStorage.getItem('currentStoreId') || '',
    // 菜单数据
    menus: [
      {
        id: 2,
        parentId: 0,
        name: '系统管理',
        path: 'system',
        icon: 'Setting',
        type: 1,
        children: [
          {
            id: 21,
            parentId: 2,
            name: '用户管理',
            path: '/system/users',
            icon: 'User',
            type: 2,
            code: 'system:user:list'
          },
          {
            id: 22,
            parentId: 2,
            name: '角色管理',
            path: '/system/roles',
            icon: 'UserFilled',
            type: 2,
            code: 'system:role:list'
          },
          {
            id: 23,
            parentId: 2,
            name: '菜单管理',
            path: '/system/menus',
            icon: 'Menu',
            type: 2,
            code: 'system:menu:list'
          },
          {
            id: 24,
            parentId: 2,
            name: '子系统管理',
            path: '/system/subsystems',
            icon: 'Grid',
            type: 2,
            code: 'system:subsystem:list'
          },
          {
            id: 25,
            parentId: 2,
            name: '组织架构',
            path: '/system/organizations',
            icon: 'OfficeBuilding',
            type: 2,
            code: 'system:org:list'
          },
          {
            id: 26,
            parentId: 2,
            name: '租户管理',
            path: '/system/tenants',
            icon: 'School',
            type: 2,
            code: 'system:tenant:list'
          },
          {
            id: 27,
            parentId: 2,
            name: '审计日志',
            path: '/system/audit-logs',
            icon: 'Document',
            type: 2,
            code: 'system:audit:list'
          },
          {
            id: 28,
            parentId: 2,
            name: '系统配置',
            path: '/system/system-configs',
            icon: 'Tools',
            type: 2,
            code: 'system:config:list'
          }
        ]
      }
    ] as Menu[]
  }),

  getters: {
    // 判断当前用户是否为超级管理员
    isSuperAdmin: (state): boolean => {
      return state.userInfo.roles.includes('super_admin')
    },
    // 判断当前用户是否为管理员
    isTenantAdmin: (state): boolean => {
      return state.userInfo.roles.includes('tenant_admin')
    },
    // 获取当前用户所属租户ID
    currentTenantId: (state): number | string | undefined => {
      // tenantId 为 0 或 undefined 时返回 undefined
      return state.userInfo.tenantId || undefined
    },
    // 获取当前用户最高角色等级（数字越小权限越大，无角色时为 100）
    maxRoleLevel: (state): number => {
      return state.userInfo.maxRoleLevel ?? 100
    },
    // 判断当前用户是否拥有指定权限（超级管理员不受限制）
    hasPermission: (state) => (permissionCode: string): boolean => {
      // 超级管理员拥有所有权限
      if (state.userInfo.roles.includes('super_admin')) {
        return true
      }
      // 检查用户权限列表中是否包含指定权限
      return state.userInfo.permissions.includes(permissionCode)
    },
    // 判断当前子系统是否为 store 子系统（用于门店切换器显示判断）
    isStoreSubsystem: (state): boolean => {
      if (!state.currentSubsystemId) return false
      const current = state.authorizedSubsystems.find(s => String(s.id) === state.currentSubsystemId)
      return current?.code === 'StoreManagement'
    },
    // 当前门店名称（用于门店切换器显示）
    currentStoreName: (state): string => {
      if (!state.currentStoreId) return ''
      const store = state.authorizedStores.find(s => String(s.id) === state.currentStoreId)
      return store?.name || ''
    },
    /**
     * 当前子系统授权菜单中排序第1的可导航叶子菜单路径
     * 用于首页跳转：根路径 / 及 /dashboard 应跳转到此路径，而非硬编码 dashboard
     * 菜单树按 sort 排序，深度优先遍历返回第一个有 path 的叶子（跳过按钮类型 type=2）
     */
    firstAuthorizedLeafPath: (state): string | null => {
      const findLeaf = (menus: Menu[]): string | null => {
        for (const menu of menus) {
          // 跳过按钮类型，按钮不可导航
          if (menu.type === 2) continue
          // 有子菜单时递归查找
          if (menu.children && menu.children.length > 0) {
            const path = findLeaf(menu.children)
            if (path) return path
          } else if (menu.path) {
            return menu.path
          }
        }
        return null
      }
      const result = findLeaf(state.menus)
      return result
    }
  },

  actions: {
    async login(username: string, password: string) {
      const response = await login(username, password)
      this.token = response.access_token
      localStorage.setItem('token', this.token)

      // 登录成功后获取用户信息
      await this.getUserInfo()

      // 预加载授权子系统和菜单，确保路由守卫能获取到 firstAuthorizedLeafPath
      // 否则路由守卫因 userInfo 已设置而跳过加载，firstAuthorizedLeafPath 返回 null 导致跳转失效
      await this.getAuthorizedSubsystems()
      await this.getMenus()

      // 若当前子系统的第一个是 store 子系统，预加载授权门店
      if (this.isStoreSubsystem && this.authorizedStores.length === 0) {
        await this.getAuthorizedStores()
      }

      return true
    },

    async logout() {
      // 清除动态路由注册：先移除 dynamic_ 路由再重置标记
      // 使用动态导入避免循环依赖（router/index.ts -> stores/user.ts -> router/index.ts）
      try {
        const [{ default: router }, { unregisterDynamicRoutes, resetRouteRegistration }] = await Promise.all([
          import('@/router'),
          import('@/router/modules')
        ])
        unregisterDynamicRoutes(router)
        resetRouteRegistration()
      } catch {
        // 路由清理失败不阻断 logout
      }

      // 清空用户 store
      this.token = ''
      this.userInfo = <UserInfo>{
        id: 0,
        userName: '',
        realName: '',
        avatar: '',
        email: '',
        phone: '',
        roles: [],
        roleIds: [],
        permissions: [],
        tenantId: 0,
        tenantCode: undefined,
        maxRoleLevel: 100
      }
      // 清空权限数据
      this.authorizedSubsystems = []
      this.currentSubsystemId = ''
      this.menus = []
      // 清空门店数据
      this.authorizedStores = []
      this.currentStoreId = ''
      localStorage.removeItem('token')
      localStorage.removeItem('tenantId')
      localStorage.removeItem('tenantCode')
      localStorage.removeItem('currentSubsystemId')
      localStorage.removeItem('currentStoreId')
    },

    async getUserInfo() {
      const user = await getCurrentUser()
      this.userInfo = {
        id: user.id,
        userName: user.userName || '',
        realName: user.realName || '',
        avatar: user.avatar || 'https://cube.elemecdn.com/0/88/03b0d39583f48206768a7534e55bcpng.png',
        email: user.email || '',
        phone: user.phone || '',
        // 后端可能返回角色对象而非字符串（类型声明为 string[]，此处防御性兼容对象形状）
        roles: user.roles?.map(r => typeof r === 'string' ? r : ((r as unknown as { code?: string }).code ?? '')) || [],
        roleIds: user.roleIds || [],
        permissions: user.permissions || [],
        tenantId: user.tenantId,
        tenantCode: user.tenantCode,
        maxRoleLevel: user.maxRoleLevel ?? 100
      }
      // 缓存租户信息到 localStorage
      localStorage.setItem('tenantId', String(user.tenantId))
      if (user.tenantCode) {
        localStorage.setItem('tenantCode', user.tenantCode)
      }
      return this.userInfo
    },

    /**
     * 获取用户授权的子系统列表
     * 根据租户ID获取该租户被授权访问的子系统
     */
    async getAuthorizedSubsystems() {
      try {
        const tenantId = this.userInfo.tenantId
        if (!tenantId) {
          this.authorizedSubsystems = []
          this.currentSubsystemId = ''
          localStorage.removeItem('currentSubsystemId')
          return []
        }

        // 使用 getSubsystems API 获取子系统列表（权限要求较低）
        const { getSubsystems } = await import('@/api/system')
        const allSubsystems = await getSubsystems({})

        // getSubsystems({}) 返回的就是用户有权限看到的子系统（后端已做权限过滤）
        // 直接使用返回结果，按 sort 排序
        const authorizedSubsystems = allSubsystems
          .filter(s => s.status === 1)
          .sort((a, b) => (a.sort ?? 0) - (b.sort ?? 0))
        this.authorizedSubsystems = authorizedSubsystems

        // 设置当前子系统：优先沿用已保存的选择（刷新场景），否则取 sort 最小的子系统
        if (this.authorizedSubsystems.length > 0) {
          const savedId = this.currentSubsystemId
          const isValid = !!savedId && this.authorizedSubsystems.some(s => String(s.id) === savedId)
          this.currentSubsystemId = isValid ? savedId : String(this.authorizedSubsystems[0].id)
          localStorage.setItem('currentSubsystemId', this.currentSubsystemId)
        } else {
          // 无授权子系统：清空 currentSubsystemId，避免 getMenus 误用旧值导致菜单泄漏
          this.currentSubsystemId = ''
          localStorage.removeItem('currentSubsystemId')
        }

        return this.authorizedSubsystems
      } catch {
        this.authorizedSubsystems = []
        this.currentSubsystemId = ''
        localStorage.removeItem('currentSubsystemId')
        return []
      }
    },

    /**
     * 切换当前子系统
     * @param subsystemId 子系统ID
     */
    async switchSubsystem(subsystemId: number | string) {
      const id = String(subsystemId)
      this.currentSubsystemId = id
      localStorage.setItem('currentSubsystemId', id)
      // 切换子系统后重新获取菜单
      await this.getMenus()

      // 切换子系统后用户仍在 layout，路由守卫 beforeEach 不会触发
      // 需主动清除旧子系统动态路由并注册新子系统路由
      try {
        const [{ default: router }, { unregisterDynamicRoutes, registerDynamicRoutes, registerNotFoundRoute }] = await Promise.all([
          import('@/router'),
          import('@/router/modules')
        ])
        unregisterDynamicRoutes(router)
        registerDynamicRoutes(router, this.menus, new Set<string>())
        registerNotFoundRoute(router)
      } catch {
        // 路由注册失败不影响菜单切换，但可能导致页面 404
      }

      // 切换到 store 子系统时，加载授权门店列表（用于门店切换器）
      const target = this.authorizedSubsystems.find(s => String(s.id) === id)
      if (target?.code === 'StoreManagement') {
        await this.getAuthorizedStores()
      }
    },

    /**
     * 获取当前租户下授权的门店列表
     * 仅在进入 store 子系统时调用，用于填充门店切换器
     */
    async getAuthorizedStores() {
      try {
        const stores = await fetchAuthorizedStores()
        this.authorizedStores = stores || []

        // 设置当前门店：优先沿用已保存的选择，否则取第一个门店
        if (this.authorizedStores.length > 0) {
          const savedId = this.currentStoreId
          const isValid = !!savedId && this.authorizedStores.some(s => String(s.id) === savedId)
          this.currentStoreId = isValid ? savedId : String(this.authorizedStores[0].id)
          localStorage.setItem('currentStoreId', this.currentStoreId)
        } else {
          this.currentStoreId = ''
          localStorage.removeItem('currentStoreId')
        }

        return this.authorizedStores
      } catch {
        this.authorizedStores = []
        return []
      }
    },

    /**
     * 切换当前门店
     * @param storeId 门店ID
     */
    async switchStore(storeId: number | string) {
      const id = String(storeId)
      this.currentStoreId = id
      localStorage.setItem('currentStoreId', id)
    },

    /**
     * 根据子系统ID获取该子系统的菜单ID列表
     */
    async getSubsystemMenuIds(subsystemId: number | string): Promise<number[]> {
      try {
        const { getSubsystemMenus } = await import('@/api/system')
        const menuIds = await getSubsystemMenus(subsystemId)
        return menuIds
      } catch {
        return []
      }
    },

    /**
     * 根据角色获取菜单权限ID列表
     */
    async getRoleMenuIds(): Promise<number[]> {
      try {
        const roleMenuIdsSet = new Set<number>()

        if (!this.userInfo.roleIds || this.userInfo.roleIds.length === 0) {
          return []
        }

        // 直接使用 roleIds 获取每个角色的菜单权限
        for (const roleId of this.userInfo.roleIds) {
          const { getRoleMenuAuths } = await import('@/api/system')
          const menuIds = await getRoleMenuAuths(roleId)

          for (const menuId of menuIds) {
            roleMenuIdsSet.add(menuId)
          }
        }

        return Array.from(roleMenuIdsSet)
      } catch {
        return []
      }
    },

    /**
     * 根据当前用户权限过滤菜单
     * @param allMenus 完整菜单树
     * @param authorizedMenuIds 用户有权限的菜单ID集合
     * 逻辑：显示授权菜单及其所有父级目录
     */
    filterMenusByPermission(allMenus: Menu[], authorizedMenuIds: Set<number>): Menu[] {
      // 构建 parentId -> menuIds 的映射，用于查找子菜单
      const parentIdMap = new Map<number | null, Menu[]>()
      const allMenuMap = new Map<number, Menu>() // 用于快速查找菜单

      const buildParentMap = (menus: Menu[], parentId: number | null) => {
        for (const menu of menus) {
          allMenuMap.set(menu.id, menu)
          if (!parentIdMap.has(parentId)) {
            parentIdMap.set(parentId, [])
          }
          parentIdMap.get(parentId)!.push(menu)
          if (menu.children && menu.children.length > 0) {
            buildParentMap(menu.children, menu.id)
          }
        }
      }
      buildParentMap(allMenus, null)

      // 找出授权菜单的所有祖先ID
      const ancestorIds = new Set<number>()
      const findAncestors = (menuId: number) => {
        const menu = allMenuMap.get(menuId)
        if (menu && menu.parentId) {
          ancestorIds.add(menu.parentId)
          findAncestors(menu.parentId)
        }
      }
      for (const menuId of authorizedMenuIds) {
        findAncestors(menuId)
      }

      // 合并授权菜单ID和祖先ID
      const visibleIds = new Set<number>()
      for (const id of authorizedMenuIds) {
        visibleIds.add(id)
      }
      for (const id of ancestorIds) {
        visibleIds.add(id)
      }

      // 过滤菜单树：只显示在可见ID集合中的菜单
      const result: Menu[] = []

      for (const menu of allMenus) {
        // 如果菜单不在可见集合中，跳过
        if (!visibleIds.has(menu.id)) {
          continue
        }

        // 目录类型：递归过滤子菜单
        const isDirectory = menu.type === 0
        if (isDirectory) {
          if (menu.children && menu.children.length > 0) {
            const filteredChildren = this.filterMenusByPermission(menu.children, authorizedMenuIds)
            result.push({
              ...menu,
              children: filteredChildren
            })
          } else {
            // 空目录也可能被添加到结果（作为其他菜单的父级）
            result.push({ ...menu, children: [] })
          }
        } else if (menu.type === 1) {
          // 菜单类型(type=1)：如果在可见集合中，显示并递归过滤子菜单
          if (visibleIds.has(menu.id) && menu.children && menu.children.length > 0) {
            const filteredChildren = this.filterMenusByPermission(menu.children, authorizedMenuIds)
            result.push({
              ...menu,
              children: filteredChildren
            })
          } else if (visibleIds.has(menu.id)) {
            result.push({ ...menu })
          }
        } else {
          // 按钮类型(type=2)：不显示在侧边栏菜单中
          // 按钮权限通过 permissions 列表（hasPermission）控制页面内操作
          // 菜单可见性由"页面查看"按钮的祖先推导决定，无需将按钮放入菜单树
        }
      }

      return result
    },

    async getMenus() {
      try {
        const allMenus = await getMenuTree()

        // 图标名称映射：将后端图标名称转换为 Element Plus Icons 组件名称
        const iconMap: Record<string, string> = {
          'Setting': 'Setting',
          'User': 'User',
          'Lock': 'Lock',
          'Menu': 'Menu',
          'OfficeBuilding': 'OfficeBuilding',
          'Building': 'House',
          'House': 'House',
          'Document': 'Document',
          'Tools': 'Tools',
          'Folder': 'Folder',
          'FolderOpened': 'FolderOpened',
          'HomeFilled': 'HomeFilled',
          'UserFilled': 'UserFilled',
          'Grid': 'Grid',
          'School': 'School',
          // 旧图标名兼容映射（Element Plus Icons 中不存在的名称）
          'Category': 'Collection',
          'Time': 'Timer',
          'Flash': 'Lightning',
          'Gift': 'Present',
          'UserPlus': 'Avatar'
        }
        // 处理后端返回的菜单数据，转换为前端需要的格式
        const formatMenus = (items: Menu[]): Menu[] => {
          return items.map(item => {
            // 处理路径：相对路径转换为绝对路径（只需加上前缀 /）
            let fullPath = item.path || ''
            if (fullPath && !fullPath.startsWith('/')) {
              fullPath = `/${fullPath}`
            }
            // 处理图标名称
            const iconName = item.icon ? (iconMap[item.icon] || item.icon) : 'Folder'
            return {
              id: item.id,
              parentId: item.parentId ?? 0,
              name: item.name,
              path: fullPath,
              component: item.component,
              code: item.code,
              icon: iconName,
              type: item.type,
              sort: item.sort ?? 0,
              status: item.status ?? 1,
              isVisible: typeof item.isVisible === 'boolean' ? (item.isVisible ? 1 : 0) : item.isVisible,
              isCache: typeof item.isCache === 'boolean' ? (item.isCache ? 1 : 0) : item.isCache,
              isAffix: item.isAffix ?? 0,
              isAlwaysShow: item.isAlwaysShow ?? false,
              permissionCode: item.permissionCode || item.permission,
              children: item.children && item.children.length > 0 ? formatMenus(item.children) : undefined
            }
          })
        }
        const formattedMenus = formatMenus(allMenus)

        // 根据当前子系统和角色权限过滤菜单
        // 无当前子系统（租户未分配任何子系统）时返回空菜单，避免泄漏全部菜单
        let filteredMenus: Menu[] = []

        if (this.currentSubsystemId) {
          // 获取当前子系统的菜单ID列表
          const subsystemMenuIds = await this.getSubsystemMenuIds(this.currentSubsystemId)

          // 获取用户角色菜单权限ID列表
          const roleMenuIds = await this.getRoleMenuIds()

          // 取两者交集 = 最终授权菜单ID
          const subsystemSet = new Set(subsystemMenuIds)
          const roleSet = new Set(roleMenuIds)
          const authorizedIds = new Set<number>()

          for (const id of subsystemSet) {
            if (roleSet.has(id)) {
              authorizedIds.add(id)
            }
          }

          // 如果没有交集（即没有权限），返回空菜单
          if (authorizedIds.size === 0) {
            filteredMenus = []
          } else {
            // 所有角色都要按子系统过滤菜单（包括超级管理员和管理员）
            filteredMenus = this.filterMenusByPermission(formattedMenus, authorizedIds)
          }
        }

        this.menus = filteredMenus
        return this.menus
      } catch {
        // 如果API调用失败，返回本地菜单
        return this.menus
      }
    }
  }
})
