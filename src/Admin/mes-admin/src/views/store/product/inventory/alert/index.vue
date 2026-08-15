<template>
  <div class="inventory-alert">
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
          <el-form-item label="预警类型">
            <el-select v-model="searchForm.alertType" placeholder="全部" clearable style="width: 120px">
              <el-option label="低库存" :value="1" />
              <el-option label="效期" :value="2" />
              <el-option label="积压" :value="3" />
            </el-select>
          </el-form-item>
          <el-form-item label="处理状态">
            <el-select v-model="searchForm.isProcessed" placeholder="未处理" clearable style="width: 120px">
              <el-option label="未处理" :value="false" />
              <el-option label="已处理" :value="true" />
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
        <el-button type="warning" :loading="scanning" @click="handleScan">
          <el-icon><Search /></el-icon>
          立即扫描
        </el-button>
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
        <el-table-column prop="productName" label="商品名称" min-width="160" />
        <el-table-column prop="productCode" label="商品编码" width="120" />
        <el-table-column label="批次" width="140" align="center">
          <template #default="{ row }">
            {{ row.batchNo || '-' }}
          </template>
        </el-table-column>
        <el-table-column label="预警类型" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="alertTypeTagType(row.alertType)" size="small" effect="dark">
              {{ alertTypeText(row.alertType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="当前库存" width="90" align="center">
          <template #default="{ row }">
            <span v-if="row.alertType === 2">-</span>
            <span v-else class="stock-danger">{{ row.currentQuantity }}</span>
          </template>
        </el-table-column>
        <el-table-column label="预警阈值" width="90" align="center">
          <template #default="{ row }">
            {{ alertValueText(row) }}
          </template>
        </el-table-column>
        <el-table-column label="缺口/剩余" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.alertType === 1" type="danger" size="small" effect="dark">
              {{ row.shortageAmount }}
            </el-tag>
            <span v-else-if="row.alertType === 2">{{ row.alertValue }} 天</span>
            <span v-else>-</span>
          </template>
        </el-table-column>
        <el-table-column label="过期日期" width="120" align="center">
          <template #default="{ row }">
            {{ row.expirationDate ? row.expirationDate.split('T')[0] : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="storeName" label="门店名称" width="120" />
        <el-table-column label="处理状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isProcessed ? 'success' : 'warning'" size="small" effect="dark">
              {{ row.isProcessed ? '已处理' : '未处理' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="更新时间" width="160">
          <template #default="{ row }">
            {{ formatDate(row.updatedAt || row.createdAt) }}
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
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import { getInventoryAlerts, scanInventoryAlerts } from '@/api/inventory'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { InventoryAlert } from '@/api/inventory/types'

const systemConfigStore = useSystemConfigStore()

// 预警类型文本映射：1-低库存 2-效期 3-积压
const alertTypeText = (type: number): string => {
  const map: Record<number, string> = { 1: '低库存', 2: '效期', 3: '积压' }
  return map[type] ?? '未知'
}

// 预警类型标签颜色：低库存 danger，效期 warning，积压 info
const alertTypeTagType = (type: number): 'danger' | 'warning' | 'info' => {
  const map: Record<number, 'danger' | 'warning' | 'info'> = { 1: 'danger', 2: 'warning', 3: 'info' }
  return map[type] ?? 'info'
}

// 预警阈值/剩余天数显示：低库存=阈值，效期=剩余天数，积压=阈值
const alertValueText = (row: InventoryAlert): string => {
  if (row.alertType === 2) return `${row.alertValue} 天`
  return String(row.alertValue)
}

// 日期格式化
const formatDate = (dateStr?: string): string => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// 搜索表单：默认只看未处理预警
const searchForm = reactive({
  productName: '',
  alertType: undefined as number | undefined,
  isProcessed: false as boolean | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<InventoryAlert[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: 20,
  total: 0
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getInventoryAlerts({
      productName: searchForm.productName || undefined,
      alertType: searchForm.alertType,
      isProcessed: searchForm.isProcessed,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error('加载预警数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 手动扫描
const scanning = ref(false)
const handleScan = async () => {
  scanning.value = true
  try {
    const result = await scanInventoryAlerts()
    ElMessage.success(`扫描完成：低库存 ${result.lowStockCreated} 条、效期 ${result.expiryCreated} 条、积压 ${result.overstockCreated} 条`)
    pagination.pageIndex = 1
    await loadData()
  } catch (error) {
    ElMessage.error('扫描失败')
  } finally {
    scanning.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置：恢复默认只看未处理
const handleReset = () => {
  searchForm.productName = ''
  searchForm.alertType = undefined
  searchForm.isProcessed = false
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
.inventory-alert {
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
  margin-left: auto;
}

/* 库存危险样式 */
.stock-danger {
  color: var(--el-color-danger);
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
