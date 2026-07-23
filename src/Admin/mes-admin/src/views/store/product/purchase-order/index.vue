<template>
  <div class="purchase-order-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="供应商">
            <el-select
              v-model="searchForm.supplierId"
              placeholder="全部供应商"
              clearable
              filterable
              style="width: 200px"
            >
              <el-option
                v-for="item in supplierOptions"
                :key="item.id"
                :label="item.name"
                :value="item.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="采购类型">
            <el-select v-model="searchForm.purchaseType" placeholder="全部类型" clearable style="width: 150px">
              <el-option label="零售商品采购" :value="1" />
              <el-option label="耗材采购" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部状态" clearable style="width: 120px">
              <el-option label="待审核" :value="1" />
              <el-option label="已审核" :value="2" />
              <el-option label="已入库" :value="3" />
              <el-option label="已取消" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="采购日期">
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
      <div class="toolbar-left">
        <span class="toolbar-title">采购记录列表</span>
      </div>
      <div class="toolbar-right">
        <el-button type="info" plain @click="handleViewSummary">
          <el-icon><DataAnalysis /></el-icon>
          供应商采购统计
        </el-button>
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
        <el-table-column prop="orderNo" label="采购单号" width="150" />
        <el-table-column prop="supplierName" label="供应商" min-width="180" show-overflow-tooltip />
        <el-table-column label="采购日期" width="110">
          <template #default="{ row }">
            {{ formatDate(row.orderDate) }}
          </template>
        </el-table-column>
        <el-table-column label="采购类型" width="120">
          <template #default="{ row }">
            <el-tag :type="row.purchaseType === 1 ? 'primary' : 'warning'" size="small" effect="plain">
              {{ row.purchaseType === 1 ? '零售商品' : '耗材' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="采购总金额" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥{{ row.totalAmount.toFixed(2) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="100" />
        <el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleViewDetail(row)">
              <el-icon><View /></el-icon>
              查看详情
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

    <!-- 采购单详情弹窗 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="采购单详情"
      width="750px"
    >
      <div v-if="currentOrder" v-loading="detailLoading">
        <!-- 基本信息 -->
        <el-descriptions :column="2" border class="mb-20">
          <el-descriptions-item label="采购单号">{{ currentOrder.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="供应商">{{ currentOrder.supplierName }}</el-descriptions-item>
          <el-descriptions-item label="采购日期">{{ formatDate(currentOrder.orderDate) }}</el-descriptions-item>
          <el-descriptions-item label="采购类型">
            {{ currentOrder.purchaseType === 1 ? '零售商品采购' : '耗材采购' }}
          </el-descriptions-item>
          <el-descriptions-item label="采购总金额">
            <span class="amount-text">¥{{ currentOrder.totalAmount.toFixed(2) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusTagType(currentOrder.status)" size="small" effect="dark">
              {{ getStatusText(currentOrder.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="操作人">{{ currentOrder.operatorName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="备注">{{ currentOrder.remark || '-' }}</el-descriptions-item>
        </el-descriptions>

        <!-- 明细列表 -->
        <div class="detail-section-title">采购明细</div>
        <el-table :data="currentOrder.items || []" style="width: 100%" size="small">
          <el-table-column prop="productCode" label="商品编码" width="120" />
          <el-table-column prop="productName" label="商品名称" min-width="180" />
          <el-table-column prop="quantity" label="数量" width="80" align="right" />
          <el-table-column label="单价" width="100" align="right">
            <template #default="{ row }">
              ¥{{ row.unitPrice.toFixed(2) }}
            </template>
          </el-table-column>
          <el-table-column label="小计" width="120" align="right">
            <template #default="{ row }">
              <span class="amount-text">¥{{ row.subtotal.toFixed(2) }}</span>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-dialog>

    <!-- 供应商采购统计弹窗 -->
    <el-dialog
      v-model="summaryDialogVisible"
      title="供应商累计采购统计"
      width="600px"
    >
      <el-table :data="supplierSummary" style="width: 100%" v-loading="summaryLoading">
        <el-table-column type="index" label="排名" width="60" />
        <el-table-column prop="supplierName" label="供应商" min-width="180" />
        <el-table-column prop="orderCount" label="采购单数" width="100" align="center" />
        <el-table-column label="累计采购额" width="140" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥{{ row.totalAmount.toFixed(2) }}</span>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, View, DataAnalysis } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import {
  getPurchaseOrders,
  getPurchaseOrder,
  getSupplierPurchaseSummary
} from '@/api/purchase'
import { getSuppliers } from '@/api/supplier'
import type { PurchaseOrder, SupplierPurchaseSummary } from '@/api/purchase/types'
import type { Supplier } from '@/api/supplier/types'

const systemConfigStore = useSystemConfigStore()

// 供应商下拉选项
const supplierOptions = ref<Supplier[]>([])

// 搜索表单
const searchForm = reactive({
  supplierId: undefined as number | undefined,
  purchaseType: undefined as number | undefined,
  status: undefined as number | undefined,
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<PurchaseOrder[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 加载供应商选项
const loadSupplierOptions = async () => {
  try {
    const res = await getSuppliers({ pageSize: 100 })
    supplierOptions.value = res.list
  } catch {
    // 供应商列表加载失败不阻断主流程
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getPurchaseOrders({
      supplierId: searchForm.supplierId,
      purchaseType: searchForm.purchaseType as any,
      status: searchForm.status as any,
      startDate: searchForm.dateRange?.[0],
      endDate: searchForm.dateRange?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载采购记录失败')
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
  searchForm.supplierId = undefined
  searchForm.purchaseType = undefined
  searchForm.status = undefined
  searchForm.dateRange = []
  handleSearch()
}

// 详情弹窗
const detailDialogVisible = ref(false)
const detailLoading = ref(false)
const currentOrder = ref<PurchaseOrder | null>(null)

// 查看详情
const handleViewDetail = async (row: PurchaseOrder) => {
  detailDialogVisible.value = true
  detailLoading.value = true
  currentOrder.value = null
  try {
    currentOrder.value = await getPurchaseOrder(row.id)
  } catch {
    ElMessage.error('加载详情失败')
    detailDialogVisible.value = false
  } finally {
    detailLoading.value = false
  }
}

// 供应商采购统计弹窗
const summaryDialogVisible = ref(false)
const summaryLoading = ref(false)
const supplierSummary = ref<SupplierPurchaseSummary[]>([])

// 查看供应商采购统计
const handleViewSummary = async () => {
  summaryDialogVisible.value = true
  summaryLoading.value = true
  try {
    supplierSummary.value = await getSupplierPurchaseSummary()
  } catch {
    ElMessage.error('加载统计数据失败')
  } finally {
    summaryLoading.value = false
  }
}

// 格式化日期（仅日期部分）
const formatDate = (dateStr: string): string => {
  return dateStr.split('T')[0]
}

// 获取状态文本
const getStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '待审核', 2: '已审核', 3: '已入库', 4: '已取消' }
  return map[status] || '未知'
}

// 获取状态标签类型
const getStatusTagType = (status: number): string => {
  const map: Record<number, string> = { 1: 'warning', 2: 'primary', 3: 'success', 4: 'info' }
  return map[status] || 'info'
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadSupplierOptions()
  loadData()
})
</script>

<style scoped>
.purchase-order-management {
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

.toolbar-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 金额文本 */
.amount-text {
  font-weight: 600;
  color: var(--text-primary);
}

/* 详情区域标题 */
.detail-section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 20px 0 12px;
  padding-left: 8px;
  border-left: 3px solid var(--el-color-primary);
}
</style>
