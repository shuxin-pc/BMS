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
  /** 分类编码 */
  code?: string
  /** 父分类ID */
  parentId: number
  /** 创建时间 */
  createdAt?: string
  /** 子分类 */
  children?: ProductCategory[]
}

/**
 * 商品信息（主表+子表字段合并输出）
 * 商品多态模型：主表区分类型，仅服务项目（type=2）有子表字段
 * - type=2 服务项目：duration、requiredRoomType、equipmentIds、equipmentNames、applicableSkills
 * Master 字段（name/code/type/spec/unit/brand 等）来自 ProductMaster，只读展示
 * Store 字段（price/costPrice/status 等）门店独立
 */
export interface Product {
  /** 商品ID */
  id: number
  /** 关联商品主档ID */
  masterId: number
  /** 商品名称（Master 字段，只读） */
  name: string
  /** 商品编码（Master 字段，只读） */
  code: string
  /** 商品分类ID（Master 字段，只读） */
  categoryId: number
  /** 商品分类名称 */
  categoryName?: string
  /** 商品类型：1-实物商品，2-服务项目，3-耗材，4-样品，5-赠品（Master 字段，只读） */
  type: ProductType
  /** 规格（Master 字段，只读） */
  spec?: string
  /** 单位（Master 字段，只读） */
  unit?: string
  /** 品牌（Master 字段，只读） */
  brand?: string
  /** 默认供应商ID（从 ProductSupplier.IsDefault=true 派生） */
  defaultSupplierId?: number
  /** 默认供应商名称 */
  defaultSupplierName?: string
  /** 售价（Store 字段，分店独立） */
  price: number
  /** 成本价（Store 字段） */
  costPrice?: number
  /** 上次采购价（采购入库时自动更新，分店独立采购） */
  lastPurchasePrice?: number
  /** 低库存预警阈值（Store 字段，空=不预警） */
  lowStockThreshold?: number
  /** 效期预警天数（Store 字段，空=不预警） */
  expiryAlertDays?: number
  /** 积压预警阈值（Store 字段，空=不预警） */
  overstockThreshold?: number
  /** 商品图片URL（Master 字段，只读） */
  imageUrl?: string
  /** 上架状态（Store 字段）：1-上架，2-下架 */
  status: ProductStatus
  /** 是否可销售（Master 字段，样品/赠品为 false） */
  isSalable: boolean
  /** 分店级备注（Store 字段） */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string

  // ========== 服务项目子表字段（type=2，Master 层）==========
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
 * 创建门店商品档案请求（仅 Store 字段 + 关联主档）
 * Master 字段由主档统一管理，门店仅承载定价/预警/上架等独立配置
 * 对应后端 ProductCreateDto
 */
export interface ProductCreate {
  /** 关联商品主档ID */
  masterId: number
  /** 售价（Store 字段，分店独立定价） */
  price: number
  /** 成本价（Store 字段） */
  costPrice?: number
  /** 低库存预警阈值（Store 字段，空=不预警） */
  lowStockThreshold?: number
  /** 效期预警天数（Store 字段，空=不预警） */
  expiryAlertDays?: number
  /** 积压预警阈值（Store 字段，空=不预警） */
  overstockThreshold?: number
  /** 上架状态（Store 字段）：1-上架，2-下架 */
  status: ProductStatus
  /** 分店级备注 */
  remark?: string
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
