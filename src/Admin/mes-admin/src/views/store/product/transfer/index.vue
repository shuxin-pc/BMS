<template>
  <div class="transfer-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="调拨单号">
            <el-input
              v-model="searchForm.transferNo"
              placeholder="请输入调拨单号"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部状态" clearable style="width: 130px">
              <el-option label="待调出" :value="1" />
              <el-option label="已调出" :value="2" />
              <el-option label="已调入" :value="3" />
              <el-option label="已取消" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="调出门店">
            <el-select v-model="searchForm.fromStoreId" placeholder="全部门店" clearable style="width: 150px">
              <el-option v-for="store in storeOptions" :key="store.id" :label="store.name" :value="store.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="调入门店">
            <el-select v-model="searchForm.toStoreId" placeholder="全部门店" clearable style="width: 150px">
              <el-option v-for="store in storeOptions" :key="store.id" :label="store.name" :value="store.id" />
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
        <el-button type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增调拨
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
        <el-table-column prop="transferNo" label="调拨单号" width="160" />
        <el-table-column prop="fromStoreName" label="调出门店" width="120" />
        <el-table-column prop="toStoreName" label="调入门店" width="120" />
        <el-table-column label="商品明细" width="80" align="center">
          <template #default="{ row }">
            {{ row.items.length }} 项
          </template>
        </el-table-column>
        <el-table-column label="总数量" width="100" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.items.reduce((sum: number, item: StockTransferItem) => sum + item.quantity, 0)) }}
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="transferStatusTagType[row.status as TransferStatus]" size="small" effect="dark">
              {{ transferStatusMap[row.status as TransferStatus] }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="operatorName" label="操作人" width="100" />
        <el-table-column label="调拨日期" width="120">
          <template #default="{ row }">
            {{ formatDate(row.transferDate) }}
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleViewDetail(row)">
              <el-icon><View /></el-icon>
              详情
            </el-button>
            <el-button
              v-if="row.status === 1"
              link type="warning" size="small"
              @click="handleUpdateStatus(row, 2)"
            >
              确认调出
            </el-button>
            <el-button
              v-if="row.status === 2"
              link type="success" size="small"
              @click="handleUpdateStatus(row, 3)"
            >
              确认调入
            </el-button>
            <el-button
              v-if="row.status === 1"
              link type="danger" size="small"
              @click="handleUpdateStatus(row, 4)"
            >
              取消
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

    <!-- 新增调拨弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      title="新增调拨"
      width="800px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="调出门店" prop="fromStoreId">
              <el-select v-model="formData.fromStoreId" placeholder="请选择调出门店" filterable style="width: 100%">
                <el-option v-for="store in storeOptions" :key="store.id" :label="store.name" :value="store.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="调入门店" prop="toStoreId">
              <el-select v-model="formData.toStoreId" placeholder="请选择调入门店" filterable style="width: 100%">
                <el-option v-for="store in storeOptions" :key="store.id" :label="store.name" :value="store.id" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="调拨日期" prop="transferDate">
              <el-date-picker
                v-model="formData.transferDate"
                type="date"
                placeholder="请选择调拨日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="2" placeholder="请输入调拨备注" />
        </el-form-item>

        <!-- 商品明细动态表格 -->
        <el-form-item label="商品明细" prop="items">
          <div class="items-container">
            <el-button type="primary" plain size="small" @click="handleAddItem">
              <el-icon><Plus /></el-icon>
              添加商品
            </el-button>
            <el-table :data="formData.items" border style="width: 100%; margin-top: 10px">
              <el-table-column label="序号" type="index" width="60" align="center" />
              <el-table-column label="商品" min-width="200">
                <template #default="{ row }">
                  <el-select
                    v-model="row.productId"
                    placeholder="请选择商品"
                    filterable
                    size="small"
                    style="width: 100%"
                    @change="(val: number) => handleItemProductChange(row, val)"
                  >
                    <el-option
                      v-for="item in productOptions"
                      :key="item.id"
                      :label="`${item.name}（${item.code}）`"
                      :value="item.id"
                    />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column label="数量" width="140">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.quantity"
                    :min="0.01"
                    :precision="2"
                    :step="1"
                    size="small"
                    style="width: 120px"
                  />
                </template>
              </el-table-column>
              <el-table-column label="批次号" width="140">
                <template #default="{ row }">
                  <el-input v-model="row.batchNo" placeholder="批次号" size="small" />
                </template>
              </el-table-column>
              <el-table-column label="操作" width="80" align="center">
                <template #default="{ $index }">
                  <el-button link type="danger" size="small" @click="handleRemoveItem($index)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
            <div v-if="formData.items.length === 0" class="empty-tip">
              请添加至少一条调拨明细
            </div>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 调拨详情弹窗 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="调拨单详情"
      width="700px"
    >
      <el-descriptions :column="2" border>
        <el-descriptions-item label="调拨单号">{{ detailData.transferNo }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="transferStatusTagType[detailData.status]" size="small" effect="dark">
            {{ transferStatusMap[detailData.status] }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="调出门店">{{ detailData.fromStoreName }}</el-descriptions-item>
        <el-descriptions-item label="调入门店">{{ detailData.toStoreName }}</el-descriptions-item>
        <el-descriptions-item label="调拨日期">{{ formatDate(detailData.transferDate) }}</el-descriptions-item>
        <el-descriptions-item label="操作人">{{ detailData.operatorName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="备注" :span="2">{{ detailData.remark || '-' }}</el-descriptions-item>
      </el-descriptions>

      <div class="detail-items-title">调拨明细</div>
      <el-table :data="detailData.items" border style="width: 100%">
        <el-table-column label="序号" type="index" width="60" align="center" />
        <el-table-column prop="productName" label="商品名称" min-width="160" />
        <el-table-column prop="productCode" label="商品编码" width="120" />
        <el-table-column label="数量" width="100" align="center">
          <template #default="{ row }">
            {{ formatNumber(row.quantity) }} {{ row.unit || '' }}
          </template>
        </el-table-column>
        <el-table-column prop="batchNo" label="批次号" width="120" />
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
      </el-table>

      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, View } from '@element-plus/icons-vue'
import {
  getStockTransferList,
  getStockTransferDetail,
  createStockTransfer,
  updateTransferStatus,
  getStoreOptions,
  getProductOptions,
  transferStatusMap,
  transferStatusTagType
} from '@/api/stock-transfer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { StockTransfer, StockTransferItem, TransferStatus } from '@/api/stock-transfer/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  transferNo: '',
  status: undefined as TransferStatus | undefined,
  fromStoreId: undefined as number | undefined,
  toStoreId: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<StockTransfer[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 下拉选项
const storeOptions = ref<{ id: number; code: string; name: string }[]>([])
const productOptions = ref<{ id: number; name: string; code: string; unit: string }[]>([])

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getStockTransferList({
      transferNo: searchForm.transferNo || undefined,
      status: searchForm.status,
      fromStoreId: searchForm.fromStoreId,
      toStoreId: searchForm.toStoreId,
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
    const [stores, products] = await Promise.all([
      getStoreOptions(),
      getProductOptions()
    ])
    storeOptions.value = stores
    productOptions.value = products
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
  searchForm.transferNo = ''
  searchForm.status = undefined
  searchForm.fromStoreId = undefined
  searchForm.toStoreId = undefined
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

interface FormItem {
  productId: number | undefined
  quantity: number
  batchNo: string
}

const formData = reactive({
  fromStoreId: undefined as number | undefined,
  toStoreId: undefined as number | undefined,
  transferDate: new Date().toISOString().slice(0, 10),
  remark: '',
  items: [] as FormItem[]
})

const formRules: FormRules = {
  fromStoreId: [
    { required: true, message: '请选择调出门店', trigger: 'change' }
  ],
  toStoreId: [
    { required: true, message: '请选择调入门店', trigger: 'change' },
    {
      validator: (_rule, value, callback) => {
        if (value && formData.fromStoreId && value === formData.fromStoreId) {
          callback(new Error('调入门店不能与调出门店相同'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  transferDate: [
    { required: true, message: '请选择调拨日期', trigger: 'change' }
  ]
}

// 添加商品明细
const handleAddItem = () => {
  formData.items.push({
    productId: undefined,
    quantity: 1,
    batchNo: ''
  })
}

// 删除商品明细
const handleRemoveItem = (index: number) => {
  formData.items.splice(index, 1)
}

// 商品选择变化时无需额外操作（名称编码在提交时从选项列表中查找）
const handleItemProductChange = (_row: FormItem, _val: number) => {
  // 商品的名称和编码在创建时由 API 端自动填充
}

// 重置表单
const resetFormData = () => {
  formData.fromStoreId = undefined
  formData.toStoreId = undefined
  formData.transferDate = new Date().toISOString().slice(0, 10)
  formData.remark = ''
  formData.items = []
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
      // 校验明细
      if (formData.items.length === 0) {
        ElMessage.error('请至少添加一条调拨明细')
        return
      }
      for (const item of formData.items) {
        if (!item.productId) {
          ElMessage.error('请为所有明细选择商品')
          return
        }
        if (!item.quantity || item.quantity <= 0) {
          ElMessage.error('调拨数量必须大于0')
          return
        }
      }
      submitLoading.value = true
      try {
        await createStockTransfer({
          fromStoreId: formData.fromStoreId!,
          toStoreId: formData.toStoreId!,
          transferDate: formData.transferDate,
          remark: formData.remark || undefined,
          items: formData.items.map(item => ({
            productId: item.productId!,
            quantity: item.quantity,
            batchNo: item.batchNo || undefined
          }))
        })
        ElMessage.success('创建调拨单成功')
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '创建失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 详情弹窗
const detailDialogVisible = ref(false)
const detailData = reactive({
  id: 0,
  transferNo: '',
  fromStoreName: '',
  toStoreName: '',
  transferDate: '',
  status: 1 as TransferStatus,
  operatorName: '',
  remark: '',
  items: [] as StockTransferItem[]
})

// 查看详情
const handleViewDetail = async (row: StockTransfer) => {
  try {
    const detail = await getStockTransferDetail(row.id)
    detailData.id = detail.id
    detailData.transferNo = detail.transferNo
    detailData.fromStoreName = detail.fromStoreName || ''
    detailData.toStoreName = detail.toStoreName || ''
    detailData.transferDate = detail.transferDate
    detailData.status = detail.status
    detailData.operatorName = detail.operatorName || ''
    detailData.remark = detail.remark || ''
    detailData.items = detail.items
    detailDialogVisible.value = true
  } catch (error: any) {
    ElMessage.error(error.message || '获取详情失败')
  }
}

// 更新调拨单状态
const handleUpdateStatus = async (row: StockTransfer, status: TransferStatus) => {
  const statusLabel = transferStatusMap[status]
  const confirmMessage = status === 4
    ? `确定要取消调拨单 "${row.transferNo}" 吗？`
    : `确定要将调拨单 "${row.transferNo}" 更新为"${statusLabel}"吗？`
  try {
    await ElMessageBox.confirm(confirmMessage, '确认操作', {
      type: status === 4 ? 'warning' : 'info',
      confirmButtonText: '确定',
      cancelButtonText: '取消'
    })
    await updateTransferStatus(row.id, status)
    ElMessage.success('操作成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '操作失败')
    }
  }
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
.transfer-management {
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

/* 明细容器 */
.items-container {
  width: 100%;
}

/* 空明细提示 */
.empty-tip {
  text-align: center;
  color: var(--text-tertiary);
  padding: 20px;
  font-size: 14px;
}

/* 详情明细标题 */
.detail-items-title {
  font-weight: 600;
  margin: 20px 0 10px;
  color: var(--text-primary);
}
</style>
