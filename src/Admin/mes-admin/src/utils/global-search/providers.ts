// 全局搜索 - 搜索源注册表
// 三类内置搜索源：功能导航（本地菜单树）/ 门店业务数据（/api/store/search）/ 系统数据（/api/system/search）
// 接入新子系统搜索：实现 SearchProvider 后在 getActiveProviders 注册即可，面板逻辑无需改动

import type { Menu } from '@/api/system/types'
import { searchStore, searchSystem } from '@/api/global-search'
import { useUserStore } from '@/stores/user'
import { getIntentBoost } from './intent'
import type { SearchEntry, SearchProvider } from './types'

/** 业务数据分组 → 跳转路由映射（子系统用稳定 Code 标识，跳转时经 authorizedSubsystems 换算真实雪花ID） */
const GROUP_ROUTE_MAP: Record<string, { path: string; subsystemCode: string }> = {
  顾客: { path: '/store/customer/profile', subsystemCode: 'StoreManagement' },
  订单: { path: '/store/pos/order', subsystemCode: 'StoreManagement' },
  商品: { path: '/store/product/master', subsystemCode: 'StoreManagement' },
  项目卡: { path: '/store/treatment/sale', subsystemCode: 'StoreManagement' },
  储值: { path: '/store/storedvalue/account', subsystemCode: 'StoreManagement' },
  预约: { path: '/store/appointment/list', subsystemCode: 'StoreManagement' },
  用户: { path: '/system/users', subsystemCode: 'BasicDataManagement' }
}

/** 分组基础权重（功能 > 顾客 > 订单/预约 > 其余），未登记的分组取默认值 */
const GROUP_BASE_WEIGHT: Record<string, number> = {
  功能: 1.4,
  顾客: 1.2,
  订单: 1.0,
  预约: 1.0
}
const DEFAULT_BASE_WEIGHT = 0.9

/** store 源可见门槛：以顾客档案查看权为代表（Store 服务无分组权限码设施，登录即可查的分组不再细分） */
const STORE_SOURCE_PERMISSION = 'store:customer:profile:view'
/** system 源可见门槛 */
const SYSTEM_SOURCE_PERMISSION = 'system:user:view'

/** 计算某分组的最终权重 = 基础权重 + 意图加权 */
function groupWeight(group: string, intent: Record<string, number>): number {
  return (GROUP_BASE_WEIGHT[group] ?? DEFAULT_BASE_WEIGHT) + (intent[group] ?? 0)
}

/**
 * 功能导航源：本地过滤所有已授权子系统的菜单树（懒加载缓存），无网络开销
 * 条目附带 menuJump（子系统真实雪花ID + 页面路径）
 */
export function buildMenuProvider(): SearchProvider {
  return {
    id: 'menu',
    async search(keyword: string, limit: number): Promise<SearchEntry[]> {
      const userStore = useUserStore()
      const subsystemMenus = await userStore.getAllSubsystemMenus()
      const kw = keyword.toLowerCase()
      const entries: SearchEntry[] = []

      const walk = (menus: Menu[], parentName: string, subsystemId: string, subsystemName: string) => {
        for (const menu of menus) {
          // 仅可导航的页面菜单（type=1）参与搜索，目录与按钮不收集
          if (menu.type === 1 && menu.path && menu.name.toLowerCase().includes(kw)) {
            const path = menu.path.startsWith('/') ? menu.path : `/${menu.path}`
            entries.push({
              title: menu.name,
              subtitle: `${subsystemName} · ${parentName}`,
              group: '功能',
              weight: groupWeight('功能', {}),
              menuJump: { subsystemId, path }
            })
          }
          if (menu.children && menu.children.length > 0) {
            walk(menu.children, menu.name, subsystemId, subsystemName)
          }
          if (entries.length >= limit * 3) return
        }
      }

      for (const sub of subsystemMenus) {
        walk(sub.menus, sub.subsystemName, String(sub.subsystemId), sub.subsystemName)
        if (entries.length >= limit * 3) break
      }
      return entries.slice(0, limit)
    }
  }
}

/** 门店业务数据源：调 /api/store/search，返回按分组摊平的条目 */
export function buildStoreProvider(): SearchProvider {
  return {
    id: 'store',
    async search(keyword: string, limit: number): Promise<SearchEntry[]> {
      const groups = await searchStore(keyword, limit)
      const intent = getIntentBoost(keyword)
      return groups.flatMap(g =>
        g.items.map(item => ({ ...item, weight: groupWeight(g.group, intent) }))
      )
    }
  }
}

/** 系统数据源：调 /api/system/search */
export function buildSystemProvider(): SearchProvider {
  return {
    id: 'system',
    async search(keyword: string, limit: number): Promise<SearchEntry[]> {
      const groups = await searchSystem(keyword, limit)
      const intent = getIntentBoost(keyword)
      return groups.flatMap(g =>
        g.items.map(item => ({ ...item, weight: groupWeight(g.group, intent) }))
      )
    }
  }
}

/**
 * 获取当前用户可用的搜索源列表
 * 功能导航源恒可用；业务数据源按代表性权限码过滤（无权限用户搜索不发起无谓请求）
 */
export function getActiveProviders(): SearchProvider[] {
  const userStore = useUserStore()
  const providers: SearchProvider[] = [buildMenuProvider()]
  if (userStore.hasPermission(STORE_SOURCE_PERMISSION)) {
    providers.push(buildStoreProvider())
  }
  if (userStore.hasPermission(SYSTEM_SOURCE_PERMISSION)) {
    providers.push(buildSystemProvider())
  }
  return providers
}

/**
 * 解析搜索条目的跳转目标（子系统ID + 页面路径）
 * 菜单源条目直接用自带 menuJump；业务数据条目按分组路由映射换算子系统真实ID
 * @returns 无法解析（分组无路由映射或未授权对应子系统）时返回 null
 */
export function resolveSearchTarget(entry: SearchEntry): { subsystemId: string; path: string } | null {
  if (entry.menuJump) {
    return { subsystemId: entry.menuJump.subsystemId, path: entry.menuJump.path }
  }
  const route = GROUP_ROUTE_MAP[entry.group]
  if (!route) return null
  const userStore = useUserStore()
  const subsystem = userStore.authorizedSubsystems.find(s => s.code === route.subsystemCode)
  if (!subsystem) return null
  return { subsystemId: String(subsystem.id), path: route.path }
}
