// 服务人员模块 - API服务
// 对接后端 TechniciansController / TechnicianStatisticsController
import { request, buildQuery } from '../shared/storeRequest'
import type { PagedResponse } from '../shared/storeRequest'
import type {
  Technician,
  TechnicianQuery,
  TechnicianCreate,
  TechnicianUpdate,
  TechnicianStatistics,
  TechnicianStatisticsQuery,
  TechnicianStatisticReport,
  TechnicianStatReport,
  TechnicianServiceItem,
  TechnicianSource
} from './types'

// 导出类型供外部使用
export type {
  Technician,
  TechnicianQuery,
  TechnicianCreate,
  TechnicianUpdate,
  TechnicianStatistics,
  TechnicianStatisticsQuery,
  TechnicianStatisticReport,
  TechnicianStatReport,
  TechnicianServiceItem,
  TechnicianSource,
  PagedResponse
}

// ==================== 技师档案管理 ====================
// 对接后端 TechniciansController（路由 /api/store/technicians）

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
 * 获取技师详情
 * @param id 技师ID
 * @returns 技师详情
 */
export async function getTechnician(id: number): Promise<Technician> {
  return request<Technician>(`/technicians/${id}`)
}

/**
 * 创建技师
 * @param data 技师信息
 * @returns 创建后的技师信息
 */
export async function createTechnician(data: TechnicianCreate): Promise<Technician> {
  return request<Technician>('/technicians', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新技师
 * @param data 技师信息
 * @returns 更新后的技师信息
 */
export async function updateTechnician(data: TechnicianUpdate): Promise<Technician> {
  return request<Technician>(`/technicians/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除技师
 * @param id 技师ID
 */
export async function deleteTechnician(id: number): Promise<void> {
  await request(`/technicians/${id}`, { method: 'DELETE' })
}

/**
 * 批量删除技师
 * @param ids 技师ID列表
 */
export async function deleteTechnicians(ids: number[]): Promise<void> {
  await request('/technicians/batch', {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
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

/**
 * 查询技师可服务的服务项目列表（技师页展示擅长项目，双向匹配展示用）
 * @param id 技师ID
 * @returns 可服务项目列表
 */
export async function getTechnicianServices(id: number): Promise<TechnicianServiceItem[]> {
  return request<TechnicianServiceItem[]>(`/technicians/${id}/services`)
}

// ==================== 技师统计 ====================
// 对接后端 TechnicianStatisticsController（路由 /api/store/technicianStatistics）

/**
 * 获取技师统计分页列表
 * @param query 查询参数
 * @returns 分页技师统计列表
 */
export async function getTechnicianStatistics(query?: TechnicianStatisticsQuery): Promise<PagedResponse<TechnicianStatistics>> {
  const qs = buildQuery({
    technicianId: query?.technicianId,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TechnicianStatistics>>(`/technicianStatistics${qs}`)
}

/**
 * 获取技师业绩统计报表（从 OrderItem 实时聚合，仅商家技师）
 * 纯平台技师门店返回 isPurePlatformStore=true + 空列表，业绩由平台统一统计
 * @param query 查询参数（日期范围、技师ID、分页、force 强制查看）
 * @returns 技师业绩报表响应（含纯平台技师门店标识）
 */
export async function getTechnicianStatisticReport(query?: TechnicianStatisticsQuery): Promise<TechnicianStatReport> {
  const qs = buildQuery({
    technicianId: query?.technicianId,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize,
    // 仅在 force=true 时传递，避免默认 false 污染 URL
    force: query?.force === true ? 'true' : undefined
  })
  return request<TechnicianStatReport>(`/technicianStatistics/report${qs}`)
}
