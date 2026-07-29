<template>
  <div class="outbound-management">
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
          <el-form-item label="日期范围">
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
          新增出库
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
        <el-table-column label="出库数量" width="100" align="center">
          <template #default="{ row }">
            <span class="quantity-negative">{{ formatNumber(row.quantity) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="出库来源" width="100" align="center">
          <template #default="{ row }">
            {{ row.sourceType ? (outboundSourceTypeMap[row.sourceType as OutboundSourceType] || '-') : '-' }}
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
        <el-table-column prop="operatorName" label="操作人" width="100" />
        <el-table-column label="出库时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
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

    <!-- 新增出库弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      title="新增出库"
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
        <el-form-item label="出库来源" prop="sourceType">
          <el-select
            v-model="formData.sourceType"
            placeholder="请选择出库来源"
            style="width: 100%"
          >
            <el-option
              v-for="(label, value) in outboundSourceTypeMap"
              :key="value"
              :label="label"
              :value="Number(value)"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="当前库存">
          <span class="current-stock">{{ currentStock }}</span>
        </el-form-item>
        <el-form-item label="扣减方式">
          <el-radio-group v-model="deductMode">
            <el-radio value="fefo">FEFO 自动</el-radio>
            <el-radio value="manual">手动指定批次</el-radio>
          </el-radio-group>
        </el-form-item>
        <!-- FEFO 自动模式 -->
        <template v-if="deductMode === 'fefo'">
          <el-form-item label="出库数量" prop="quantity">
            <el-input-number
              v-model="formData.quantity"
              :min="0.01"
              :max="typeof currentStock === 'number' ? currentStock : undefined"
              :precision="2"
              :step="1"
              placeholder="请输入出库数量"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item label="出库后库存">
            <span class="after-stock">{{ afterStockPreview }}</span>
          </el-form-item>
        </template>
        <!-- 手动指定模式 -->
        <template v-else>
          <el-form-item label="批次扣减">
            <el-table :data="batchOptions" border size="small" style="width: 100%">
              <el-table-column prop="batchNo" label="批次号" width="120" />
              <el-table-column label="过期日期" width="120">
                <template #default="{ row }">
                  {{ row.expirationDate ? row.expirationDate.split('T')[0] : '无效期' }}
                </template>
              </el-table-column>
              <el-table-column prop="quantity" label="可用数量" width="100" align="center" />
              <el-table-column label="扣减数量" width="150">
                <template #default="{ row }">
                  <el-input-number
                    :model-value="getBatchDeductQty(row.id)"
                    :min="0"
                    :max="row.quantity"
                    :precision="2"
                    :step="1"
                    size="small"
                    style="width: 130px"
                    @update:model-value="(val: number) => setBatchDeductQty(row.id, val)"
                  />
                </template>
              </el-table-column>
            </el-table>
            <div class="batch-summary">
              合计扣减：{{ totalBatchDeduct }} / 可用 {{ currentStock }}
            </div>
          </el-form-item>
        </template>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入出库原因/备注信息" />
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
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'
import {
  getInventoryLogList,
  createOutbound,
  getProductOptions,
  getProductStock,
  getProductBatches,
  outboundSourceTypeMap
} from '@/api/inventory-ops'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { InventoryLog, OutboundSourceType, InventoryBatchOption } from '@/api/inventory-ops/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  productName: '',
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

// 下拉选项
const productOptions = ref<{ id: number; name: string; code: string; unit: string }[]>([])

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getInventoryLogList({
      productName: searchForm.productName || undefined,
      type: 2,
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
    productOptions.value = await getProductOptions()
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
const currentStock = ref<number | string>('--')

// 扣减方式：FEFO 自动 / 手动指定
type DeductMode = 'fefo' | 'manual'
const deductMode = ref<DeductMode>('fefo')

// 出库来源
const formData = reactive({
  productId: undefined as number | undefined,
  sourceType: 6 as OutboundSourceType, // 默认"其他"
  quantity: 0,
  remark: '',
  batchItems: [] as { batchId: number; quantity: number }[]
})

// 在库批次列表（手动模式使用）
const batchOptions = ref<InventoryBatchOption[]>([])

const formRules = computed<FormRules>(() => {
  const rules: FormRules = {
    productId: [
      { required: true, message: '请选择商品', trigger: 'change' }
    ],
    sourceType: [
      { required: true, message: '请选择出库来源', trigger: 'change' }
    ]
  }
  // FEFO 模式才校验出库数量
  if (deductMode.value === 'fefo') {
    rules.quantity = [
      { required: true, message: '请输入出库数量', trigger: 'blur' },
      { type: 'number', min: 0.01, message: '出库数量必须大于0', trigger: 'blur' }
    ]
  }
  return rules
})

// 出库后库存预览
const afterStockPreview = computed(() => {
  if (typeof currentStock.value === 'number' && formData.quantity > 0) {
    const after = currentStock.value - formData.quantity
    if (after < 0) {
      return `库存不足（剩余 ${currentStock.value}）`
    }
    return after
  }
  return '--'
})

// 获取指定批次的扣减数量
const getBatchDeductQty = (batchId: number): number => {
  const item = formData.batchItems.find(i => i.batchId === batchId)
  return item?.quantity || 0
}

// 设置指定批次的扣减数量
const setBatchDeductQty = (batchId: number, qty: number) => {
  const idx = formData.batchItems.findIndex(i => i.batchId === batchId)
  if (qty > 0) {
    if (idx >= 0) {
      formData.batchItems[idx].quantity = qty
    } else {
      formData.batchItems.push({ batchId, quantity: qty })
    }
  } else {
    if (idx >= 0) formData.batchItems.splice(idx, 1)
  }
}

// 手动模式合计扣减数量
const totalBatchDeduct = computed(() => {
  return formData.batchItems.reduce((sum, i) => sum + i.quantity, 0)
})

// 商品选择变化时加载当前库存
const handleProductChange = async (productId: number) => {
  try {
    const stock = await getProductStock(productId)
    currentStock.value = stock
    // 加载在库批次列表（手动模式使用）
    batchOptions.value = await getProductBatches(productId)
    formData.batchItems = []
  } catch (error) {
    currentStock.value = '--'
    batchOptions.value = []
  }
}

// 重置表单
const resetFormData = () => {
  formData.productId = undefined
  formData.sourceType = 6
  formData.quantity = 0
  formData.remark = ''
  formData.batchItems = []
  currentStock.value = '--'
  batchOptions.value = []
  deductMode.value = 'fefo'
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
      // 二次校验库存是否充足
      if (deductMode.value === 'fefo') {
        if (typeof currentStock.value === 'number' && formData.quantity > currentStock.value) {
          ElMessage.error('出库数量不能超过当前库存')
          return
        }
      } else {
        // 手动模式：校验是否选择了批次
        const validItems = formData.batchItems.filter(i => i.quantity > 0)
        if (validItems.length === 0) {
          ElMessage.error('请至少选择一个批次并填写扣减数量')
          return
        }
      }

      submitLoading.value = true
      try {
        if (deductMode.value === 'fefo') {
          await createOutbound({
            productId: formData.productId!,
            sourceType: formData.sourceType,
            quantity: formData.quantity,
            remark: formData.remark || undefined
          })
        } else {
          const validItems = formData.batchItems.filter(i => i.quantity > 0)
          await createOutbound({
            productId: formData.productId!,
            sourceType: formData.sourceType,
            batchItems: validItems,
            remark: formData.remark || undefined
          })
        }
        ElMessage.success('出库成功')
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '出库失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
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
.outbound-management {
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

/* 出库数量负数样式 */
.quantity-negative {
  color: var(--el-color-danger);
  font-weight: 600;
}

/* 当前库存展示 */
.current-stock {
  font-weight: 600;
  color: var(--el-color-primary);
}

/* 出库后库存展示 */
.after-stock {
  font-weight: 600;
  color: var(--el-color-warning);
}

/* 批次扣减汇总 */
.batch-summary {
  margin-top: 8px;
  font-size: 13px;
  color: var(--text-tertiary);
  text-align: right;
}
</style>
