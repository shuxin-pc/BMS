<template>
  <div class="purchase-return-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="退货单号">
            <el-input
              v-model="searchForm.returnNo"
              placeholder="请输入退货单号"
              clearable
              style="width: 200px"
              @keyup.enter="handleSearch"
            />
          </el-form-item>
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
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          发起退货
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
        >
          <el-icon><Delete /></el-icon>
          批量删除
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
        @selection-change="handleSelectionChange"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column type="expand">
          <template #default="{ row }">
            <div class="expand-detail">
              <el-table :data="row.items" border size="small">
                <el-table-column label="商品ID" prop="productId" width="100" />
                <el-table-column label="商品名称" min-width="180">
                  <template #default="{ row: itemRow }">
                    {{ getProductName(itemRow.productId) }}
                  </template>
                </el-table-column>
                <el-table-column label="批次号" prop="batchNo" width="140" />
                <el-table-column label="退货数量" prop="quantity" width="100" align="right" />
                <el-table-column label="退款金额" width="120" align="right">
                  <template #default="{ row: itemRow }">
                    <span class="amount-text danger">¥{{ Number(itemRow.refundAmount).toFixed(2) }}</span>
                  </template>
                </el-table-column>
                <el-table-column label="备注" prop="remark" min-width="150" show-overflow-tooltip />
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="returnNo" label="退货单号" width="160" />
        <el-table-column label="供应商" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            {{ getSupplierName(row.supplierId) }}
          </template>
        </el-table-column>
        <el-table-column label="退货明细" width="100" align="center">
          <template #default="{ row }">
            <el-tag size="small" type="info">{{ row.items?.length || 0 }} 种商品</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="totalQuantity" label="退货总数量" width="120" align="right" />
        <el-table-column label="退款总金额" width="140" align="right">
          <template #default="{ row }">
            <span class="amount-text danger">¥{{ Number(row.totalRefundAmount).toFixed(2) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="退货时间" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.returnTime) }}
          </template>
        </el-table-column>
        <el-table-column label="凭证" width="80" align="center">
          <template #default="{ row }">
            <el-image
              v-if="row.voucherImageUrl"
              :src="row.voucherImageUrl"
              :preview-src-list="[row.voucherImageUrl]"
              fit="cover"
              style="width: 30px; height: 30px; border-radius: 4px"
              preview-teleported
            />
            <span v-else class="text-muted">无</span>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button type="danger" link size="small" @click="handleDelete(row)">删除</el-button>
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

    <!-- 新增/编辑退货弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑采购退货' : '发起采购退货'"
      width="900px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="退货单号" prop="returnNo">
              <el-input v-model="formData.returnNo" placeholder="如 PR20260716001" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="供应商" prop="supplierId">
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
          </el-col>
          <el-col :span="8">
            <el-form-item label="退货时间" prop="returnTime">
              <el-date-picker
                v-model="formData.returnTime"
                type="datetime"
                placeholder="选择退货时间"
                value-format="YYYY-MM-DDTHH:mm:ss"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 退货明细表格 -->
        <el-form-item label="退货明细" prop="items">
          <div class="items-table-wrapper">
            <el-table :data="formData.items" border style="width: 100%">
              <el-table-column label="序号" type="index" width="60" align="center" />
              <el-table-column label="商品" min-width="200">
                <template #default="{ row, $index }">
                  <el-select
                    v-model="row.productId"
                    placeholder="请选择商品"
                    filterable
                    style="width: 100%"
                    @change="handleProductChange($index)"
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
              <el-table-column label="批次号" width="140">
                <template #default="{ row }">
                  <el-input v-model="row.batchNo" placeholder="选填" />
                </template>
              </el-table-column>
              <el-table-column label="退货数量" width="140">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.quantity"
                    :min="0.01"
                    :precision="4"
                    :step="1"
                    :controls="false"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="退款金额" width="140">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.refundAmount"
                    :min="0"
                    :precision="2"
                    :step="10"
                    :controls="false"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="备注" min-width="150">
                <template #default="{ row }">
                  <el-input v-model="row.remark" placeholder="选填" />
                </template>
              </el-table-column>
              <el-table-column label="操作" width="80" align="center">
                <template #default="{ $index }">
                  <el-button type="danger" link size="small" @click="handleRemoveItem($index)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
            <div class="items-summary">
              <span>合计：{{ formData.items.length }} 种商品，退货数量
                <strong>{{ totalQuantity }}</strong>，退款金额
                <strong class="amount-text danger">¥{{ totalRefundAmount.toFixed(2) }}</strong>
              </span>
              <el-button type="primary" link size="small" @click="handleAddItem">
                <el-icon><Plus /></el-icon>
                添加明细
              </el-button>
            </div>
          </div>
        </el-form-item>

        <el-form-item label="凭证照片">
          <el-upload
            :show-file-list="true"
            :auto-upload="false"
            :limit="1"
            accept="image/*"
            :on-change="handleVoucherChange"
            :on-remove="handleVoucherRemove"
            list-type="picture-card"
          >
            <el-icon><Plus /></el-icon>
          </el-upload>
          <div class="upload-tip">支持上传退货凭证照片（非必填）</div>
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input
            v-model="formData.remark"
            type="textarea"
            :rows="3"
            placeholder="请输入退货原因或备注信息"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          {{ isEdit ? '保存修改' : '确认退货' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import {
  getPurchaseReturns,
  createPurchaseReturn,
  updatePurchaseReturn,
  deletePurchaseReturn,
  batchDeletePurchaseReturns
} from '@/api/purchase'
import type { PurchaseReturn, PurchaseReturnCreate, PurchaseReturnItemCreate } from '@/api/purchase/types'
import { getSuppliers } from '@/api/supplier'
import type { Supplier } from '@/api/supplier/types'

const systemConfigStore = useSystemConfigStore()

/**
 * 本地商品选项（避免依赖后端 product API）
 * 与采购订单明细中的商品保持一致
 */
const productOptions = ref([
  { id: 101, name: '深层修复洗发水', code: 'SP-001' },
  { id: 102, name: '丝滑护发素', code: 'SP-002' },
  { id: 103, name: '植物染发剂', code: 'SP-003' },
  { id: 104, name: '强力定型喷雾', code: 'SP-004' },
  { id: 105, name: '保湿护肤霜', code: 'SP-005' },
  { id: 106, name: '一次性毛巾', code: 'HC-001' },
  { id: 107, name: '染发碗刷套装', code: 'HC-002' }
])

// 供应商下拉选项
const supplierOptions = ref<Supplier[]>([])

// 搜索表单
const searchForm = reactive({
  returnNo: '' as string,
  supplierId: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<PurchaseReturn[]>([])
const selectedRows = ref<PurchaseReturn[]>([])

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

// 获取供应商名称
const getSupplierName = (supplierId: number): string => {
  return supplierOptions.value.find(s => s.id === supplierId)?.name || `供应商#${supplierId}`
}

// 获取商品名称
const getProductName = (productId: number): string => {
  const p = productOptions.value.find(item => item.id === productId)
  return p ? `${p.name}（${p.code}）` : `商品#${productId}`
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getPurchaseReturns({
      returnNo: searchForm.returnNo || undefined,
      supplierId: searchForm.supplierId,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error: any) {
    ElMessage.error(error.message || '加载退货记录失败')
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
  searchForm.returnNo = ''
  searchForm.supplierId = undefined
  handleSearch()
}

// 选择变化
const handleSelectionChange = (rows: PurchaseReturn[]) => {
  selectedRows.value = rows
}

// 新增/编辑弹窗
const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const isEdit = ref(false)
const editingId = ref(0)
const voucherFile = ref<File | null>(null)

interface FormItem extends PurchaseReturnItemCreate {}

const formData = reactive({
  returnNo: '',
  supplierId: undefined as number | undefined,
  returnTime: '',
  voucherImageUrl: '' as string,
  remark: '',
  items: [] as FormItem[]
})

const formRules: FormRules = {
  returnNo: [
    { required: true, message: '请输入退货单号', trigger: 'blur' },
    { max: 50, message: '退货单号最多50个字符', trigger: 'blur' }
  ],
  supplierId: [
    { required: true, message: '请选择供应商', trigger: 'change' }
  ],
  returnTime: [
    { required: true, message: '请选择退货时间', trigger: 'change' }
  ],
  items: [
    {
      validator: (_rule: any, value: FormItem[], callback: any) => {
        if (!value || value.length === 0) {
          callback(new Error('退货明细不能为空'))
          return
        }
        for (let i = 0; i < value.length; i++) {
          const item = value[i]
          if (!item.productId) {
            callback(new Error(`第 ${i + 1} 行请选择商品`))
            return
          }
          if (!item.quantity || item.quantity <= 0) {
            callback(new Error(`第 ${i + 1} 行退货数量必须大于0`))
            return
          }
          if (item.refundAmount < 0) {
            callback(new Error(`第 ${i + 1} 行退款金额不能为负数`))
            return
          }
        }
        callback()
      },
      trigger: 'change'
    }
  ]
}

// 合计计算
const totalQuantity = computed(() =>
  formData.items.reduce((sum, item) => sum + (Number(item.quantity) || 0), 0)
)

const totalRefundAmount = computed(() =>
  formData.items.reduce((sum, item) => sum + (Number(item.refundAmount) || 0), 0)
)

// 添加明细行
const handleAddItem = () => {
  formData.items.push({
    productId: undefined as unknown as number,
    quantity: 1,
    refundAmount: 0,
    batchNo: '',
    remark: ''
  })
}

// 删除明细行
const handleRemoveItem = (index: number) => {
  formData.items.splice(index, 1)
}

// 商品选择变化时可以做联动（如自动带出上次采购价）
const handleProductChange = (_index: number) => {
  // 预留：可根据选中的商品自动带出参考价格
}

// 凭证图片选择/移除
const handleVoucherChange = (file: any) => {
  voucherFile.value = file.raw
}

const handleVoucherRemove = () => {
  voucherFile.value = null
}

// 重置表单
const resetFormData = () => {
  formData.returnNo = ''
  formData.supplierId = undefined
  formData.returnTime = new Date().toISOString().slice(0, 19)
  formData.voucherImageUrl = ''
  formData.remark = ''
  formData.items = []
  voucherFile.value = null
  isEdit.value = false
  editingId.value = 0
}

// 新增
const handleAdd = () => {
  resetFormData()
  // 默认添加一行空明细
  handleAddItem()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: PurchaseReturn) => {
  resetFormData()
  isEdit.value = true
  editingId.value = row.id
  formData.returnNo = row.returnNo
  formData.supplierId = row.supplierId
  formData.returnTime = row.returnTime
  formData.voucherImageUrl = row.voucherImageUrl || ''
  formData.remark = row.remark || ''
  formData.items = (row.items || []).map(item => ({
    productId: item.productId,
    quantity: Number(item.quantity),
    refundAmount: Number(item.refundAmount),
    batchNo: item.batchNo || '',
    remark: item.remark || ''
  }))
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: PurchaseReturn) => {
  try {
    await ElMessageBox.confirm(`确认删除退货单 ${row.returnNo} 吗？`, '提示', {
      type: 'warning'
    })
    await deletePurchaseReturn(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel' && error?.message) {
      ElMessage.error(error.message)
    }
  }
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(
      `确认删除选中的 ${selectedRows.value.length} 条退货单吗？`,
      '提示',
      { type: 'warning' }
    )
    const ids = selectedRows.value.map(r => r.id)
    await batchDeletePurchaseReturns(ids)
    ElMessage.success('批量删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel' && error?.message) {
      ElMessage.error(error.message)
    }
  }
}

// 提交退货
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      const payload: PurchaseReturnCreate = {
        returnNo: formData.returnNo,
        supplierId: formData.supplierId!,
        returnTime: formData.returnTime,
        voucherImageUrl: voucherFile.value ? URL.createObjectURL(voucherFile.value) : formData.voucherImageUrl || undefined,
        remark: formData.remark || undefined,
        items: formData.items.map(item => ({
          productId: item.productId,
          quantity: Number(item.quantity),
          refundAmount: Number(item.refundAmount),
          batchNo: item.batchNo || undefined,
          remark: item.remark || undefined
        }))
      }
      if (isEdit.value) {
        await updatePurchaseReturn(editingId.value, payload)
        ElMessage.success('修改成功')
      } else {
        await createPurchaseReturn(payload)
        ElMessage.success('退货创建成功')
      }
      dialogVisible.value = false
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '操作失败')
    } finally {
      submitLoading.value = false
    }
  })
}

// 格式化日期时间
const formatDateTime = (dateStr: string): string => {
  if (!dateStr) return '-'
  const dt = new Date(dateStr)
  const date = dt.toISOString().split('T')[0]
  const time = dt.toTimeString().split(' ')[0]
  return `${date} ${time}`
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
.purchase-return-management {
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

/* 金额文本 */
.amount-text {
  font-weight: 600;
}

.amount-text.danger {
  color: var(--el-color-danger);
}

.text-muted {
  color: var(--text-tertiary);
  font-size: 12px;
}

/* 上传提示 */
.upload-tip {
  font-size: 12px;
  color: var(--text-tertiary);
  margin-top: 4px;
}

/* 明细表格容器 */
.items-table-wrapper {
  width: 100%;
}

.items-summary {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 4px 0;
  font-size: 14px;
}

/* 展开行明细 */
.expand-detail {
  padding: 16px 24px;
  background: var(--bg-secondary);
}
</style>
