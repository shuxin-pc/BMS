<template>
  <div class="sample-inventory">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="类型">
            <el-select v-model="searchForm.type" placeholder="全部" clearable style="width: 120px">
              <el-option label="样品" :value="4" />
              <el-option label="赠品" :value="5" />
            </el-select>
          </el-form-item>
          <el-form-item label="库存状态">
            <el-select v-model="searchForm.inventoryStatus" placeholder="全部" clearable style="width: 120px">
              <el-option label="充足" :value="1" />
              <el-option label="偏低" :value="2" />
              <el-option label="不足" :value="3" />
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
        <span class="toolbar-tip">共 {{ pagination.total }} 条库存记录</span>
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
        <el-table-column label="名称/编码" min-width="200">
          <template #default="{ row }">
            <div class="sample-info">
              <div class="sample-detail">
                <div class="sample-name">{{ row.name }}</div>
                <div class="sample-code">{{ row.code }}</div>
              </div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="类型" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.type === 4 ? 'primary' : 'success'" size="small" effect="dark">
              {{ row.type === 4 ? '样品' : '赠品' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="unit" label="单位" width="80" />
        <el-table-column label="当前库存" width="110" align="center">
          <template #default="{ row }">
            <span :class="{ 'stock-warn': row.inventoryStatus === 3, 'stock-low': row.inventoryStatus === 2 }">
              {{ row.currentStock }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="alertQuantity" label="预警阈值" width="100" align="center" />
        <el-table-column label="库存状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.inventoryStatus)" size="small" effect="dark">
              {{ statusText(row.inventoryStatus) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="lastInboundTime" label="上次入库时间" width="170" />
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
import { getSampleInventories } from '@/api/sample'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { SampleInventory, SampleType } from '@/api/sample/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  type: undefined as SampleType | undefined,
  inventoryStatus: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<SampleInventory[]>([])

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
    const res = await getSampleInventories({
      name: searchForm.name || undefined,
      type: searchForm.type,
      inventoryStatus: searchForm.inventoryStatus,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
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
  searchForm.name = ''
  searchForm.type = undefined
  searchForm.inventoryStatus = undefined
  handleSearch()
}

// 库存状态文本
const statusText = (status: number): string => {
  const map: Record<number, string> = { 1: '充足', 2: '偏低', 3: '不足' }
  return map[status] || '未知'
}

// 库存状态标签类型
const statusTagType = (status: number): 'success' | 'warning' | 'danger' => {
  const map: Record<number, 'success' | 'warning' | 'danger'> = {
    1: 'success',
    2: 'warning',
    3: 'danger'
  }
  return map[status] || 'success'
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
.sample-inventory {
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

.toolbar-tip {
  color: var(--text-tertiary);
  font-size: 14px;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 样品信息 */
.sample-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.sample-detail {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.sample-name {
  font-weight: 500;
  color: var(--text-primary);
}

.sample-code {
  font-size: 12px;
  color: var(--text-tertiary);
}

.stock-warn {
  color: #f56c6c;
  font-weight: 600;
}

.stock-low {
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
