// 耗材 BOM 管理 - API 服务（Mock 数据实现，后端就绪后替换为 fetch 调用）
import type {
  BomItem,
  BomQuery,
  BomCreate,
  BomUpdate,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  BomItem,
  BomQuery,
  BomCreate,
  BomUpdate,
  PagedResponse
}

// ==================== Mock 数据 ====================

// BOM Mock 数据（服务项目与耗材的关联关系）
const mockBomData: BomItem[] = [
  {
    id: 1,
    serviceProductId: 201,
    serviceProductName: '深层修复护理',
    consumableProductId: 101,
    consumableProductName: '深层修复洗发水',
    consumableProductCode: 'SP-001',
    quantity: 15,
    unit: 'ml',
    createdAt: '2026-07-01T10:00:00'
  },
  {
    id: 2,
    serviceProductId: 201,
    serviceProductName: '深层修复护理',
    consumableProductId: 102,
    consumableProductName: '丝滑护发素',
    consumableProductCode: 'SP-002',
    quantity: 10,
    unit: 'ml',
    createdAt: '2026-07-01T10:05:00'
  },
  {
    id: 3,
    serviceProductId: 202,
    serviceProductName: '植物染发服务',
    consumableProductId: 103,
    consumableProductName: '植物染发剂',
    consumableProductCode: 'SP-003',
    quantity: 80,
    unit: 'g',
    createdAt: '2026-07-02T14:30:00'
  },
  {
    id: 4,
    serviceProductId: 203,
    serviceProductName: '时尚造型设计',
    consumableProductId: 104,
    consumableProductName: '强力定型喷雾',
    consumableProductCode: 'SP-004',
    quantity: 20,
    unit: 'ml',
    createdAt: '2026-07-03T09:15:00'
  },
  {
    id: 5,
    serviceProductId: 203,
    serviceProductName: '时尚造型设计',
    consumableProductId: 105,
    consumableProductName: '海盐造型喷雾',
    consumableProductCode: 'SP-007',
    quantity: 15,
    unit: 'ml',
    createdAt: '2026-07-03T09:20:00'
  },
  {
    id: 6,
    serviceProductId: 204,
    serviceProductName: '头部精油SPA',
    consumableProductId: 106,
    consumableProductName: '植物精油',
    consumableProductCode: 'SP-006',
    quantity: 30,
    unit: 'ml',
    createdAt: '2026-07-05T11:00:00'
  },
  {
    id: 7,
    serviceProductId: 204,
    serviceProductName: '头部精油SPA',
    consumableProductId: 107,
    consumableProductName: '保湿护肤霜',
    consumableProductCode: 'SP-005',
    quantity: 25,
    unit: 'g',
    createdAt: '2026-07-05T11:10:00'
  }
]

// 自增ID计数器
let bomIdCounter = 100

// 服务项目选项 Mock（用于下拉选择）
const mockServiceProductOptions = [
  { id: 201, name: '深层修复护理' },
  { id: 202, name: '植物染发服务' },
  { id: 203, name: '时尚造型设计' },
  { id: 204, name: '头部精油SPA' },
  { id: 205, name: '头皮深层清洁' }
]

// 耗材商品选项 Mock（用于下拉选择）
const mockConsumableOptions = [
  { id: 101, name: '深层修复洗发水', code: 'SP-001', unit: 'ml' },
  { id: 102, name: '丝滑护发素', code: 'SP-002', unit: 'ml' },
  { id: 103, name: '植物染发剂', code: 'SP-003', unit: 'g' },
  { id: 104, name: '强力定型喷雾', code: 'SP-004', unit: 'ml' },
  { id: 105, name: '保湿护肤霜', code: 'SP-005', unit: 'g' },
  { id: 106, name: '植物精油', code: 'SP-006', unit: 'ml' },
  { id: 107, name: '海盐造型喷雾', code: 'SP-007', unit: 'ml' }
]

// ==================== API 函数 ====================

/**
 * 获取 BOM 分页列表（按服务项目名称筛选）
 * @param query 查询参数
 * @returns 分页 BOM 列表
 */
export async function getBomList(query?: BomQuery): Promise<PagedResponse<BomItem>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockBomData]
  if (query?.serviceProductName) {
    list = list.filter(item => item.serviceProductName.includes(query.serviceProductName!))
  }
  if (query?.consumableProductName) {
    list = list.filter(item => item.consumableProductName.includes(query.consumableProductName!))
  }
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}

/**
 * 创建 BOM 项
 * @param data 创建请求
 * @returns 创建后的 BOM 项
 */
export async function createBom(data: BomCreate): Promise<BomItem> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const serviceProduct = mockServiceProductOptions.find(p => p.id === data.serviceProductId)
  const consumable = mockConsumableOptions.find(p => p.id === data.consumableProductId)
  const newItem: BomItem = {
    id: ++bomIdCounter,
    serviceProductId: data.serviceProductId,
    serviceProductName: serviceProduct?.name || '',
    consumableProductId: data.consumableProductId,
    consumableProductName: consumable?.name || '',
    consumableProductCode: consumable?.code,
    quantity: data.quantity,
    unit: consumable?.unit,
    createdAt: new Date().toISOString()
  }
  mockBomData.push(newItem)
  return newItem
}

/**
 * 更新 BOM 项
 * @param data 更新请求
 * @returns 更新后的 BOM 项
 */
export async function updateBom(data: BomUpdate): Promise<BomItem> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const index = mockBomData.findIndex(item => item.id === data.id)
  if (index === -1) {
    throw new Error('BOM 记录不存在')
  }
  const serviceProduct = mockServiceProductOptions.find(p => p.id === data.serviceProductId)
  const consumable = mockConsumableOptions.find(p => p.id === data.consumableProductId)
  mockBomData[index] = {
    ...mockBomData[index],
    serviceProductId: data.serviceProductId,
    serviceProductName: serviceProduct?.name || mockBomData[index].serviceProductName,
    consumableProductId: data.consumableProductId,
    consumableProductName: consumable?.name || mockBomData[index].consumableProductName,
    consumableProductCode: consumable?.code || mockBomData[index].consumableProductCode,
    quantity: data.quantity,
    unit: consumable?.unit || mockBomData[index].unit,
    updatedAt: new Date().toISOString()
  }
  return mockBomData[index]
}

/**
 * 删除 BOM 项
 * @param id BOM 记录ID
 */
export async function deleteBom(id: number): Promise<void> {
  await new Promise(resolve => setTimeout(resolve, 300))
  const index = mockBomData.findIndex(item => item.id === id)
  if (index !== -1) {
    mockBomData.splice(index, 1)
  }
}

/**
 * 获取服务项目选项列表（用于下拉选择）
 * @returns 服务项目选项数组
 */
export async function getServiceProductOptions(): Promise<{ id: number; name: string }[]> {
  await new Promise(resolve => setTimeout(resolve, 100))
  return mockServiceProductOptions
}

/**
 * 获取耗材商品选项列表（用于下拉选择）
 * @returns 耗材商品选项数组
 */
export async function getConsumableOptions(): Promise<{ id: number; name: string; code: string; unit: string }[]> {
  await new Promise(resolve => setTimeout(resolve, 100))
  return mockConsumableOptions
}
