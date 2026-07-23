// 效期销售统计 - API 服务
// 对接后端 ProductExpirySalesStatsController：/api/store/productExpirySalesStats/*
import { request, buildQuery, type PagedResponse } from '../shared/storeRequest'

// ==================== 类型定义 ====================

/**
 * 效期销售统计查询参数
 * 对齐后端 Bms.Store.Application.Dtos.Statistics.ProductExpirySalesQueryDto
 */
export interface ProductExpirySalesQuery {
  /** 开始日期（默认本月1日，含当天） */
  startDate?: string
  /** 结束日期（默认今日，含当天） */
  endDate?: string
  /** 商品类型筛选：1=实物商品，3=耗材，undefined=全部 */
  productType?: 1 | 3
  /** 商品分类ID */
  categoryId?: number
  /** 效期区间筛选：1=已过期, 2=7天内, 3=30天内, 4=90天内, 5=90天以上 */
  expiryBucket?: 1 | 2 | 3 | 4 | 5
  /** 关键词（商品名称/编码模糊匹配） */
  keyword?: string
  /** 门店ID（多门店场景） */
  storeId?: number
  /** 排序字段：quantity=销售数量(默认), amount=销售金额, count=销售笔数 */
  sortBy?: 'quantity' | 'amount' | 'count'
  /** 页码（从1开始） */
  pageIndex?: number
  /** 每页数量 */
  pageSize?: number
}

/**
 * 效期销售统计结果项
 * 对齐后端 Bms.Store.Application.Dtos.Statistics.ProductExpirySalesStatDto
 */
export interface ProductExpirySalesStat {
  /** 商品ID */
  productId: number
  /** 商品名称 */
  productName: string
  /** 商品编码 */
  productCode: string
  /** 商品类型：1=实物商品，3=耗材 */
  productType: number
  /** 商品类型名称 */
  productTypeName: string
  /** 商品分类ID */
  categoryId?: number
  /** 商品分类名称 */
  categoryName?: string
  /** 效期区间编号：1=已过期, 2=7天内, 3=30天内, 4=90天内, 5=90天以上 */
  expiryBucket: number
  /** 效期区间名称 */
  expiryBucketName: string
  /** 批次过期日期范围-起 */
  expirationDateFrom?: string
  /** 批次过期日期范围-止 */
  expirationDateTo?: string
  /** 销售数量（已扣除退款） */
  salesQuantity: number
  /** 销售金额（已扣除退款） */
  salesAmount: number
  /** 销售笔数（订单数） */
  orderCount: number
  /** 占该商品总销售数量的百分比 */
  quantityPercentage: number
}

// ==================== API 方法 ====================

/**
 * 获取效期销售统计报表
 * 对接后端：GET /api/store/productExpirySalesStats/report
 * @param query 查询参数
 * @returns 分页统计结果
 */
export async function getProductExpirySalesStats(
  query: ProductExpirySalesQuery
): Promise<PagedResponse<ProductExpirySalesStat>> {
  const params: Record<string, string | number | undefined> = {
    startDate: query.startDate,
    endDate: query.endDate,
    productType: query.productType,
    categoryId: query.categoryId,
    expiryBucket: query.expiryBucket,
    keyword: query.keyword,
    storeId: query.storeId,
    sortBy: query.sortBy ?? 'quantity',
    pageIndex: query.pageIndex ?? 1,
    pageSize: query.pageSize ?? 20
  }
  // 移除 undefined 值
  Object.keys(params).forEach(k => {
    if (params[k] === undefined || params[k] === null) delete params[k]
  })
  const path = `/productExpirySalesStats/report${buildQuery(params)}`
  // request 已自动解包 ApiResponse，直接返回 data
  return await request<PagedResponse<ProductExpirySalesStat>>(path)
}
