// 库存盘点 - API 服务（Mock 数据实现，后端就绪后替换为 fetch 调用）
import type {
  InventoryCheck,
  InventoryCheckQuery,
  InventoryCheckCreate,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  InventoryCheck,
  InventoryCheckQuery,
  InventoryCheckCreate,
  PagedResponse
}

// ==================== Mock 数据 ====================

// 盘点记录 Mock 数据
const mockInventoryChecks: InventoryCheck[] = [
  {
    id: 1,
    productId: 101,
    productName: '深层修复洗发水',
    productCode: 'SP-001',
    beforeQuantity: 50,
    actualQuantity: 48,
    diffQuantity: -2,
    diffAmount: -57.00,
    unitCost: 28.50,
    checkTime: '2026-07-09T17:00:00',
    operatorName: '张管理员',
    remark: '日常盘点，少了2瓶'
  },
  {
    id: 2,
    productId: 103,
    productName: '植物染发剂',
    productCode: 'SP-003',
    beforeQuantity: 80,
    actualQuantity: 85,
    diffQuantity: 5,
    diffAmount: 225.00,
    unitCost: 45.00,
    checkTime: '2026-07-09T17:10:00',
    operatorName: '张管理员',
    remark: '盘盈5g，可能是上次称重误差'
  },
  {
    id: 3,
    productId: 105,
    productName: '保湿护肤霜',
    productCode: 'SP-005',
    beforeQuantity: 200,
    actualQuantity: 198,
    diffQuantity: -2,
    diffAmount: -136.00,
    unitCost: 68.00,
    checkTime: '2026-07-09T17:20:00',
    operatorName: '张管理员',
    remark: '盘亏2瓶，疑似破损报废未登记'
  },
  {
    id: 4,
    productId: 104,
    productName: '强力定型喷雾',
    productCode: 'SP-004',
    beforeQuantity: 10,
    actualQuantity: 12,
    diffQuantity: 2,
    diffAmount: 24.00,
    unitCost: 12.00,
    checkTime: '2026-07-10T14:30:00',
    operatorName: '李店长',
    remark: '盘盈2瓶，可能是之前入库漏登记'
  },
  {
    id: 5,
    productId: 106,
    productName: '植物精油',
    productCode: 'SP-006',
    beforeQuantity: 30,
    actualQuantity: 30,
    diffQuantity: 0,
    diffAmount: 0,
    unitCost: 50.00,
    checkTime: '2026-07-10T14:45:00',
    operatorName: '李店长',
    remark: '账实相符'
  },
  {
    id: 6,
    productId: 107,
    productName: '海盐造型喷雾',
    productCode: 'SP-007',
    beforeQuantity: 25,
    actualQuantity: 22,
    diffQuantity: -3,
    diffAmount: -36.00,
    unitCost: 12.00,
    checkTime: '2026-07-11T10:00:00',
    operatorName: '张管理员',
    remark: '盘亏3瓶，已记录损耗'
  },
  {
    id: 7,
    productId: 102,
    productName: '丝滑护发素',
    productCode: 'SP-002',
    beforeQuantity: 15,
    actualQuantity: 16,
    diffQuantity: 1,
    diffAmount: 15.00,
    unitCost: 15.00,
    checkTime: '2026-07-11T10:15:00',
    operatorName: '张管理员',
    remark: '盘盈1瓶'
  }
]

// 自增ID计数器
let checkIdCounter = 100

// 商品选项 Mock（用于下拉选择，含当前库存和成本价）
const mockProductOptions = [
  { id: 101, name: '深层修复洗发水', code: 'SP-001', unit: 'ml', stock: 48, costPrice: 28.50 },
  { id: 102, name: '丝滑护发素', code: 'SP-002', unit: 'ml', stock: 16, costPrice: 15.00 },
  { id: 103, name: '植物染发剂', code: 'SP-003', unit: 'g', stock: 85, costPrice: 45.00 },
  { id: 104, name: '强力定型喷雾', code: 'SP-004', unit: 'ml', stock: 12, costPrice: 12.00 },
  { id: 105, name: '保湿护肤霜', code: 'SP-005', unit: 'g', stock: 198, costPrice: 68.00 },
  { id: 106, name: '植物精油', code: 'SP-006', unit: 'ml', stock: 30, costPrice: 50.00 },
  { id: 107, name: '海盐造型喷雾', code: 'SP-007', unit: 'ml', stock: 22, costPrice: 12.00 }
]

// ==================== API 函数 ====================

/**
 * 获取盘点记录分页列表
 * @param query 查询参数
 * @returns 分页盘点记录列表
 */
export async function getInventoryCheckList(query?: InventoryCheckQuery): Promise<PagedResponse<InventoryCheck>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockInventoryChecks]
  if (query?.productName) {
    list = list.filter(item => item.productName.includes(query.productName!))
  }
  if (query?.startDate) {
    list = list.filter(item => item.checkTime >= query.startDate!)
  }
  if (query?.endDate) {
    list = list.filter(item => item.checkTime <= query.endDate + 'T23:59:59')
  }
  // 按盘点时间倒序
  list.sort((a, b) => b.checkTime.localeCompare(a.checkTime))
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 创建盘点记录
 * @param data 创建请求
 * @returns 创建后的盘点记录
 */
export async function createInventoryCheck(data: InventoryCheckCreate): Promise<InventoryCheck> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const product = mockProductOptions.find(p => p.id === data.productId)
  if (!product) {
    throw new Error('商品不存在')
  }
  const beforeQuantity = product.stock
  const diffQuantity = data.actualQuantity - beforeQuantity
  const diffAmount = product.costPrice ? diffQuantity * product.costPrice : undefined

  // 更新模拟库存
  product.stock = data.actualQuantity

  const newCheck: InventoryCheck = {
    id: ++checkIdCounter,
    productId: data.productId,
    productName: product.name,
    productCode: product.code,
    beforeQuantity,
    actualQuantity: data.actualQuantity,
    diffQuantity,
    diffAmount,
    unitCost: product.costPrice,
    checkTime: new Date().toISOString(),
    operatorName: '当前用户',
    remark: data.remark
  }
  mockInventoryChecks.push(newCheck)
  return newCheck
}

/**
 * 获取商品选项列表（含当前库存和成本价，用于盘点新增）
 * @returns 商品选项数组
 */
export async function getProductOptionsForCheck(): Promise<{
  id: number
  name: string
  code: string
  unit: string
  stock: number
  costPrice: number
}[]> {
  await new Promise(resolve => setTimeout(resolve, 100))
  return mockProductOptions
}
