// ==========================================
// 统计看板类型定义
// 对齐后端 Bms.Store.Domain.Entities.DailyStat / ProductSalesStat
// ==========================================

/**
 * 每日统计
 * 对齐后端 Bms.Store.Domain.Entities.DailyStat
 */
export interface DailyStat {
  /** 统计日期 */
  statDate: string
  /** 营收金额 */
  revenue: number
  /** 商品成本 */
  cost: number
  /** 毛利 */
  grossProfit: number
  /** 订单数 */
  orderCount: number
  /** 退款金额 */
  refundAmount: number
  /** 储值充值金额 */
  storedValueRecharge: number
  /** 储值消费金额 */
  storedValueConsume: number
  /** 今日消费客户数 */
  consumeCustomerCount: number
  /** 今日新客数 */
  newCustomerCount: number
  /** 今日预约数 */
  appointmentCount: number
  /** 库存预警数 */
  inventoryAlertCount: number
}

/**
 * 商品类型
 * - 1: 零售
 * - 2: 服务
 * - 3: 耗材
 * - 4: 疗程卡
 */
export type ProductType = 1 | 2 | 3 | 4

/**
 * 商品销售统计
 * 对齐后端 Bms.Store.Domain.Entities.ProductSalesStat
 */
export interface ProductSalesStat {
  /** 统计日期 */
  statDate: string
  /** 商品ID */
  productId: number
  /** 商品名称 */
  productName: string
  /** 商品类型：1-零售，2-服务，3-耗材，4-疗程卡 */
  productType: ProductType
  /** 销售次数 */
  salesCount: number
  /** 销售金额 */
  salesAmount: number
}

/**
 * 首页看板核心指标
 */
export interface DashboardSummary {
  /** 今日营收 */
  todayRevenue: number
  /** 今日订单数 */
  todayOrderCount: number
  /** 今日毛利 */
  todayGrossProfit: number
  /** 今日消费客户数 */
  todayConsumeCustomerCount: number
  /** 今日新客数 */
  todayNewCustomerCount: number
  /** 今日预约数 */
  todayAppointmentCount: number
  /** 库存预警数 */
  inventoryAlertCount: number
}

/**
 * 营收趋势查询参数
 */
export interface TrendQuery {
  /** 年份 */
  year: number
  /** 月份 */
  month: number
}

/**
 * 热门商品查询参数
 */
export interface TopProductsQuery {
  /** 年份 */
  year: number
  /** 月份 */
  month: number
  /** 商品类型筛选（多值，1:零售 2:服务 3:耗材 4:疗程卡） */
  productTypes?: ProductType[]
  /** 返回前N条 */
  top?: number
  /** 排序字段（amount:按金额 count:按次数），默认 amount */
  sortBy?: 'amount' | 'count'
}

/**
 * 通用API响应
 */
export interface ApiResponse<T> {
  code: number
  message?: string
  data: T
}
