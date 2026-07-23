// 价格变更记录 - API服务（Mock 数据实现，后端就绪后替换为 fetch 调用）
import type {
  PriceChangeLog,
  PriceChangeLogQuery,
  PagedResponse
} from './types'

// 导出类型供外部使用
export type {
  PriceChangeLog,
  PriceChangeLogQuery,
  PagedResponse
}

// ==================== Mock 数据 ====================

// 价格变更记录 Mock 数据
const mockPriceChangeLogs: PriceChangeLog[] = [
  {
    id: 1,
    productId: 101,
    productName: '深层修复洗发水',
    productCode: 'SP-001',
    oldPrice: 128.00,
    newPrice: 98.00,
    changeTime: '2026-06-15T10:30:00',
    operatorId: 1,
    operatorName: '张店长',
    remark: '夏季促销调价，提升销量'
  },
  {
    id: 2,
    productId: 102,
    productName: '丝滑护发素',
    productCode: 'SP-002',
    oldPrice: 88.00,
    newPrice: 68.00,
    changeTime: '2026-06-20T14:00:00',
    operatorId: 1,
    operatorName: '张店长',
    remark: '与洗发水搭配销售，下调价格'
  },
  {
    id: 3,
    productId: 103,
    productName: '植物染发剂',
    productCode: 'SP-003',
    oldPrice: 168.00,
    newPrice: 198.00,
    changeTime: '2026-07-01T09:45:00',
    operatorId: 2,
    operatorName: '李经理',
    remark: '原材料成本上涨，上调售价'
  },
  {
    id: 4,
    productId: 104,
    productName: '强力定型喷雾',
    productCode: 'SP-004',
    oldPrice: 58.00,
    newPrice: 45.00,
    changeTime: '2026-07-05T16:20:00',
    operatorId: 1,
    operatorName: '张店长',
    remark: '库存较多，清仓调价'
  },
  {
    id: 5,
    productId: 105,
    productName: '保湿护肤霜',
    productCode: 'SP-005',
    oldPrice: 258.00,
    newPrice: 218.00,
    changeTime: '2026-07-10T11:15:00',
    operatorId: 2,
    operatorName: '李经理',
    remark: '品牌方建议零售价调整'
  },
  {
    id: 6,
    productId: 101,
    productName: '深层修复洗发水',
    productCode: 'SP-001',
    oldPrice: 98.00,
    newPrice: 108.00,
    changeTime: '2026-07-12T08:30:00',
    operatorId: 1,
    operatorName: '张店长',
    remark: '促销结束后回调价格'
  },
  {
    id: 7,
    productId: 106,
    productName: '一次性毛巾',
    productCode: 'HC-001',
    oldPrice: 2.00,
    newPrice: 1.50,
    changeTime: '2026-07-11T15:00:00',
    operatorId: 1,
    operatorName: '张店长',
    remark: '耗材采购成本下降，调整内部领用价'
  }
]

// ==================== API 方法 ====================

/**
 * 获取价格变更记录分页列表
 * @param query 查询参数（商品名称、时间范围）
 * @returns 分页价格变更记录列表
 */
export async function getPriceChangeLogs(query?: PriceChangeLogQuery): Promise<PagedResponse<PriceChangeLog>> {
  await new Promise(resolve => setTimeout(resolve, 300))
  let list = [...mockPriceChangeLogs]
  if (query?.productId !== undefined) {
    list = list.filter(item => item.productId === query.productId)
  }
  if (query?.productName) {
    list = list.filter(item => item.productName.includes(query.productName!))
  }
  if (query?.startDate) {
    list = list.filter(item => item.changeTime >= query.startDate!)
  }
  if (query?.endDate) {
    list = list.filter(item => item.changeTime <= query.endDate! + 'T23:59:59')
  }
  // 按变更时间倒序排列
  list.sort((a, b) => b.changeTime.localeCompare(a.changeTime))
  const total = list.length
  const pageIndex = query?.pageIndex || 1
  const pageSize = query?.pageSize || 20
  const start = (pageIndex - 1) * pageSize
  return { list: list.slice(start, start + pageSize), total, pageIndex, pageSize }
}
