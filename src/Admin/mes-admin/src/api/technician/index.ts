// 商家技师管理 - API 服务
// 对接后端 TechniciansController（路由 /api/store/technicians）
import { request, buildQuery, type PagedResponse } from '../shared/storeRequest'
import type { Technician, TechnicianQuery } from './types'

export type { Technician, TechnicianQuery, PagedResponse }

/**
 * 获取技师分页列表
 * @param query 查询参数
 * @returns 分页技师列表
 */
export async function getTechnicians(query?: TechnicianQuery): Promise<PagedResponse<Technician>> {
  const qs = buildQuery({
    keyword: query?.keyword,
    status: query?.status,
    source: query?.source,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Technician>>(`/technicians${qs}`)
}

/**
 * 按服务项目查询可用技师（预约时技能匹配过滤 + 服务项目页展示可服务技师）
 * 技能匹配：服务项目适用技能与技师技能标签沿技能分类树展开求交集，
 * 选父级分类时自动匹配其所有子级技能。
 * @param serviceProductId 服务项目ID（预约页语境，为空时返回指定来源全部启用技师）
 * @param masterId 商品主档ID（服务项目页语境，自动反查租户内 ServiceProduct）
 * @param source 技师来源（1:商家 2:平台，可选）
 * @returns 可用技师列表
 */
export async function getTechniciansAvailableByService(
  serviceProductId?: number,
  masterId?: number,
  source?: number
): Promise<Technician[]> {
  const qs = buildQuery({
    serviceProductId: serviceProductId,
    masterId: masterId,
    source: source
  })
  return request<Technician[]>(`/technicians/available-by-service${qs}`)
}
