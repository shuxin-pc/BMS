<template>
  <div class="order-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称/手机号">
            <el-input
              v-model="searchForm.keyword"
              placeholder="姓名或手机号"
              clearable
              style="width: 170px"
            />
          </el-form-item>
          <el-form-item label="订单状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="已完成" :value="2" />
              <el-option label="已退款" :value="3" />
              <el-option label="已取消" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="订单类型">
            <el-select v-model="searchForm.orderType" placeholder="全部" clearable style="width: 120px">
              <el-option label="零售" :value="1" />
              <el-option label="服务" :value="2" />
              <el-option label="项目卡核销" :value="3" />
              <el-option label="储值消费" :value="4" />
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
              style="width: 240px"
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
        <el-table-column prop="orderNo" label="订单号" width="200" />
        <el-table-column label="客户" width="120">
          <template #default="{ row }">
            {{ row.customerName || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="订单类型" width="110" align="center">
          <template #default="{ row }">
            <el-tag :type="orderTypeTagType(row.orderType)" size="small" effect="plain">
              {{ orderTypeText(row.orderType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="订单金额" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥{{ formatPrice(row.productAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="优惠" width="110" align="right">
          <template #default="{ row }">
            <span v-if="row.discountAmount" class="discount-text">-¥{{ formatPrice(row.discountAmount) }}</span>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column label="实付金额" width="120" align="right">
          <template #default="{ row }">
            <!-- 实付金额展示净实付（实收 - 已退款），退款后金额随之减少 -->
            <span class="price-text">¥{{ formatPrice((row.paidAmount || 0) - (row.refundAmount || 0)) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="支付方式" width="120" align="center">
          <template #default="{ row }">
            <span v-if="row.paidAmount === 0" class="text-muted">-</span>
            <el-tag v-else :type="paymentMethodTagType(row.payMethod)" size="small" effect="plain">
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
        <el-table-column label="下单时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.orderTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleDetail(row)">
              <el-icon><View /></el-icon>
              详情
            </el-button>
            <el-button
              v-if="hasPermission('store:pos:order:refund') && row.status === 2 && row.orderType !== 3"
              link
              type="warning"
              size="small"
              @click="handleRefund(row)"
            >
              <el-icon><RefreshLeft /></el-icon>
              退款
            </el-button>
            <el-button
              v-if="hasPermission('store:pos:order:cancel') && row.status === 2 && !(row.refundAmount > 0)"
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
      :width="detailWidth || 1400"
      :close-on-click-modal="false"
      modal-class="order-detail-dialog"
    >
      <div v-loading="detailLoading">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="订单号">{{ currentOrder?.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="订单状态">
            <el-tag :type="statusTagType(currentOrder?.status || 0)" size="small" effect="dark">
              {{ statusText(currentOrder?.status || 0) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="客户">
            {{ currentOrder?.customerName || '-' }}
          </el-descriptions-item>
          <el-descriptions-item label="订单类型">
            <el-tag :type="orderTypeTagType(currentOrder?.orderType || 0)" size="small" effect="plain">
              {{ orderTypeText(currentOrder?.orderType || 0) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="支付方式">
            <span v-if="currentOrder?.paidAmount === 0" class="text-muted">-</span>
            <span v-else>{{ paymentMethodText(currentOrder?.payMethod || 0) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="下单时间">{{ formatDateTime(currentOrder?.orderTime) }}</el-descriptions-item>
          <el-descriptions-item label="订单金额">¥{{ formatPrice(currentOrder?.productAmount) }}</el-descriptions-item>
          <el-descriptions-item label="折扣率">{{ discountRateText(currentOrder?.productAmount, currentOrder?.discountAmount) }}</el-descriptions-item>
          <el-descriptions-item label="优惠金额">¥{{ formatPrice(currentOrder?.discountAmount) }}</el-descriptions-item>
          <el-descriptions-item label="实付金额">
            <!-- 实付金额展示净实付（实收 - 已退款），已退款金额单独展示 -->
            <span class="price-text">¥{{ formatPrice((currentOrder?.paidAmount || 0) - (currentOrder?.refundAmount || 0)) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="退款金额" v-if="currentOrder?.refundAmount">
            <span class="discount-text">¥{{ formatPrice(currentOrder.refundAmount) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ currentOrder?.remark || '-' }}</el-descriptions-item>
        </el-descriptions>

        <div class="detail-section-title">订单明细</div>
        <el-table :data="currentOrder?.items || []" border style="width: 100%">
          <el-table-column
            v-for="col in detailColumns"
            :key="col.prop"
            :prop="col.prop"
            :label="col.label"
            :min-width="col.minWidth"
            :width="col.width"
            :align="col.align"
          >
            <template #default="{ row }">
              <template v-if="col.type === 'productType'">
                {{ productTypeText(row.productType) }}
              </template>
              <template v-else-if="col.type === 'technicianSource'">
                {{ row.technicianSource ? technicianSourceText(row.technicianSource) : '-' }}
              </template>
              <template v-else-if="col.type === 'datetime'">
                {{ formatDateTime(row[col.prop]) }}
              </template>
              <template v-else-if="col.type === 'price'">
                <span :class="col.prop === 'discountedAmount' ? 'price-text' : ''">
                  ¥{{ formatPrice(row[col.prop]) }}
                </span>
              </template>
              <template v-else-if="col.type === 'batch'">
                {{ batchNosText(row) }}
              </template>
              <template v-else>
                {{ row[col.prop] || '-' }}
              </template>
            </template>
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
      width="820px"
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
        <el-form-item label="支付方式">
          <span>{{ paymentMethodText(refundForm.payMethod) }}</span>
          <span v-if="refundForm.payMethod === 7" class="pay-detail">
            {{ cashPayMethodText(refundForm.cashPayMethod) }} {{ formatPrice(refundForm.cashAmount) }} + 储值 {{ formatPrice(refundForm.storedValueAmount) }} + 积分抵扣 {{ formatPrice(refundForm.pointsAmount) }}
          </span>
        </el-form-item>
        <el-form-item label="退款去向" v-if="refundPreview.length">
          <div style="width: 100%">
            <div
              v-for="row in refundPreview"
              :key="row.label"
              class="refund-preview-row"
              :class="{ 'refund-preview-row--cash': row.highlight, 'refund-preview-row--net': row.net }"
            >
              <span class="refund-preview-label">{{ row.label }}</span>
              <span>{{ row.text }}</span>
            </div>
            <div class="refund-tip">按原支付方式比例分摊，实际以系统执行为准；若下单赠送积分已不足扣回，将从退款金额中折现扣除。</div>
          </div>
        </el-form-item>
        <el-form-item label="退库明细">
          <div style="width: 100%">
            <div class="refund-tip">
              请选择需要退回库存的批次与数量（服务项目/项目卡展示其 BOM 耗材；填 0 表示该批次不退）
            </div>
            <el-table
              v-if="refundBatches.length"
              :data="refundBatches"
              border
              size="small"
              max-height="260"
              style="width: 100%"
            >
              <el-table-column label="所属商品" prop="orderItemName" min-width="120" show-overflow-tooltip />
              <el-table-column label="退库商品" prop="productName" min-width="110" show-overflow-tooltip />
              <el-table-column label="批次号" prop="batchNo" min-width="110" show-overflow-tooltip />
              <el-table-column label="过期日期" width="100" align="center">
                <template #default="{ row }">
                  {{ row.expirationDate ? row.expirationDate.slice(0, 10) : '-' }}
                </template>
              </el-table-column>
              <el-table-column label="可退数量" prop="maxQuantity" width="80" align="center" />
              <el-table-column label="退库数量" width="150" align="center">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.quantity"
                    :min="0"
                    :max="row.maxQuantity"
                    :precision="0"
                    :step="1"
                    controls-position="right"
                    size="small"
                    style="width: 128px"
                  />
                </template>
              </el-table-column>
            </el-table>
            <el-empty
              v-else
              description="该订单无可退批次（无批次扣减记录），本次仅退金额不退库存"
              :image-size="60"
            />
          </div>
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
      width="560px"
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
          取消订单将全量回滚库存/BOM/项目卡/积分/储值/统计/消费记录，订单视为未发生。
        </el-alert>
        <el-form-item label="支付方式">
          <span>{{ paymentMethodText(cancelForm.payMethod) }}</span>
          <span v-if="cancelForm.payMethod === 7" class="pay-detail">
            {{ cashPayMethodText(cancelForm.cashPayMethod) }} {{ formatPrice(cancelForm.cashAmount) }} + 储值 {{ formatPrice(cancelForm.storedValueAmount) }} + 积分抵扣 {{ formatPrice(cancelForm.pointsAmount) }}
          </span>
        </el-form-item>
        <el-form-item label="退回去向" v-if="cancelRefundPreview.length">
          <div style="width: 100%">
            <div
              v-for="row in cancelRefundPreview"
              :key="row.label"
              class="refund-preview-row"
              :class="{ 'refund-preview-row--cash': row.highlight, 'refund-preview-row--net': row.net }"
            >
              <span class="refund-preview-label">{{ row.label }}</span>
              <span>{{ row.text }}</span>
            </div>
            <div class="refund-tip">取消后按原支付方式全额原路退回，实际以系统执行为准。</div>
          </div>
        </el-form-item>
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
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, View, RefreshLeft, CircleClose } from '@element-plus/icons-vue'
import { getOrders, getOrder, refundOrder, cancelOrder } from '@/api/order'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { Order, OrderItem, OrderStatus, OrderType } from '@/api/order/types'
import { formatDateTime } from '@/utils/date'
import { roundMoney } from '@/utils/money'

/** 订单明细动态列配置项 */
interface DetailColumn {
  prop: string
  label: string
  minWidth?: number
  width?: number
  align?: 'left' | 'center' | 'right'
  type?: 'productType' | 'technicianSource' | 'datetime' | 'price' | 'batch'
}

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  keyword: '',
  status: undefined as OrderStatus | undefined,
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
      keyword: searchForm.keyword || undefined,
      status: searchForm.status,
      orderType: searchForm.orderType,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error((error as Error).message || '加载数据失败')
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
  searchForm.keyword = ''
  searchForm.status = undefined
  searchForm.orderType = undefined
  searchForm.dateRange = []
  handleSearch()
}

// ==================== 订单详情 ====================
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentOrder = ref<Order>()
/** 详情弹窗宽度（按明细列数动态计算，避免过宽导致左侧进入菜单栏区域被遮挡） */
const detailWidth = ref(0)

// 订单明细列：公共列 + 服务列（仅当订单含服务商品时显示），按商品类型展示不同明细列
const detailColumns = computed<DetailColumn[]>(() => {
  const hasService = (currentOrder.value?.items || []).some((i) => i.productType === 2)
  const hasRetail = (currentOrder.value?.items || []).some((i) => i.productType === 1)
  // 商品名称列与批次号列用 minWidth（弹性），其余列固定宽度；
  // 固定列总和 + 弹性列最小值 = 最小表格宽，弹窗 1400px 内容区需 ≥ 该值才不出现横向滚动条
  const base: DetailColumn[] = [
    // 商品名称用 minWidth（弹性列）：表格有多余宽度时由它吸收，避免折后金额列右侧出现空白区域
    { prop: 'productName', label: '商品名称', minWidth: 150 },
    { prop: 'productCode', label: '商品编码', width: 110 },
    { prop: 'productType', label: '商品类型', width: 100, align: 'center', type: 'productType' }
  ]
  const retailCols: DetailColumn[] = hasRetail
    ? [{ prop: 'batches', label: '批次号', minWidth: 140, type: 'batch' }]
    : []
  const serviceCols: DetailColumn[] = hasService
    ? [
        { prop: 'technicianName', label: '技师', width: 80, align: 'center' },
        { prop: 'technicianSource', label: '技师来源', width: 90, align: 'center', type: 'technicianSource' },
        { prop: 'roomName', label: '房间/床位', width: 95 },
        { prop: 'equipmentName', label: '设备', width: 95 },
        { prop: 'serviceStartTime', label: '服务开始', width: 130, type: 'datetime' },
        { prop: 'serviceEndTime', label: '服务结束', width: 130, type: 'datetime' }
      ]
    : []
  const moneyCols: DetailColumn[] = [
    { prop: 'quantity', label: '数量', width: 60, align: 'center' },
    { prop: 'price', label: '单价', width: 80, align: 'right', type: 'price' },
    { prop: 'discountedAmount', label: '折后金额', width: 90, align: 'right', type: 'price' }
  ]
  return [...base, ...retailCols, ...serviceCols, ...moneyCols]
})

/**
 * 计算订单详情弹窗宽度
 * 按明细列数动态计算所需宽度，并限制在可视主内容区宽度内（视口 - 侧边栏 220px - 留白），
 * 避免弹窗过宽导致左侧进入侧边栏菜单区域被遮挡
 */
const calcDetailWidth = (items: OrderItem[]) => {
  const hasService = items.some((i) => i.productType === 2)
  const hasRetail = items.some((i) => i.productType === 1)
  // 与 detailColumns 定义一致的固定列宽总和（批次号列为弹性列，按最小值 140 计入）
  const fixedW =
    150 + 110 + 100 + // 商品名称/编码/类型
    (hasRetail ? 140 : 0) + // 批次号列
    (hasService ? 80 + 90 + 95 + 95 + 130 + 130 : 0) + // 技师/来源/房间/设备/服务起止
    60 + 80 + 90 // 数量/单价/折后金额
  // 弹窗内边距 + 表格边框留白
  const needed = fixedW + 60
  // 上限：视口宽度 - 侧边栏(220) - 左右留白(60)，保证弹窗落在主内容区内
  const maxW = window.innerWidth - 220 - 60
  return Math.min(needed, maxW)
}

const handleDetail = async (row: Order) => {
  detailVisible.value = true
  detailLoading.value = true
  try {
    currentOrder.value = await getOrder(row.id)
    detailWidth.value = calcDetailWidth(currentOrder.value?.items || [])
  } catch (error) {
    ElMessage.error((error as Error).message || '加载详情失败')
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
  reason: '',
  // 支付信息（用于展示退款去向拆分）
  payMethod: 0,
  cashAmount: 0,
  cashPayMethod: 0,
  storedValueAmount: 0,
  pointsAmount: 0,
  deductRate: 0,
  // 下单时发放的赠送积分（Order.Points，用于计算退款需按比例收回的积分）
  points: 0
})

/** 退款弹窗可退批次行（粒度 = 订单明细批次 OrderItemBatch） */
interface RefundBatchRow {
  /** 订单明细批次ID（OrderItemBatch.Id） */
  orderItemBatchId: number
  /** 所属订单明细商品名称（服务项目名/实物商品名） */
  orderItemName: string
  /** 退库商品名称（服务/核销行为其 BOM 耗材名，实物商品行为商品名） */
  productName: string
  /** 批次号 */
  batchNo: string
  /** 过期日期（ISO 字符串，可空） */
  expirationDate?: string
  /** 可退数量 = 扣减数量 - 已退款数量 */
  maxQuantity: number
  /** 本次退库数量（默认 = 可退数量，填 0 表示该批次不退） */
  quantity: number
}

/** 可退批次清单（打开退款弹窗时按订单详情批次构建，默认全选可退数量） */
const refundBatches = ref<RefundBatchRow[]>([])

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

const handleRefund = async (row: Order) => {
  refundForm.orderId = row.id
  refundForm.orderNo = row.orderNo
  refundForm.paidAmount = row.paidAmount
  refundForm.refundedAmount = row.refundAmount || 0
  refundForm.maxRefundAmount = roundMoney(row.paidAmount - (row.refundAmount || 0))
  refundForm.refundAmount = refundForm.maxRefundAmount
  refundForm.reason = ''
  refundForm.payMethod = row.payMethod || 0
  refundForm.cashAmount = row.cashAmount || 0
  refundForm.cashPayMethod = row.cashPayMethod || 0
  refundForm.storedValueAmount = row.storedValueAmount || 0
  refundForm.pointsAmount = row.pointsAmount || 0
  refundForm.deductRate = row.deductRate || 0
  refundForm.points = row.points || 0
  refundBatches.value = []
  refundVisible.value = true
  // 拉取订单详情（含明细批次），构建可退批次清单（默认全选可退数量，门店按实际未消耗情况调整）
  try {
    const detail = await getOrder(row.id)
    const batches: RefundBatchRow[] = []
    for (const item of detail.items || []) {
      for (const b of item.batches || []) {
        const maxQty = b.quantity - (b.refundedQuantity || 0)
        if (maxQty <= 0) continue
        batches.push({
          orderItemBatchId: b.id,
          orderItemName: item.productName,
          productName: b.productName || item.productName,
          batchNo: b.batchNo,
          expirationDate: b.expirationDate,
          maxQuantity: maxQty,
          quantity: maxQty
        })
      }
    }
    refundBatches.value = batches
  } catch (error) {
    // 无法加载可退批次时关闭弹窗，避免在无批次数据下盲目退款
    refundVisible.value = false
    ElMessage.error((error as Error).message || '加载可退批次失败')
  }
}

const handleRefundSubmit = async () => {
  if (!refundFormRef.value) return
  await refundFormRef.value.validate(async (valid) => {
    if (valid) {
      refundLoading.value = true
      try {
        // 组装退库明细：只提交退库数量 > 0 的批次（粒度 OrderItemBatch，只从订单已有批次退回）
        const refundItems = refundBatches.value
          .filter((r) => r.quantity > 0)
          .map((r) => ({ orderItemBatchId: r.orderItemBatchId, quantity: r.quantity }))
        await refundOrder({
          orderId: refundForm.orderId,
          refundAmount: roundMoney(refundForm.refundAmount),
          reason: refundForm.reason,
          refundItems
        })
        ElMessage.success('退款成功')
        refundVisible.value = false
        loadData()
      } catch (error) {
        ElMessage.error((error as Error).message || '退款失败')
      } finally {
        refundLoading.value = false
      }
    }
  })
}

/** 退款去向拆分行 */
interface RefundPreviewRow {
  label: string
  text: string
  /** 是否线下/第三方退回（需店员实际退款，高亮提示） */
  highlight?: boolean
  /** 是否积分净退回汇总行（加粗醒目，与会员实际到账一致） */
  net?: boolean
}

/**
 * 计算退款金额按支付路径的拆分（与后端 OrderAppService.RefundCombinedPaymentAsync 按比例分摊一致）
 * - 组合支付（payMethod=7）：按 CashAmount/StoredValueAmount/PointsAmount 三栏比例分摊，最后一栏减法补齐
 * - 单一支付：整笔按对应路径退回（积分按 DeductRate 快照换算积分数量）
 */
const calcRefundBreakdown = (
  f: { payMethod: number; cashAmount: number; cashPayMethod: number; storedValueAmount: number; pointsAmount: number; deductRate: number; points: number; paidAmount: number },
  amount: number
): RefundPreviewRow[] => {
  if (amount <= 0) return []
  const rows: RefundPreviewRow[] = []
  const round2 = (n: number) => Math.round(n * 100) / 100
  const rate = f.deductRate > 0 ? f.deductRate : 0
  const pointsText = (money: number) => {
    if (rate > 0) {
      const pts = Math.floor(money / rate)
      return `¥${formatPrice(money)} → 退 ${pts} 积分`
    }
    return `¥${formatPrice(money)} → 按订单抵扣比例退积分`
  }
  if (f.payMethod === 7) {
    const cash = f.cashAmount || 0
    const sv = f.storedValueAmount || 0
    const points = f.pointsAmount || 0
    const total = cash + sv + points
    if (total <= 0) return [{ label: '合计', text: '无支付拆分数据' }]
    const svRefund = round2(amount * (sv / total))
    const pointsRefund = round2(amount * (points / total))
    const cashRefund = round2(amount - svRefund - pointsRefund)
    // 积分去向精简为一行：退款涉及积分联动（退还抵扣积分 或 收回赠送积分）时，
    // 将各项积分口径（退还抵扣 / 收回赠送 / 净退回）合并到同一行，明确各积分项内容，与会员实际到账一致
    const returnPts = pointsRefund > 0 && rate > 0 ? Math.floor(pointsRefund / rate) : 0
    const deductPts = f.points > 0 && f.paidAmount > 0 ? Math.round(f.points * (amount / f.paidAmount)) : 0
    if (pointsRefund > 0 && deductPts === 0) {
      // 仅退还抵扣积分、无赠送积分收回联动：维持单行积分抵扣
      rows.push({ label: '积分抵扣', text: pointsText(pointsRefund) })
    } else if (pointsRefund > 0 || deductPts > 0) {
      // 积分联动存在：合并为一行，逐项列明退还抵扣、收回赠送、净退回
      const parts: string[] = []
      if (pointsRefund > 0) {
        parts.push(rate > 0 ? `退 ¥${formatPrice(pointsRefund)} → ${returnPts} 抵扣积分` : `退 ¥${formatPrice(pointsRefund)} 抵扣（按订单比例换算）`)
      }
      if (deductPts > 0) parts.push(`扣回 ${deductPts} 赠送积分`)
      const netPts = returnPts - deductPts
      if (netPts !== 0) parts.push(`实际到账 ${netPts > 0 ? '+' : ''}${netPts} 积分`)
      rows.push({ label: '积分', text: `${parts.join('，')}`, net: true })
    }
    if (svRefund > 0) rows.push({ label: '储值', text: `¥${formatPrice(svRefund)} → 退回储值账户实收余额` })
    if (cashRefund > 0) rows.push({ label: cashPayMethodText(f.cashPayMethod), text: `¥${formatPrice(cashRefund)} → 线下/第三方退回`, highlight: true })
    return rows
  }
  if (f.payMethod >= 1 && f.payMethod <= 4) {
    return [{ label: cashPayMethodText(f.payMethod), text: `¥${formatPrice(amount)} → 线下/第三方退回`, highlight: true }]
  }
  if (f.payMethod === 5) {
    return [{ label: '储值', text: `¥${formatPrice(amount)} → 退回储值账户实收余额` }]
  }
  if (f.payMethod === 6) {
    return [{ label: '积分抵扣', text: pointsText(amount) }]
  }
  return []
}

/** 退款弹窗：本次退款金额对应的去向拆分（随退款金额输入联动） */
const refundPreview = computed(() =>
  calcRefundBreakdown(
    {
      payMethod: refundForm.payMethod,
      cashAmount: refundForm.cashAmount,
      cashPayMethod: refundForm.cashPayMethod,
      storedValueAmount: refundForm.storedValueAmount,
      pointsAmount: refundForm.pointsAmount,
      deductRate: refundForm.deductRate,
      points: refundForm.points,
      paidAmount: refundForm.paidAmount
    },
    refundForm.refundAmount
  )
)

/** 取消弹窗：取消后全额原路退回拆分 */
const cancelRefundPreview = computed(() =>
  calcRefundBreakdown(
    {
      payMethod: cancelForm.payMethod,
      cashAmount: cancelForm.cashAmount,
      cashPayMethod: cancelForm.cashPayMethod,
      storedValueAmount: cancelForm.storedValueAmount,
      pointsAmount: cancelForm.pointsAmount,
      deductRate: cancelForm.deductRate,
      points: cancelForm.points,
      paidAmount: cancelForm.paidAmount
    },
    cancelForm.paidAmount
  )
)

// ==================== 取消订单处理 ====================
const cancelVisible = ref(false)
const cancelLoading = ref(false)
const cancelFormRef = ref<FormInstance>()

const cancelForm = reactive({
  orderId: 0,
  orderNo: '',
  reason: '',
  // 支付信息（用于展示取消后原路退回拆分）
  paidAmount: 0,
  payMethod: 0,
  cashAmount: 0,
  cashPayMethod: 0,
  storedValueAmount: 0,
  pointsAmount: 0,
  deductRate: 0,
  // 下单时发放的赠送积分（Order.Points，用于计算取消需按比例收回的积分）
  points: 0
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
  cancelForm.paidAmount = row.paidAmount
  cancelForm.payMethod = row.payMethod || 0
  cancelForm.cashAmount = row.cashAmount || 0
  cancelForm.cashPayMethod = row.cashPayMethod || 0
  cancelForm.storedValueAmount = row.storedValueAmount || 0
  cancelForm.pointsAmount = row.pointsAmount || 0
  cancelForm.deductRate = row.deductRate || 0
  cancelForm.points = row.points || 0
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
      } catch (error) {
        ElMessage.error((error as Error).message || '取消订单失败')
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

/** 整单折扣率文本（1 - 优惠金额/商品金额，如 8.5折；无优惠或商品金额为 0 时显示 "-"） */
const discountRateText = (productAmount: number | undefined, discountAmount: number | undefined) => {
  if (!productAmount || !discountAmount) return '-'
  const rate = ((productAmount - discountAmount) / productAmount) * 10
  return `${rate.toFixed(1)}折`
}

/** 批次号文本：实物商品明细行的批次号用"、"组合显示，非实物行或无批次扣减记录时显示 "-" */
const batchNosText = (row: OrderItem) => {
  if (row.productType !== 1) return '-'
  const batches = row.batches || []
  if (!batches.length) return '-'
  return batches.map((b) => b.batchNo).join('、')
}

/** 商品类型文本 */
const productTypeText = (type: number) => {
  const map: Record<number, string> = { 1: '实物商品', 2: '服务商品', 3: '耗材', 4: '样品', 5: '赠品' }
  return map[type] || '未知'
}

/** 技师来源文本 */
const technicianSourceText = (source: number) => {
  const map: Record<number, string> = { 1: '商家技师', 2: '平台技师' }
  return map[source] || '未知'
}


/** 支付方式文本 */
const paymentMethodText = (method: number) => {
  const map: Record<number, string> = { 1: '现金', 2: '支付宝', 3: '微信', 4: '银行卡', 5: '储值卡', 6: '积分抵扣', 7: '组合支付' }
  return map[method] || '未知'
}

/** 组合支付类别1（现金类）具体方式文本 */
const cashPayMethodText = (method: number) => {
  const map: Record<number, string> = { 1: '现金', 2: '支付宝', 3: '微信', 4: '银行卡' }
  return map[method] || '现金类'
}

/** 支付方式标签类型 */
const paymentMethodTagType = (method: number) => {
  const map: Record<number, string> = { 1: '', 2: 'warning', 3: 'success', 4: 'info', 5: 'danger', 6: 'warning', 7: 'danger' }
  return map[method] || ''
}

/** 订单类型文本 */
const orderTypeText = (type: number) => {
  const map: Record<number, string> = { 1: '零售', 2: '服务', 3: '项目卡核销', 4: '储值消费' }
  return map[type] || '未知'
}

/** 订单类型标签类型 */
const orderTypeTagType = (type: number) => {
  const map: Record<number, string> = { 1: 'info', 2: 'success', 3: 'warning', 4: '' }
  return map[type] || ''
}

/** 订单状态文本 */
const statusText = (status: number) => {
  const map: Record<number, string> = { 2: '已完成', 3: '已退款', 4: '已取消' }
  return map[status] || '未知'
}

/** 订单状态标签类型 */
const statusTagType = (status: number) => {
  const map: Record<number, string> = { 2: 'success', 3: 'info', 4: 'danger' }
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

/* 退款弹窗：可退批次提示 */
.refund-tip {
  color: var(--text-tertiary);
  font-size: 12px;
  line-height: 1.6;
  margin-bottom: 8px;
}

/* 退款/取消弹窗：组合支付方式详情 */
.pay-detail {
  color: var(--text-tertiary);
  font-size: 12px;
  margin-left: 8px;
}

/* 退款/取消弹窗：退款去向拆分行 */
.refund-preview-row {
  display: flex;
  gap: 8px;
  line-height: 1.8;
  font-size: 13px;
}

.refund-preview-label {
  color: var(--text-tertiary);
  width: 64px;
  flex-shrink: 0;
}

/* 线下/第三方退回行高亮：需店员实际退款现金，醒目提示 */
.refund-preview-row--cash {
  color: var(--el-color-warning);
  font-weight: 600;
}

/* 积分净退回汇总行：加粗醒目，展示与会员实际到账一致的最终积分 */
.refund-preview-row--net {
  color: var(--el-color-primary);
  font-weight: 700;
  margin-top: 2px;
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

/* 订单详情弹窗整体右移侧边栏宽度的一半(110px)，使其相对主内容区居中，
   避免弹窗过宽时左侧进入侧边栏菜单区域被遮挡；
   el-dialog 默认通过 margin: auto 水平居中，故用 transform 在居中基础上偏移 */
:global(.order-detail-dialog .el-overlay-dialog .el-dialog) {
  transform: translateX(110px);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
