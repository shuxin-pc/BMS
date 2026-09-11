// ==========================================
// 项目卡转让类型定义
// 对齐后端 Bms.Store.Application.Dtos.TreatmentCardTransfers
// 注意：雪花ID主键经后端 LongToStringConverter 序列化为字符串，必须按字符串传递，禁止 Number() 转换
// ==========================================

/**
 * 转让状态
 * - 1: 已转让
 */
export type TransferStatus = number

/**
 * 项目卡转让记录
 * 对齐后端 TreatmentCardTransferDto（含 join 展示字段）
 */
export interface TreatmentCardTransfer {
  /** 转让记录ID（雪花ID，字符串） */
  id: string
  /** 操作门店ID（雪花ID，字符串） */
  storeId?: string
  /** 操作门店编码 */
  storeCode?: string
  /** 操作门店名称（join Store 展示） */
  storeName?: string
  /** 项目卡销售记录ID（雪花ID，字符串） */
  cardSaleId: string
  /** 项目卡名称（join TreatmentCard 展示） */
  cardName?: string
  /** 原客户ID（雪花ID，字符串） */
  fromCustomerId: string
  /** 原客户名称（join Customer 展示） */
  fromCustomerName?: string
  /** 原客户手机号（join Customer 展示） */
  fromCustomerPhone?: string
  /** 新客户ID（雪花ID，字符串） */
  toCustomerId: string
  /** 新客户名称（join Customer 展示） */
  toCustomerName?: string
  /** 新客户手机号（join Customer 展示） */
  toCustomerPhone?: string
  /** 转让日期（后端 DateTime 序列化为 ISO 字符串） */
  transferDate: string
  /** 转让手续费 */
  transferFee: number
  /** 操作员ID（雪花ID，字符串） */
  operatorId?: string
  /** 操作员姓名（转让时姓名快照） */
  operatorName?: string
  /** 状态：1-已转让 */
  status: TransferStatus
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 项目卡转让查询参数
 * 对齐后端 TreatmentCardTransferQueryDto
 */
export interface TreatmentCardTransferQuery {
  /** 转让日期范围 - 开始日期（yyyy-MM-dd） */
  startDate?: string
  /** 转让日期范围 - 结束日期（yyyy-MM-dd） */
  endDate?: string
  /** 客户名称或手机号关键字（模糊匹配，同时匹配原客户/新客户的姓名或手机号，OR 语义） */
  keyword?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建转让请求
 * 对齐后端 TreatmentCardTransferCreateDto
 */
export interface TreatmentCardTransferCreate {
  /** 项目卡销售记录ID（雪花ID，字符串） */
  cardSaleId: string
  /** 原客户ID（雪花ID，字符串，由选卡自动带出） */
  fromCustomerId: string
  /** 新客户ID（雪花ID，字符串） */
  toCustomerId: string
  /** 转让日期（yyyy-MM-dd） */
  transferDate: string
  /** 转让手续费 */
  transferFee: number
  /** 备注 */
  remark?: string
}

/**
 * 可转让项目卡销售记录选项（转卡弹窗选择用）
 * 对齐后端 TreatmentCardTransferOptionDto
 */
export interface CardSaleOption {
  /** 项目卡销售记录ID（雪花ID，字符串） */
  id: string
  /** 卡名称 */
  cardName: string
  /** 当前客户ID（卡归属客户，即原客户） */
  customerId: string
  /** 客户名称 */
  customerName: string
  /** 客户手机号 */
  customerPhone: string
  /** 剩余次数 */
  remainingTimes: number
  /** 总次数 */
  totalTimes: number
}

/**
 * 客户简要信息（转让选择新客户用）
 */
export interface CustomerOption {
  /** 客户ID（雪花ID，字符串） */
  id: string
  /** 客户名称 */
  name: string
  /** 手机号 */
  phone: string
}
