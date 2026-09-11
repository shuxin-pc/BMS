<template>
  <div class="treatment-card-verify">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称/手机号">
            <el-input
              v-model="searchForm.keyword"
              placeholder="姓名或手机号"
              clearable
              style="width: 170px"
            />
          </el-form-item>
          <el-form-item label="核销时间">
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
        <el-button circle @click="loadVerifyRecords">
          <el-icon><Refresh /></el-icon>
        </el-button>
      </div>
    </div>

    <!-- 核销记录列表 -->
    <div class="card">
      <el-table
        v-loading="recordLoading"
        :data="verifyRecords"
        style="width: 100%"
      >
        <el-table-column prop="customerName" label="客户名称" width="160" show-overflow-tooltip />
        <el-table-column prop="phone" label="手机号" width="160" />
        <el-table-column prop="cardName" label="卡名称" min-width="130" show-overflow-tooltip />
        <el-table-column label="核销金额" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥{{ formatPrice(row.verifyAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="verifyTimes" label="核销次数" width="120" align="center" />
        <el-table-column label="剩余次数" width="120" align="center">
          <template #default="{ row }">
            <span :class="{ 'count-warn': row.remainingCount <= 2 }">{{ row.remainingCount ?? '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="核销时间" width="180">
          <template #default="{ row }">{{ formatDateTime(row.verifyTime) }}</template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="140" show-overflow-tooltip />
        <el-table-column label="操作" width="140" align="center" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" @click="openDetail(row)">详情</el-button>
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
          @size-change="loadVerifyRecords"
          @current-change="loadVerifyRecords"
        />
      </div>
    </div>

    <!-- 核销详情弹窗 -->
    <el-dialog v-model="detailVisible" title="核销详情" width="680px" :close-on-click-modal="false">
      <div v-loading="detailLoading">
        <template v-if="detailData">
          <el-descriptions :column="2" border>
            <el-descriptions-item label="客户名称">{{ detailData.customerName || '-' }}</el-descriptions-item>
            <el-descriptions-item label="手机号">{{ detailData.phone || '-' }}</el-descriptions-item>
            <el-descriptions-item label="卡名称">{{ detailData.cardName || '-' }}</el-descriptions-item>
            <el-descriptions-item label="核销金额">¥{{ formatPrice(detailData.verifyAmount) }}</el-descriptions-item>
            <el-descriptions-item label="核销次数">{{ detailData.verifyTimes }}</el-descriptions-item>
            <el-descriptions-item label="剩余次数">{{ detailData.remainingCount ?? '-' }}</el-descriptions-item>
            <el-descriptions-item label="核销时间">{{ formatDateTime(detailData.verifyTime) }}</el-descriptions-item>
            <el-descriptions-item label="操作人">{{ detailData.operatorName || '-' }}</el-descriptions-item>
          </el-descriptions>

          <div class="detail-items-title">核销项目明细</div>
          <el-table :data="detailData.items || []" border style="width: 100%">
            <el-table-column prop="productName" label="核销项目" min-width="140" show-overflow-tooltip />
            <el-table-column prop="verifyTimes" label="次数" width="60" align="center" />
            <el-table-column label="金额" width="90" align="right">
              <template #default="{ row }">¥{{ formatPrice(row.subAmount) }}</template>
            </el-table-column>
            <el-table-column label="服务时间" width="140">
              <template #default="{ row }">{{ formatDateTime(row.serviceStartTime) }}</template>
            </el-table-column>
            <el-table-column label="备注" min-width="120" show-overflow-tooltip>
              <template #default="{ row }">{{ row.remark || '-' }}</template>
            </el-table-column>
          </el-table>
        </template>
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, Search } from '@element-plus/icons-vue'
import { getTreatmentCardVerifies, getTreatmentCardVerify } from '@/api/treatment-card'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { TreatmentCardVerify } from '@/api/treatment-card/types'

const systemConfigStore = useSystemConfigStore()

// 筛选条件
const searchForm = reactive({
  keyword: '',
  dateRange: [] as string[]
})

// 核销记录
const recordLoading = ref(false)
const verifyRecords = ref<TreatmentCardVerify[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 核销详情
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<TreatmentCardVerify | null>(null)

/**
 * 金额格式化（保留两位小数）
 * @param price 金额
 * @returns 两位小数字符串
 */
const formatPrice = (price: number | undefined) => (price ?? 0).toFixed(2)

/**
 * 后端 DateTime ISO 字符串格式化为 yyyy-MM-dd HH:mm
 * 例："2026-08-22T14:30:00" -> "2026-08-22 14:30"
 * @param dateTime DateTime ISO 字符串
 * @returns yyyy-MM-dd HH:mm 或 "-"
 */
const formatDateTime = (dateTime?: string) => {
  if (!dateTime) return '-'
  return dateTime.slice(0, 16).replace('T', ' ')
}

// 加载核销记录
const loadVerifyRecords = async () => {
  recordLoading.value = true
  try {
    const res = await getTreatmentCardVerifies({
      keyword: searchForm.keyword || undefined,
      startDate: searchForm.dateRange?.[0],
      endDate: searchForm.dateRange?.[1],
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    verifyRecords.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载核销记录失败')
  } finally {
    recordLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadVerifyRecords()
}

// 重置
const handleReset = () => {
  searchForm.keyword = ''
  searchForm.dateRange = []
  handleSearch()
}

// 打开核销详情
// id 为雪花ID（运行时字符串），直接传给详情接口拼 URL，禁止 Number() 转换（丢精度）
const openDetail = async (row: TreatmentCardVerify) => {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = row // 先以行数据兜底展示，接口返回后刷新（含核销项目明细）
  try {
    detailData.value = await getTreatmentCardVerify(row.id)
  } catch {
    ElMessage.error('加载核销详情失败')
  } finally {
    detailLoading.value = false
  }
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadVerifyRecords()
})
</script>

<style scoped>
.treatment-card-verify {
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

/* 搜索区域 */
.search-form {
  padding: 20px 24px 0;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
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

.amount-text {
  font-weight: 600;
  color: var(--text-primary);
}

.count-warn {
  color: #e6a23c;
  font-weight: 600;
}

/* 详情弹窗 */
.detail-items-title {
  margin: 16px 0 8px;
  font-size: 14px;
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
</style>
