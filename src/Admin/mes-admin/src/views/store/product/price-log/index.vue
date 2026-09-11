<template>
  <div class="price-log-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="商品名称">
            <el-input
              v-model="searchForm.productName"
              placeholder="请输入商品名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="变更时间">
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
        <el-table-column prop="productCode" label="商品编码" width="120" />
        <el-table-column prop="productName" label="商品名称" min-width="160" show-overflow-tooltip />
        <el-table-column label="原价格" width="100" align="right">
          <template #default="{ row }">
            ¥{{ row.oldPrice.toFixed(2) }}
          </template>
        </el-table-column>
        <el-table-column label="新价格" width="100" align="right">
          <template #default="{ row }">
            ¥{{ row.newPrice.toFixed(2) }}
          </template>
        </el-table-column>
        <el-table-column label="变更幅度" width="140" align="right">
          <template #default="{ row }">
            <span :class="getChangeClass(row.oldPrice, row.newPrice)">
              {{ getChangeText(row.oldPrice, row.newPrice) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="变更时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.changeTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="100" />
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
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { getPriceChangeLogs } from '@/api/price-log'
import type { PriceChangeLog } from '@/api/price-log/types'
import { formatDateTimeSeconds as formatDateTime } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  productName: '',
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<PriceChangeLog[]>([])

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
    const res = await getPriceChangeLogs({
      productName: searchForm.productName || undefined,
      startDate: searchForm.dateRange?.[0],
      endDate: searchForm.dateRange?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载价格变更记录失败')
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
  searchForm.productName = ''
  searchForm.dateRange = []
  handleSearch()
}

/**
 * 计算变更幅度文本（涨/跌金额和百分比）
 */
const getChangeText = (oldPrice: number, newPrice: number): string => {
  const diff = newPrice - oldPrice
  const percent = oldPrice > 0 ? ((diff / oldPrice) * 100).toFixed(1) : '0.0'
  const sign = diff > 0 ? '+' : ''
  return `${sign}¥${diff.toFixed(2)} (${sign}${percent}%)`
}

/**
 * 获取变更幅度的样式类名
 * - 上涨：危险色
 * - 下跌：成功色
 * - 不变：默认色
 */
const getChangeClass = (oldPrice: number, newPrice: number): string => {
  if (newPrice > oldPrice) return 'change-up'
  if (newPrice < oldPrice) return 'change-down'
  return 'change-none'
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
.price-log-management {
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

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 变更幅度样式 */
.change-up {
  color: var(--el-color-danger);
  font-weight: 600;
}

.change-down {
  color: var(--el-color-success);
  font-weight: 600;
}

.change-none {
  color: var(--text-tertiary);
}
</style>
