// 入库/出库操作 - API 服务（Mock 数据实现，后端就绪后替换为 fetch 调用）
import type {
  InventoryLog,
  InventoryLogQuery,
  InboundRequest,
  OutboundRequest,
  InventoryOpType,
  InboundSourceType,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  InventoryLog,
  InventoryLogQuery,
  InboundRequest,
  OutboundRequest,
  InventoryOpType,
  InboundSourceType,
  PagedResponse
}

// ==================== Mock 数据 ====================

// 模拟当前库存（用于计算操作前后库存）
const mockStockMap: Record<number, number> = {
  101: 50,
  102: 15,
  103: 80,
  104: 10,
  105: 200,
  106: 30,
  107: 25
}

// 入库/出库记录 Mock 数据
const mockInventoryLogs: InventoryLog[] = [
  {
    id: 1,
    productId: 101,
    productName: '深层修复洗发水',
    productCode: 'SP-001',
    type: 1,
    sourceType: 1,
    supplierId: 1,
    supplierName: '广州洗护用品有限公司',
    unitPrice: 28.50,
    quantity: 100,
    beforeQuantity: 0,
    afterQuantity: 100,
    batchNo: 'B20260701',
    expirationDate: '2027-07-01',
    remark: '首批采购入库',
    createdAt: '2026-07-01T09:30:00',
    operatorName: '张管理员'
  },
  {
    id: 2,
    productId: 101,
    productName: '深层修复洗发水',
    productCode: 'SP-001',
    type: 2,
    quantity: -50,
    beforeQuantity: 100,
    afterQuantity: 50,
    remark: '日常消耗出库',
    createdAt: '2026-07-05T14:20:00',
    operatorName: '李店长'
  },
  {
    id: 3,
    productId: 103,
    productName: '植物染发剂',
    productCode: 'SP-003',
    type: 1,
    sourceType: 1,
    supplierId: 2,
    supplierName: '上海化妆品供应商',
    unitPrice: 45.00,
    quantity: 80,
    beforeQuantity: 0,
    afterQuantity: 80,
    batchNo: 'B20260615',
    expirationDate: '2027-06-15',
    remark: '染发剂采购入库',
    createdAt: '2026-06-15T10:00:00',
    operatorName: '张管理员'
  },
  {
    id: 4,
    productId: 102,
    productName: '丝滑护发素',
    productCode: 'SP-002',
    type: 1,
    sourceType: 2,
    quantity: 15,
    beforeQuantity: 0,
    afterQuantity: 15,
    remark: '客户退货入库',
    createdAt: '2026-07-08T16:45:00',
    operatorName: '王前台'
  },
  {
    id: 5,
    productId: 105,
    productName: '保湿护肤霜',
    productCode: 'SP-005',
    type: 1,
    sourceType: 1,
    supplierId: 3,
    supplierName: '北京护肤用品批发',
    unitPrice: 68.00,
    quantity: 200,
    beforeQuantity: 0,
    afterQuantity: 200,
    batchNo: 'B20260710',
    expirationDate: '2028-07-10',
    remark: '护肤霜大批量采购',
    createdAt: '2026-07-10T11:30:00',
    operatorName: '张管理员'
  },
  {
    id: 6,
    productId: 104,
    productName: '强力定型喷雾',
    productCode: 'SP-004',
    type: 2,
    quantity: -5,
    beforeQuantity: 15,
    afterQuantity: 10,
    remark: '服务消耗出库',
    createdAt: '2026-07-11T15:00:00',
    operatorName: '李店长'
  },
  {
    id: 7,
    productId: 106,
    productName: '植物精油',
    productCode: 'SP-006',
    type: 1,
    sourceType: 4,
    quantity: 30,
    beforeQuantity: 0,
    afterQuantity: 30,
    batchNo: 'B20260705',
    remark: '从总店调拨入库',
    createdAt: '2026-07-05T13:00:00',
    operatorName: '张管理员'
  },
  {
    id: 8,
    productId: 107,
    productName: '海盐造型喷雾',
    productCode: 'SP-007',
    type: 1,
    sourceType: 3,
    quantity: 25,
    beforeQuantity: 0,
    afterQuantity: 25,
    remark: '盘点盘盈入库',
    createdAt: '2026-07-09T17:30:00',
    operatorName: '张管理员'
  }
]

// 自增ID计数器
let logIdCounter = 1000

// 商品选项 Mock（用于下拉选择）
const mockProductOptions = [
  { id: 101, name: '深层修复洗发水', code: 'SP-001', unit: 'ml' },
  { id: 102, name: '丝滑护发素', code: 'SP-002', unit: 'ml' },
  { id: 103, name: '植物染发剂', code: 'SP-003', unit: 'g' },
  { id: 104, name: '强力定型喷雾', code: 'SP-004', unit: 'ml' },
  { id: 105, name: '保湿护肤霜', code: 'SP-005', unit: 'g' },
  { id: 106, name: '植物精油', code: 'SP-006', unit: 'ml' },
  { id: 107, name: '海盐造型喷雾', code: 'SP-007', unit: 'ml' }
]

// 供应商选项 Mock（用于下拉选择）
const mockSupplierOptions = [
  { id: 1, name: '广州洗护用品有限公司' },
  { id: 2, name: '上海化妆品供应商' },
  { id: 3, name: '北京护肤用品批发' },
  { id: 4, name: '深圳造型用品贸易' }
]

// ==================== API 函数 ====================

/**
 * 获取库存操作日志分页列表
 * @param query 查询参数
 * @returns 分页日志列表
 */
export async function getInventoryLogList(query?: InventoryLogQuery): Promise<PagedResponse<InventoryLog>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockInventoryLogs]
  if (query?.productName) {
    list = list.filter(item => item.productName.includes(query.productName!))
  }
  if (query?.type) {
    list = list.filter(item => item.type === query.type)
  }
  if (query?.sourceType) {
    list = list.filter(item => item.sourceType === query.sourceType)
  }
  if (query?.startDate) {
    list = list.filter(item => item.createdAt >= query.startDate!)
  }
  if (query?.endDate) {
    list = list.filter(item => item.createdAt <= query.endDate + 'T23:59:59')
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
 * 入库操作
 * @param data 入库请求
 * @returns 创建后的库存日志
 */
export async function createInbound(data: InboundRequest): Promise<InventoryLog> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const product = mockProductOptions.find(p => p.id === data.productId)
  const supplier = data.supplierId ? mockSupplierOptions.find(s => s.id === data.supplierId) : undefined
  const beforeQuantity = mockStockMap[data.productId] || 0
  const afterQuantity = beforeQuantity + data.quantity
  mockStockMap[data.productId] = afterQuantity

  const newLog: InventoryLog = {
    id: ++logIdCounter,
    productId: data.productId,
    productName: product?.name || '',
    productCode: product?.code,
    type: 1,
    sourceType: data.sourceType,
    supplierId: data.supplierId,
    supplierName: supplier?.name,
    unitPrice: data.unitPrice,
    quantity: data.quantity,
    beforeQuantity,
    afterQuantity,
    batchNo: data.batchNo,
    expirationDate: data.expirationDate,
    remark: data.remark,
    createdAt: new Date().toISOString(),
    operatorName: '当前用户'
  }
  mockInventoryLogs.push(newLog)
  return newLog
}

/**
 * 出库操作
 * @param data 出库请求
 * @returns 创建后的库存日志
 */
export async function createOutbound(data: OutboundRequest): Promise<InventoryLog> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const product = mockProductOptions.find(p => p.id === data.productId)
  const beforeQuantity = mockStockMap[data.productId] || 0
  const afterQuantity = beforeQuantity - data.quantity
  if (afterQuantity < 0) {
    throw new Error('库存不足，无法出库')
  }
  mockStockMap[data.productId] = afterQuantity

  const newLog: InventoryLog = {
    id: ++logIdCounter,
    productId: data.productId,
    productName: product?.name || '',
    productCode: product?.code,
    type: 2,
    quantity: -data.quantity,
    beforeQuantity,
    afterQuantity,
    remark: data.remark,
    createdAt: new Date().toISOString(),
    operatorName: '当前用户'
  }
  mockInventoryLogs.push(newLog)
  return newLog
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
 * 获取供应商选项列表（用于下拉选择）
 * @returns 供应商选项数组
 */
export async function getSupplierOptions(): Promise<{ id: number; name: string }[]> {
  await new Promise(resolve => setTimeout(resolve, 100))
  return mockSupplierOptions
}

/**
 * 获取商品当前库存
 * @param productId 商品ID
 * @returns 当前库存数量
 */
export async function getProductStock(productId: number): Promise<number> {
  await new Promise(resolve => setTimeout(resolve, 100))
  return mockStockMap[productId] || 0
}

/**
 * 入库来源类型标签映射
 */
export const inboundSourceTypeMap: Record<InboundSourceType, string> = {
  1: '采购入库',
  2: '退货入库',
  3: '盘点入库',
  4: '调拨入库'
}

/**
 * 库存操作类型标签映射
 */
export const inventoryOpTypeMap: Record<InventoryOpType, string> = {
  1: '入库',
  2: '出库',
  3: '盘点',
  4: '调拨'
}
