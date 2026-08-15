// 商品主档管理 - API 服务
// 对应后端 ProductMastersController（路由 api/store/product/product-masters）
// Master 字段全租户生效，Store 字段通过 batchConfigStoreFields 统一配置
import type { PagedResponse } from './types'
import type { ProductType, RequiredRoomType } from './types'
import { request, buildQuery } from '../shared/storeRequest'

// Store 子系统下商品主档路由前缀（storeRequest 的 API_BASE 已是 /api/store）
const API_BASE = '/product/product-masters'

// ==================== 类型定义 ====================

/**
 * 商品主档（租户级，承载商品本质属性）
 * 对应后端 ProductMasterDto
 */
export interface ProductMaster {
  /** 主档ID */
  id: number
  /** 商品编码（租户内唯一） */
  code: string
  /** 商品名称 */
  name: string
  /** 商品类型：1-实物 2-服务 3-耗材 4-样品 5-赠品 */
  type: ProductType
  /** 分类ID（租户级 ProductCategory） */
  categoryId: number
  /** 分类名称（展示用） */
  categoryName?: string
  /** 单位 */
  unit?: string
  /** 规格 */
  specification?: string
  /** 品牌 */
  brand?: string
  /** 商品图片URL */
  imageUrl?: string
  /** 是否可销售（样品/赠品为 false，跟 Type 走） */
  isSalable: boolean
  /** 主档级备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ========== 服务项目子表字段（type=2）==========
  /** 服务时长（分钟，服务项目） */
  duration?: number
  /** 所需房间/床位类型（1:房间 2:床位，undefined=不限，服务项目） */
  requiredRoomType?: RequiredRoomType
  /** 所需设备类型 ID 列表（服务项目） */
  equipmentTypeIds?: number[]
  /** 所需设备类型名称列表（服务项目，展示用） */
  equipmentTypeNames?: string[]
  /** 适用技师技能分类 ID 列表（服务项目） */
  skillCategoryIds?: number[]
  /** 适用技师技能分类名称列表（服务项目，展示用） */
  skillCategoryNames?: string[]
}

/**
 * 创建商品主档请求
 * 对应后端 ProductMasterCreateDto
 */
export interface ProductMasterCreate {
  code: string
  name: string
  type: ProductType
  categoryId: number
  unit?: string
  specification?: string
  brand?: string
  imageUrl?: string
  remark?: string
  // 服务项目子表字段（type=2）
  duration?: number
  requiredRoomType?: RequiredRoomType
  equipmentTypeIds?: number[]
  skillCategoryIds?: number[]
}

/**
 * 更新商品主档请求
 * 对应后端 ProductMasterUpdateDto
 */
export interface ProductMasterUpdate extends ProductMasterCreate {
  id: number
}

/**
 * 商品主档查询参数
 * 对应后端 ProductMasterQueryDto
 */
export interface ProductMasterQuery {
  name?: string
  code?: string
  type?: ProductType
  categoryId?: number
  pageIndex?: number
  pageSize?: number
}

/**
 * 商品主档轻量选项（下拉选择用）
 * 对应后端 ProductMasterOptionDto
 */
export interface ProductMasterOption {
  id: number
  code: string
  name: string
  type: ProductType
  unit?: string
}

/**
 * 统一配置门店档案 Store 字段请求
 * 对应后端 ProductStoreBatchConfigDto
 * 将 Store 字段值应用到选中门店：已有 Product -> 覆盖；无 Product -> 自动创建
 */
export interface ProductStoreBatchConfig {
  masterId: number
  /** 选中门店ID列表（雪花ID，以字符串形式传递避免 JS 精度丢失；后端 List<long>+LongToStringConverter 支持） */
  storeIds: string[]
  /** 零售价（分店独立定价） */
  price: number
  /** 成本价（分店独立） */
  costPrice?: number
  /** 低库存预警阈值（null=不预警） */
  lowStockThreshold?: number
  /** 效期预警天数（null=不预警） */
  expiryAlertDays?: number
  /** 积压预警阈值（null=不预警） */
  overstockThreshold?: number
  /** 上架状态（1:上架 2:下架） */
  status: 1 | 2
  /** 分店级备注 */
  remark?: string
}

/**
 * 门店档案配置预览结果
 * 对应后端 ProductStoreConfigPreviewDto
 * 用于二次确认时区分"将被覆盖"和"将新建"的门店
 */
export interface StoreConfigPreview {
  /** 选中门店中已存在档案的门店ID列表（雪花ID字符串，这些门店将被覆盖） */
  existingStoreIds: string[]
}

// ==================== API 函数 ====================

/**
 * 获取商品主档分页列表
 * @param query 查询参数
 */
export async function getProductMasters(query?: ProductMasterQuery): Promise<PagedResponse<ProductMaster>> {
  const params: Record<string, string | number | undefined | null> = {
    pageIndex: query?.pageIndex || 1,
    pageSize: query?.pageSize || 20
  }
  if (query?.name) params.name = query.name
  if (query?.code) params.code = query.code
  if (query?.type !== undefined) params.type = query.type
  if (query?.categoryId !== undefined) params.categoryId = query.categoryId
  return request<PagedResponse<ProductMaster>>(`${API_BASE}${buildQuery(params)}`)
}

/**
 * 获取商品主档详情
 * @param id 主档ID
 */
export async function getProductMaster(id: number): Promise<ProductMaster> {
  return request<ProductMaster>(`${API_BASE}/${id}`)
}

/**
 * 创建商品主档
 * @param data 主档信息
 */
export async function createProductMaster(data: ProductMasterCreate): Promise<ProductMaster> {
  return request<ProductMaster>(API_BASE, {
    method: 'POST',
    body: JSON.stringify(data)
  })
}

/**
 * 更新商品主档（Master 字段全租户生效）
 * @param data 主档信息
 */
export async function updateProductMaster(data: ProductMasterUpdate): Promise<ProductMaster> {
  return request<ProductMaster>(`${API_BASE}/${data.id}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  })
}

/**
 * 删除商品主档（库存检查 + 级联软删除，对应设计文档 8.1 节）
 * @param id 主档ID
 */
export async function deleteProductMaster(id: number): Promise<void> {
  return request<void>(`${API_BASE}/${id}`, {
    method: 'DELETE'
  })
}

/**
 * 统一配置门店档案 Store 字段（对应设计文档 7.2/7.3 节）
 * 将 Store 字段值应用到选中门店：已有 Product -> 覆盖；无 Product -> 自动创建
 * @param masterId 主档ID
 * @param data Store 字段配置
 */
export async function batchConfigStoreFields(masterId: number, data: Omit<ProductStoreBatchConfig, 'masterId'>): Promise<void> {
  // body 内回填 masterId：FluentValidation 自动验证在 action 赋值前运行，
  // 若 body 缺失 masterId 会被验证为 0 导致 400；后端 Controller 仍以 URL 路由的 masterId 为准
  return request<void>(`${API_BASE}/${masterId}/store-config`, {
    method: 'POST',
    body: JSON.stringify({ ...data, masterId })
  })
}

/**
 * 预览门店档案配置（对应设计文档 7.3 节二次确认）
 * 查询选中门店中哪些已存在档案（将被覆盖）、哪些无档案（将新建）
 * @param masterId 主档ID
 * @param storeIds 选中门店ID列表（雪花ID字符串，避免精度丢失）
 */
export async function getStoreConfigPreview(masterId: number, storeIds: string[]): Promise<StoreConfigPreview> {
  const qs = storeIds.length > 0 ? `?storeIds=${storeIds.join(',')}` : ''
  return request<StoreConfigPreview>(`${API_BASE}/${masterId}/store-config/preview${qs}`)
}

/**
 * 获取商品主档轻量选项列表（下拉选择用）
 */
export async function getProductMasterOptions(): Promise<ProductMasterOption[]> {
  return request<ProductMasterOption[]>(`${API_BASE}/options`)
}
