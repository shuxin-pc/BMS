// ==========================================
// 疗程卡转让类型定义
// 对齐后端 Bms.Store.Domain.Entities.TreatmentCardTransfer
// ==========================================

/**
 * 转让状态
 * - 1: 已转让
 */
export type TransferStatus = number

/**
 * 疗程卡转让记录
 */
export interface TreatmentCardTransfer {
  /** 转让记录ID */
  id: number
  /** 操作门店ID */
  storeId?: number
  /** 操作门店编码 */
  storeCode?: string
  /** 操作门店名称（展示用） */
  storeName?: string
  /** 疗程卡销售记录ID */
  cardSaleId: number
  /** 疗程卡名称（展示用） */
  cardName?: string
  /** 销售单号（展示用） */
  saleNo?: string
  /** 原客户ID */
  fromCustomerId: number
  /** 原客户名称（展示用） */
  fromCustomerName?: string
  /** 原客户手机号（展示用） */
  fromCustomerPhone?: string
  /** 新客户ID */
  toCustomerId: number
  /** 新客户名称（展示用） */
  toCustomerName?: string
  /** 新客户手机号（展示用） */
  toCustomerPhone?: string
  /** 转让日期 */
  transferDate: string
  /** 转让手续费 */
  transferFee: number
  /** 操作员ID */
  operatorId?: number
  /** 操作员名称（展示用） */
  operatorName?: string
  /** 状态：1-已转让 */
  status: TransferStatus
  /** 备注 */
  remark?: string
}

/**
 * 疗程卡转让查询参数
 */
export interface TreatmentCardTransferQuery {
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 客户名称（模糊匹配，同时匹配原客户和新客户） */
  customerName?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建转让请求
 */
export interface TreatmentCardTransferCreate {
  /** 疗程卡销售记录ID */
  cardSaleId: number
  /** 新客户ID */
  toCustomerId: number
  /** 转让日期 */
  transferDate: string
  /** 转让手续费 */
  transferFee: number
  /** 备注 */
  remark?: string
}

/**
 * 疗程卡销售记录简要信息（转让选择用）
 */
export interface CardSaleOption {
  /** 销售记录ID */
  id: number
  /** 销售单号 */
  saleNo: string
  /** 卡名称 */
  cardName: string
  /** 客户ID */
  customerId: number
  /** 客户名称 */
  customerName: string
  /** 客户手机号 */
  customerPhone: string
  /** 剩余次数 */
  remainingCount: number
  /** 总次数 */
  totalCount: number
}

/**
 * 客户简要信息（转让选择新客户用）
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
