// ==========================================
// 商品档案管理类型定义
// ==========================================

/**
 * 商品状态
 * - 1: 上架
 * - 2: 下架
 */
export type ProductStatus = 1 | 2

/**
 * 商品类型
 * - 1: 实物商品（关联 PhysicalProduct 子表，需库存管理）
 * - 2: 服务项目（关联 ServiceProduct 子表，无需库存管理）
 * - 3: 耗材（关联 Consumable 子表，需库存管理，不直接销售）
 * - 4: 样品（关联 SampleGift 子表，需库存管理，用于客户体验领用）
 * - 5: 赠品（关联 SampleGift 子表，需库存管理，用于活动赠送）
 * 依据：通用需求规格.md G2.2 + 设计决策"品项多态模型：主表+子表"
 */
export type ProductType = 1 | 2 | 3 | 4 | 5

/**
 * 所需房间/床位类型（服务项目）
 * - 1: 房间
 * - 2: 床位
 * - undefined: 不限
 */
export type RequiredRoomType = 1 | 2 | undefined

/**
 * 商品分类
 */
export interface ProductCategory {
  /** 分类ID */
  id: number
  /** 分类名称 */
  name: string
  /** 父分类ID */
  parentId: number
  /** 排序 */
  sort: number
  /** 子分类 */
  children?: ProductCategory[]
}

/**
 * 商品信息（主表+子表字段合并输出）
 * 商品多态模型：主表区分类型，仅服务项目（type=2）有子表字段
 * - type=2 服务项目：duration、requiredRoomType、equipmentIds、equipmentNames、applicableSkills
 */
export interface Product {
  /** 商品ID */
  id: number
  /** 商品名称 */
  name: string
  /** 商品编码 */
  code: string
  /** 商品分类ID */
  categoryId: number
  /** 商品分类名称 */
  categoryName?: string
  /** 商品类型：1-实物商品，2-服务项目，3-耗材 */
  type: ProductType
  /** 规格 */
  spec?: string
  /** 单位 */
  unit?: string
  /** 品牌 */
  brand?: string
  /** 供应商ID */
  supplierId?: number
  /** 售价 */
  price: number
  /** 成本价 */
  costPrice?: number
  /** 低库存预警阈值（低于此值触发预警，空=不预警） */
  lowStockThreshold?: number
  /** 效期预警天数（剩余天数小于等于此值触发预警，空=不预警） */
  expiryAlertDays?: number
  /** 积压预警阈值（超过此值触发预警，空=不预警） */
  overstockThreshold?: number
  /** 商品图片URL */
  imageUrl?: string
  /** 商品状态：1-上架，2-下架 */
  status: ProductStatus
  /** 是否可销售（样品/赠品为 false，不可通过 POS 销售下单） */
  isSalable: boolean
  /** 商品描述 */
  description?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ========== 服务项目子表字段（type=2）==========
  /** 服务时长（分钟，服务项目） */
  duration?: number
  /** 所需房间/床位类型（1:房间 2:床位，undefined=不限，服务项目） */
  requiredRoomType?: RequiredRoomType
  /** 所需仪器 ID 列表（服务项目） */
  equipmentIds?: number[]
  /** 所需仪器名称列表（服务项目，后端返回用于展示） */
  equipmentNames?: string[]
  /** 适用技师技能标签（服务项目） */
  applicableSkills?: string
}

/**
 * 商品查询参数
 */
export interface ProductQuery {
  /** 商品名称（模糊匹配） */
  name?: string
  /** 商品编码（模糊匹配） */
  code?: string
  /** 分类ID */
  categoryId?: number
  /** 供应商ID */
  supplierId?: number
  /** 状态筛选 */
  status?: ProductStatus
  /** 类型筛选 */
  type?: ProductType
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建商品请求（主表+子表字段合并）
 * 调用方根据 type 填充对应子表字段
 */
export interface ProductCreate {
  name: string
  code: string
  categoryId: number
  type: ProductType
  spec?: string
  unit?: string
  price: number
  costPrice?: number
  lowStockThreshold?: number
  expiryAlertDays?: number
  overstockThreshold?: number
  imageUrl?: string
  status: ProductStatus
  description?: string

  // 服务项目子表字段（type=2）
  duration?: number
  requiredRoomType?: RequiredRoomType
  /** 所需仪器 ID 列表（服务项目） */
  equipmentIds?: number[]
  applicableSkills?: string
}

/**
 * 更新商品请求
 */
export interface ProductUpdate extends ProductCreate {
  id: number
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
