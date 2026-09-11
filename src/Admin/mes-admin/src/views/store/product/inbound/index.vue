<template>
  <div class="inbound-management">
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
          <el-form-item label="批次号">
            <el-input
              v-model="searchForm.batchNo"
              placeholder="请输入批次号"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="入库来源">
            <el-select v-model="searchForm.sourceType" placeholder="全部来源" clearable style="width: 150px">
              <el-option label="采购入库" :value="1" />
              <el-option label="退货入库" :value="2" />
              <el-option label="盘点入库" :value="3" />
              <el-option label="调拨入库" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="入库日期">
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
        <el-table-column prop="batchNo" label="批次号" width="120" />
        <el-table-column prop="productName" label="商品名称" min-width="160" />
        <el-table-column prop="productCode" label="商品编码" width="120" />
        <el-table-column label="入库来源" width="110">
          <template #default="{ row }">
            <el-tag v-if="row.sourceType" type="success" size="small" effect="dark">
              {{ inboundSourceTypeMap[row.sourceType as InboundSourceType] || '-' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="单价" width="100" align="right">
          <template #default="{ row }">
            {{ row.unitPrice != null ? `¥${formatNumber(row.unitPrice)}` : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="入库数量" width="100" align="center">
          <template #default="{ row }">
            <span class="quantity-positive">+{{ formatNumber(row.quantity) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作前库存" width="100" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.beforeQuantity) }}
          </template>
        </el-table-column>
        <el-table-column label="操作后库存" width="100" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.afterQuantity) }}
          </template>
        </el-table-column>
        <el-table-column label="过期日期" width="120">
          <template #default="{ row }">
            {{ row.expirationDate ? formatDate(row.expirationDate) : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="100" />
        <el-table-column label="入库时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleDetail(row)">详情</el-button>
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

    <!-- 入库详情弹窗 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="入库详情"
      width="640px"
    >
      <el-descriptions v-if="detailData" :column="2" border>
        <el-descriptions-item label="商品名称">{{ detailData.productName }}</el-descriptions-item>
        <el-descriptions-item label="商品编码">{{ detailData.productCode || '-' }}</el-descriptions-item>
        <el-descriptions-item label="入库来源">
          {{ detailData.sourceType ? (inboundSourceTypeMap[detailData.sourceType as InboundSourceType] || '-') : '-' }}
        </el-descriptions-item>
        <el-descriptions-item label="供应商">{{ detailData.supplierName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="单价">{{ detailData.unitPrice != null ? `¥${formatNumber(detailData.unitPrice)}` : '-' }}</el-descriptions-item>
        <el-descriptions-item label="入库数量">
          <span class="quantity-positive">+{{ formatNumber(detailData.quantity) }}</span>
        </el-descriptions-item>
        <el-descriptions-item label="操作前库存">{{ formatNumber(detailData.beforeQuantity) }}</el-descriptions-item>
        <el-descriptions-item label="操作后库存">{{ formatNumber(detailData.afterQuantity) }}</el-descriptions-item>
        <el-descriptions-item label="批次号">{{ detailData.batchNo || '-' }}</el-descriptions-item>
        <el-descriptions-item label="过期日期">{{ detailData.expirationDate ? formatDate(detailData.expirationDate) : '-' }}</el-descriptions-item>
        <el-descriptions-item label="操作人">{{ detailData.operatorName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="入库时间">{{ formatDate(detailData.createdAt) }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import {
  getInventoryLogList,
  inboundSourceTypeMap
} from '@/api/inventory-ops'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { InventoryLog, InboundSourceType } from '@/api/inventory-ops/types'
import { formatDate } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  productName: '',
  batchNo: '',
  sourceType: undefined as InboundSourceType | undefined,
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<InventoryLog[]>([])

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
    const res = await getInventoryLogList({
      productName: searchForm.productName || undefined,
      batchNo: searchForm.batchNo || undefined,
      type: 1,
      sourceType: searchForm.sourceType,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
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
  searchForm.productName = ''
  searchForm.batchNo = ''
  searchForm.sourceType = undefined
  searchForm.dateRange = []
  handleSearch()
}

// 详情弹窗
const detailDialogVisible = ref(false)
const detailData = ref<InventoryLog | null>(null)

// 查看详情
const handleDetail = (row: InventoryLog) => {
  detailData.value = row
  detailDialogVisible.value = true
}

// 格式化数字
const formatNumber = (num: number) => {
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
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
.inbound-management {
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

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 入库数量正数样式 */
.quantity-positive {
  color: var(--el-color-success);
  font-weight: 600;
}

/* 当前库存展示 */
.current-stock {
  font-weight: 600;
  color: var(--el-color-primary);
}

/* 效期录入提示 */
.expiry-hint {
  font-size: 12px;
  color: var(--text-tertiary, #909399);
  margin-bottom: 12px;
  padding-left: 4px;
}
</style>
