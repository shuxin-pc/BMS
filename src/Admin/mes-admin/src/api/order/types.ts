// ==========================================
// 订单管理类型定义
// 字段与后端 OrderDto / OrderItemDto 对齐
// ==========================================

/**
 * 支付方式
 * - 1: 现金
 * - 2: 支付宝
 * - 3: 微信
 * - 4: 银行卡
 * - 5: 储值卡
 * - 6: 积分抵扣
 * - 7: 组合支付（CashAmount + StoredValueAmount + PointsAmount 三栏拆分）
 */
export type PaymentMethod = 1 | 2 | 3 | 4 | 5 | 6 | 7

/**
 * 订单状态
 * - 2: 已完成
 * - 3: 已退款
 * - 4: 已取消
 */
export type OrderStatus = 2 | 3 | 4

/**
 * 订单类型
 * - 1: 零售（实物商品销售）
 * - 2: 服务（服务项目消费）
 * - 3: 项目卡核销
 * - 4: 储值消费
 */
export type OrderType = 1 | 2 | 3 | 4

/**
 * 技师来源
 * - 1: 商家技师
 * - 2: 平台技师
 */
export type TechnicianSource = 1 | 2

/**
 * 订单明细项（与后端 OrderItemDto 对齐）
 */
export interface OrderItem {
  /** 明细ID */
  id: number
  /** 订单ID */
  orderId: number
  /** 商品ID */
  productId: number
  /** 商品名称 */
  productName: string
  /** 商品编码 */
  productCode: string
  /** 商品类型：1-实物商品，2-服务商品，3-耗材，4-样品，5-赠品（来自商品主档 ProductMaster.Type） */
  productType: number
  /** 技师ID（服务商品时选择） */
  technicianId?: number
  /** 技师来源：1-商家技师，2-平台技师 */
  technicianSource?: TechnicianSource
  /** 技师姓名（后端关联查询填充） */
  technicianName?: string
  /** 房间/床位ID */
  roomId?: number
  /** 房间/床位名称（后端关联查询填充） */
  roomName?: string
  /** 设备ID */
  equipmentId?: number
  /** 设备名称（后端关联查询填充） */
  equipmentName?: string
  /** 服务开始时间（服务内容弹窗/核销项目录入的真实服务开始时间，ISO 字符串） */
  serviceStartTime?: string
  /** 服务结束时间（服务开始时间 + 服务时长自动计算，ISO 字符串） */
  serviceEndTime?: string
  /** 数量 */
  quantity: number
  /** 单价 */
  price: number
  /** 折扣率 */
  discountRate?: number
  /** 折后金额 */
  discountedAmount?: number
  /** 技师服务费用 */
  technicianFee?: number
  /** 耗材成本（后端按批次聚合：Σ OrderItemBatch.CostAmount） */
  consumableCost?: number
  /** 批次扣减明细（结构化数据，替代旧版 consumableDeduction JSON） */
  batches?: OrderItemBatch[]
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
}

/**
 * 订单明细批次扣减记录（与后端 OrderItemBatchDto 对齐）
 * 销售出库时为每个扣减的库存批次生成一条记录，退款时按批次精确回补
 */
export interface OrderItemBatch {
  /** 记录ID */
  id: number
  /** 订单明细ID */
  orderItemId: number
  /** 订单ID（冗余，便于按订单直接查询） */
  orderId: number
  /** 商品ID（冗余，用于按商品聚合统计） */
  productId: number
  /** 商品名称（后端关联查询填充，用于退款弹窗展示可退批次所属商品；服务项目行的批次即 BOM 耗材，此处为耗材名称） */
  productName?: string
  /** 库存批次ID（可空：库存批次被删除时仍保留快照） */
  batchId?: number
  /** 批次号（快照） */
  batchNo: string
  /** 过期日期（快照，核心统计字段） */
  expirationDate?: string
  /** 单价（快照，用于成本核算） */
  unitPrice: number
  /** 扣减数量（正数） */
  quantity: number
  /** 成本金额 = quantity × unitPrice */
  costAmount: number
  /** 已退款数量（部分退款累计） */
  refundedQuantity?: number
  /** 创建时间 */
  createdAt?: string
}

/**
 * 订单信息（与后端 OrderDto 对齐）
 */
export interface Order {
  /** 订单ID */
  id: number
  /** 订单号 */
  orderNo: string
  /** 客户ID */
  customerId?: number
  /** 客户姓名（后端左连接 Customer 填充，散客订单为空） */
  customerName?: string
  /** 客户手机号（后端左连接 Customer 填充，散客订单为空） */
  phone?: string
  /** 订单类型：1-零售，2-服务，3-项目卡核销，4-储值消费 */
  orderType: OrderType
  /** 订单状态：2-已完成，3-已退款，4-已取消 */
  status: OrderStatus
  /** 补录状态 */
  backfillStatus?: number
  /** 商品金额（应付） */
  productAmount: number
  /** 优惠金额 */
  discountAmount?: number
  /** 实付金额 */
  paidAmount: number
  /** 积分 */
  points?: number
  /** 支付方式：1-现金，2-支付宝，3-微信，4-银行卡，5-储值卡，6-积分抵扣，7-组合支付 */
  payMethod?: PaymentMethod
  /** 组合支付-类别1金额（现金/支付宝/微信/银行卡，payMethod=7 时返回） */
  cashAmount?: number
  /** 组合支付-类别1具体方式：1-现金，2-支付宝，3-微信，4-银行卡 */
  cashPayMethod?: 1 | 2 | 3 | 4
  /** 组合支付-类别2储值扣款金额（payMethod=7 时返回） */
  storedValueAmount?: number
  /** 组合支付-类别3积分抵扣金额（payMethod=7 时返回） */
  pointsAmount?: number
  /** 下单时的积分抵扣比例快照（PointsRule.DeductRate，元/积分，100分=1元时=0.01；0 表示无快照） */
  deductRate?: number
  /** 下单时间 */
  orderTime: string
  /** 完成时间 */
  completeTime?: string
  /** 退款金额 */
  refundAmount?: number
  /** 退款时间 */
  refundTime?: string
  /** 退款原因 */
  refundReason?: string
  /** 操作员ID */
  operatorId?: number
  /** 备注 */
  remark?: string
  /** 创建时间 */
  createdAt?: string
  /** 更新时间 */
  updatedAt?: string
  /** 订单明细列表（后端 OrderDto 不返回，需单独获取） */
  items?: OrderItem[]
}

/**
 * 订单查询参数（与后端 OrderQueryDto 对齐）
 */
export interface OrderQuery {
  /** 订单号（模糊匹配） */
  orderNo?: string
  /** 客户ID */
  customerId?: number
  /** 客户名称或手机号关键字（模糊匹配，OR 语义） */
  keyword?: string
  /** 订单类型 */
  orderType?: OrderType
  /** 订单状态 */
  status?: OrderStatus
  /** 支付方式 */
  payMethod?: PaymentMethod
  /** 补录状态（0:非补录 1:待补录） */
  backfillStatus?: number
  /** 下单开始日期（yyyy-MM-dd，含） */
  startDate?: string
  /** 下单结束日期（yyyy-MM-dd，含当天） */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 退款退库明细项（对齐后端 RefundItemDto）
 * 粒度：订单明细批次（OrderItemBatch.Id），只从订单中已有的批次选择退回，绝不新建退货批次；
 * 服务项目/项目卡核销行的 BOM 耗材扣减明细同样是 OrderItemBatch（ProductId = 耗材），可直接选择退回
 */
export interface RefundItem {
  /** 订单明细批次ID（OrderItemBatch.Id） */
  orderItemBatchId: number
  /** 退库数量（>0，≤ 可退数量 = quantity - refundedQuantity） */
  quantity: number
}

/**
 * 退款请求
 * 对齐后端 RefundRequestDto（POST /orders/{id}/refund）
 */
export interface RefundRequest {
  /** 订单ID */
  orderId: number
  /** 退款金额 */
  refundAmount: number
  /** 退款原因 */
  reason: string
  /** 退库明细列表（门店手动选择退回的批次+数量，可空/为空表示本次退款不执行库存回退） */
  refundItems?: RefundItem[]
}

/**
 * 取消订单请求
 * 对齐后端 OrderCancelDto（POST /orders/{id}/cancel）
 * 取消订单视为订单未发生，后端执行全量事务回滚（库存/BOM/项目卡/积分/储值/统计/消费记录），订单 Status 改为 4（已取消）
 */
export interface CancelRequest {
  /** 订单ID */
  orderId: number
  /** 取消原因（必填，用于审计追溯） */
  reason: string
}

/**
 * 退款结果
 * 对齐后端 RefundResultDto
 */
export interface RefundResult {
  /** 订单ID */
  orderId: number
  /** 订单编号 */
  orderNo: string
  /** 本次退款金额 */
  thisRefundAmount: number
  /** 累计已退款金额 */
  totalRefundedAmount: number
  /** 订单状态（2:已完成 3:已退款 4:已取消） */
  orderStatus: number
  /** 退款时间 */
  refundTime: string
  /** 联动操作记录 */
  actions: string[]
}

/**
 * 通用分页响应
 */
export interface PagedResponse<T> {
  list: T[]
  total: number
  pageIndex: number
  pageSize: number
}

/**
 * 通用API响应
 */
export interface ApiResponse<T> {
  code: number
  message?: string
  data: T
}

// ==========================================
// 订单创建类型（对齐后端 OrderCreateDto / OrderItemCreateDto）
// ==========================================

/**
 * 创建订单明细项
 */
/**
 * 服务项目绑定耗材的效期选择（快速开单加购服务项目时店员选择的耗材效期）
 * 服务项目订单明细（OrderItemCreate.consumableExpiries）与项目卡核销项（TreatmentCardVerifyItemInput.consumableExpiries）共用
 */
export interface ConsumableExpiry {
  /** 耗材商品ID（对应 ServiceBom.ConsumableProductId） */
  productId: number
  /** 耗材商品名称（冗余，便于展示/提示） */
  productName: string
  /** 需求数量 = BOM 单次消耗量 × 服务数量/核销次数 */
  quantity: number
  /** 店员选择的效期列表（按扣减顺序）；undefined 表示系统自动按近效期扣减（FEFO）；null 元素表示"无效期限制"批次 */
  expirationDates?: (string | null)[]
}

export interface OrderItemCreate {
  productId: number
  productName: string
  productCode: string
  technicianId?: number
  technicianSource?: TechnicianSource
  /** 房间/床位ID（服务订单占用房间资源，可空） */
  roomId?: number
  /** 设备ID（服务订单占用设备资源，可空） */
  equipmentId?: number
  /** 服务开始时间（服务内容弹窗/核销项目录入的真实服务开始时间，ISO 字符串，可空） */
  serviceStartTime?: string
  /** 服务结束时间（服务开始时间 + 服务时长自动计算，ISO 字符串，可空） */
  serviceEndTime?: string
  quantity: number
  price: number
  discountRate?: number
  discountedAmount: number
  technicianFee?: number
  /** 店员选择的效期列表（按扣减顺序），为空表示系统自动按近效期扣减（FEFO）；null 元素表示"无效期限制"批次 */
  expirationDates?: (string | null)[]
  /** 选中效期库存不足时，是否允许系统自动从近效期补足（默认 false） */
  allowAutoFillBeyondSelection?: boolean
  /** 关联活动ID（可选，仅赠品项 Type=5 有意义，用于活动维度归因统计） */
  activityId?: number | null
  remark?: string
  /** 服务项目绑定耗材的效期选择（仅服务项目 Type=2 有意义，对应后端 OrderItemCreateDto.ConsumableExpiries） */
  consumableExpiries?: ConsumableExpiry[]
}

/**
 * 创建订单请求
 */
export interface OrderCreate {
  /** 订单号（可选：不再由前端生成，后端 OrderAppService 自动生成正式单号） */
  orderNo?: string
  customerId?: number
  /** 订单类型：1-零售，2-服务，3-项目卡核销 */
  orderType: OrderType
  /** 订单状态：2-已完成 */
  status: OrderStatus
  /** 补录状态：0-非补录，1-待补录 */
  backfillStatus?: number
  productAmount: number
  discountAmount?: number
  paidAmount: number
  points?: number
  /** 支付方式：1-现金，2-支付宝，3-微信，4-银行卡，5-储值卡，6-积分抵扣，7-组合支付 */
  payMethod?: PaymentMethod
  /** 组合支付-类别1金额（现金/支付宝/微信/银行卡，payMethod=7 时使用） */
  cashAmount?: number
  /** 组合支付-类别1具体方式：1-现金，2-支付宝，3-微信，4-银行卡 */
  cashPayMethod?: 1 | 2 | 3 | 4
  /** 组合支付-类别2储值扣款金额（payMethod=7 时使用） */
  storedValueAmount?: number
  /** 组合支付-类别3积分抵扣金额（payMethod=7 时使用） */
  pointsAmount?: number
  orderTime: string
  completeTime?: string
  operatorId?: number
  remark?: string
  items: OrderItemCreate[]
  /** 项目卡销售ID（仅 OrderType=3 项目卡核销时需要） */
  cardSaleId?: number
  /** 源预约ID（预约转订单时由前端传入；后端会从源预约复制 TechnicianId/RoomId/EquipmentId 到 OrderItem） */
  sourceAppointmentId?: number
  /** 购物车结算批次号（POS 购物车一次结算生成，用于订单/核销/开卡三单据聚合追溯；独立下单为空） */
  checkoutSessionNo?: string
}
