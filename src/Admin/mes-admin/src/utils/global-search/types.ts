// 全局搜索 - 搜索源契约与条目扩展类型
// 扩展共享契约 api/global-search/types.ts（SearchResultItem），不改动 API 层类型

import type { SearchResultItem } from '@/api/global-search/types'

/** 菜单源条目的跳转信息（子系统真实雪花ID + 前端路由路径） */
export interface MenuJump {
  /** 目标子系统ID（字符串，雪花ID防精度丢失） */
  subsystemId: string
  /** 目标页面路径（带前导斜杠） */
  path: string
}

/** 命令面板搜索条目（共享契约 + 排序权重 + 菜单跳转信息） */
export interface SearchEntry extends SearchResultItem {
  /** 排序权重（越大越靠前），由搜索源按分组基础权重 + 意图加权计算 */
  weight?: number
  /** 菜单源条目专属：直接携带跳转信息；业务数据条目跳转经 resolveSearchTarget 按分组路由映射解析 */
  menuJump?: MenuJump
}

/** 搜索源契约：每个子系统/数据域实现一个，接入新搜索源只需新增实现并注册 */
export interface SearchProvider {
  /** 源唯一标识（menu / store / system） */
  id: string
  /**
   * 执行搜索
   * @param keyword 关键字
   * @param limit 单源返回条数上限
   * @returns 命中条目（含权重）；失败时抛错由调用方 Promise.allSettled 静默降级
   */
  search(keyword: string, limit: number): Promise<SearchEntry[]>
}
