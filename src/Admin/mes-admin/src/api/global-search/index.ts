// 全局搜索 - API服务
// store 源走 shared/storeRequest（/api/store 前缀，自动注入 X-Store-Id 与未选门店拦截）
// system 源复用 api/system 的 searchSystem（/api/system 前缀，无门店上下文）
import { request as storeRequest, buildQuery } from '../shared/storeRequest'

export type { SearchResultItem, SearchResultGroup } from './types'
import type { SearchResultGroup } from './types'

export { searchSystem } from '../system'

/**
 * 门店业务数据全局搜索（顾客/订单/商品/项目卡/储值/预约）
 * @param keyword 搜索关键字
 * @param limit 每组返回条数上限
 */
export async function searchStore(keyword: string, limit = 5): Promise<SearchResultGroup[]> {
  return storeRequest<SearchResultGroup[]>(`/search${buildQuery({ keyword, limit })}`)
}
