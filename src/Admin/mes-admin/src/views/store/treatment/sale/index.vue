<template>
  <div class="treatment-card-sale">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称/手机号">
            <el-input
              v-model="searchForm.keyword"
              placeholder="姓名或手机号"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="卡名称">
            <el-input
              v-model="searchForm.cardName"
              placeholder="请输入卡名称"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="有效" :value="1" />
              <el-option label="已用完" :value="2" />
              <el-option label="已退款" :value="3" />
            </el-select>
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
        <el-table-column prop="saleNo" label="销售单号" width="150" />
        <el-table-column prop="customerName" label="客户名称" width="100" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="cardName" label="卡名称" min-width="140" show-overflow-tooltip />
        <el-table-column label="项目/剩余" width="110" align="center">
          <template #default="{ row }">
            <span class="count-text">
              {{ row.totalCount }} / <span :class="{ 'count-warn': row.remainingTimes <= 2 }">{{ row.remainingTimes }}</span>
            </span>
          </template>
        </el-table-column>
        <el-table-column label="实付金额" width="110" align="right">
          <template #default="{ row }">
            <span class="price-text">¥{{ formatPrice(row.amount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="支付方式" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small" :type="getPaymentTagType(row.payMethod)">
              {{ getPaymentText(row.payMethod) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="销售时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.purchaseDate) }}</template>
        </el-table-column>
        <el-table-column label="到期时间" width="170">
          <template #default="{ row }">
            {{ row.validityDays > 0 ? formatDateTime(row.expiryDate) : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getSaleStatusType(row.status)" size="small" effect="dark">
              {{ getSaleStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">
              <el-icon><View /></el-icon>
              详情
            </el-button>
            <el-button
              v-if="hasPermission('store:treatment:sale:refund') && (row.status === 1 || row.status === 2)"
              link
              type="warning"
              size="small"
              @click="handleRefund(row)"
            >
              <el-icon><RefreshLeft /></el-icon>
              退卡
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

    <!-- 详情弹窗 -->
    <el-dialog v-model="detailVisible" title="销售记录详情" width="560px">
      <el-descriptions :column="2" border v-if="detailData">
        <el-descriptions-item label="销售单号">{{ detailData.saleNo }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="getSaleStatusType(detailData.status)" size="small" effect="dark">
            {{ getSaleStatusText(detailData.status) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="客户名称">{{ detailData.customerName }}</el-descriptions-item>
        <el-descriptions-item label="手机号">{{ detailData.phone }}</el-descriptions-item>
        <el-descriptions-item label="卡名称" :span="2">{{ detailData.cardName }}</el-descriptions-item>
        <el-descriptions-item label="购买次数">{{ detailData.totalCount }}</el-descriptions-item>
        <el-descriptions-item label="剩余次数">{{ detailData.remainingTimes }}</el-descriptions-item>
        <el-descriptions-item label="实付金额">
          <span class="price-text">¥{{ formatPrice(detailData.amount) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="支付方式">
          <el-tag size="small" :type="getPaymentTagType(detailData.payMethod)">
            {{ getPaymentText(detailData.payMethod) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="销售时间" :span="2">{{ formatDateTime(detailData.purchaseDate) }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>

    <!-- 退卡弹窗 -->
    <el-dialog
      v-model="refundVisible"
      title="项目卡退卡"
      width="480px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="refundFormRef"
        :model="refundForm"
        :rules="refundRules"
        label-width="100px"
      >
        <el-form-item label="卡名称">
          <span>{{ refundForm.cardName }}</span>
        </el-form-item>
        <el-form-item label="客户">
          <span>{{ refundForm.customerName }}</span>
        </el-form-item>
        <el-form-item label="原售价">
          <span class="price-text">¥{{ formatPrice(refundForm.amount) }}</span>
        </el-form-item>
        <el-form-item label="已消费">
          <span>¥{{ formatPrice(refundForm.consumedAmount) }}</span>
        </el-form-item>
        <el-form-item label="应退金额">
          <span class="refund-text">¥{{ formatPrice(refundForm.refundAmount) }}</span>
        </el-form-item>
        <el-form-item label="退卡原因" prop="remark">
          <el-input
            v-model="refundForm.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入退卡原因"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="refundVisible = false">取消</el-button>
        <el-button type="primary" :loading="refundLoading" @click="handleRefundSubmit">
          确认退卡
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { Search, Refresh, View, RefreshLeft } from '@element-plus/icons-vue'
import { getTreatmentCardSales, refundTreatmentCardSale } from '@/api/treatment-card'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import { formatDateTime } from '@/utils/date'
import type { TreatmentCardSale } from '@/api/treatment-card/types'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  keyword: '',
  cardName: '',
  status: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<TreatmentCardSale[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 格式化价格
const formatPrice = (price: number) => price.toFixed(2)

// 销售状态文本
const getSaleStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '有效', 2: '已用完', 3: '已退款' }
  return map[status] || '未知'
}

// 销售状态标签类型
const getSaleStatusType = (status: number): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: 'success',
    2: 'info',
    3: 'danger'
  }
  return map[status] || 'info'
}

// 支付方式文本（对齐订单 Order.PayMethod 枚举：1现金 2支付宝 3微信 4银行卡 5储值卡 6积分抵扣 7组合支付）
const getPaymentText = (method: number | undefined): string => {
  if (!method) return '-'
  const map: Record<number, string> = { 1: '现金', 2: '支付宝', 3: '微信', 4: '银行卡', 5: '储值卡', 6: '积分抵扣', 7: '组合支付' }
  return map[method] || '未知'
}

// 支付方式标签类型
const getPaymentTagType = (method: number | undefined): '' | 'success' | 'info' | 'warning' | 'danger' => {
  if (!method) return 'info'
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: 'warning',
    2: 'warning',
    3: 'success',
    4: 'info',
    5: 'danger',
    6: 'warning',
    7: 'danger'
  }
  return map[method] || 'info'
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTreatmentCardSales({
      keyword: searchForm.keyword || undefined,
      cardName: searchForm.cardName || undefined,
      status: searchForm.status,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载数据失败')
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
  searchForm.cardName = ''
  searchForm.status = undefined
  handleSearch()
}

// 详情弹窗
const detailVisible = ref(false)
const detailData = ref<TreatmentCardSale | null>(null)

const handleView = (row: TreatmentCardSale) => {
  detailData.value = row
  detailVisible.value = true
}

// ==================== 退卡处理 ====================
const refundVisible = ref(false)
const refundLoading = ref(false)
const refundFormRef = ref<FormInstance>()
const refundForm = reactive({
  id: '',
  cardName: '',
  customerName: '',
  amount: 0,
  consumedAmount: 0,
  refundAmount: 0,
  remark: ''
})

const refundRules: FormRules = {
  remark: [
    { required: true, message: '退卡原因不能为空', trigger: 'blur' },
    { max: 200, message: '退卡原因最多200个字符', trigger: 'blur' }
  ]
}

// 打开退卡弹窗：应退金额 = 售价 - 已消费金额（与后端 RefundAsync 口径一致）
const handleRefund = (row: TreatmentCardSale) => {
  refundForm.id = String(row.id)
  refundForm.cardName = row.cardName || '-'
  refundForm.customerName = row.customerName || '-'
  refundForm.amount = row.amount
  refundForm.consumedAmount = row.totalConsumedAmount || 0
  refundForm.refundAmount = Math.max(0, refundForm.amount - refundForm.consumedAmount)
  refundForm.remark = ''
  refundVisible.value = true
}

const handleRefundSubmit = async () => {
  if (!refundFormRef.value) return
  await refundFormRef.value.validate(async (valid) => {
    if (!valid) return
    refundLoading.value = true
    try {
      const res = await refundTreatmentCardSale({
        id: refundForm.id,
        remark: refundForm.remark
      })
      ElMessage.success(`退卡成功，退款 ¥${formatPrice(res.refundAmount)}`)
      refundVisible.value = false
      loadData()
    } catch (error) {
      ElMessage.error((error as Error).message || '退卡失败')
    } finally {
      refundLoading.value = false
    }
  })
}

// 全局搜索跳转预填：读取路由 keyword 参数回填搜索框
const route = useRoute()
const routeKeyword = typeof route.query.keyword === 'string' ? route.query.keyword : ''

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  if (routeKeyword) {
    searchForm.keyword = routeKeyword
  }
  loadData()
})
</script>

<style scoped>
.treatment-card-sale {
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
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding: 0 4px;
}

.toolbar-right {
  display: flex;
  gap: 8px;
  /* 操作栏仅含右侧按钮时 space-between 对单子元素不生效，用 margin-left:auto 推到最右（对齐客户管理页面） */
  margin-left: auto;
}

.price-text {
  color: var(--primary);
  font-weight: 600;
}

.refund-text {
  color: #f56c6c;
  font-weight: 600;
}

.count-text {
  color: var(--text-primary);
}

.count-warn {
  color: #e6a23c;
  font-weight: 600;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
