// ==========================================
// 供应商管理类型定义
// 字段与后端 SupplierDto / SupplierCreateDto / SupplierQueryDto 对齐（camelCase）
// ==========================================

/**
 * 合作状态
 * - 0: 禁用
 * - 1: 启用
 */
export type CooperationStatus = number

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
 * 供应商信息（与后端 SupplierDto 对齐）
 */
export interface Supplier {
  /** 供应商ID */
  id: number
  /** 供应商名称 */
  name: string
  /** 供应商编码 */
  code: string
  /** 联系人 */
  contact?: string
  /** 联系电话 */
  phone?: string
  /** 地址 */
  address?: string
  /** 银行账户 */
  bankAccount?: string
  /** 状态：0-禁用，1-启用 */
  status: CooperationStatus
  /** 数据范围：1-门店通用，2-门店私用 */
  scope: number
  /** 备注 */
  remark?: string
  /** 累计采购额（只读，后端计算） */
  totalPurchaseAmount: number
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 供应商查询参数（与后端 SupplierQueryDto 对齐）
 */
export interface SupplierQuery {
  /** 供应商名称（模糊匹配） */
  name?: string
  /** 供应商编码 */
  code?: string
  /** 状态筛选：0-禁用，1-启用 */
  status?: CooperationStatus
  /** 数据范围筛选：1-门店通用，2-门店私用 */
  scope?: number
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建供应商请求（与后端 SupplierCreateDto 对齐）
 */
export interface SupplierCreate {
  name: string
  code: string
  contact?: string
  phone?: string
  address?: string
  bankAccount?: string
  status: CooperationStatus
  /** 数据范围：1-门店通用，2-门店私用（默认门店私用） */
  scope: number
  remark?: string
}

/**
 * 更新供应商请求（与后端 SupplierUpdateDto 对齐，继承 Create + id）
 */
export interface SupplierUpdate extends SupplierCreate {
  id: number
}

/**
 * 供应商-品项关联（与后端 ProductSupplierDto 对齐）
 */
export interface ProductSupplier {
  /** 关联ID */
  id: number
  /** 商品ID */
  productId: number
  /** 商品编码 */
  productCode?: string
  /** 商品名称 */
  productName?: string
  /** 商品类型（1:实物商品 2:服务商品 3:耗材 4:样品 5:赠品），用于采购订单按采购类型过滤 */
  productType?: number
  /** 供应商ID */
  supplierId: number
  /** 供应商编码 */
  supplierCode?: string
  /** 供应商名称 */
  supplierName?: string
  /** 是否默认供应商 */
  isDefault: boolean
  /** 参考价 */
  referencePrice?: number
  /** 供货周期（天，0=即时供货，前端展示为空） */
  leadTimeDays?: number
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 批量绑定品项到供应商请求（与后端 BindProductsDto 对齐）
 */
export interface BindProductsRequest {
  /** 供应商ID */
  supplierId: number
  /** 品项ID列表 */
  productIds: number[]
}

/**
 * 设置品项默认供应商请求（与后端 SetDefaultSupplierDto 对齐）
 */
export interface SetDefaultSupplierRequest {
  /** 商品ID */
  productId: number
  /** 供应商ID */
  supplierId: number
  /** 参考价（可选） */
  referencePrice?: number
  /** 供货周期天数（可选） */
  leadTimeDays?: number
}
