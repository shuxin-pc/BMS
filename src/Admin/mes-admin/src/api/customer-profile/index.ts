// 客户档案增强 - API服务
// 肤质档案、服务反应记录、服务对比照片、身体数据记录均走真实后端 API
import type {
  CustomerBeautyProfile,
  BeautyProfileQuery,
  BeautyProfileSave,
  BeautyProfileUpdate,
  ServiceReaction,
  ServiceReactionQuery,
  ServiceReactionCreate,
  ServiceComparisonPhoto,
  ServiceComparisonPhotoItem,
  ComparisonPhotoQuery,
  ComparisonPhotoCreate,
  ComparisonPhotoItemSave,
  ComparisonPhotoUpdate,
  BodyDataRecord,
  BodyDataQuery,
  BodyDataCreate,
  CustomerOption
} from './types'
import { request, buildQuery, type ApiResponse, type PagedResponse } from '../shared/storeRequest'
import { getCustomers } from '../customer'

// 导出类型供外部使用
export type {
  CustomerBeautyProfile,
  BeautyProfileQuery,
  BeautyProfileSave,
  BeautyProfileUpdate,
  ServiceReaction,
  ServiceReactionQuery,
  ServiceReactionCreate,
  ServiceComparisonPhoto,
  ServiceComparisonPhotoItem,
  ComparisonPhotoQuery,
  ComparisonPhotoCreate,
  ComparisonPhotoItemSave,
  ComparisonPhotoUpdate,
  BodyDataRecord,
  BodyDataQuery,
  BodyDataCreate,
  CustomerOption,
  ApiResponse,
  PagedResponse
}

// ==================== 公共方法 ====================

/**
 * 获取客户列表（下拉选择用）
 * 对接后端：GET /api/store/customers，拉取全量后映射为简要信息
 * @returns 客户列表
 */
export async function getCustomerOptions(): Promise<CustomerOption[]> {
  const res = await getCustomers({ pageIndex: 1, pageSize: 9999 })
  return res.list.map(c => ({ id: c.id, name: c.name, phone: c.phone }))
}

// ==================== 肤质档案（真实后端 API） ====================

/**
 * 获取肤质档案分页列表
 * 对接后端：GET /api/store/customerbeautyprofiles
 * @param query 查询参数
 * @returns 分页肤质档案列表
 */
export async function getBeautyProfiles(query?: BeautyProfileQuery): Promise<PagedResponse<CustomerBeautyProfile>> {
  const params = buildQuery({
    customerId: query?.customerId,
    keyword: query?.keyword,
    skinType: query?.skinType,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  return request<PagedResponse<CustomerBeautyProfile>>(`/customerbeautyprofiles${params}`)
}

/**
 * 创建肤质档案
 * 对接后端：POST /api/store/customerbeautyprofiles
 * @param data 肤质档案信息
 * @returns 创建后的肤质档案
 */
export async function createBeautyProfile(data: BeautyProfileSave): Promise<CustomerBeautyProfile> {
  return request<CustomerBeautyProfile>(`/customerbeautyprofiles`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新肤质档案
 * 对接后端：PUT /api/store/customerbeautyprofiles/{id}
 * @param data 肤质档案信息（含档案 ID）
 * @returns 更新后的肤质档案
 */
export async function updateBeautyProfile(data: BeautyProfileUpdate): Promise<CustomerBeautyProfile> {
  return request<CustomerBeautyProfile>(`/customerbeautyprofiles/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除肤质档案（软删除）
 * 对接后端：DELETE /api/store/customerbeautyprofiles/{id}
 * @param id 档案ID
 */
export async function deleteBeautyProfile(id: number): Promise<void> {
  return request<void>(`/customerbeautyprofiles/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除肤质档案（软删除）
 * 对接后端：POST /api/store/customerbeautyprofiles/batch
 * @param ids 档案ID列表
 */
export async function batchDeleteBeautyProfiles(ids: number[]): Promise<void> {
  return request<void>(`/customerbeautyprofiles/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

// ==================== 过敏/服务反应记录（真实后端 API） ====================

/**
 * 获取服务反应分页列表
 * 对接后端：GET /api/store/servicereactions
 * @param query 查询参数
 * @returns 分页服务反应列表
 */
export async function getServiceReactions(query?: ServiceReactionQuery): Promise<PagedResponse<ServiceReaction>> {
  const params = buildQuery({
    customerId: query?.customerId,
    keyword: query?.keyword,
    startDate: query?.startDate,
    endDate: query?.endDate,
    severity: query?.severity,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  const res = await request<PagedResponse<ServiceReaction>>(`/servicereactions${params}`)
  // 后端 ReactionDate 为 DateTime 类型，JSON 序列化为 ISO 字符串，前端截取为 YYYY-MM-DD 以兼容展示
  res.list.forEach(item => {
    if (item.reactionDate) {
      item.reactionDate = item.reactionDate.substring(0, 10)
    }
  })
  return res
}

/**
 * 创建服务反应记录
 * 对接后端：POST /api/store/servicereactions
 * @param data 反应记录信息
 * @returns 创建后的记录
 */
export async function createServiceReaction(data: ServiceReactionCreate): Promise<ServiceReaction> {
  return request<ServiceReaction>(`/servicereactions`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

// ==================== 服务对比照片（真实后端 API） ====================

/**
 * 获取对比照片分页列表
 * 对接后端：GET /api/store/servicecomparisonphotos
 * @param query 查询参数
 * @returns 分页照片列表
 */
export async function getComparisonPhotos(query?: ComparisonPhotoQuery): Promise<PagedResponse<ServiceComparisonPhoto>> {
  const params = buildQuery({
    customerId: query?.customerId,
    keyword: query?.keyword,
    serviceItem: query?.serviceItem,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  const res = await request<PagedResponse<ServiceComparisonPhoto>>(`/servicecomparisonphotos${params}`)
  // 后端 PhotoDate 为 DateTime 类型，JSON 序列化为 ISO 字符串，前端截取为 YYYY-MM-DD 以兼容展示
  res.list.forEach(item => {
    if (item.photoDate) {
      item.photoDate = item.photoDate.substring(0, 10)
    }
  })
  return res
}

/**
 * 创建对比照片记录
 * 对接后端：POST /api/store/servicecomparisonphotos
 * @param data 照片信息
 * @returns 创建后的照片记录
 */
export async function createComparisonPhoto(data: ComparisonPhotoCreate): Promise<ServiceComparisonPhoto> {
  return request<ServiceComparisonPhoto>(`/servicecomparisonphotos`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新对比照片记录
 * 对接后端：PUT /api/store/servicecomparisonphotos/{id}
 * @param data 照片信息（含记录 ID）
 * @returns 更新后的照片记录
 */
export async function updateComparisonPhoto(data: ComparisonPhotoUpdate): Promise<ServiceComparisonPhoto> {
  return request<ServiceComparisonPhoto>(`/servicecomparisonphotos/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除对比照片记录
 * 对接后端：DELETE /api/store/servicecomparisonphotos/{id}
 * @param id 照片记录ID
 */
export async function deleteComparisonPhoto(id: number): Promise<void> {
  return request<void>(`/servicecomparisonphotos/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 批量删除对比照片记录
 * 对接后端：POST /api/store/servicecomparisonphotos/batch
 * @param ids 照片记录ID列表
 */
export async function batchDeleteComparisonPhotos(ids: number[]): Promise<void> {
  return request<void>(`/servicecomparisonphotos/batch`, {
    method: 'POST',
    body: JSON.stringify({ ids })
  })
}

// ==================== 身体数据记录（真实后端 API） ====================

/**
 * 获取身体数据分页列表
 * 对接后端：GET /api/store/bodydatarecords
 * @param query 查询参数
 * @returns 分页身体数据列表
 */
export async function getBodyDataRecords(query?: BodyDataQuery): Promise<PagedResponse<BodyDataRecord>> {
  const params = buildQuery({
    customerId: query?.customerId,
    keyword: query?.keyword,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  })
  const res = await request<PagedResponse<BodyDataRecord>>(`/bodydatarecords${params}`)
  // 后端 RecordDate 为 DateTime 类型，JSON 序列化为 ISO 字符串，前端截取为 YYYY-MM-DD 以兼容展示
  res.list.forEach(item => {
    if (item.recordDate) {
      item.recordDate = item.recordDate.substring(0, 10)
    }
  })
  return res
}

/**
 * 创建身体数据记录
 * 对接后端：POST /api/store/bodydatarecords
 * @param data 身体数据信息
 * @returns 创建后的记录
 */
export async function createBodyDataRecord(data: BodyDataCreate): Promise<BodyDataRecord> {
  return request<BodyDataRecord>(`/bodydatarecords`, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}
