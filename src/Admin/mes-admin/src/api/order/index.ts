// 订单管理 - API 服务
// 对接后端 OrdersController / OrderItemsController
import {
  request,
  buildQuery,
  type PagedResponse
} from '../shared/storeRequest'
import type {
  Order,
  OrderItem,
  OrderQuery,
  RefundRequest,
  RefundResult,
  OrderCreate,
  OrderItemCreate,
  CancelRequest,
  ApiResponse
} from './types'

// 导出类型供外部使用
export type {
  Order,
  OrderItem,
  OrderQuery,
  RefundRequest,
  RefundResult,
  ApiResponse,
  OrderCreate,
  OrderItemCreate,
  CancelRequest,
  PagedResponse
}

// ==================== API 函数 ====================

/**
 * 获取订单分页列表
 * 对接后端：GET /api/store/orders
 * @param query 查询参数
 * @returns 分页订单列表
 */
export async function getOrders(query?: OrderQuery): Promise<PagedResponse<Order>> {
  const qs = buildQuery({
    orderNo: query?.orderNo,
    customerId: query?.customerId,
    orderType: query?.orderType,
    status: query?.status,
    payMethod: query?.payMethod,
    backfillStatus: query?.backfillStatus,
    startDate: query?.startDate,
    endDate: query?.endDate,
    pageIndex: query?.pageIndex,
    pageSize: query?.pageSize
  })
  return request<PagedResponse<Order>>(`/orders${qs}`)
}

/**
 * 获取订单详情（含订单明细）
 * 对接后端：GET /api/store/orders/{id}
 * 后端 GetByIdAsync 已 Include OrderItems 并关联填充 TechnicianName
 * @param id 订单ID
 * @returns 订单详情（包含 items 明细列表）
 */
export async function getOrder(id: number): Promise<Order> {
  return request<Order>(`/orders/${id}`)
}

/**
 * 订单退款
 * 对接后端：POST /api/store/orders/{id}/refund
 * 事务包裹，按订单类型联动库存/疗程卡/储值/积分
 * @param data 退款请求
 * @returns 退款结果（含联动操作记录）
 */
export async function refundOrder(data: RefundRequest): Promise<RefundResult> {
  return request<RefundResult>(`/orders/${data.orderId}/refund`, {
    method: 'POST',
    body: JSON.stringify({
      refundAmount: data.refundAmount,
      reason: data.reason
    })
  })
}

/**
 * 取消订单
 * 对接后端：POST /api/store/orders/{id}/cancel
 * 事务包裹，按 OrderType 全量回滚库存/BOM/疗程卡/积分/储值/统计/消费记录
 * 订单 Status 改为 4（已取消），视为订单未发生
 * 仅 Status=1（进行中）或 Status=2（已完成）的订单可取消
 * @param data 取消请求（含取消原因）
 */
export async function cancelOrder(data: CancelRequest): Promise<void> {
  return request<void>(`/orders/${data.orderId}/cancel`, {
    method: 'POST',
    body: JSON.stringify({
      reason: data.reason
    })
  })
}

/**
 * 创建订单
 * 对接后端：POST /api/store/orders
 * 事务包裹：OrderType 联动库存出库/BOM 扣减，PayMethod=5 联动储值扣减，积分发放
 * @param data 订单创建请求
 * @returns 创建后的订单
 */
export async function createOrder(data: OrderCreate): Promise<Order> {
  return request<Order>('/orders', {
    method: 'POST',
    body: JSON.stringify(data)
  })
}
