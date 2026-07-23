<template>
  <div class="check-management">
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
          <el-form-item label="盘点日期">
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
        <el-button type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增盘点
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
        <el-table-column label="账面库存" width="100" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.beforeQuantity) }}
          </template>
        </el-table-column>
        <el-table-column label="实际数量" width="100" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.actualQuantity) }}
          </template>
        </el-table-column>
        <el-table-column label="差异数量" width="110" align="center">
          <template #default="{ row }">
            <span :class="getDiffClass(row.diffQuantity)">
              {{ formatDiff(row.diffQuantity) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="差异金额" width="120" align="right">
          <template #default="{ row }">
            <span :class="getDiffClass(row.diffQuantity)">
              {{ row.diffAmount != null ? formatCurrency(row.diffAmount) : '-' }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="100" />
        <el-table-column label="盘点时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.checkTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip />
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

    <!-- 新增盘点弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      title="新增盘点"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="商品" prop="productId">
          <el-select
            v-model="formData.productId"
            placeholder="请选择商品"
            filterable
            style="width: 100%"
            @change="handleProductChange"
          >
            <el-option
              v-for="item in productOptions"
              :key="item.id"
              :label="`${item.name}（${item.code}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="账面库存">
          <span class="book-stock">{{ formData.beforeQuantity != null ? formatNumber(formData.beforeQuantity) : '--' }}</span>
        </el-form-item>
        <el-form-item label="实际数量" prop="actualQuantity">
          <el-input-number
            v-model="formData.actualQuantity"
            :min="0"
            :precision="2"
            :step="1"
            placeholder="请输入实际盘点数量"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="差异数量">
          <span :class="getDiffClass(formData.diffQuantity)">
            {{ formData.diffQuantity != null ? formatDiff(formData.diffQuantity) : '--' }}
          </span>
          <span v-if="formData.diffQuantity !== null && formData.diffQuantity !== 0" class="diff-label">
            （{{ formData.diffQuantity > 0 ? '盘盈' : '盘亏' }}）
          </span>
        </el-form-item>
        <el-form-item label="差异金额">
          <span :class="getDiffClass(formData.diffQuantity)">
            {{ formData.diffAmount != null ? formatCurrency(formData.diffAmount) : '--' }}
          </span>
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入盘点说明/差异原因" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'
import {
  getInventoryCheckList,
  createInventoryCheck,
  getProductOptionsForCheck
} from '@/api/inventory-check'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { InventoryCheck } from '@/api/inventory-check/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  productName: '',
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<InventoryCheck[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 下拉选项
const productOptions = ref<{
  id: number
  name: string
  code: string
  unit: string
  stock: number
  costPrice: number
}[]>([])

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getInventoryCheckList({
      productName: searchForm.productName || undefined,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
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

// 加载下拉选项
const loadOptions = async () => {
  try {
    productOptions.value = await getProductOptionsForCheck()
  } catch (error) {
    ElMessage.error('加载选项数据失败')
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

// 弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  productId: undefined as number | undefined,
  beforeQuantity: null as number | null,
  unitCost: null as number | null,
  actualQuantity: 0,
  diffQuantity: null as number | null,
  diffAmount: null as number | null,
  remark: ''
})

const formRules: FormRules = {
  productId: [
    { required: true, message: '请选择商品', trigger: 'change' }
  ],
  actualQuantity: [
    { required: true, message: '请输入实际盘点数量', trigger: 'blur' },
    { type: 'number', min: 0, message: '实际数量不能为负数', trigger: 'blur' }
  ]
}

// 商品选择变化时带出账面库存和成本价，并自动计算差异
const handleProductChange = (productId: number) => {
  const product = productOptions.value.find(p => p.id === productId)
  if (product) {
    formData.beforeQuantity = product.stock
    formData.unitCost = product.costPrice
    // 重置实际数量并重新计算差异
    formData.actualQuantity = product.stock
    calculateDiff()
  }
}

// 自动计算差异
const calculateDiff = () => {
  if (formData.beforeQuantity != null && formData.actualQuantity != null) {
    formData.diffQuantity = formData.actualQuantity - formData.beforeQuantity
    if (formData.unitCost != null) {
      formData.diffAmount = formData.diffQuantity * formData.unitCost
    }
  }
}

// 重置表单
const resetFormData = () => {
  formData.productId = undefined
  formData.beforeQuantity = null
  formData.unitCost = null
  formData.actualQuantity = 0
  formData.diffQuantity = null
  formData.diffAmount = null
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  resetFormData()
  dialogVisible.value = true
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        await createInventoryCheck({
          productId: formData.productId!,
          actualQuantity: formData.actualQuantity,
          remark: formData.remark || undefined
        })
        ElMessage.success('盘点成功')
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '盘点失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 获取差异样式类（盘盈绿色，盘亏红色，无差异默认色）
const getDiffClass = (diff: number | null) => {
  if (diff == null || diff === 0) return 'diff-zero'
  return diff > 0 ? 'diff-positive' : 'diff-negative'
}

// 格式化差异数量（带正负号）
const formatDiff = (diff: number) => {
  if (diff > 0) return `+${formatNumber(diff)}`
  return formatNumber(diff)
}

// 格式化金额
const formatCurrency = (amount: number) => {
  const absAmount = Math.abs(amount)
  const formatted = absAmount.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return amount >= 0 ? `¥${formatted}` : `-¥${formatted}`
}

// 格式化数字
const formatNumber = (num: number) => {
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
}

// 格式化日期
const formatDate = (dateStr: string) => {
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

// 监听实际数量变化，自动计算差异
import { watch } from 'vue'
watch(() => formData.actualQuantity, () => {
  calculateDiff()
})

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  await loadOptions()
  loadData()
})
</script>

<style scoped>
.check-management {
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

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 差异样式 */
.diff-positive {
  color: var(--el-color-success);
  font-weight: 600;
}

.diff-negative {
  color: var(--el-color-danger);
  font-weight: 600;
}

.diff-zero {
  color: var(--text-tertiary);
}

/* 账面库存展示 */
.book-stock {
  font-weight: 600;
  color: var(--el-color-primary);
}

/* 差异标签 */
.diff-label {
  margin-left: 8px;
  color: var(--text-tertiary);
  font-size: 14px;
}
</style>
