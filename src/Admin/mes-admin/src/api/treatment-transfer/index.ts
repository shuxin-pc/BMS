// 疗程卡转让 - API服务（Mock 数据）
import type {
  TreatmentCardTransfer,
  TreatmentCardTransferQuery,
  TreatmentCardTransferCreate,
  CardSaleOption,
  CustomerOption,
  ApiResponse,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  TreatmentCardTransfer,
  TreatmentCardTransferQuery,
  TreatmentCardTransferCreate,
  CardSaleOption,
  CustomerOption,
  ApiResponse,
  PagedResponse
}

// ==================== Mock 数据 ====================

// 可转让的疗程卡销售记录（仅状态为有效的卡可转让）
const mockCardSaleOptions: CardSaleOption[] = [
  {
    id: 1,
    saleNo: 'TC20260701001',
    cardName: '面部护理季卡',
    customerId: 1001,
    customerName: '张丽华',
    customerPhone: '13800138001',
    remainingCount: 5,
    totalCount: 12
  },
  {
    id: 3,
    saleNo: 'TC20260705001',
    cardName: '足疗月卡',
    customerId: 1003,
    customerName: '王芳',
    customerPhone: '13800138003',
    remainingCount: 3,
    totalCount: 8
  },
  {
    id: 5,
    saleNo: 'TC20260710001',
    cardName: '头部SPA次卡',
    customerId: 1005,
    customerName: '孙丽',
    customerPhone: '13800138005',
    remainingCount: 8,
    totalCount: 10
  },
  {
    id: 6,
    saleNo: 'TC20260708001',
    cardName: '面部护理季卡',
    customerId: 1006,
    customerName: '刘婷',
    customerPhone: '13800138006',
    remainingCount: 10,
    totalCount: 12
  },
  {
    id: 7,
    saleNo: 'TC20260709001',
    cardName: '全身按摩年卡',
    customerId: 1007,
    customerName: '陈静',
    customerPhone: '13800138007',
    remainingCount: 20,
    totalCount: 24
  }
]

// 客户列表（选择新客户用）
const mockCustomerOptions: CustomerOption[] = [
  { id: 1001, name: '张丽华', phone: '13800138001' },
  { id: 1002, name: '李秀英', phone: '13800138002' },
  { id: 1003, name: '王芳', phone: '13800138003' },
  { id: 1004, name: '赵敏', phone: '13800138004' },
  { id: 1005, name: '孙丽', phone: '13800138005' },
  { id: 1006, name: '刘婷', phone: '13800138006' },
  { id: 1007, name: '陈静', phone: '13800138007' },
  { id: 1008, name: '周琳', phone: '13800138008' }
]

// 转让记录 Mock 数据
const mockTransfers: TreatmentCardTransfer[] = [
  {
    id: 1,
    storeId: 1,
    storeCode: 'S001',
    storeName: '总店',
    cardSaleId: 1,
    cardName: '面部护理季卡',
    saleNo: 'TC20260701001',
    fromCustomerId: 1001,
    fromCustomerName: '张丽华',
    fromCustomerPhone: '13800138001',
    toCustomerId: 1008,
    toCustomerName: '周琳',
    toCustomerPhone: '13800138008',
    transferDate: '2026-07-08',
    transferFee: 50,
    operatorId: 1,
    operatorName: '前台小李',
    status: 1,
    remark: '客户搬家，转让给朋友'
  },
  {
    id: 2,
    storeId: 1,
    storeCode: 'S001',
    storeName: '总店',
    cardSaleId: 3,
    cardName: '足疗月卡',
    fromCustomerId: 1003,
    fromCustomerName: '王芳',
    fromCustomerPhone: '13800138003',
    toCustomerId: 1004,
    toCustomerName: '赵敏',
    toCustomerPhone: '13800138004',
    transferDate: '2026-07-10',
    transferFee: 0,
    operatorId: 1,
    operatorName: '前台小李',
    status: 1,
    remark: '姐妹间转让，免手续费'
  },
  {
    id: 3,
    storeId: 2,
    storeCode: 'S002',
    storeName: '城南分店',
    cardSaleId: 5,
    cardName: '头部SPA次卡',
    fromCustomerId: 1005,
    fromCustomerName: '孙丽',
    fromCustomerPhone: '13800138005',
    toCustomerId: 1006,
    toCustomerName: '刘婷',
    toCustomerPhone: '13800138006',
    transferDate: '2026-07-11',
    transferFee: 30,
    operatorId: 2,
    operatorName: '前台小张',
    status: 1,
    remark: ''
  },
  {
    id: 4,
    storeId: 1,
    storeCode: 'S001',
    storeName: '总店',
    cardSaleId: 6,
    cardName: '面部护理季卡',
    fromCustomerId: 1006,
    fromCustomerName: '刘婷',
    fromCustomerPhone: '13800138006',
    toCustomerId: 1002,
    toCustomerName: '李秀英',
    toCustomerPhone: '13800138002',
    transferDate: '2026-07-09',
    transferFee: 50,
    operatorId: 1,
    operatorName: '前台小李',
    status: 1,
    remark: '客户个人原因转让'
  },
  {
    id: 5,
    storeId: 2,
    storeCode: 'S002',
    storeName: '城南分店',
    cardSaleId: 7,
    cardName: '全身按摩年卡',
    fromCustomerId: 1007,
    fromCustomerName: '陈静',
    fromCustomerPhone: '13800138007',
    toCustomerId: 1008,
    toCustomerName: '周琳',
    toCustomerPhone: '13800138008',
    transferDate: '2026-07-12',
    transferFee: 100,
    operatorId: 2,
    operatorName: '前台小张',
    status: 1,
    remark: '高价卡转让，收取手续费'
  },
  {
    id: 6,
    storeId: 1,
    storeCode: 'S001',
    storeName: '总店',
    cardSaleId: 1,
    cardName: '面部护理季卡',
    fromCustomerId: 1008,
    fromCustomerName: '周琳',
    fromCustomerPhone: '13800138008',
    toCustomerId: 1003,
    toCustomerName: '王芳',
    toCustomerPhone: '13800138003',
    transferDate: '2026-07-05',
    transferFee: 50,
    operatorId: 1,
    operatorName: '前台小李',
    status: 1,
    remark: '二次转让'
  }
]

let mockIdCounter = 100

// ==================== API 方法 ====================

/**
 * 获取疗程卡转让分页列表
 * @param query 查询参数
 * @returns 分页转让记录列表
 */
export async function getTreatmentCardTransfers(query?: TreatmentCardTransferQuery): Promise<PagedResponse<TreatmentCardTransfer>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockTransfers]
  if (query?.startDate) list = list.filter(item => item.transferDate >= query.startDate!)
  if (query?.endDate) list = list.filter(item => item.transferDate <= query.endDate!)
  if (query?.customerName) {
    const keyword = query.customerName
    list = list.filter(item =>
      (item.fromCustomerName || '').includes(keyword) ||
      (item.toCustomerName || '').includes(keyword)
    )
  }
  // 按转让日期降序
  list.sort((a, b) => b.transferDate.localeCompare(a.transferDate))
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 获取可转让的疗程卡销售记录列表
 * @returns 可转让的疗程卡列表
 */
export async function getTransferableCardSales(): Promise<CardSaleOption[]> {
  await new Promise(resolve => setTimeout(resolve, 300))
  return mockCardSaleOptions.map(item => ({ ...item }))
}

/**
 * 获取客户列表（选择新客户用）
 * @returns 客户列表
 */
export async function getCustomerOptions(): Promise<CustomerOption[]> {
  await new Promise(resolve => setTimeout(resolve, 300))
  return mockCustomerOptions.map(item => ({ ...item }))
}

/**
 * 创建疗程卡转让
 * 转让后原客户的疗程卡余额转移到新客户
 * @param data 转让请求
 * @returns 创建后的转让记录
 */
export async function createTreatmentCardTransfer(data: TreatmentCardTransferCreate): Promise<TreatmentCardTransfer> {
  await new Promise(resolve => setTimeout(resolve, 300))

  const cardSale = mockCardSaleOptions.find(c => c.id === data.cardSaleId)
  if (!cardSale) throw new Error('疗程卡销售记录不存在')

  const toCustomer = mockCustomerOptions.find(c => c.id === data.toCustomerId)
  if (!toCustomer) throw new Error('新客户不存在')

  if (cardSale.customerId === data.toCustomerId) {
    throw new Error('不能转让给原客户本人')
  }

  const newRecord: TreatmentCardTransfer = {
    id: ++mockIdCounter,
    storeId: 1,
    storeCode: 'S001',
    storeName: '总店',
    cardSaleId: cardSale.id,
    cardName: cardSale.cardName,
    saleNo: cardSale.saleNo,
    fromCustomerId: cardSale.customerId,
    fromCustomerName: cardSale.customerName,
    fromCustomerPhone: cardSale.customerPhone,
    toCustomerId: toCustomer.id,
    toCustomerName: toCustomer.name,
    toCustomerPhone: toCustomer.phone,
    transferDate: data.transferDate,
    transferFee: data.transferFee,
    operatorId: 1,
    operatorName: '当前操作员',
    status: 1,
    remark: data.remark
  }

  mockTransfers.unshift(newRecord)

  // 转让后更新卡销售记录的客户信息
  cardSale.customerId = toCustomer.id
  cardSale.customerName = toCustomer.name
  cardSale.customerPhone = toCustomer.phone

  return { ...newRecord }
}
