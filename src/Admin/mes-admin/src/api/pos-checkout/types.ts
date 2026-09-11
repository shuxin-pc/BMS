// POS 快速开单混合结算类型定义
// 对接后端 PosCheckoutsController（POST /api/store/pos/checkouts）
// 核销（扣卡次+建核销订单）、建单（商品/服务行收款）、开卡（项目卡独立记账）
// 三类单据在同一数据库事务内执行，任一失败整体回滚，保证结算原子性
import type { OrderCreate } from '@/api/order/types'
import type { TreatmentCardVerifyItemInput, TreatmentCardSaleCreate } from '@/api/treatment-card/types'

/** 混合结算核销子项（购物车核销行按 cardSaleId 聚合后一次核销） */
export interface PosCheckoutVerify {
  /** 项目卡销售记录ID */
  cardSaleId: number
  /** 核销项目列表（每项含项目ID/次数/技师/房间/设备/服务时间） */
  items: TreatmentCardVerifyItemInput[]
}

/** 混合结算请求（三类单据至少其一，缺省字段不传） */
export interface PosCheckoutCreate {
  /** 购物车结算批次号（订单/核销/开卡三单据共用，用于跨单据聚合追溯） */
  checkoutSessionNo: string
  /** 商品/服务行建单（可空：纯核销/纯开卡购物车不传） */
  order?: OrderCreate
  /** 项目卡核销组（可空：无核销行不传） */
  verifies?: PosCheckoutVerify[]
  /** 项目卡开卡组（可空：无开卡行不传） */
  sales?: TreatmentCardSaleCreate[]
}

/** 商品/服务订单结果项 */
export interface PosCheckoutOrder {
  orderId: number
  orderNo: string
  amount: number
}

/** 项目卡核销结果项 */
export interface PosCheckoutVerifyResult {
  /** 项目卡销售记录ID（前端据此映射卡名展示） */
  cardSaleId: number
  times: number
  amount: number
}

/** 项目卡开卡结果项 */
export interface PosCheckoutSale {
  saleId: number
  amount: number
}

/** 混合结算结果 */
export interface PosCheckoutResult {
  checkoutSessionNo: string
  orders: PosCheckoutOrder[]
  verifies: PosCheckoutVerifyResult[]
  sales: PosCheckoutSale[]
}
