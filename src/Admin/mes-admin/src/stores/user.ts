import { defineStore } from 'pinia'
import { getMenuTree, login, getCurrentUser } from '@/api/system'
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
      tenantCode: localStorage.getItem('tenantCode') || undefined
    },
    // 用户授权的子系统列表
    authorizedSubsystems: [] as Subsystem[],
    // 当前选中的子系统ID
    currentSubsystemId: 0 as number,
    // 菜单数据
    menus: [
      {
        id: 1,
        parentId: 0,
        name: '首页',
        path: '/dashboard',
        icon: 'HomeFilled',
        type: 1
      },
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
    // 判断当前用户是否拥有指定权限（超级管理员不受限制）
    hasPermission: (state) => (permissionCode: string): boolean => {
      // 超级管理员拥有所有权限
      if (state.userInfo.roles.includes('super_admin')) {
        return true
      }
      // 检查用户权限列表中是否包含指定权限
      return state.userInfo.permissions.includes(permissionCode)
    }
  },

  actions: {
    async login(username: string, password: string) {
      const response = await login(username, password)
      this.token = response.access_token
      localStorage.setItem('token', this.token)

      // 登录成功后获取用户信息
      await this.getUserInfo()

      return true
    },

    logout() {
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
        tenantCode: undefined
      }
      // 清空权限数据
      this.authorizedSubsystems = []
      this.currentSubsystemId = 0
      this.menus = []
      localStorage.removeItem('token')
      localStorage.removeItem('tenantId')
      localStorage.removeItem('tenantCode')
    },

    async getUserInfo() {
      try {
        const user = await getCurrentUser()
        this.userInfo = {
          id: user.id,
          userName: user.userName || '',
          realName: user.realName || '',
          avatar: user.avatar || 'https://cube.elemecdn.com/0/88/03b0d39583f48206768a7534e55bcpng.png',
          email: user.email || '',
          phone: user.phone || '',
          roles: user.roles?.map(r => typeof r === 'string' ? r : (r as any).code) || [],
          roleIds: user.roleIds || [],
          permissions: user.permissions || [],
          tenantId: user.tenantId,
          tenantCode: user.tenantCode
        }
        // 缓存租户信息到 localStorage
        localStorage.setItem('tenantId', String(user.tenantId))
        if (user.tenantCode) {
          localStorage.setItem('tenantCode', user.tenantCode)
        }
        return this.userInfo
      } catch (error) {
        console.error('获取用户信息失败', error)
        throw error
      }
    },

    /**
     * 获取用户授权的子系统列表
     * 根据租户ID获取该租户被授权访问的子系统
     */
    async getAuthorizedSubsystems() {
      try {
        const tenantId = this.userInfo.tenantId
        if (!tenantId) {
          console.warn('用户无租户ID，无法获取授权子系统')
          this.authorizedSubsystems = []
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

        // 设置当前子系统为sort最小的子系统
        if (this.authorizedSubsystems.length > 0) {
          this.currentSubsystemId = this.authorizedSubsystems[0].id
        }

        return this.authorizedSubsystems
      } catch (error) {
        console.error('获取授权子系统失败', error)
        this.authorizedSubsystems = []
        return []
      }
    },

    /**
     * 切换当前子系统
     * @param subsystemId 子系统ID
     */
    async switchSubsystem(subsystemId: number) {
      this.currentSubsystemId = subsystemId
      // 切换子系统后重新获取菜单
      await this.getMenus()
    },

    /**
     * 根据子系统ID获取该子系统的菜单ID列表
     */
    async getSubsystemMenuIds(subsystemId: number): Promise<number[]> {
      try {
        const { getSubsystemMenus } = await import('@/api/system')
        const menuIds = await getSubsystemMenus(subsystemId)
        return menuIds
      } catch (error) {
        console.error('获取子系统菜单失败', error)
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
      } catch (error) {
        console.error('获取角色菜单权限失败', error)
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
        // 首页不过滤，始终显示
        if (menu.path === '/dashboard') {
          result.push({ ...menu })
          continue
        }

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
          // 按钮类型(type=2)：需要在授权列表中
          if (authorizedMenuIds.has(menu.id)) {
            result.push({ ...menu })
          }
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
          'School': 'School'
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
        let filteredMenus = formattedMenus

        if (this.currentSubsystemId > 0) {
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

          // 如果没有交集（即没有权限），返回空菜单（只保留首页）
          if (authorizedIds.size === 0) {
            filteredMenus = []
          } else {
            // 所有角色都要按子系统过滤菜单（包括超级管理员和管理员）
            filteredMenus = this.filterMenusByPermission(formattedMenus, authorizedIds)
          }
        }

        // 添加首页在最前面（固定，不受子系统影响）
        this.menus = [
          {
            id: 0,
            parentId: 0,
            name: '首页',
            path: '/dashboard',
            icon: 'HomeFilled',
            type: 1,
            code: 'dashboard',
            sort: 0,
            isVisible: 1,
            isCache: 0,
            isAffix: 0,
            createdAt: ''
          },
          ...filteredMenus
        ]
        return this.menus
      } catch (error) {
        // 如果API调用失败，返回本地菜单
        console.warn('获取菜单失败，使用本地菜单', error)
        return this.menus
      }
    }
  }
})
