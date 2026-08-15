import type { Component } from 'vue'
import type { Router } from 'vue-router'
import type { Menu } from '@/api/system/types'

// 收集所有 views 下的 .vue 文件（Vite 构建时静态分析，天然防止路径遍历）
const modules = import.meta.glob('/src/views/**/*.vue')

// 模块级标记：动态路由是否已注册
let isDynamicRoutesRegistered = false
// 模块级标记：404 兜底路由是否已注册（独立于动态路由，logout 不清除）
let isNotFoundRegistered = false

/**
 * 根据后端 component 字段（如 "system/users/index"）解析组件
 * 返回 null 表示组件无法解析（字段为空或文件不存在）
 */
function resolveComponent(component?: string | null): Component | null {
  if (!component) return null
  // 后端 component 字段形如 "system/users/index"，对应 "/src/views/system/users/index.vue"
  const path = `/src/views/${component.replace(/^\/+/, '')}.vue`
  const loader = modules[path]
  if (!loader) {
    console.warn(`[动态路由] 组件未找到: ${component} -> ${path}`)
    return null
  }
  // import.meta.glob 返回的 loader 是 () => Promise<Module>，符合 Vue Router 异步组件签名
  return loader as unknown as Component
}

/**
 * 递归注册动态路由
 * - 跳过 type=2（按钮）：按钮不可导航，通过权限码控制页面内操作
 * - 递归 type=0（目录）的 children
 * - 注册 type=1（菜单）：作为 Layout 子路由注册，path 去掉前导 /
 *   name 使用 dynamic_{menu.id} 避免与静态路由冲突
 *   component 解析失败时跳过并警告
 * @param router 路由实例
 * @param menus 后端返回的菜单树
 * @param registered 已注册的 path 集合，防止重复注册
 */
export function registerDynamicRoutes(router: Router, menus: Menu[], registered: Set<string>): void {
  const walk = (list: Menu[]) => {
    for (const menu of list) {
      // 按钮类型：跳过
      if (menu.type === 2) continue

      // 菜单类型：注册路由
      if (menu.type === 1) {
        if (!menu.path) continue
        // path 已注册则跳过（防止菜单树重复节点）
        if (registered.has(menu.path)) {
          console.warn(`[动态路由] 路径重复注册已跳过: ${menu.path}`)
          continue
        }
        const component = resolveComponent(menu.component)
        if (!component) continue

        // 动态路由 path 必须去掉前导 /，作为 Layout 子路由的相对路径
        const relativePath = menu.path.replace(/^\//, '')
        router.addRoute('Layout', {
          path: relativePath,
          name: `dynamic_${menu.id}`,
          component,
          meta: {
            title: menu.name,
            icon: menu.icon,
            menuId: menu.id,
            permissionCode: menu.permissionCode || menu.code,
            isCache: menu.isCache
          }
        })
        registered.add(menu.path)
      }

      // 递归子菜单（目录 type=0 或菜单 type=1 都可能有 children）
      if (menu.children && menu.children.length > 0) {
        walk(menu.children)
      }
    }
  }

  walk(menus)
  isDynamicRoutesRegistered = true
}

/**
 * 清除所有 dynamic_ 前缀路由（不动 404 和静态路由）
 * 用于 logout / switchSubsystem 时清理旧路由
 */
export function unregisterDynamicRoutes(router: Router): void {
  const dynamicRoutes = router.getRoutes().filter(r => r.name?.toString().startsWith('dynamic_'))
  for (const route of dynamicRoutes) {
    if (route.name) {
      router.removeRoute(route.name)
    }
  }
  isDynamicRoutesRegistered = false
}

/**
 * 注册 404 catch-all 路由（顶层路由，与 /login 同级），只注册一次
 * 独立标记 isNotFoundRegistered，不被 unregisterDynamicRoutes 清除
 */
export function registerNotFoundRoute(router: Router): void {
  if (isNotFoundRegistered) return
  router.addRoute({
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/error/404.vue')
  })
  isNotFoundRegistered = true
}

/**
 * 重置动态路由注册标记（logout 时调用）
 * 注意：404 标记不重置，避免 logout 后下次登录重复注册 404
 */
export function resetRouteRegistration(): void {
  isDynamicRoutesRegistered = false
}

/**
 * 查询动态路由是否已注册
 */
export function isDynamicRegistered(): boolean {
  return isDynamicRoutesRegistered
}
