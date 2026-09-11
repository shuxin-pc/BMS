// ==========================================
// POS 挂单类型定义
// 字段与后端 ParkedOrderDto / ParkedOrderCreateDto / ParkedOrderQueryDto 对齐
// ==========================================

/**
 * 挂单状态
 * - 1: 挂起（待取单）
 * - 2: 已取走（取单后归档）
 * - 3: 已取消
 */
export type ParkedOrderStatus = 1 | 2 | 3

/**
 * 挂单信息（列表/取单返回）
 * 后端 long 主键序列化为字符串，故 id 用 string
 */
export interface ParkedOrder {
  /** 挂单ID */
  id: string
  /** 挂单号（PK{yyyyMMdd}{序号}） */
  parkNo: string
  /** 会员客户ID（散客为空） */
  customerId?: number
  /** 会员客户姓名 */
  customerName?: string
  /** 挂单备注 */
  remark?: string
  /** 挂单人姓名（冗余展示，识别谁挂的单） */
  createdByName?: string
  /** 状态（1=挂起 2=已取走 3=已取消） */
  status: ParkedOrderStatus
  /** 挂单时间 */
  heldTime: string
  /** 购物车行数（后端解析 CartJson 计算） */
  itemCount: number
  /** 购物车商品合计金额（仅商品/服务行） */
  totalAmount: number
  /** 购物车 JSON 快照（仅取单接口返回，前端 JSON.parse 恢复购物车） */
  cartJson?: string
}

/**
 * 挂单分页查询参数
 */
export interface ParkedOrderQuery {
  /** 关键字（挂单号/备注/会员姓名 模糊匹配） */
  keyword?: string
  pageIndex?: number
  pageSize?: number
}

/**
 * 挂单创建请求
 * cartJson 为购物车数组序列化字符串（前端 JSON.stringify(cart)）
 */
export interface ParkedOrderCreate {
  /** 会员客户ID（散客为空） */
  customerId?: number | null
  /** 会员客户姓名 */
  customerName?: string
  /** 挂单备注（选填） */
  remark?: string
  /** 购物车 JSON 快照 */
  cartJson: string
}
