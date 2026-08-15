<template>
  <div class="treatment-card-sale">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称">
            <el-input
              v-model="searchForm.customerName"
              placeholder="请输入客户名称"
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
      <div class="toolbar-left">
        <span class="toolbar-hint">疗程卡销售记录</span>
      </div>
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
        <el-table-column label="购买/剩余" width="110" align="center">
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
            <el-tag size="small" :type="getPaymentTagType(row.paymentMethod)">
              {{ getPaymentText(row.paymentMethod) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="purchaseDate" label="销售时间" width="170" />
        <el-table-column prop="storeCode" label="销售门店" width="100" />
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getSaleStatusType(row.status)" size="small" effect="dark">
              {{ getSaleStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">
              <el-icon><View /></el-icon>
              详情
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
          <el-tag size="small" :type="getPaymentTagType(detailData.paymentMethod)">
            {{ getPaymentText(detailData.paymentMethod) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="销售时间">{{ detailData.purchaseDate }}</el-descriptions-item>
        <el-descriptions-item label="销售门店">{{ detailData.storeCode }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, View } from '@element-plus/icons-vue'
import { getTreatmentCardSales } from '@/api/treatment-card'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { TreatmentCardSale, PaymentMethod } from '@/api/treatment-card/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerName: '',
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

// 支付方式文本
const getPaymentText = (method: PaymentMethod | undefined): string => {
  if (!method) return '-'
  const map: Record<string, string> = {
    cash: '现金',
    wechat: '微信',
    alipay: '支付宝',
    card: '银行卡',
    balance: '余额'
  }
  return map[method] || method
}

// 支付方式标签类型
const getPaymentTagType = (method: PaymentMethod | undefined): '' | 'success' | 'info' | 'warning' => {
  if (!method) return 'info'
  const map: Record<string, '' | 'success' | 'info' | 'warning'> = {
    cash: 'warning',
    wechat: 'success',
    alipay: '',
    card: 'info',
    balance: 'warning'
  }
  return map[method] || 'info'
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTreatmentCardSales({
      customerName: searchForm.customerName || undefined,
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
  searchForm.customerName = ''
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

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
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

.toolbar-left {
  display: flex;
  gap: 12px;
  align-items: center;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

.toolbar-hint {
  font-size: 13px;
  color: var(--text-tertiary);
}

.price-text {
  color: var(--primary);
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
