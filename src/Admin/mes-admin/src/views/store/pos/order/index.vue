<template>
  <div class="order-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="订单号">
            <el-input
              v-model="searchForm.orderNo"
              placeholder="请输入订单号"
              clearable
              style="width: 200px"
            />
          </el-form-item>
          <el-form-item label="订单状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 130px">
              <el-option label="进行中" :value="1" />
              <el-option label="已完成" :value="2" />
              <el-option label="已退款" :value="3" />
              <el-option label="已取消" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="订单类型">
            <el-select v-model="searchForm.orderType" placeholder="全部" clearable style="width: 130px">
              <el-option label="零售" :value="1" />
              <el-option label="服务" :value="2" />
              <el-option label="疗程卡核销" :value="3" />
              <el-option label="储值消费" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="支付方式">
            <el-select v-model="searchForm.payMethod" placeholder="全部" clearable style="width: 130px">
              <el-option label="现金" :value="1" />
              <el-option label="支付宝" :value="2" />
              <el-option label="微信" :value="3" />
              <el-option label="银行卡" :value="4" />
              <el-option label="储值卡" :value="5" />
              <el-option label="积分抵扣" :value="6" />
            </el-select>
          </el-form-item>
          <el-form-item label="下单日期">
            <el-date-picker
              v-model="searchForm.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 260px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" @click="handleSearch">
              <el-icon><Search /></el-icon>
              搜索
            </el-button>
            <el-button @click="handleReset">
              <el-icon><Refresh /></el-icon>
              重置
            </el-button>
          </el-form-item>
        </el-form>
      </div>
    </div>

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-right">
        <el-button circle @click="loadData">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <!-- 表格区域 -->
    <div class="card">
      <el-table
        v-loading="tableLoading"
        :data="tableData"
        style="width: 100%"
      >
        <el-table-column prop="orderNo" label="订单号" width="160" />
        <el-table-column label="门店" width="120">
          <template #default>
            <!-- TODO: 后端 Order 不返回 storeName -->
            -
          </template>
        </el-table-column>
        <el-table-column label="客户" width="100">
          <template #default="{ row }">
            <!-- TODO: 后端 Order 只返回 customerId，需关联客户信息 -->
            {{ row.customerId || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="订单类型" width="110" align="center">
          <template #default="{ row }">
            <el-tag :type="orderTypeTagType(row.orderType)" size="small" effect="plain">
              {{ orderTypeText(row.orderType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="订单金额" width="100" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥{{ formatPrice(row.productAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="优惠" width="90" align="right">
          <template #default="{ row }">
            <span v-if="row.discountAmount" class="discount-text">-¥{{ formatPrice(row.discountAmount) }}</span>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column label="实付金额" width="110" align="right">
          <template #default="{ row }">
            <span class="price-text">¥{{ formatPrice(row.paidAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="支付方式" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="paymentMethodTagType(row.payMethod)" size="small" effect="plain">
              {{ paymentMethodText(row.payMethod) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="订单状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)" size="small" effect="dark">
              {{ statusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="orderTime" label="下单时间" width="170" />
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleDetail(row)">
              <el-icon><View /></el-icon>
              详情
            </el-button>
            <el-button
              v-if="row.status === 2 && row.orderType !== 3"
              link
              type="warning"
              size="small"
              @click="handleRefund(row)"
            >
              <el-icon><RefreshLeft /></el-icon>
              退款
            </el-button>
            <el-button
              v-if="row.status === 1 || row.status === 2"
              link
              type="danger"
              size="small"
              @click="handleCancel(row)"
            >
              <el-icon><CircleClose /></el-icon>
              取消订单
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="pagination.pageIndex"
          v-model:page-size="pagination.pageSize"
          :page-sizes="systemConfigStore.defaultPageSizes"
          :total="pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="loadData"
          @current-change="loadData"
        />
      </div>
    </div>

    <!-- 订单详情弹窗 -->
    <el-dialog
      v-model="detailVisible"
      title="订单详情"
      width="720px"
      :close-on-click-modal="false"
    >
      <div v-loading="detailLoading">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="订单号">{{ currentOrder?.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="订单状态">
            <el-tag :type="statusTagType(currentOrder?.status || 0)" size="small" effect="dark">
              {{ statusText(currentOrder?.status || 0) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="门店">
            <!-- TODO: 后端 Order 不返回 storeName -->
            -
          </el-descriptions-item>
          <el-descriptions-item label="客户">
            <!-- TODO: 后端 Order 只返回 customerId，需关联客户信息 -->
            {{ currentOrder?.customerId || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="订单类型">
            <el-tag :type="orderTypeTagType(currentOrder?.orderType || 0)" size="small" effect="plain">
              {{ orderTypeText(currentOrder?.orderType || 0) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="支付方式">
            {{ paymentMethodText(currentOrder?.payMethod || 0) }}
          </el-descriptions-item>
          <el-descriptions-item label="下单时间">{{ currentOrder?.orderTime }}</el-descriptions-item>
          <el-descriptions-item label="订单金额">¥{{ formatPrice(currentOrder?.productAmount) }}</el-descriptions-item>
          <el-descriptions-item label="优惠金额">¥{{ formatPrice(currentOrder?.discountAmount) }}</el-descriptions-item>
          <el-descriptions-item label="实付金额">
            <span class="price-text">¥{{ formatPrice(currentOrder?.paidAmount) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="退款金额" v-if="currentOrder?.refundAmount">
            <span class="discount-text">¥{{ formatPrice(currentOrder.refundAmount) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ currentOrder?.remark || '-' }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-section-title">订单明细</div>
        <el-table :data="currentOrder?.items || []" border style="width: 100%">
          <el-table-column label="商品名称" min-width="180">
            <template #default="{ row }">
              {{ row.productName }}
              <div class="text-muted text-xs">{{ row.productCode }}</div>
            </template>
          </el-table-column>
          <el-table-column label="技师" width="100" align="center">
            <template #default="{ row }">
              {{ row.technicianName || '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="quantity" label="数量" width="80" align="center" />
          <el-table-column label="单价" width="90" align="right">
            <template #default="{ row }">¥{{ formatPrice(row.price) }}</template>
          </el-table-column>
          <el-table-column label="折后金额" width="100" align="right">
            <template #default="{ row }">
              <span class="price-text">¥{{ formatPrice(row.discountedAmount) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="技师服务费" width="100" align="right">
            <template #default="{ row }">
              <span v-if="row.technicianFee">¥{{ formatPrice(row.technicianFee) }}</span>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column label="批次扣减" width="100" align="center">
            <template #default="{ row }">
              <el-popover
                v-if="row.batches && row.batches.length"
                trigger="click"
                placement="left"
                :width="380"
              >
                <template #reference>
                  <el-button link type="warning" size="small">查看</el-button>
                </template>
                <div class="consumable-detail">
                  <div class="consumable-title">批次扣减明细</div>
                  <el-table :data="row.batches" size="small" border style="width: 100%">
                    <el-table-column label="批次号" prop="batchNo" min-width="110" show-overflow-tooltip />
                    <el-table-column label="效期" width="110">
                      <template #default="{ row: b }">{{ formatDate(b.expirationDate) }}</template>
                    </el-table-column>
                    <el-table-column label="数量" width="80" align="center">
                      <template #default="{ row: b }">{{ b.quantity }}</template>
                    </el-table-column>
                    <el-table-column label="单价" width="90" align="right">
                      <template #default="{ row: b }">¥{{ formatPrice(b.unitPrice) }}</template>
                    </el-table-column>
                    <el-table-column label="已退" width="70" align="center">
                      <template #default="{ row: b }">
                        <span v-if="b.refundedQuantity" class="discount-text">{{ b.refundedQuantity }}</span>
                        <span v-else class="text-muted">-</span>
                      </template>
                    </el-table-column>
                  </el-table>
                  <div v-if="row.consumableCost" class="consumable-total">
                    耗材成本合计：¥{{ formatPrice(row.consumableCost) }}
                  </div>
                </div>
              </el-popover>
              <span v-else class="text-muted">-</span>
            </template>
          </el-table-column>
          <el-table-column label="备注" min-width="100">
            <template #default="{ row }">{{ row.remark || '-' }}</template>
          </el-table-column>
        </el-table>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 退款弹窗 -->
    <el-dialog
      v-model="refundVisible"
      title="订单退款"
      width="480px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="refundFormRef"
        :model="refundForm"
        :rules="refundRules"
        label-width="100px"
      >
        <el-form-item label="订单号">
          <span>{{ refundForm.orderNo }}</span>
        </el-form-item>
        <el-form-item label="实付金额">
          <span class="price-text">¥{{ formatPrice(refundForm.paidAmount) }}</span>
        </el-form-item>
        <el-form-item label="已退款">
          <span class="discount-text">¥{{ formatPrice(refundForm.refundedAmount) }}</span>
        </el-form-item>
        <el-form-item label="可退款金额">
          <span>¥{{ formatPrice(refundForm.maxRefundAmount) }}</span>
        </el-form-item>
        <el-form-item label="退款金额" prop="refundAmount">
          <el-input-number
            v-model="refundForm.refundAmount"
            :min="0.01"
            :max="refundForm.maxRefundAmount"
            :precision="2"
            :step="1"
            controls-position="right"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="退款原因" prop="reason">
          <el-input
            v-model="refundForm.reason"
            type="textarea"
            :rows="3"
            placeholder="请输入退款原因"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="refundVisible = false">取消</el-button>
        <el-button type="primary" :loading="refundLoading" @click="handleRefundSubmit">
          确认退款
        </el-button>
      </template>
    </el-dialog>

    <!-- 取消订单弹窗 -->
    <el-dialog
      v-model="cancelVisible"
      title="取消订单"
      width="480px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="cancelFormRef"
        :model="cancelForm"
        :rules="cancelRules"
        label-width="100px"
      >
        <el-form-item label="订单号">
          <span>{{ cancelForm.orderNo }}</span>
        </el-form-item>
        <el-alert
          type="warning"
          :closable="false"
          show-icon
          style="margin-bottom: 16px"
        >
          取消订单将全量回滚库存/BOM/疗程卡/积分/储值/统计/消费记录，订单视为未发生。
        </el-alert>
        <el-form-item label="取消原因" prop="reason">
          <el-input
            v-model="cancelForm.reason"
            type="textarea"
            :rows="3"
            placeholder="请输入取消原因（用于审计追溯）"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="cancelVisible = false">关闭</el-button>
        <el-button type="danger" :loading="cancelLoading" @click="handleCancelSubmit">
          确认取消
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, View, RefreshLeft, CircleClose } from '@element-plus/icons-vue'
import { getOrders, getOrder, refundOrder, cancelOrder } from '@/api/order'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Order, OrderStatus, PaymentMethod, OrderType } from '@/api/order/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  orderNo: '',
  status: undefined as OrderStatus | undefined,
  payMethod: undefined as PaymentMethod | undefined,
  orderType: undefined as OrderType | undefined,
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Order[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getOrders({
      orderNo: searchForm.orderNo || undefined,
      status: searchForm.status,
      payMethod: searchForm.payMethod,
      orderType: searchForm.orderType,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error: any) {
    ElMessage.error(error.message || '加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.orderNo = ''
  searchForm.status = undefined
  searchForm.payMethod = undefined
  searchForm.orderType = undefined
  searchForm.dateRange = []
  handleSearch()
}

// ==================== 订单详情 ====================
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentOrder = ref<Order>()

const handleDetail = async (row: Order) => {
  detailVisible.value = true
  detailLoading.value = true
  try {
    currentOrder.value = await getOrder(row.id)
  } catch (error: any) {
    ElMessage.error(error.message || '加载详情失败')
  } finally {
    detailLoading.value = false
  }
}

// ==================== 退款处理 ====================
const refundVisible = ref(false)
const refundLoading = ref(false)
const refundFormRef = ref<FormInstance>()

const refundForm = reactive({
  orderId: 0,
  orderNo: '',
  paidAmount: 0,
  refundedAmount: 0,
  maxRefundAmount: 0,
  refundAmount: 0,
  reason: ''
})

const refundRules: FormRules = {
  refundAmount: [
    { required: true, message: '退款金额不能为空', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '退款金额必须大于0', trigger: 'blur' }
  ],
  reason: [
    { required: true, message: '退款原因不能为空', trigger: 'blur' },
    { max: 200, message: '退款原因最多200个字符', trigger: 'blur' }
  ]
}

const handleRefund = (row: Order) => {
  refundForm.orderId = row.id
  refundForm.orderNo = row.orderNo
  refundForm.paidAmount = row.paidAmount
  refundForm.refundedAmount = row.refundAmount || 0
  refundForm.maxRefundAmount = row.paidAmount - (row.refundAmount || 0)
  refundForm.refundAmount = refundForm.maxRefundAmount
  refundForm.reason = ''
  refundVisible.value = true
}

const handleRefundSubmit = async () => {
  if (!refundFormRef.value) return
  await refundFormRef.value.validate(async (valid) => {
    if (valid) {
      refundLoading.value = true
      try {
        await refundOrder({
          orderId: refundForm.orderId,
          refundAmount: refundForm.refundAmount,
          reason: refundForm.reason
        })
        ElMessage.success('退款成功')
        refundVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '退款失败')
      } finally {
        refundLoading.value = false
      }
    }
  })
}

// ==================== 取消订单处理 ====================
const cancelVisible = ref(false)
const cancelLoading = ref(false)
const cancelFormRef = ref<FormInstance>()

const cancelForm = reactive({
  orderId: 0,
  orderNo: '',
  reason: ''
})

const cancelRules: FormRules = {
  reason: [
    { required: true, message: '取消原因不能为空', trigger: 'blur' },
    { max: 500, message: '取消原因最多500个字符', trigger: 'blur' }
  ]
}

const handleCancel = (row: Order) => {
  cancelForm.orderId = row.id
  cancelForm.orderNo = row.orderNo
  cancelForm.reason = ''
  cancelVisible.value = true
}

const handleCancelSubmit = async () => {
  if (!cancelFormRef.value) return
  await cancelFormRef.value.validate(async (valid) => {
    if (valid) {
      cancelLoading.value = true
      try {
        await cancelOrder({
          orderId: cancelForm.orderId,
          reason: cancelForm.reason
        })
        ElMessage.success('取消订单成功')
        cancelVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '取消订单失败')
      } finally {
        cancelLoading.value = false
      }
    }
  })
}

// ==================== 工具方法 ====================

/** 格式化价格 */
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}

/** 格式化日期为 yyyy-MM-dd */
const formatDate = (date?: string) => {
  if (!date) return '-'
  return date.substring(0, 10)
}

/** 支付方式文本 */
const paymentMethodText = (method: number) => {
  const map: Record<number, string> = { 1: '现金', 2: '支付宝', 3: '微信', 4: '银行卡', 5: '储值卡', 6: '积分抵扣' }
  return map[method] || '未知'
}

/** 支付方式标签类型 */
const paymentMethodTagType = (method: number) => {
  const map: Record<number, string> = { 1: '', 2: 'warning', 3: 'success', 4: 'info', 5: 'danger', 6: 'warning' }
  return map[method] || ''
}

/** 订单类型文本 */
const orderTypeText = (type: number) => {
  const map: Record<number, string> = { 1: '零售', 2: '服务', 3: '疗程卡核销', 4: '储值消费' }
  return map[type] || '未知'
}

/** 订单类型标签类型 */
const orderTypeTagType = (type: number) => {
  const map: Record<number, string> = { 1: 'info', 2: 'success', 3: 'warning', 4: '' }
  return map[type] || ''
}

/** 订单状态文本 */
const statusText = (status: number) => {
  const map: Record<number, string> = { 1: '进行中', 2: '已完成', 3: '已退款', 4: '已取消' }
  return map[status] || '未知'
}

/** 订单状态标签类型 */
const statusTagType = (status: number) => {
  const map: Record<number, string> = { 1: 'warning', 2: 'success', 3: 'info', 4: 'danger' }
  return map[status] || ''
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadData()
})
</script>

<style scoped>
.order-management {
  width: 100%;
}

/* 卡片样式 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

/* 表格样式 */
:deep(.el-table) {
  --el-table-bg-color: transparent !important;
  --el-table-text-color: var(--text-primary) !important;
  --el-table-border-color: transparent !important;
  --el-table-header-bg-color: var(--bg-tertiary) !important;
  --el-table-row-hover-bg-color: var(--bg-hover) !important;
}

:deep(.el-table th.el-table__cell) {
  background: var(--bg-tertiary) !important;
  color: var(--text-tertiary) !important;
  font-weight: 600;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table td.el-table__cell) {
  background-color: var(--bg-tertiary) !important;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table__row:hover > td.el-table__cell) {
  background-color: var(--bg-hover) !important;
}

/* 搜索区域 */
.search-form {
  padding: 20px 24px 0;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

/* 操作栏 */
.table-toolbar {
  display: flex;
  justify-content: flex-end;
  align-items: center;
  margin-bottom: 16px;
  padding: 0 4px;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 金额样式 */
.amount-text {
  color: var(--text-primary);
}

.price-text {
  color: var(--primary);
  font-weight: 600;
}

.discount-text {
  color: var(--el-color-danger);
  font-weight: 500;
}

.text-muted {
  color: var(--text-tertiary);
}

/* 详情区域 */
.detail-section-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 20px 0 12px;
  padding-left: 10px;
  border-left: 3px solid var(--primary);
}

/* 耗材扣减明细 */
.consumable-detail {
  padding: 4px;
}

.consumable-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 8px;
}

.consumable-total {
  margin-top: 8px;
  padding-top: 6px;
  border-top: 1px dashed var(--border-primary);
  font-size: 12px;
  color: var(--text-secondary);
  text-align: right;
}

.text-xs {
  font-size: 12px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
