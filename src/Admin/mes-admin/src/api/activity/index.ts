// 活动管理模块 - API 服务
// 对接后端 ActivitiesController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Activity,
  ActivityQuery,
  ActivityCreate,
  ActivityUpdate,
  ActivityOption
} from './types'

// 导出类型供外部使用
export type {
  Activity,
  ActivityQuery,
  ActivityCreate,
  ActivityUpdate,
  ActivityOption,
  PagedResponse
}

/**
 * 获取活动分页列表
 * 对接后端：GET /api/store/activities
 * @param query 查询参数
 * @returns 分页活动列表
 */
export async function getActivities(query?: ActivityQuery): Promise<PagedResponse<Activity>> {
  const qs = buildQuery({
    name: query?.name,
    startDate: query?.startDate,
    endDate: query?.endDate,
    status: query?.status,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Activity>>(`/activities${qs}`)
}

/**
 * 获取活动详情
 * 对接后端：GET /api/store/activities/{id}
 * @param id 活动ID
 * @returns 活动详情
 */
export async function getActivityById(id: number): Promise<Activity> {
  return request<Activity>(`/activities/${id}`)
}

/**
 * 创建活动
 * 对接后端：POST /api/store/activities
 * @param data 活动信息
 * @returns 创建后的活动信息
 */
export async function createActivity(data: ActivityCreate): Promise<Activity> {
  return request<Activity>('/activities', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新活动
 * 对接后端：PUT /api/store/activities/{id}
 * @param data 活动信息
 * @returns 更新后的活动信息
 */
export async function updateActivity(data: ActivityUpdate): Promise<Activity> {
  return request<Activity>(`/activities/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除活动（软删除）
 * 对接后端：DELETE /api/store/activities/{id}
 * @param id 活动ID
 */
export async function deleteActivity(id: number): Promise<void> {
  await request<void>(`/activities/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 获取进行中活动下拉选项
 * 对接后端：GET /api/store/activities/options
 * @returns 进行中活动选项列表
 */
export async function getActivityOptions(): Promise<ActivityOption[]> {
  return request<ActivityOption[]>('/activities/options')
}
