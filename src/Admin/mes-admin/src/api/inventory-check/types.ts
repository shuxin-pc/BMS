// ==========================================
// 库存盘点类型定义
// 对应后端实体 InventoryCheck
// ==========================================

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
 * 库存盘点记录
 * 对应后端 InventoryCheckDto
 */
export interface InventoryCheck {
  /** 盘点记录ID */
  id: number
  /** 商品ID */
  productId: number
  /** 商品名称（联表 Product 查询填充） */
  productName?: string
  /** 商品编码（联表 Product 查询填充） */
  productCode?: string
  /** 盘点前账面库存 */
  beforeQuantity: number
  /** 实际盘点数量 */
  actualQuantity: number
  /** 差异数量（实际 - 账面，正数为盘盈，负数为盘亏） */
  diffQuantity: number
  /** 差异金额（提交时持久化，按真实批次单价计算；正数盘盈，负数盘亏） */
  diffAmount?: number
  /** 单价（盘亏取首批扣减批次单价；盘盈取录入估值单价） */
  unitCost?: number
  /** 批次号（盘亏取首批扣减批次号；盘盈取新建盘盈批次号） */
  batchNo?: string
  /** 过期日期（盘亏取首批扣减批次过期日期；盘盈取录入过期日期） */
  expirationDate?: string
  /** 盘点时间 */
  checkTime: string
  /** 操作员ID */
  operatorId?: number
  /** 操作员姓名（冗余存储，创建时取 ICurrentUser.RealName ?? UserName） */
  operatorName?: string
  /** 盘点单状态（0=草稿 1=已完成 2=已取消） */
  status: number
  /** 备注 */
  remark?: string
  /** 本次盘点涉及批次明细（盘盈累加批次 / 盘亏扣减批次，无差异时为空） */
  batches: InventoryCheckBatch[]
}

/**
 * 盘点批次明细（盘盈累加批次 / 盘亏扣减批次）
 * 对应后端 InventoryCheckBatchDto
 */
export interface InventoryCheckBatch {
  /** 明细ID */
  id: number
  /** 库存批次ID */
  batchId: number
  /** 批次号 */
  batchNo: string
  /** 该批次盘点数量（盘盈为累加数量、盘亏为扣减数量，均为正数） */
  quantity: number
  /** 该批次单价 */
  unitPrice?: number
  /** 该批次过期日期 */
  expirationDate?: string
}

/**
 * 盘点查询参数
 * 对应后端 InventoryCheckQueryDto
 */
export interface InventoryCheckQuery {
  /** 商品名称（模糊匹配） */
  productName?: string
  /** 盘点单状态（0=草稿 1=已完成 2=已取消） */
  status?: number
  /** 开始日期（按盘点时间筛选） */
  startDate?: string
  /** 结束日期（按盘点时间筛选） */
  endDate?: string
  /** 页码 */
  pageIndex?: number
  /** 每页条数 */
  pageSize?: number
}

/**
 * 创建盘点请求（草稿状态）
 * 对应后端 InventoryCheckCreateDto
 */
export interface InventoryCheckCreate {
  /** 商品ID */
  productId: number
  /** 盘点前账面库存 */
  beforeQuantity: number
  /** 实际盘点数量 */
  actualQuantity: number
  /** 差异数量（草稿阶段传 0，提交时后端自动计算） */
  diffQuantity: number
  /** 盘点时间 */
  checkTime: string
  /** 备注 */
  remark?: string
}

/**
 * 提交盘点单请求
 * 对应后端 SubmitCheckDto
 * 按差异方向分支：盘亏用 deductBatches，盘盈用 gain* 字段，无差异时全部忽略
 */
export interface SubmitCheckRequest {
  /** 实际盘点数量 */
  actualQuantity: number
  /** 盘亏批次扣减明细（差异为负时使用，留空走 FIFO 兜底） */
  deductBatches?: BatchDeductItem[]
  /** 盘盈批次累加明细（差异为正时使用，可多个批次，数量合计需与差异数量匹配） */
  gainBatches?: GainBatchItem[]
  /** 盘盈批次单价（差异为正时使用，留空取 Product.CostPrice） */
  gainUnitPrice?: number
  /** 盘盈批次生产日期 */
  gainProductionDate?: string
  /** 盘盈批次保质期天数（与生产日期联动计算过期日期：过期日期 = 生产日期 + 保质期天数） */
  gainShelfLifeDays?: number
  /** 盘盈批次过期日期 */
  gainExpirationDate?: string
  /** 备注 */
  remark?: string
}

/**
 * 创建并提交盘点单请求（原子操作）
 * 对应后端 CreateAndSubmitCheckDto
 * 合并 Create + Submit 两步为一个事务，避免草稿残留
 * 按差异方向分支：盘亏用 deductBatches，盘盈用 gainBatchNo，无差异时全部忽略
 */
export interface CreateAndSubmitRequest {
  /** 商品ID */
  productId: number
  /** 盘点前账面库存（前端从商品选项带出） */
  beforeQuantity: number
  /** 实际盘点数量 */
  actualQuantity: number
  /** 盘亏批次扣减明细（差异为负时使用，留空走 FIFO 兜底） */
  deductBatches?: BatchDeductItem[]
  /** 盘盈批次累加明细（差异为正时使用，可多个批次，数量合计需与差异数量匹配） */
  gainBatches?: GainBatchItem[]
  /** 备注（可选） */
  remark?: string
}

/**
 * 盘盈批次累加明细项（盘盈累加批次时使用）
 * 对应后端 GainBatchItem
 */
export interface GainBatchItem {
  /** 批次ID（InventoryBatch.Id） */
  batchId: number
  /** 该批次累加数量，>0 */
  quantity: number
}

/**
 * 批次扣减明细项（盘亏指定批次模式时使用）
 */
export interface BatchDeductItem {
  /** 批次ID（InventoryBatch.Id） */
  batchId: number
  /** 该批次扣减数量，>0 */
  quantity: number
}

/**
 * 盘点专用商品选项（含账面库存、成本价、在库批次列表）
 * 对应后端 InventoryCheckProductOptionDto
 */
export interface InventoryCheckProductOption {
  /** 商品ID */
  id: number
  /** 商品名称 */
  name: string
  /** 商品编码 */
  code: string
  /** 单位 */
  unit?: string
  /** 账面库存（当前门店该商品库存量） */
  stock: number
  /** 成本价（来自 Product.CostPrice，盘盈时作为默认估值单价） */
  costPrice?: number
  /** 当前在库批次列表（供盘亏选择） */
  batches: InventoryCheckBatchOption[]
}

/**
 * 盘点专用批次选项（盘亏时供操作员选择扣减批次）
 * 对应后端 InventoryCheckBatchOptionDto
 */
export interface InventoryCheckBatchOption {
  /** 批次ID */
  id: number
  /** 批次号 */
  batchNo: string
  /** 当前批次库存数量 */
  quantity: number
  /** 批次单价 */
  unitPrice: number
  /** 过期日期 */
  expirationDate?: string
  /** 批次创建时间（用于 FIFO 排序展示） */
  createdTime: string
}

/**
 * 盘盈批次选项/查询结果（当前商品在当前门店的全部批次，含已用完/已过期）
 * 对应后端 InventoryCheckBatchLookupDto
 */
export interface InventoryCheckBatchLookup {
  /** 批次ID */
  id: number
  /** 批次号 */
  batchNo: string
  /** 当前批次库存数量 */
  quantity: number
  /** 批次单价 */
  unitPrice: number
  /** 生产日期 */
  productionDate?: string
  /** 保质期天数 */
  shelfLifeDays?: number
  /** 过期日期 */
  expirationDate?: string
  /** 状态（1:在库 2:已用完 3:已过期） */
  status: number
}
