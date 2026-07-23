// 库存调拨 - API 服务（Mock 数据实现，后端就绪后替换为 fetch 调用）
import type {
  StockTransfer,
  StockTransferItem,
  StockTransferQuery,
  StockTransferCreate,
  TransferItemCreate,
  TransferStatus,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  StockTransfer,
  StockTransferItem,
  StockTransferQuery,
  StockTransferCreate,
  TransferItemCreate,
  TransferStatus,
  PagedResponse
}

// ==================== Mock 数据 ====================

// 调拨单 Mock 数据
const mockStockTransfers: StockTransfer[] = [
  {
    id: 1,
    transferNo: 'TF20260701001',
    fromStoreId: 1,
    fromStoreCode: 'S001',
    fromStoreName: '总店',
    toStoreId: 2,
    toStoreCode: 'S002',
    toStoreName: '城南分店',
    transferDate: '2026-07-01',
    status: 3,
    operatorName: '张管理员',
    remark: '洗发水调拨至城南分店',
    createdAt: '2026-07-01T09:00:00',
    items: [
      { id: 1, stockTransferId: 1, productId: 101, productName: '深层修复洗发水', productCode: 'SP-001', quantity: 20, unit: '瓶', batchNo: 'B20260701' },
      { id: 2, stockTransferId: 1, productId: 102, productName: '丝滑护发素', productCode: 'SP-002', quantity: 15, unit: '瓶', batchNo: 'B20260701' }
    ]
  },
  {
    id: 2,
    transferNo: 'TF20260705001',
    fromStoreId: 2,
    fromStoreCode: 'S002',
    fromStoreName: '城南分店',
    toStoreId: 1,
    toStoreCode: 'S001',
    toStoreName: '总店',
    transferDate: '2026-07-05',
    status: 3,
    operatorName: '李店长',
    remark: '植物精油退回总店',
    createdAt: '2026-07-05T14:00:00',
    items: [
      { id: 3, stockTransferId: 2, productId: 106, productName: '植物精油', productCode: 'SP-006', quantity: 10, unit: '瓶', batchNo: 'B20260605' }
    ]
  },
  {
    id: 3,
    transferNo: 'TF20260708001',
    fromStoreId: 1,
    fromStoreCode: 'S001',
    fromStoreName: '总店',
    toStoreId: 3,
    toStoreCode: 'S003',
    toStoreName: '城北分店',
    transferDate: '2026-07-08',
    status: 2,
    operatorName: '张管理员',
    remark: '染发剂调拨至城北分店',
    createdAt: '2026-07-08T10:30:00',
    items: [
      { id: 4, stockTransferId: 3, productId: 103, productName: '植物染发剂', productCode: 'SP-003', quantity: 30, unit: '盒', batchNo: 'B20260615' },
      { id: 5, stockTransferId: 3, productId: 104, productName: '强力定型喷雾', productCode: 'SP-004', quantity: 10, unit: '瓶' }
    ]
  },
  {
    id: 4,
    transferNo: 'TF20260710001',
    fromStoreId: 2,
    fromStoreCode: 'S002',
    fromStoreName: '城南分店',
    toStoreId: 3,
    toStoreCode: 'S003',
    toStoreName: '城北分店',
    transferDate: '2026-07-10',
    status: 1,
    operatorName: '李店长',
    remark: '护肤霜调拨至城北分店，待调出',
    createdAt: '2026-07-10T16:00:00',
    items: [
      { id: 6, stockTransferId: 4, productId: 105, productName: '保湿护肤霜', productCode: 'SP-005', quantity: 50, unit: '瓶', batchNo: 'B20260710' }
    ]
  },
  {
    id: 5,
    transferNo: 'TF20260711001',
    fromStoreId: 1,
    fromStoreCode: 'S001',
    fromStoreName: '总店',
    toStoreId: 2,
    toStoreCode: 'S002',
    toStoreName: '城南分店',
    transferDate: '2026-07-11',
    status: 4,
    operatorName: '张管理员',
    remark: '海盐造型喷雾调拨，因库存不足取消',
    createdAt: '2026-07-11T11:00:00',
    items: [
      { id: 7, stockTransferId: 5, productId: 107, productName: '海盐造型喷雾', productCode: 'SP-007', quantity: 15, unit: '瓶' }
    ]
  },
  {
    id: 6,
    transferNo: 'TF20260712001',
    fromStoreId: 3,
    fromStoreCode: 'S003',
    fromStoreName: '城北分店',
    toStoreId: 1,
    toStoreCode: 'S001',
    toStoreName: '总店',
    transferDate: '2026-07-12',
    status: 1,
    operatorName: '王店长',
    remark: '城北分店退回总店多余库存',
    createdAt: '2026-07-12T08:30:00',
    items: [
      { id: 8, stockTransferId: 6, productId: 103, productName: '植物染发剂', productCode: 'SP-003', quantity: 5, unit: '盒', batchNo: 'B20260615' },
      { id: 9, stockTransferId: 6, productId: 104, productName: '强力定型喷雾', productCode: 'SP-004', quantity: 3, unit: '瓶' }
    ]
  }
]

// 自增ID计数器
let transferIdCounter = 100
let itemIdCounter = 1000

// 门店选项 Mock（用于下拉选择）
const mockStoreOptions = [
  { id: 1, code: 'S001', name: '总店' },
  { id: 2, code: 'S002', name: '城南分店' },
  { id: 3, code: 'S003', name: '城北分店' }
]

// 商品选项 Mock（用于下拉选择）
const mockProductOptions = [
  { id: 101, name: '深层修复洗发水', code: 'SP-001', unit: '瓶' },
  { id: 102, name: '丝滑护发素', code: 'SP-002', unit: '瓶' },
  { id: 103, name: '植物染发剂', code: 'SP-003', unit: '盒' },
  { id: 104, name: '强力定型喷雾', code: 'SP-004', unit: '瓶' },
  { id: 105, name: '保湿护肤霜', code: 'SP-005', unit: '瓶' },
  { id: 106, name: '植物精油', code: 'SP-006', unit: '瓶' },
  { id: 107, name: '海盐造型喷雾', code: 'SP-007', unit: '瓶' }
]

// ==================== API 函数 ====================

/**
 * 获取调拨单分页列表
 * @param query 查询参数
 * @returns 分页调拨单列表
 */
export async function getStockTransferList(query?: StockTransferQuery): Promise<PagedResponse<StockTransfer>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockStockTransfers]
  if (query?.transferNo) {
    list = list.filter(item => item.transferNo.includes(query.transferNo!))
  }
  if (query?.status) {
    list = list.filter(item => item.status === query.status)
  }
  if (query?.fromStoreId) {
    list = list.filter(item => item.fromStoreId === query.fromStoreId)
  }
  if (query?.toStoreId) {
    list = list.filter(item => item.toStoreId === query.toStoreId)
  }
  if (query?.startDate) {
    list = list.filter(item => item.transferDate >= query.startDate!)
  }
  if (query?.endDate) {
    list = list.filter(item => item.transferDate <= query.endDate!)
  }
  // 按创建时间倒序
  list.sort((a, b) => b.createdAt.localeCompare(a.createdAt))
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 获取调拨单详情（含明细列表）
 * @param id 调拨单ID
 * @returns 调拨单详情
 */
export async function getStockTransferDetail(id: number): Promise<StockTransfer> {
  await new Promise(resolve => setTimeout(resolve, 200))
  const transfer = mockStockTransfers.find(item => item.id === id)
  if (!transfer) {
    throw new Error('调拨单不存在')
  }
  return { ...transfer, items: [...transfer.items] }
}

/**
 * 创建调拨单
 * @param data 创建请求
 * @returns 创建后的调拨单
 */
export async function createStockTransfer(data: StockTransferCreate): Promise<StockTransfer> {
  await new Promise(resolve => setTimeout(resolve, 300))
  if (data.fromStoreId === data.toStoreId) {
    throw new Error('调出门店和调入门店不能相同')
  }
  if (!data.items || data.items.length === 0) {
    throw new Error('请至少添加一条调拨明细')
  }
  const fromStore = mockStoreOptions.find(s => s.id === data.fromStoreId)
  const toStore = mockStoreOptions.find(s => s.id === data.toStoreId)
  const newId = ++transferIdCounter
  const now = new Date()
  const transferNo = `TF${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}${String(transferIdCounter).padStart(3, '0')}`

  const items: StockTransferItem[] = data.items.map(item => {
    const product = mockProductOptions.find(p => p.id === item.productId)
    return {
      id: ++itemIdCounter,
      stockTransferId: newId,
      productId: item.productId,
      productName: product?.name || '',
      productCode: product?.code,
      quantity: item.quantity,
      unit: product?.unit,
      batchNo: item.batchNo,
      remark: item.remark
    }
  })

  const newTransfer: StockTransfer = {
    id: newId,
    transferNo,
    fromStoreId: data.fromStoreId,
    fromStoreCode: fromStore?.code,
    fromStoreName: fromStore?.name,
    toStoreId: data.toStoreId,
    toStoreCode: toStore?.code,
    toStoreName: toStore?.name,
    transferDate: data.transferDate,
    status: 1,
    operatorName: '当前用户',
    remark: data.remark,
    items,
    createdAt: now.toISOString()
  }
  mockStockTransfers.push(newTransfer)
  return newTransfer
}

/**
 * 更新调拨单状态
 * @param id 调拨单ID
 * @param status 目标状态
 */
export async function updateTransferStatus(id: number, status: TransferStatus): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const transfer = mockStockTransfers.find(item => item.id === id)
  if (!transfer) {
    throw new Error('调拨单不存在')
  }
  transfer.status = status
}

/**
 * 获取门店选项列表（用于下拉选择）
 * @returns 门店选项数组
 */
export async function getStoreOptions(): Promise<{ id: number; code: string; name: string }[]> {
  await new Promise(resolve => setTimeout(resolve, 100))
  return mockStoreOptions
}

/**
 * 获取商品选项列表（用于下拉选择）
 * @returns 商品选项数组
 */
export async function getProductOptions(): Promise<{ id: number; name: string; code: string; unit: string }[]> {
  await new Promise(resolve => setTimeout(resolve, 100))
  return mockProductOptions
}

/**
 * 调拨单状态标签映射
 */
export const transferStatusMap: Record<TransferStatus, string> = {
  1: '待调出',
  2: '已调出',
  3: '已调入',
  4: '已取消'
}

/**
 * 调拨单状态对应的 tag 类型
 */
export const transferStatusTagType: Record<TransferStatus, 'warning' | 'primary' | 'success' | 'info'> = {
  1: 'warning',
  2: 'primary',
  3: 'success',
  4: 'info'
}
