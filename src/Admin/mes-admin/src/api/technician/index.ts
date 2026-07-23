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
    name: query?.name,
    phone: query?.phone,
    status: query?.status,
    source: query?.source,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Technician>>(`/technicians${qs}`)
}
