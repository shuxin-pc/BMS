import { createRouter, createWebHashHistory, RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { getCurrentUser } from '@/api/system'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/login'
  },
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
    redirect: '/dashboard',
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/dashboard/index.vue'),
        meta: {
          title: '首页',
          icon: 'HomeFilled'
        }
      },
      {
        path: 'system/users',
        name: 'SystemUsers',
        component: () => import('@/views/system/users/index.vue'),
        meta: {
          title: '用户管理',
          icon: 'User'
        }
      },
      {
        path: 'system/roles',
        name: 'SystemRoles',
        component: () => import('@/views/system/roles/index.vue'),
        meta: {
          title: '角色管理',
          icon: 'UserFilled'
        }
      },
      {
        path: 'system/menus',
        name: 'SystemMenus',
        component: () => import('@/views/system/menus/index.vue'),
        meta: {
          title: '菜单管理',
          icon: 'Menu'
        }
      },
      {
        path: 'system/subsystems',
        name: 'SystemSubsystems',
        component: () => import('@/views/system/subsystems/index.vue'),
        meta: {
          title: '子系统管理',
          icon: 'Grid'
        }
      },
      {
        path: 'system/organizations',
        name: 'SystemOrganizations',
        component: () => import('@/views/system/organizations/index.vue'),
        meta: {
          title: '组织架构管理',
          icon: 'OfficeBuilding'
        }
      },
      {
        path: 'system/tenants',
        name: 'SystemTenants',
        component: () => import('@/views/system/tenants/index.vue'),
        meta: {
          title: '租户管理',
          icon: 'School'
        }
      },
      {
        path: 'system/audit-logs',
        name: 'SystemAuditLogs',
        component: () => import('@/views/system/audit-logs/index.vue'),
        meta: {
          title: '审计日志',
          icon: 'Document'
        }
      },
      {
        path: 'system/system-configs',
        name: 'SystemConfigs',
        component: () => import('@/views/system/system-configs/index.vue'),
        meta: {
          title: '系统配置',
          icon: 'Setting'
        }
      },
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
  document.title = `${to.meta.title} - BMS后台管理系统`
  const token = localStorage.getItem('token')
  const userStore = useUserStore()

  if (to.path === '/login') {
    next()
  } else {
    if (!token) {
      next('/login')
    } else {
      // 刷新页面后，如果 token 存在但 userInfo 为空，则重新获取用户信息和权限数据
      if (!userStore.userInfo.id || !userStore.userInfo.roles.length) {
        try {
          const user = await getCurrentUser()

          const roles = user.roles?.map((r: any) => typeof r === 'string' ? r : r.code) || []
          const roleIds = user.roleIds || []

          userStore.userInfo = {
            id: user.id,
            userName: user.userName,
            realName: user.realName,
            avatar: user.avatar || '',
            email: user.email,
            phone: user.phone,
            roles: roles,
            roleIds: roleIds,
            permissions: user.permissions || [],
            tenantId: user.tenantId,
            tenantCode: user.tenantCode
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
        } catch (error) {
          console.error('获取用户信息失败', error)
          // 获取用户信息失败，跳转到登录页
          localStorage.removeItem('token')
          next('/login')
          return
        }
      }
      next()
    }
  }
})

export default router
