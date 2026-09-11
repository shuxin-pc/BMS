// ==========================================
// 日结管理类型定义
// ==========================================

import type { PagedResponse } from '@/api/shared/storeRequest'

export type { PagedResponse }

/**
 * 日结状态
 * - 0: 待确认
 * - 1: 已确认
 */
export type SettlementStatus = 0 | 1

/**
 * 日结记录
 */
export interface DailySettlement {
  /** 日结ID */
  id: number
  /** 日结日期（仅日期部分） */
  settlementDate: string
  /** 日结时间（完整时间戳） */
  settlementTime: string
  /** 操作员ID */
  operatorId?: number
  /** 总营收 */
  totalRevenue: number
  /** 现金类营收（现金+支付宝+微信+银行卡，含组合支付类别1部分） */
  cashRevenue: number
  /** 储值扣款营收（含组合支付类别2部分） */
  storedValueRevenue: number
  /** 积分抵扣金额（仅记录，不纳入营收） */
  pointsDeductAmount: number
  /** 总退款 */
  totalRefund: number
  /** 储值充值总额 */
  totalStoredValueRecharge: number
  /** 储值消费总额 */
  totalStoredValueConsume: number
  /** 订单数 */
  orderCount: number
  /** 总成本（主营成本 = 销售出库成本 + 项目卡核销出库成本） */
  totalCost: number
  /** 销售出库成本 */
  salesOutboundCost: number
  /** 项目卡核销出库成本 */
  treatmentCardOutboundCost: number
  /** 盘亏损失（营业外支出） */
  inventoryLossAmount: number
  /** 样品赠品费用（营业外支出） */
  sampleGiftAmount: number
  /** 调拨出库金额（资产变动） */
  transferOutAmount: number
  /** 调拨入库金额（资产变动） */
  transferInAmount: number
  /** 采购退货金额（资产变动） */
  purchaseReturnAmount: number
  /** 总毛利（总营收-总成本，未扣退款） */
  totalGrossProfit: number
  /** 净营收 = 总营收 - 总退款（可为负） */
  netRevenue: number
  /** 退款率（0~1+，超过1表示退款大于营收） */
  refundRatio: number
  /** 退款是否大于营收 */
  isRefundExceedRevenue: boolean
  /** 营业外支出合计 = 盘亏损失 + 样品赠品费用 */
  totalOperatingExpense: number
  /** 资产变动净额 = 调拨出库 - 调拨入库 + 采购退货 */
  netTransferAmount: number
  /** 净毛利 = 净营收 - 总成本（可为负） */
  netGrossProfit: number
  /** 状态：0-待确认 1-已确认 */
  status: SettlementStatus
  /** 确认人ID */
  confirmedBy?: number
  /** 确认时间 */
  confirmedTime?: string
  /** 反日结人ID */
  reversedBy?: number
  /** 反日结时间 */
  reversedTime?: string
  /** 反日结原因 */
  reversedReason?: string
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 日结查询参数
 */
export interface DailySettlementQuery {
  /** 开始日期 */
  startDate?: string
  /** 结束日期 */
  endDate?: string
  /** 状态：0-待确认 1-已确认 */
  status?: SettlementStatus
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 今日经营汇总数据
 */
export interface TodaySummary {
  /** 今日日期 */
  date: string
  /** 总营收 */
  totalRevenue: number
  /** 总退款 */
  totalRefund: number
  /** 储值充值总额 */
  totalStoredValueRecharge: number
  /** 储值消费总额 */
  totalStoredValueConsume: number
  /** 订单数 */
  orderCount: number
  /** 是否已存在日结记录（待确认或已确认） */
  isSettled: boolean
  /** 已存在的日结记录ID（若有） */
  settlementId?: number
  /** 净营收 = 总营收 - 总退款（可为负） */
  netRevenue: number
  /** 退款是否大于营收 */
  isRefundExceedRevenue: boolean
}

/**
 * 防漏单校验结果
 */
export interface SettlementValidationResult {
  /** 是否可以确认 */
  canConfirm: boolean
  /** 警告信息列表 */
  warnings: string[]
}

/**
 * 手动汇总请求
 */
export interface ManualSummarizeRequest {
  /** 汇总日期（默认今日） */
  date?: string
  /** 备注 */
  remark?: string
}

/**
 * 反日结请求
 */
export interface ReverseRequest {
  /** 反日结原因（可选） */
  reason?: string
}
