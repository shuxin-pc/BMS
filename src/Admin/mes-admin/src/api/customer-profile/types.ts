// ==========================================
// 客户档案增强类型定义
// 包含：肤质档案、过敏记录、服务对比照片、身体数据记录
// ==========================================

/**
 * 肤质类型
 * - dry: 干性
 * - oily: 油性
 * - combination: 混合性
 * - sensitive: 敏感性
 * - normal: 中性
 */
export type SkinType = 'dry' | 'oily' | 'combination' | 'sensitive' | 'normal'

/**
 * 敏感程度
 * - low: 低
 * - medium: 中
 * - high: 高
 */
export type SensitivityLevel = 'low' | 'medium' | 'high'

/**
 * 肤质档案
 * 对齐后端 Bms.Store.Domain.Entities.CustomerBeautyProfile
 */
export interface CustomerBeautyProfile {
  /** 档案ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称（展示用） */
  customerName?: string
  /** 客户手机号（展示用） */
  customerPhone?: string
  /** 肤质类型 */
  skinType?: SkinType
  /** 敏感程度 */
  sensitivity?: SensitivityLevel
  /** 发质情况 */
  hairType?: string
  /** 过敏史和禁忌成分 */
  allergyHistory?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 肤质档案查询参数
 */
export interface BeautyProfileQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 肤质类型 */
  skinType?: SkinType
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建/更新肤质档案请求
 */
export interface BeautyProfileSave {
  /** 客户ID */
  customerId: number
  /** 肤质类型 */
  skinType?: SkinType
  /** 敏感程度 */
  sensitivity?: SensitivityLevel
  /** 发质情况 */
  hairType?: string
  /** 过敏史和禁忌成分 */
  allergyHistory?: string
  /** 备注 */
  remark?: string
}

// ==========================================
// 过敏/服务反应记录
// ==========================================

/**
 * 严重程度
 * - 1: 轻微
 * - 2: 中等
 * - 3: 严重
 */
export type SeverityLevel = 1 | 2 | 3

/**
 * 服务反应记录
 * 对齐后端 Bms.Store.Domain.Entities.ServiceReaction
 */
export interface ServiceReaction {
  /** 记录ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称（展示用） */
  customerName?: string
  /** 客户手机号（展示用） */
  customerPhone?: string
  /** 关联订单ID */
  orderId?: number
  /** 关联订单号（展示用） */
  orderNo?: string
  /** 服务项目 */
  serviceItem?: string
  /** 反应日期 */
  reactionDate: string
  /** 服务反应描述 */
  reaction?: string
  /** 严重程度：1-轻微，2-中等，3-严重 */
  severity?: SeverityLevel
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
}

/**
 * 服务反应查询参数
 */
export interface ServiceReactionQuery {
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 严重程度 */
  severity?: SeverityLevel
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建服务反应记录请求
 */
export interface ServiceReactionCreate {
  /** 客户ID */
  customerId: number
  /** 关联订单ID */
  orderId?: number
  /** 服务项目 */
  serviceItem?: string
  /** 反应日期 */
  reactionDate: string
  /** 服务反应描述 */
  reaction?: string
  /** 严重程度 */
  severity?: SeverityLevel
  /** 备注 */
  remark?: string
}

// ==========================================
// 服务前后对比照片
// ==========================================

/**
 * 照片类型
 * - 1: 服务前
 * - 2: 服务后
 */
export type PhotoType = 1 | 2

/**
 * 服务对比照片
 * 对齐后端 Bms.Store.Domain.Entities.ServiceComparisonPhoto
 */
export interface ServiceComparisonPhoto {
  /** 照片ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称（展示用） */
  customerName?: string
  /** 关联订单ID */
  orderId?: number
  /** 关联订单号（展示用） */
  orderNo?: string
  /** 服务项目 */
  serviceItem?: string
  /** 拍照日期 */
  photoDate: string
  /** 照片类型：1-服务前，2-服务后 */
  photoType: PhotoType
  /** 照片URL */
  photoUrl: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
}

/**
 * 对比照片查询参数
 */
export interface ComparisonPhotoQuery {
  /** 客户ID */
  customerId?: number
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 服务项目（模糊匹配） */
  serviceItem?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建对比照片请求
 */
export interface ComparisonPhotoCreate {
  /** 客户ID */
  customerId: number
  /** 关联订单ID */
  orderId?: number
  /** 服务项目 */
  serviceItem?: string
  /** 拍照日期 */
  photoDate: string
  /** 照片类型：1-服务前，2-服务后 */
  photoType: PhotoType
  /** 照片URL */
  photoUrl: string
  /** 备注 */
  remark?: string
}

/**
 * 照片配对组（服务前 + 服务后）
 */
export interface PhotoPair {
  /** 服务前照片 */
  before?: ServiceComparisonPhoto
  /** 服务后照片 */
  after?: ServiceComparisonPhoto
  /** 服务项目 */
  serviceItem?: string
  /** 拍照日期 */
  photoDate: string
}

// ==========================================
// 身体数据记录
// ==========================================

/**
 * 身体数据记录
 * 对齐后端 Bms.Store.Domain.Entities.BodyDataRecord
 */
export interface BodyDataRecord {
  /** 记录ID */
  id: number
  /** 客户ID */
  customerId: number
  /** 客户名称（展示用） */
  customerName?: string
  /** 客户手机号（展示用） */
  customerPhone?: string
  /** 记录日期 */
  recordDate: string
  /** 体重(kg) */
  weight?: number
  /** 体脂率(%) */
  bodyFat?: number
  /** 胸围(cm) */
  bust?: number
  /** 腰围(cm) */
  waist?: number
  /** 臀围(cm) */
  hip?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
}

/**
 * 身体数据查询参数
 */
export interface BodyDataQuery {
  /** 客户ID */
  customerId?: number
  /** 客户名称（模糊匹配） */
  customerName?: string
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建身体数据记录请求
 */
export interface BodyDataCreate {
  /** 客户ID */
  customerId: number
  /** 记录日期 */
  recordDate: string
  /** 体重(kg) */
  weight?: number
  /** 体脂率(%) */
  bodyFat?: number
  /** 胸围(cm) */
  bust?: number
  /** 腰围(cm) */
  waist?: number
  /** 臀围(cm) */
  hip?: number
  /** 备注 */
  remark?: string
}

/**
 * 客户简要信息（下拉选择用）
 */
export interface CustomerOption {
  /** 客户ID */
  id: number
  /** 客户名称 */
  name: string
  /** 手机号 */
  phone: string
}

/**
 * 通用分页响应
 */
export interface PagedResponse<T> {
  list: T[]
  total: number
  pageIndex: number
  pageSize: number
}

/**
 * 通用API响应
 */
export interface ApiResponse<T> {
  code: number
  message?: string
  data: T
}
