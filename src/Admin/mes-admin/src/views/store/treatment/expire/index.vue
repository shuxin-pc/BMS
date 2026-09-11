<template>
  <div class="treatment-card-expiry">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称/手机号">
            <el-input
              v-model="searchForm.keyword"
              placeholder="姓名或手机号"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="预警级别">
            <el-select v-model="searchForm.alertLevel" placeholder="全部" clearable style="width: 140px">
              <el-option label="即将到期" :value="1" />
              <el-option label="已到期" :value="2" />
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

    <!-- 统计卡片 -->
    <div class="stats-row mb-20">
      <div class="stat-card stat-warning">
        <div class="stat-icon">
          <el-icon><WarningFilled /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-label">即将到期</div>
          <div class="stat-value">{{ expiringCount }}</div>
        </div>
      </div>
      <div class="stat-card stat-danger">
        <div class="stat-icon">
          <el-icon><CircleCloseFilled /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-label">已到期</div>
          <div class="stat-value">{{ expiredCount }}</div>
        </div>
      </div>
      <div class="stat-card stat-info">
        <div class="stat-icon">
          <el-icon><DataAnalysis /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-label">总计</div>
          <div class="stat-value">{{ totalCount }}</div>
        </div>
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
        <el-table-column prop="customerName" label="客户名称" width="120" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="cardName" label="卡名称" min-width="180" show-overflow-tooltip />
        <el-table-column label="购买时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.purchaseDate) }}</template>
        </el-table-column>
        <el-table-column label="到期日期" width="170">
          <template #default="{ row }">{{ formatDateTime(row.expiryDate) }}</template>
        </el-table-column>
        <el-table-column label="剩余天数" width="110" align="center">
          <template #default="{ row }">
            <span :class="getDaysClass(row.remainingDays)">
              {{ row.remainingDays > 0 ? `${row.remainingDays}天` : `已过期${Math.abs(row.remainingDays)}天` }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="剩余次数" width="80" align="center">
          <template #default="{ row }">
            <span :class="{ 'count-warn': row.remainingTimes <= 2 }">{{ row.remainingTimes }}</span>
          </template>
        </el-table-column>
        <el-table-column label="预警级别" width="100">
          <template #default="{ row }">
            <el-tag :type="row.alertLevel === 1 ? 'warning' : 'danger'" size="small" effect="dark">
              {{ row.alertLevel === 1 ? '即将到期' : '已到期' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)" size="small">
              {{ getStatusText(row.status) }}
            </el-tag>
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
import {
  Search,
  Refresh,
  WarningFilled,
  CircleCloseFilled,
  DataAnalysis
} from '@element-plus/icons-vue'
import { getTreatmentCardExpiries } from '@/api/treatment-card'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { TreatmentCardExpiry } from '@/api/treatment-card/types'
import { formatDateTime } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  keyword: '',
  alertLevel: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<TreatmentCardExpiry[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 统计数据（全量级别统计，来自后端，不受分页与预警级别筛选影响）
const expiringCount = ref(0)
const expiredCount = ref(0)
const totalCount = computed(() => expiringCount.value + expiredCount.value)

// 状态文本
const getStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '有效', 2: '已用完', 3: '已退款' }
  return map[status] || '未知'
}

// 状态标签类型
const getStatusType = (status: number): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<number, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    1: 'success',
    2: 'info',
    3: 'danger'
  }
  return map[status] || 'info'
}

// 剩余天数样式
const getDaysClass = (days: number): string => {
  if (days < 0) return 'days-expired'
  if (days <= 7) return 'days-urgent'
  return 'days-normal'
}


// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTreatmentCardExpiries({
      keyword: searchForm.keyword || undefined,
      alertLevel: searchForm.alertLevel,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
    expiringCount.value = res.expiringCount
    expiredCount.value = res.expiredCount
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
  searchForm.alertLevel = undefined
  handleSearch()
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
.treatment-card-expiry {
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

/* 统计卡片 */
.stats-row {
  display: flex;
  gap: 16px;
}

.stat-card {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px 24px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.stat-warning .stat-icon {
  background: rgba(230, 162, 60, 0.15);
  color: #e6a23c;
}

.stat-danger .stat-icon {
  background: rgba(245, 108, 108, 0.15);
  color: #f56c6c;
}

.stat-info .stat-icon {
  background: rgba(64, 158, 255, 0.15);
  color: var(--primary);
}

.stat-content {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.stat-label {
  font-size: 13px;
  color: var(--text-tertiary);
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: var(--text-primary);
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

/* 天数样式 */
.days-expired {
  color: #f56c6c;
  font-weight: 600;
}

.days-urgent {
  color: #e6a23c;
  font-weight: 600;
}

.days-normal {
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
