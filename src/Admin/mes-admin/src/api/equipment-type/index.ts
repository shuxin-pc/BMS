// 设备类型管理 - API 服务
// 对接后端 EquipmentTypesController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  EquipmentType,
  EquipmentTypeQuery,
  EquipmentTypeCreate,
  EquipmentTypeUpdate
} from './types'

// 导出类型供外部使用
export type {
  EquipmentType,
  EquipmentTypeQuery,
  EquipmentTypeCreate,
  EquipmentTypeUpdate,
  PagedResponse
}

/**
 * 获取设备类型分页列表
 * 对接后端：GET /api/store/equipment-types
 * @param query 查询参数
 * @returns 分页设备类型列表
 */
export async function getEquipmentTypeList(query?: EquipmentTypeQuery): Promise<PagedResponse<EquipmentType>> {
  const qs = buildQuery({
    name: query?.name,
    code: query?.code,
    isActive: query?.isActive,
    parentId: query?.parentId,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<EquipmentType>>(`/equipment-types${qs}`)
}

/**
 * 获取全部启用设备类型（用于下拉选择，如设备档案表单、服务项目所需仪器）
 * 对接后端：GET /api/store/equipment-types/options
 * 仅返回叶子节点（具体型号），不含作为分组的父级分类节点
 * @returns 启用设备类型列表
 */
export async function getEquipmentTypeOptions(): Promise<EquipmentType[]> {
  return request<EquipmentType[]>('/equipment-types/options')
}

/**
 * 获取设备类型树（含父级分类节点，用于管理页树形展示）
 * 对接后端：GET /api/store/equipment-types/tree
 * @returns 设备类型树
 */
export async function getEquipmentTypeTree(): Promise<EquipmentType[]> {
  return request<EquipmentType[]>('/equipment-types/tree')
}

/**
 * 获取设备类型详情
 * 对接后端：GET /api/store/equipment-types/{id}
 * @param id 类型ID
 * @returns 设备类型详情
 */
export async function getEquipmentType(id: number): Promise<EquipmentType> {
  return request<EquipmentType>(`/equipment-types/${id}`)
}

/**
 * 新建设备类型
 * 对接后端：POST /api/store/equipment-types
 * @param data 设备类型信息
 * @returns 创建后的设备类型
 */
export async function createEquipmentType(data: EquipmentTypeCreate): Promise<EquipmentType> {
  return request<EquipmentType>('/equipment-types', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新设备类型
 * 对接后端：PUT /api/store/equipment-types/{id}
 * @param data 设备类型信息
 * @returns 更新后的设备类型
 */
export async function updateEquipmentType(data: EquipmentTypeUpdate): Promise<EquipmentType> {
  return request<EquipmentType>(`/equipment-types/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除设备类型
 * 对接后端：DELETE /api/store/equipment-types/{id}
 * 被设备或服务项目引用时后端会拒绝删除
 * @param id 类型ID
 */
export async function deleteEquipmentType(id: number): Promise<void> {
  await request<unknown>(`/equipment-types/${id}`, {
    method: 'DELETE'
  })
}
