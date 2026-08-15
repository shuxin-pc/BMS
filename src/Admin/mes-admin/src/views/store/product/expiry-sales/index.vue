<template>
  <div class="expiry-sales-stat">
    <!-- 查询表单 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="日期范围">
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 260px"
            />
          </el-form-item>
          <el-form-item label="商品类型">
            <el-select v-model="searchForm.productType" placeholder="全部" clearable style="width: 140px">
              <el-option label="实物商品" :value="1" />
              <el-option label="耗材" :value="3" />
            </el-select>
          </el-form-item>
          <el-form-item label="效期区间">
            <el-select v-model="searchForm.expiryBucket" placeholder="全部区间" clearable style="width: 140px">
              <el-option label="已过期" :value="1" />
              <el-option label="7天内" :value="2" />
              <el-option label="30天内" :value="3" />
              <el-option label="90天内" :value="4" />
              <el-option label="90天以上" :value="5" />
            </el-select>
          </el-form-item>
          <el-form-item label="关键词">
            <el-input
              v-model="searchForm.keyword"
              placeholder="商品名称/编码"
              clearable
              style="width: 180px"
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

    <!-- 效期区间分布概览 -->
    <div class="card mb-20" v-if="bucketSummary.length">
      <div class="card-header">
        <span class="card-title">效期区间分布</span>
      </div>
      <div class="bucket-summary">
        <div
          v-for="bucket in bucketSummary"
          :key="bucket.bucket"
          class="bucket-item"
          :class="getBucketClass(bucket.bucket)"
        >
          <div class="bucket-name">{{ bucket.name }}</div>
          <div class="bucket-quantity">{{ formatNumber(bucket.quantity) }}</div>
          <div class="bucket-amount">{{ formatCurrency(bucket.amount) }}</div>
          <div class="bucket-bar">
            <div
              class="bucket-bar-inner"
              :style="{ width: getBucketBarWidth(bucket.quantity) + '%' }"
            ></div>
          </div>
          <div class="bucket-percent">{{ getBucketPercent(bucket.quantity) }}%</div>
        </div>
      </div>
    </div>

    <!-- 表格区域 -->
    <div class="card">
      <div class="table-toolbar">
        <div class="toolbar-left">
          <span class="table-title">效期销售统计明细</span>
        </div>
        <div class="toolbar-right">
          <el-radio-group v-model="searchForm.sortBy" size="small" @change="handleSearch">
            <el-radio-button label="quantity">按数量</el-radio-button>
            <el-radio-button label="amount">按金额</el-radio-button>
            <el-radio-button label="count">按笔数</el-radio-button>
          </el-radio-group>
          <el-button circle @click="loadData">
            <el-icon><Refresh /></el-icon>
          </el-button>
        </div>
      </div>

      <el-table v-loading="tableLoading" :data="tableData" style="width: 100%">
        <el-table-column prop="productName" label="商品名称" min-width="160" />
        <el-table-column prop="productCode" label="商品编码" width="120" />
        <el-table-column label="商品类型" width="100">
          <template #default="{ row }">
            <el-tag :type="row.productType === 1 ? 'primary' : 'warning'" size="small">
              {{ row.productTypeName }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="categoryName" label="分类" width="120" show-overflow-tooltip />
        <el-table-column label="效期区间" width="120">
          <template #default="{ row }">
            <el-tag :type="getBucketTagType(row.expiryBucket)" size="small" effect="plain">
              {{ row.expiryBucketName }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="批次效期范围" width="200">
          <template #default="{ row }">
            <span v-if="row.expirationDateFrom">
              {{ row.expirationDateFrom?.substring(0, 10) }} ~ {{ row.expirationDateTo?.substring(0, 10) }}
            </span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column prop="salesQuantity" label="销售数量" width="110" align="right">
          <template #default="{ row }">
            {{ formatNumber(row.salesQuantity) }}
          </template>
        </el-table-column>
        <el-table-column prop="salesAmount" label="销售金额" width="120" align="right">
          <template #default="{ row }">
            {{ formatCurrency(row.salesAmount) }}
          </template>
        </el-table-column>
        <el-table-column prop="orderCount" label="订单笔数" width="100" align="right" />
        <el-table-column label="占比" width="100" align="right">
          <template #default="{ row }">
            <span class="percent-text">{{ row.quantityPercentage }}%</span>
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import { getProductExpirySalesStats, type ProductExpirySalesStat, type ProductExpirySalesQuery } from '@/api/statistics/expiry-sales'
import { useSystemConfigStore } from '@/stores/systemConfig'

const systemConfigStore = useSystemConfigStore()

// 查询表单
const searchForm = reactive<{
  productType?: 1 | 3
  expiryBucket?: 1 | 2 | 3 | 4 | 5
  keyword?: string
  sortBy: 'quantity' | 'amount' | 'count'
}>({
  productType: undefined,
  expiryBucket: undefined,
  keyword: undefined,
  sortBy: 'quantity'
})

// 日期范围（默认本月1日至今）
const today = new Date()
const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1)
const dateRange = ref<[string, string]>([
  formatDate(firstDayOfMonth),
  formatDate(today)
])

function formatDate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

// 表格数据
const tableLoading = ref(false)
const tableData = ref<ProductExpirySalesStat[]>([])
const pagination = reactive({
  pageIndex: 1,
  pageSize: 20,
  total: 0
})

// 效期区间汇总（基于当前查询结果的聚合）
const bucketSummary = computed(() => {
  const buckets = [
    { bucket: 1, name: '已过期', quantity: 0, amount: 0 },
    { bucket: 2, name: '7天内', quantity: 0, amount: 0 },
    { bucket: 3, name: '30天内', quantity: 0, amount: 0 },
    { bucket: 4, name: '90天内', quantity: 0, amount: 0 },
    { bucket: 5, name: '90天以上', quantity: 0, amount: 0 }
  ]
  // 仅基于当前页数据汇总（前端聚合，作为概览）
  tableData.value.forEach(item => {
    const b = buckets.find(x => x.bucket === item.expiryBucket)
    if (b) {
      b.quantity += item.salesQuantity
      b.amount += item.salesAmount
    }
  })
  return buckets.filter(b => b.quantity > 0)
})

const totalQuantity = computed(() => bucketSummary.value.reduce((sum, b) => sum + b.quantity, 0))

function getBucketPercent(quantity: number): string {
  if (totalQuantity.value === 0) return '0'
  return ((quantity / totalQuantity.value) * 100).toFixed(1)
}

function getBucketBarWidth(quantity: number): number {
  if (totalQuantity.value === 0) return 0
  return Math.max(2, (quantity / totalQuantity.value) * 100)
}

// 加载数据
async function loadData() {
  tableLoading.value = true
  try {
    const query: ProductExpirySalesQuery = {
      startDate: dateRange.value?.[0],
      endDate: dateRange.value?.[1],
      productType: searchForm.productType,
      expiryBucket: searchForm.expiryBucket,
      keyword: searchForm.keyword,
      sortBy: searchForm.sortBy,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    }
    const result = await getProductExpirySalesStats(query)
    tableData.value = result.list || []
    pagination.total = result.total || 0
  } catch (err) {
    ElMessage.error((err as Error).message || '加载效期销售统计失败')
    tableData.value = []
    pagination.total = 0
  } finally {
    tableLoading.value = false
  }
}

function handleSearch() {
  pagination.pageIndex = 1
  loadData()
}

function handleReset() {
  searchForm.productType = undefined
  searchForm.expiryBucket = undefined
  searchForm.keyword = undefined
  searchForm.sortBy = 'quantity'
  dateRange.value = [formatDate(firstDayOfMonth), formatDate(today)]
  pagination.pageIndex = 1
  loadData()
}

// 格式化工具
function formatNumber(n: number): string {
  return Number(n || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 })
}

function formatCurrency(n: number): string {
  return '¥' + Number(n || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

// 效期区间样式
function getBucketClass(bucket: number): string {
  return `bucket-${bucket}`
}

function getBucketTagType(bucket: number): 'danger' | 'warning' | 'success' | 'info' {
  switch (bucket) {
    case 1: return 'danger'      // 已过期
    case 2: return 'danger'      // 7天内
    case 3: return 'warning'     // 30天内
    case 4: return 'success'     // 90天内
    case 5: return 'info'        // 90天以上
    default: return 'info'
  }
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.expiry-sales-stat {
  width: 100%;
}

/* 卡片样式 - 对齐项目深色科技风规范 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  padding: 16px 20px;
  border: 1px solid var(--border-primary);
  box-shadow: var(--shadow-md);
}

.mb-20 {
  margin-bottom: 20px;
}

.card-header {
  margin-bottom: 16px;
}

.card-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

/* 效期区间分布概览 */
.bucket-summary {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
}

.bucket-item {
  padding: 12px 16px;
  border-radius: var(--radius-md);
  background: var(--bg-elevated);
  border-left: 3px solid var(--border-secondary);
}

.bucket-item.bucket-1 { border-left-color: var(--danger); }
.bucket-item.bucket-2 { border-left-color: var(--danger); }
.bucket-item.bucket-3 { border-left-color: var(--warning); }
.bucket-item.bucket-4 { border-left-color: var(--success); }
.bucket-item.bucket-5 { border-left-color: var(--text-tertiary); }

.bucket-name {
  font-size: 13px;
  color: var(--text-tertiary);
  margin-bottom: 6px;
}

.bucket-quantity {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
}

.bucket-amount {
  font-size: 12px;
  color: var(--text-tertiary);
  margin-top: 2px;
}

.bucket-bar {
  height: 4px;
  background: var(--border-primary);
  border-radius: 2px;
  margin-top: 8px;
  overflow: hidden;
}

.bucket-bar-inner {
  height: 100%;
  background: var(--primary);
  border-radius: 2px;
  transition: width 0.3s ease;
}

.bucket-item.bucket-1 .bucket-bar-inner { background: var(--danger); }
.bucket-item.bucket-2 .bucket-bar-inner { background: var(--danger); }
.bucket-item.bucket-3 .bucket-bar-inner { background: var(--warning); }
.bucket-item.bucket-4 .bucket-bar-inner { background: var(--success); }
.bucket-item.bucket-5 .bucket-bar-inner { background: var(--text-tertiary); }

.bucket-percent {
  font-size: 11px;
  color: var(--text-tertiary);
  margin-top: 4px;
}

/* 表格工具栏 */
.table-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.table-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.percent-text {
  color: var(--primary);
  font-weight: 600;
}

/* 表格 - 对齐项目深色科技风规范 */
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

/* 分页 */
.pagination-container {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
