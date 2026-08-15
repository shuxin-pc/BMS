import { createRouter, createWebHashHistory, RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getCurrentUser } from '@/api/system'
import {
  registerDynamicRoutes,
  registerNotFoundRoute,
  isDynamicRegistered,
  resetRouteRegistration
} from './modules'

// 静态路由：仅保留登录页、Layout 容器、以及所有登录用户可见的辅助页面
// 业务路由全部由 registerDynamicRoutes 根据后端菜单动态注册
const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/login/index.vue'),
    meta: {
      title: '登录'
    }
  },
  {
    path: '/',
    component: () => import('@/layout/index.vue'),
    name: 'Layout',
    children: [
      {
        path: 'no-permission',
        name: 'NoPermission',
        component: () => import('@/views/no-permission/index.vue'),
        meta: {
          title: '无权限',
          icon: 'Lock'
        }
      },
      {
        path: 'store/no-store',
        name: 'StoreNoStore',
        component: () => import('@/views/store/no-store/index.vue'),
        meta: {
          title: '暂无授权门店'
        }
      },
      // 个人中心：所有登录用户可见，不走动态注册
      {
        path: 'system/profile',
        name: 'SystemProfile',
        component: () => import('@/views/system/profile/index.vue'),
        meta: {
          title: '个人中心',
          icon: 'User'
        }
      }
    ]
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

// 路由守卫
router.beforeEach(async (to, _from, next) => {
  document.title = `${to.meta.title || 'BMS后台管理系统'} - BMS后台管理系统`
  const token = localStorage.getItem('token')
  const userStore = useUserStore()

  // 登录页直接放行
  if (to.path === '/login') {
    next()
    return
  }

  // 无 token 跳转 /login
  if (!token) {
    next('/login')
    return
  }

  // userInfo 为空时加载用户信息+子系统+菜单+门店（刷新场景）
  if (!userStore.userInfo.id || !userStore.userInfo.roles.length) {
    try {
      const user = await getCurrentUser()

      // 后端可能返回角色对象而非字符串（类型声明为 string[]，此处防御性兼容对象形状）
      const roles = user.roles?.map(r => typeof r === 'string' ? r : ((r as unknown as { code?: string }).code ?? '')) || []
      const roleIds = user.roleIds || []

      userStore.userInfo = {
        id: user.id,
        userName: user.userName,
        realName: user.realName || '',
        avatar: user.avatar || '',
        email: user.email || '',
        phone: user.phone || '',
        roles: roles,
        roleIds: roleIds,
        permissions: user.permissions || [],
        tenantId: user.tenantId,
        tenantCode: user.tenantCode,
        maxRoleLevel: user.maxRoleLevel ?? 100
      }

      // 缓存租户信息到 localStorage
      if (user.tenantId) {
        localStorage.setItem('tenantId', String(user.tenantId))
      }
      if (user.tenantCode) {
        localStorage.setItem('tenantCode', user.tenantCode)
      }

      // 加载授权子系统列表
      await userStore.getAuthorizedSubsystems()

      // 加载菜单（会根据当前子系统过滤）
      await userStore.getMenus()

      // 若当前是 store 子系统且门店列表未加载，先加载授权门店列表
      // 注意：刷新后 currentStoreId 会从 localStorage 恢复，但 authorizedStores 是 Pinia state 会重置为空
      // 所以条件必须检查 authorizedStores.length，而非 currentStoreId
      if (userStore.isStoreSubsystem && userStore.authorizedStores.length === 0) {
        await userStore.getAuthorizedStores()
      }
    } catch {
      // 获取用户信息失败，跳转到登录页
      localStorage.removeItem('token')
      next('/login')
      return
    }
  } else {
    // 已有用户信息：若目标路由是 store 子系统且门店列表未加载，兜底加载
    if (to.path.startsWith('/store') && userStore.isStoreSubsystem && userStore.authorizedStores.length === 0) {
      await userStore.getAuthorizedStores()
    }
  }

  // 动态路由未注册时：注册后重新导航（必须 return，不能 fall through）
  // 确保 next({ ...to, replace: true }) 重新触发守卫走后续分支
  if (!isDynamicRegistered()) {
    // registeredPaths 收集本次注册的动态路由 path（绝对路径），用于下一步匹配检查
    const registeredPaths = new Set<string>()
    registerDynamicRoutes(router, userStore.menus, registeredPaths)
    registerNotFoundRoute(router)
    // 注册后判断 to 是否能匹配：静态路由(to.matched.length>0) 或 已注册动态路由可直接重导航
    // 否则跳 /no-permission，避免 next({...to}) 对未注册路径触发 "No match found" 警告
    if (to.matched.length === 0 && !registeredPaths.has(to.path)) {
      next('/no-permission')
      return
    }
    next({ ...to, replace: true })
    return
  }

  // 首页跳转：进入子系统时显示已授权菜单排序第1的页面，而非硬编码 dashboard
  // 同时拦截 / 和 /dashboard，避免登录后或直接访问 dashboard 时绕过授权菜单逻辑
  // 无授权菜单时：跳转到 /no-permission 提示页
  if (to.path === '/' || to.path === '/dashboard') {
    const firstPath = userStore.firstAuthorizedLeafPath
    if (firstPath && firstPath !== to.path) {
      next(firstPath)
      return
    }
    if (!firstPath) {
      next('/no-permission')
      return
    }
  }

  // 已登录但 to.matched 为空（路由未注册，即未授权或不存在）时跳 /no-permission
  // 区分语义：401=未认证，403/无权限=有路径但无授权，404=路径不存在
  // 已登录用户访问未授权业务页面跳 /no-permission 而非 404，提示更准确
  if (to.matched.length === 0) {
    next('/no-permission')
    return
  }

  next()
})

// 导出 resetRouteRegistration 供 user store 在 logout 时调用
export { resetRouteRegistration }

export default router
