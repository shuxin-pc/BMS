// 项目卡转让 - API服务
// 对接后端 TreatmentCardTransfersController（路由 /api/store/treatmentCardTransfers）
// 注意：雪花ID主键经后端 LongToStringConverter 序列化为字符串，全程按字符串传递，禁止 Number() 转换
import { request, buildQuery, type PagedResponse } from '../shared/storeRequest'
import { getCustomers } from '@/api/customer'
import type {
  TreatmentCardTransfer,
  TreatmentCardTransferQuery,
  TreatmentCardTransferCreate,
  CardSaleOption,
  CustomerOption
} from './types'

// 导出类型供外部使用
export type {
  TreatmentCardTransfer,
  TreatmentCardTransferQuery,
  TreatmentCardTransferCreate,
  CardSaleOption,
  CustomerOption,
  PagedResponse
}

/**
 * 获取项目卡转让分页列表
 * 对接后端：GET /api/store/treatmentCardTransfers
 * @param query 查询参数
 * @returns 分页转让记录列表
 */
export async function getTreatmentCardTransfers(query?: TreatmentCardTransferQuery): Promise<PagedResponse<TreatmentCardTransfer>> {
  const qs = buildQuery({
    startDate: query?.startDate,
    endDate: query?.endDate,
    keyword: query?.keyword,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<TreatmentCardTransfer>>(`/treatmentCardTransfers${qs}`)
}

/**
 * 获取可转让的项目卡销售记录列表
 * 对接后端：GET /api/store/treatmentCardTransfers/transferable-card-sales
 * 仅返回状态有效且有剩余次数的卡销售记录（跨店购卡均可见）
 * @returns 可转让的项目卡列表
 */
export async function getTransferableCardSales(): Promise<CardSaleOption[]> {
  return request<CardSaleOption[]>('/treatmentCardTransfers/transferable-card-sales')
}

/**
 * 获取客户列表（选择新客户用）
 * 复用客户分页接口拉取全量，映射为下拉选项
 * @returns 客户列表
 */
export async function getCustomerOptions(): Promise<CustomerOption[]> {
  const result = await getCustomers({ pageIndex: 1, pageSize: 9999 })
  return result.list.map(c => ({
    id: String(c.id),
    name: c.name,
    phone: c.phone
  }))
}

/**
 * 创建项目卡转让
 * 对接后端：POST /api/store/treatmentCardTransfers
 * 后端在同一事务内将卡归属更新为新客户并写入审计日志
 * @param data 转让请求
 * @returns 创建后的转让记录
 */
export async function createTreatmentCardTransfer(data: TreatmentCardTransferCreate): Promise<TreatmentCardTransfer> {
  return request<TreatmentCardTransfer>('/treatmentCardTransfers', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}
