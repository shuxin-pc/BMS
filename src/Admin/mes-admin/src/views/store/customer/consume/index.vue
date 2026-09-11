<template>
  <div class="consume-management">
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
          <el-form-item label="消费时间">
            <el-date-picker
              v-model="searchForm.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 260px"
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
        <el-table-column prop="customerName" label="客户名称" min-width="110" />
        <el-table-column prop="phone" label="手机号" min-width="130" />
        <el-table-column prop="orderNo" label="订单号" min-width="200" />
        <el-table-column label="消费金额" min-width="100" align="right">
          <template #default="{ row }">
            <span class="price-text">¥{{ formatPrice(row.amount) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="projectName" label="消费项目" min-width="220" show-overflow-tooltip />
        <el-table-column label="订单状态" min-width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="statusTagType(row.status)" size="small" effect="dark">
              {{ statusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="支付方式" min-width="110" align="center">
          <template #default="{ row }">
            <el-tag :type="paymentMethodTagType(row.paymentMethod)" size="small" effect="plain">
              {{ paymentMethodText(row.paymentMethod) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="消费时间" min-width="160">
          <template #default="{ row }">
            {{ formatDateTime(row.consumeTime) }}
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
import { getConsumeRecords } from '@/api/customer'
import { formatDateTime } from '@/utils/date'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { ConsumeRecord } from '@/api/customer/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  keyword: '',
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<ConsumeRecord[]>([])

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
    const res = await getConsumeRecords({
      keyword: searchForm.keyword || undefined,
      startDate: searchForm.dateRange?.[0],
      endDate: searchForm.dateRange?.[1],
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
  searchForm.dateRange = []
  handleSearch()
}

// ==================== 工具方法 ====================

/** 格式化价格 */
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}

/** 支付方式文本 */
const paymentMethodText = (method: number) => {
  const map: Record<number, string> = { 1: '现金', 2: '支付宝', 3: '微信', 4: '银行卡', 5: '储值卡', 6: '积分抵扣', 7: '组合支付' }
  return map[method] || '未知'
}

/** 支付方式标签类型 */
const paymentMethodTagType = (method: number) => {
  const map: Record<number, string> = { 1: '', 2: 'success', 3: 'warning', 4: 'info', 5: 'danger', 6: 'info', 7: 'danger' }
  return map[method] || ''
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
.consume-management {
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
.price-text {
  color: var(--primary);
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
