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
          <el-form-item label="入库来源">
            <el-select v-model="searchForm.sourceType" placeholder="全部来源" clearable style="width: 150px">
              <el-option label="采购入库" :value="1" />
              <el-option label="退货入库" :value="2" />
              <el-option label="盘点入库" :value="3" />
              <el-option label="调拨入库" :value="4" />
            </el-select>
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
          新增入库
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
        <el-table-column label="入库来源" width="110">
          <template #default="{ row }">
            <el-tag v-if="row.sourceType" type="success" size="small" effect="dark">
              {{ inboundSourceTypeMap[row.sourceType as InboundSourceType] || '-' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="supplierName" label="供应商" min-width="160" show-overflow-tooltip />
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
        <el-table-column prop="batchNo" label="批次号" width="120" />
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
        <el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
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

    <!-- 新增入库弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      title="新增入库"
      width="600px"
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
        <el-form-item label="入库来源" prop="sourceType">
          <el-radio-group v-model="formData.sourceType">
            <el-radio :value="1">采购入库</el-radio>
            <el-radio :value="2">退货入库</el-radio>
            <el-radio :value="3">盘点入库</el-radio>
            <el-radio :value="4">调拨入库</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item v-if="formData.sourceType === 1" label="供应商" prop="supplierId">
          <el-select
            v-model="formData.supplierId"
            placeholder="请选择供应商"
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="item in supplierOptions"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="入库数量" prop="quantity">
              <el-input-number
                v-model="formData.quantity"
                :min="0.01"
                :precision="2"
                :step="1"
                placeholder="请输入入库数量"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="单价" prop="unitPrice">
              <el-input-number
                v-model="formData.unitPrice"
                :min="0"
                :precision="2"
                :step="1"
                placeholder="请输入单价"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="批次号" prop="batchNo">
              <el-input v-model="formData.batchNo" placeholder="请输入批次号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="生产日期" prop="productionDate">
              <el-date-picker
                v-model="formData.productionDate"
                type="date"
                placeholder="请选择生产日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="保质期天数" prop="shelfLifeDays">
              <el-input-number
                v-model="formData.shelfLifeDays"
                :min="1"
                :step="1"
                placeholder="天数"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="过期日期" prop="expirationDate">
              <el-date-picker
                v-model="formData.expirationDate"
                type="date"
                placeholder="自动计算或手动选择"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <div class="expiry-hint">提示：录入"生产日期+保质期天数"后自动计算过期日期，或直接选择过期日期</div>
        <el-form-item label="当前库存">
          <span class="current-stock">{{ currentStock }}</span>
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注信息" />
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
import { ref, reactive, onMounted, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus } from '@element-plus/icons-vue'
import {
  getInventoryLogList,
  createInbound,
  getProductOptions,
  getSupplierOptions,
  getProductStock,
  inboundSourceTypeMap
} from '@/api/inventory-ops'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { InventoryLog, InboundSourceType } from '@/api/inventory-ops/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  productName: '',
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

// 下拉选项
const productOptions = ref<{ id: number; name: string; code: string; unit: string }[]>([])
const supplierOptions = ref<{ id: number; name: string }[]>([])

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getInventoryLogList({
      productName: searchForm.productName || undefined,
      type: 1,
      sourceType: searchForm.sourceType,
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
    const [products, suppliers] = await Promise.all([
      getProductOptions(),
      getSupplierOptions()
    ])
    productOptions.value = products
    supplierOptions.value = suppliers
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
  searchForm.sourceType = undefined
  searchForm.dateRange = []
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const currentStock = ref<number | string>('--')

const formData = reactive({
  productId: undefined as number | undefined,
  sourceType: 1 as InboundSourceType,
  supplierId: undefined as number | undefined,
  quantity: 0,
  unitPrice: undefined as number | undefined,
  batchNo: '',
  productionDate: '',
  shelfLifeDays: undefined as number | undefined,
  expirationDate: '',
  remark: ''
})

// 生产日期+保质期天数变化时自动计算过期日期
watch(
  () => [formData.productionDate, formData.shelfLifeDays],
  ([prodDate, shelfLife]) => {
    if (prodDate && shelfLife && shelfLife > 0) {
      const date = new Date(prodDate)
      date.setDate(date.getDate() + shelfLife)
      formData.expirationDate = date.toISOString().split('T')[0]
    }
  }
)

const formRules: FormRules = {
  productId: [
    { required: true, message: '请选择商品', trigger: 'change' }
  ],
  sourceType: [
    { required: true, message: '请选择入库来源', trigger: 'change' }
  ],
  supplierId: [
    {
      validator: (_rule, value, callback) => {
        if (formData.sourceType === 1 && !value) {
          callback(new Error('采购入库时必须选择供应商'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  quantity: [
    { required: true, message: '请输入入库数量', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '入库数量必须大于0', trigger: 'blur' }
  ],
  // 到期日期为可选字段：未填到期日期的批次视为"无效期限制"批次，效期选择界面排末尾展示
  // 仍支持由"生产日期+保质期天数"自动计算填充，但允许留空
  expirationDate: []
}

// 商品选择变化时加载当前库存
const handleProductChange = async (productId: number) => {
  try {
    const stock = await getProductStock(productId)
    currentStock.value = stock
  } catch (error) {
    currentStock.value = '--'
  }
}

// 重置表单
const resetFormData = () => {
  formData.productId = undefined
  formData.sourceType = 1
  formData.supplierId = undefined
  formData.quantity = 0
  formData.unitPrice = undefined
  formData.batchNo = ''
  formData.productionDate = ''
  formData.shelfLifeDays = undefined
  formData.expirationDate = ''
  formData.remark = ''
  currentStock.value = '--'
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
        await createInbound({
          productId: formData.productId!,
          sourceType: formData.sourceType,
          supplierId: formData.sourceType === 1 ? formData.supplierId : undefined,
          quantity: formData.quantity,
          unitPrice: formData.unitPrice || undefined,
          batchNo: formData.batchNo || undefined,
          productionDate: formData.productionDate || undefined,
          shelfLifeDays: formData.shelfLifeDays || undefined,
          expirationDate: formData.expirationDate || undefined,
          remark: formData.remark || undefined
        })
        ElMessage.success('入库成功')
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '入库失败')
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
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
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
