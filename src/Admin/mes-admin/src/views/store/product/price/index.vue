<template>
  <div class="price-management">
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
          <el-form-item label="商品编码">
            <el-input
              v-model="searchForm.productCode"
              placeholder="请输入商品编码"
              clearable
              style="width: 150px"
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
        <el-button type="primary" disabled>
          <el-icon><Download /></el-icon>
          导出
        </el-button>
        <el-button
          type="warning"
          :disabled="selectedRows.length === 0"
          @click="handleBatchAdjust"
        >
          <el-icon><Edit /></el-icon>
          批量调价
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
        @selection-change="handleSelectionChange"
        style="width: 100%"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column prop="productName" label="商品名称" min-width="160" />
        <el-table-column prop="productCode" label="商品编码" width="110" />
        <el-table-column prop="categoryName" label="分类" width="110" />
        <el-table-column label="原价" width="90" align="right">
          <template #default="{ row }">
            <span class="original-price">¥{{ formatPrice(row.originalPrice) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="现价" width="90" align="right">
          <template #default="{ row }">
            <span class="current-price">¥{{ formatPrice(row.currentPrice) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="成本价" width="90" align="right">
          <template #default="{ row }">
            <span class="cost-price">¥{{ formatPrice(row.costPrice) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="利润率" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="getProfitTagType(row.profitMargin)" size="small" effect="plain">
              {{ row.profitMargin }}%
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="最后调价时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.lastAdjustedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleAdjust(row)">
              <el-icon><Edit /></el-icon>
              调价
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

    <!-- 调价弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      title="调整价格"
      width="450px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="商品名称">
          <span>{{ currentProductName }}</span>
        </el-form-item>
        <el-form-item label="当前价格">
          <span class="current-price">¥{{ formatPrice(formData.oldPrice) }}</span>
        </el-form-item>
        <el-form-item label="新价格" prop="newPrice">
          <el-input-number
            v-model="formData.newPrice"
            :min="0"
            :precision="2"
            :step="1"
            controls-position="right"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="调价备注" prop="remark">
          <el-input
            v-model="formData.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入调价原因或备注"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定调价
        </el-button>
      </template>
    </el-dialog>

    <!-- 批量调价弹窗 -->
    <el-dialog
      v-model="batchDialogVisible"
      title="批量调价"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="batchFormRef"
        :model="batchForm"
        :rules="batchFormRules"
        label-width="100px"
      >
        <el-form-item label="调价范围">
          <span>已选 {{ selectedRows.length }} 个商品</span>
        </el-form-item>
        <el-form-item label="调价方式" prop="adjustType">
          <el-radio-group v-model="batchForm.adjustType">
            <el-radio :label="1">按百分比</el-radio>
            <el-radio :label="2">固定金额增减</el-radio>
            <el-radio :label="3">设置为新值</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="调价值" prop="adjustValue">
          <el-input-number
            v-model="batchForm.adjustValue"
            :precision="2"
            :step="1"
            controls-position="right"
            style="width: 100%"
          />
          <div class="form-tip">
            <span v-if="batchForm.adjustType === 1">正数表示涨价百分比，负数表示降价百分比（如 10 表示 +10%）</span>
            <span v-else-if="batchForm.adjustType === 2">正数表示加价金额，负数表示减价金额（如 5 表示 +5 元）</span>
            <span v-else-if="batchForm.adjustType === 3">所有选中商品的价格将设置为此值</span>
          </div>
        </el-form-item>
        <el-form-item label="调价备注" prop="remark">
          <el-input
            v-model="batchForm.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入调价原因或备注"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="batchDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="batchSubmitLoading" @click="handleBatchSubmit">
          确定调价
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Download, Edit } from '@element-plus/icons-vue'
import { getPriceList, adjustPrice, batchAdjustPrice } from '@/api/price'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { PriceInfo } from '@/api/price/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  productName: '',
  productCode: ''
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<PriceInfo[]>([])
const selectedRows = ref<PriceInfo[]>([])

// 批量调价
const batchDialogVisible = ref(false)
const batchSubmitLoading = ref(false)
const batchFormRef = ref<FormInstance>()
const batchForm = reactive({
  adjustType: 1 as 1 | 2 | 3,
  adjustValue: 0,
  remark: ''
})
const batchFormRules: FormRules = {
  adjustType: [
    { required: true, message: '请选择调价方式', trigger: 'change' }
  ],
  adjustValue: [
    { required: true, message: '调价值不能为空', trigger: 'blur' },
    {
      validator: (_rule: any, value: number, callback: any) => {
        if (batchForm.adjustType === 3 && value < 0) {
          callback(new Error('设置为新值时，价格不能为负数'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

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
    const res = await getPriceList({
      // TODO: PriceQuery 不支持 productName/productCode 查询，只支持 productId
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
  searchForm.productName = ''
  searchForm.productCode = ''
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const currentProductName = ref('')

const formData = reactive({
  id: 0,
  oldPrice: 0,
  newPrice: 0,
  remark: ''
})

const formRules: FormRules = {
  newPrice: [
    { required: true, message: '新价格不能为空', trigger: 'blur' },
    { type: 'number', min: 0, message: '价格必须大于等于0', trigger: 'blur' }
  ]
}

// 调价
const handleAdjust = (row: PriceInfo) => {
  // TODO: PriceInfo 无 productName 字段，需关联商品信息显示名称
  currentProductName.value = `商品ID: ${row.productId}`
  formData.id = row.productId
  formData.oldPrice = row.newPrice
  formData.newPrice = row.newPrice
  formData.remark = ''
  dialogVisible.value = true
}

// 提交调价
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        await adjustPrice({
          productId: formData.id,
          oldPrice: formData.oldPrice,
          newPrice: formData.newPrice,
          remark: formData.remark || undefined
        })
        ElMessage.success('调价成功')
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '调价失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 选择行
const handleSelectionChange = (rows: PriceInfo[]) => {
  selectedRows.value = rows
}

// 打开批量调价弹窗
const handleBatchAdjust = () => {
  if (selectedRows.value.length === 0) return
  batchForm.adjustType = 1
  batchForm.adjustValue = 0
  batchForm.remark = ''
  batchDialogVisible.value = true
}

// 提交批量调价
const handleBatchSubmit = async () => {
  if (!batchFormRef.value) return
  await batchFormRef.value.validate(async (valid) => {
    if (!valid) return
    batchSubmitLoading.value = true
    try {
      const result = await batchAdjustPrice({
        rangeType: 1,
        productIds: selectedRows.value.map(row => row.productId),
        adjustType: batchForm.adjustType,
        adjustValue: batchForm.adjustValue,
        remark: batchForm.remark || undefined
      })
      const message = `成功调价 ${result.successCount} 个，跳过 ${result.skippedCount} 个，失败 ${result.failedCount} 个`
      if (result.failedCount > 0) {
        ElMessage.warning(message)
      } else {
        ElMessage.success(message)
      }
      batchDialogVisible.value = false
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '批量调价失败')
    } finally {
      batchSubmitLoading.value = false
    }
  })
}

// 格式化价格
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
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

// 利润率标签类型
const getProfitTagType = (margin: number): '' | 'success' | 'warning' | 'danger' => {
  if (margin >= 45) return 'success'
  if (margin >= 30) return ''
  if (margin >= 15) return 'warning'
  return 'danger'
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
.price-management {
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
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

/* 价格样式 */
.original-price {
  color: var(--text-tertiary);
  text-decoration: line-through;
}

.current-price {
  color: var(--primary);
  font-weight: 600;
}

.cost-price {
  color: var(--text-tertiary);
}

/* 表单提示文字 */
.form-tip {
  margin-top: 4px;
  font-size: 12px;
  color: var(--text-tertiary);
  line-height: 1.4;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
