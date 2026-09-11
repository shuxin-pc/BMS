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
        <el-button v-if="hasPermission('store:purchase-inventory:purchase-return:add')" type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          发起退货
        </el-button>
        <el-button
          v-if="hasPermission('store:purchase-inventory:purchase-return:batchDelete')"
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
        <el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button v-if="hasPermission('store:purchase-inventory:purchase-return:edit')" type="primary" link size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button v-if="hasPermission('store:purchase-inventory:purchase-return:delete')" type="danger" link size="small" @click="handleDelete(row)">删除</el-button>
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
      width="1100px"
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
            <el-form-item label="供应商" prop="supplierId">
              <el-select
                v-model="formData.supplierId"
                placeholder="请选择供应商"
                filterable
                style="width: 100%"
                @change="handleSupplierChange"
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
          <el-col :span="12">
            <el-form-item label="退货时间" prop="returnTime">
              <el-date-picker
                v-model="formData.returnTime"
                type="date"
                placeholder="选择退货日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 退货明细表格 -->
        <el-form-item label="退货明细">
          <div class="items-table-wrapper">
            <el-table :data="formData.items" border style="width: 100%">
              <el-table-column label="序号" type="index" width="60" align="center" />
              <el-table-column label="商品" min-width="200">
                <template #default="{ row, $index }">
                  <el-select
                    v-model="row.productId"
                    :placeholder="formData.supplierId ? '请选择商品' : '请先选择供应商'"
                    :disabled="!formData.supplierId"
                    filterable
                    style="width: 100%"
                    @change="handleProductChange($index)"
                  >
                    <el-option
                      v-for="item in getSupplierProducts()"
                      :key="item.id"
                      :label="`${item.name}（${item.code}）`"
                      :value="item.id"
                    />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column label="批次号" min-width="200">
                <template #default="{ row, $index }">
                  <el-select
                    v-model="row.batchNo"
                    :placeholder="row.productId ? '请选择批次' : '请先选择商品'"
                    :disabled="!row.productId"
                    filterable
                    style="width: 100%"
                    @change="handleBatchChange($index)"
                  >
                    <el-option
                      v-for="batch in getProductBatchOptions(row.productId)"
                      :key="batch.id"
                      :label="`${batch.batchNo}（库存: ${batch.quantity}）`"
                      :value="batch.batchNo"
                    />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column label="退货数量" width="140">
                <template #default="{ row, $index }">
                  <el-input-number
                    v-model="row.quantity"
                    :min="0"
                    :max="row.batchStock"
                    :precision="2"
                    :step="1"
                    :controls="false"
                    style="width: 100%"
                    @change="handleQuantityChange($index)"
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
                    disabled
                    style="width: 100%"
                  />
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
import { useUserStore } from '@/stores/user'
import {
  getPurchaseReturns,
  createPurchaseReturn,
  updatePurchaseReturn,
  deletePurchaseReturn,
  batchDeletePurchaseReturns
} from '@/api/purchase'
import type { PurchaseReturn, PurchaseReturnCreate, PurchaseReturnItemCreate } from '@/api/purchase/types'
import { getSuppliers, getProductsBySupplier } from '@/api/supplier'
import type { Supplier } from '@/api/supplier/types'
import { getProductOptions, getProductBatches } from '@/api/inventory-ops'
import type { InventoryBatchOption } from '@/api/inventory-ops/types'
import { formatDate as formatDateTime } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 商品下拉选项（从后端加载，用于列表展开行展示商品名称）
const productOptions = ref<{ id: number; name: string; code: string }[]>([])

// 弹窗：按供应商ID缓存的商品选项（key=supplierId），切换供应商时按需加载
const productOptionsBySupplier = ref<Record<number, { id: number; name: string; code: string }[]>>({})

// 弹窗：按商品ID缓存的在库批次列表（key=productId），选商品后按需加载
// 用于批次号下拉，展示批次号+当前库存数量
const batchesByProduct = ref<Record<number, InventoryBatchOption[]>>({})

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

// 加载商品选项（用于列表展开行展示商品名称）
const loadProductOptions = async () => {
  try {
    productOptions.value = await getProductOptions()
  } catch {
    // 商品列表加载失败不阻断主流程
  }
}

// 加载指定供应商关联的商品列表（用于弹窗明细行的商品下拉筛选）
// 已缓存则直接复用，避免重复请求
const loadProductsBySupplier = async (supplierId: number) => {
  if (productOptionsBySupplier.value[supplierId]) return
  try {
    const list = await getProductsBySupplier(supplierId)
    productOptionsBySupplier.value[supplierId] = list.map(ps => ({
      id: ps.productId,
      name: ps.productName || '',
      code: ps.productCode || ''
    }))
  } catch {
    // 供应商关联商品加载失败不阻断主流程
  }
}

// 获取当前供应商下的商品选项（供模板使用）
const getSupplierProducts = (): { id: number; name: string; code: string }[] => {
  if (!formData.supplierId) return []
  return productOptionsBySupplier.value[formData.supplierId] || []
}

// 加载指定商品的在库批次列表（用于弹窗明细行的批次号下拉）
// 已缓存则直接复用，避免重复请求
const loadProductBatches = async (productId: number) => {
  if (batchesByProduct.value[productId]) return
  try {
    batchesByProduct.value[productId] = await getProductBatches(productId)
  } catch {
    // 批次列表加载失败不阻断主流程
  }
}

// 获取指定商品的在库批次选项（供模板使用）
const getProductBatchOptions = (productId: number | undefined): InventoryBatchOption[] => {
  if (!productId) return []
  return batchesByProduct.value[productId] || []
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
  } catch (error) {
    ElMessage.error((error as Error).message || '加载退货记录失败')
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

interface FormItem extends PurchaseReturnItemCreate {
  /** 选中批次的当前库存数（前端校验退货数量上限用，不提交后端） */
  batchStock?: number
  /** 选中批次的入库单价（自动计算退款金额用，不提交后端） */
  unitPrice?: number
}

const formData = reactive({
  returnNo: '',
  supplierId: undefined as number | undefined,
  returnTime: '',
  voucherImageUrl: '' as string,
  remark: '',
  items: [] as FormItem[]
})

const formRules: FormRules = {
  supplierId: [
    { required: true, message: '请选择供应商', trigger: 'change' }
  ],
  returnTime: [
    { required: true, message: '请选择退货时间', trigger: 'change' }
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
    batchNo: undefined,
    batchStock: undefined,
    unitPrice: undefined
  })
}

// 删除明细行
const handleRemoveItem = (index: number) => {
  formData.items.splice(index, 1)
}

// 供应商变化时清空明细并加载该供应商关联的商品
const handleSupplierChange = async (supplierId: number) => {
  formData.items = []
  batchesByProduct.value = {}
  if (supplierId) {
    await loadProductsBySupplier(supplierId)
  }
}

// 商品选择变化时加载该商品的在库批次，并清空原批次选择
const handleProductChange = async (index: number) => {
  const row = formData.items[index]
  if (!row) return
  // 清空原批次选择、库存上限、单价、退款金额
  row.batchNo = undefined
  row.batchStock = undefined
  row.unitPrice = undefined
  row.refundAmount = 0
  if (row.productId) {
    await loadProductBatches(row.productId)
  }
}

// 批次选择变化时记录该批次当前库存与入库单价，并联动重算退款金额
const handleBatchChange = (index: number) => {
  const row = formData.items[index]
  if (!row) return
  const batch = getProductBatchOptions(row.productId).find(b => b.batchNo === row.batchNo)
  row.batchStock = batch?.quantity
  row.unitPrice = batch?.unitPrice
  if (row.unitPrice !== undefined) {
    row.refundAmount = Number((row.quantity * row.unitPrice).toFixed(2))
  }
}

// 退货数量变化时按批次单价自动计算退款金额
const handleQuantityChange = (index: number) => {
  const row = formData.items[index]
  if (!row) return
  // 输入0或负数时重置为1
  if (!row.quantity || row.quantity <= 0) {
    row.quantity = 1
  }
  if (row.unitPrice !== undefined) {
    row.refundAmount = Number((row.quantity * row.unitPrice).toFixed(2))
  }
}

// 重置表单
const resetFormData = () => {
  formData.returnNo = ''
  formData.supplierId = undefined
  formData.returnTime = formatDateTime(new Date())
  formData.voucherImageUrl = ''
  formData.remark = ''
  formData.items = []
  isEdit.value = false
  editingId.value = 0
  // 清空弹窗级缓存，避免上次选择的供应商/商品批次残留
  productOptionsBySupplier.value = {}
  batchesByProduct.value = {}
}

// 新增
const handleAdd = () => {
  resetFormData()
  // 默认添加一行空明细
  handleAddItem()
  dialogVisible.value = true
}

// 编辑
const handleEdit = async (row: PurchaseReturn) => {
  resetFormData()
  isEdit.value = true
  editingId.value = row.id
  formData.returnNo = row.returnNo
  formData.supplierId = row.supplierId
  formData.returnTime = row.returnTime
  formData.voucherImageUrl = row.voucherImageUrl || ''
  formData.remark = row.remark || ''
  // 预加载供应商关联商品，确保商品下拉能显示已选商品
  if (row.supplierId) {
    await loadProductsBySupplier(row.supplierId)
  }
  // 回填明细，并预加载每个商品的在库批次列表（用于批次下拉显示原批次）
  // 编辑模式不设置 batchStock：库存已扣减，批次库存校验在编辑模式跳过
  formData.items = (row.items || []).map(item => ({
    productId: item.productId,
    quantity: Number(item.quantity),
    refundAmount: Number(item.refundAmount),
    batchNo: item.batchNo || undefined,
    batchStock: undefined
  }))
  await Promise.all(
    formData.items
      .filter(item => item.productId)
      .map(item => loadProductBatches(item.productId))
  )
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
  } catch (error) {
    const errMessage = error !== 'cancel' ? (error as { message?: string }).message : undefined
    if (errMessage) {
      ElMessage.error(errMessage)
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
  } catch (error) {
    const errMessage = error !== 'cancel' ? (error as { message?: string }).message : undefined
    if (errMessage) {
      ElMessage.error(errMessage)
    }
  }
}

// 提交退货
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    // 手动校验退货明细（参考采购订单页面，不使用 el-form 校验避免红色边框残留）
    if (formData.items.length === 0) {
      ElMessage.warning('请至少添加一条退货明细')
      return
    }
    for (let i = 0; i < formData.items.length; i++) {
      const item = formData.items[i]
      if (!item.productId) {
        ElMessage.warning(`第 ${i + 1} 行请选择商品`)
        return
      }
      if (!item.batchNo) {
        ElMessage.warning(`第 ${i + 1} 行请选择批次`)
        return
      }
      if (!item.quantity || item.quantity <= 0) {
        ElMessage.warning(`第 ${i + 1} 行退货数量必须大于0`)
        return
      }
      // 新建模式下校验退货数量不超过批次当前库存（编辑模式跳过：库存已扣减）
      if (!isEdit.value && item.batchStock !== undefined && item.quantity > item.batchStock) {
        ElMessage.warning(`第 ${i + 1} 行退货数量不能超过批次库存 ${item.batchStock}`)
        return
      }
    }
    submitLoading.value = true
    try {
      const payload: PurchaseReturnCreate = {
        // 退货单号由后端自动生成：新建时不传，编辑时回填原值保持不变
        returnNo: formData.returnNo || undefined,
        supplierId: formData.supplierId!,
        returnTime: formData.returnTime,
        voucherImageUrl: formData.voucherImageUrl || undefined,
        remark: formData.remark || undefined,
        items: formData.items.map(item => ({
          productId: item.productId,
          quantity: Number(item.quantity),
          refundAmount: Number(item.refundAmount),
          batchNo: item.batchNo || undefined
        }))
      }
      if (isEdit.value) {
        // 编辑时 body 须携带 id，供后端 [FromBody] 模型绑定后通过 FluentValidation 自动验证
        await updatePurchaseReturn({ ...payload, id: editingId.value })
        ElMessage.success('修改成功')
      } else {
        await createPurchaseReturn(payload)
        ElMessage.success('退货创建成功')
      }
      dialogVisible.value = false
      loadData()
    } catch (error) {
      ElMessage.error((error as Error).message || '操作失败')
    } finally {
      submitLoading.value = false
    }
  })
}


onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadSupplierOptions()
  loadProductOptions()
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

/* 弹窗内表格 - 浅色浮层风格
   项目规范：弹窗为白底浅色浮层，弹窗内表格需跟随浅色，
   避免页面深色表格样式覆盖到弹窗，导致深色单元格+白色弹窗+白色输入框冲突 */
:deep(.el-dialog .el-table) {
  --el-table-bg-color: transparent !important;
  --el-table-text-color: #4b5563 !important;
  --el-table-border-color: #e5e7eb !important;
  --el-table-header-bg-color: #f9fafb !important;
  --el-table-header-text-color: #1f2937 !important;
  --el-table-row-hover-bg-color: #f3f4f6 !important;
}

:deep(.el-dialog .el-table th.el-table__cell) {
  background-color: #f9fafb !important;
  color: #1f2937 !important;
  border-bottom: 1px solid #e5e7eb !important;
}

:deep(.el-dialog .el-table td.el-table__cell) {
  background-color: transparent !important;
  color: #4b5563 !important;
  border-bottom: 1px solid #e5e7eb !important;
}

:deep(.el-dialog .el-table__row:hover > td.el-table__cell) {
  background-color: #f3f4f6 !important;
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
