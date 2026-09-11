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
              <el-option label="已调入" :value="3" />
              <el-option label="已取消" :value="4" />
            </el-select>
          </el-form-item>
          <el-form-item label="商品类型">
            <el-select v-model="searchForm.productType" placeholder="全部类型" clearable style="width: 130px">
              <el-option label="正品" :value="1" />
              <el-option label="样品" :value="4" />
              <el-option label="赠品" :value="5" />
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
        <el-button v-if="hasPermission('store:purchase-inventory:transfer:add')" type="primary" @click="handleAdd()">
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
              v-if="row.status === 1 && row.fromStoreId === userStore.currentStoreId && hasPermission('store:purchase-inventory:transfer:execute')"
              link type="success" size="small"
              @click="handleExecute(row)"
            >
              执行调拨
            </el-button>
            <el-button
              v-if="row.status === 1 && hasPermission('store:purchase-inventory:transfer:cancel')"
              link type="danger" size="small"
              @click="handleCancel(row)"
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
          <el-col :span="12">
            <el-form-item label="调出门店" prop="fromStoreId">
              <el-select v-model="formData.fromStoreId" placeholder="请选择调出门店" filterable disabled style="width: 100%">
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
        <el-form-item label="扣减方式">
          <el-radio-group v-model="deductMode" @change="handleDeductModeChange">
            <el-radio value="fefo">FEFO 自动</el-radio>
            <el-radio value="manual">手动指定批次</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="商品明细" prop="items">
          <div class="items-container">
            <el-button type="primary" plain size="small" :disabled="!formData.fromStoreId" @click="handleAddItem">
              <el-icon><Plus /></el-icon>
              添加商品
            </el-button>
            <el-table :data="formData.items" border style="width: 100%; margin-top: 10px">
              <el-table-column label="序号" type="index" width="60" align="center" />
              <el-table-column label="商品" min-width="220">
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
                      v-for="item in (productOptionsByFromStore[formData.fromStoreId || ''] || [])"
                      :key="item.id"
                      :label="`${item.name}（${item.code}）${productTypeMap[item.type]} 库存:${item.stock}`"
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
              <el-table-column label="批次号" width="180">
                <template #default="{ row }">
                  <span v-if="deductMode === 'fefo'" class="batch-auto-text">自动分配</span>
                  <el-select
                    v-else
                    v-model="row.batchNo"
                    placeholder="请选择批次"
                    filterable
                    size="small"
                    style="width: 100%"
                    :disabled="!row.productId"
                  >
                    <el-option
                      v-for="batch in (batchOptionsByProduct[`${formData.fromStoreId}_${row.productId}`] || [])"
                      :key="batch.id"
                      :label="`${batch.batchNo}（可用${batch.quantity}）`"
                      :value="batch.batchNo"
                    />
                  </el-select>
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
      width="900px"
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
        <el-table-column label="商品类型" width="100" align="center">
          <template #default="{ row }">
            {{ productTypeMap[row.type] || '-' }}
          </template>
        </el-table-column>
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
  executeStockTransfer,
  cancelStockTransfer,
  getStoreOptions,
  getFromStoreProducts,
  getFromStoreProductBatches,
  transferStatusMap,
  transferStatusTagType,
  productTypeMap
} from '@/api/stock-transfer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { StockTransfer, StockTransferItem, TransferStatus, StockTransferProductOption, StockTransferBatchOption } from '@/api/stock-transfer/types'
import { formatDate } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  transferNo: '',
  status: undefined as TransferStatus | undefined,
  productType: undefined as number | undefined,
  fromStoreId: undefined as string | undefined,
  toStoreId: undefined as string | undefined
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
const storeOptions = ref<{ id: string; code: string; name: string }[]>([])
// 调出门店商品选项缓存（key = fromStoreId），调出门店变更时重新加载
const productOptionsByFromStore = ref<Record<string, StockTransferProductOption[]>>({})
// 商品批次选项缓存（key = `${fromStoreId}_${productId}`），手动模式下使用
const batchOptionsByProduct = ref<Record<string, StockTransferBatchOption[]>>({})
// 扣减方式：FEFO 自动（按过期日期升序扣减） / 手动指定批次
type DeductMode = 'fefo' | 'manual'
const deductMode = ref<DeductMode>('fefo')

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getStockTransferList({
      transferNo: searchForm.transferNo || undefined,
      status: searchForm.status,
      productType: searchForm.productType,
      fromStoreId: searchForm.fromStoreId,
      toStoreId: searchForm.toStoreId,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 加载下拉选项（商品选项改为按调出门店动态加载，见 loadFromStoreProducts）
const loadOptions = async () => {
  try {
    storeOptions.value = await getStoreOptions()
  } catch {
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
  searchForm.productType = undefined
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
  fromStoreId: undefined as string | undefined,
  toStoreId: undefined as string | undefined,
  transferDate: formatDate(new Date()),
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

// 添加商品明细（调出门店未选时按钮禁用，由模板控制）
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

// 加载调出门店的库存商品列表（带缓存，避免重复请求）
const loadFromStoreProducts = async (fromStoreId: string) => {
  if (productOptionsByFromStore.value[fromStoreId]) return
  try {
    productOptionsByFromStore.value[fromStoreId] = await getFromStoreProducts(fromStoreId)
  } catch (error) {
    ElMessage.error((error as Error).message || '加载调出门店商品列表失败')
  }
}

// 加载调出门店指定商品的在库批次列表（带缓存，手动模式下使用）
const loadProductBatches = async (fromStoreId: string, productId: number) => {
  const key = `${fromStoreId}_${productId}`
  if (batchOptionsByProduct.value[key]) return
  try {
    batchOptionsByProduct.value[key] = await getFromStoreProductBatches(fromStoreId, productId)
  } catch (error) {
    ElMessage.error((error as Error).message || '加载商品批次列表失败')
  }
}

// 商品选择变化：清空已选批次；手动模式时加载该商品批次列表
const handleItemProductChange = (row: FormItem, _val: number) => {
  row.batchNo = ''
  if (deductMode.value === 'manual' && formData.fromStoreId && row.productId) {
    loadProductBatches(formData.fromStoreId, row.productId)
  }
}

// 切换扣减方式：切到手动模式时，对已选商品补加载批次列表（FEFO 模式下选商品时未加载）
const handleDeductModeChange = (val: DeductMode) => {
  if (val === 'manual' && formData.fromStoreId) {
    for (const item of formData.items) {
      if (item.productId) {
        loadProductBatches(formData.fromStoreId, item.productId)
      }
    }
  }
}

// 重置表单
const resetFormData = () => {
  formData.fromStoreId = undefined
  formData.toStoreId = undefined
  formData.transferDate = formatDate(new Date())
  formData.remark = ''
  formData.items = []
  deductMode.value = 'fefo'
  productOptionsByFromStore.value = {}
  batchOptionsByProduct.value = {}
}

// 新增
const handleAdd = () => {
  resetFormData()
  // 调出门店固定为门店切换器当前选中门店，不可更改
  formData.fromStoreId = userStore.currentStoreId || undefined
  if (formData.fromStoreId) {
    loadFromStoreProducts(formData.fromStoreId)
  }
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
        // 手动模式校验批次必选
        if (deductMode.value === 'manual' && !item.batchNo) {
          ElMessage.error('手动模式下请为每条明细选择批次')
          return
        }
      }

      // 校验库存上限：手动模式按批次聚合，FEFO 模式按商品聚合
      // 避免同一批次/商品被多条明细引用时累计数量超过可用库存
      if (deductMode.value === 'manual') {
        // 同一商品同一批次号的总数量不得超过该批次可用库存
        const batchUsageMap = new Map<string, { batchNo: string; used: number; stock: number }>()
        for (const item of formData.items) {
          const key = `${formData.fromStoreId}_${item.productId}`
          const batch = (batchOptionsByProduct.value[key] || []).find(b => b.batchNo === item.batchNo)
          if (!batch) continue
          const aggKey = `${item.productId}_${item.batchNo}`
          const agg = batchUsageMap.get(aggKey) || { batchNo: item.batchNo, used: 0, stock: batch.quantity }
          agg.used += item.quantity
          batchUsageMap.set(aggKey, agg)
        }
        for (const agg of batchUsageMap.values()) {
          if (agg.used > agg.stock) {
            ElMessage.error(`批次 ${agg.batchNo} 库存不足（可用 ${agg.stock}，已录入 ${agg.used}）`)
            return
          }
        }
      } else {
        // FEFO 模式：后端按过期日期跨批次扣减，按商品聚合校验总库存
        const productUsageMap = new Map<number, { used: number; stock: number; name: string }>()
        const products = productOptionsByFromStore.value[formData.fromStoreId || ''] || []
        for (const item of formData.items) {
          const product = products.find(p => p.id === item.productId)
          if (!product) continue
          const agg = productUsageMap.get(item.productId!) || { used: 0, stock: product.stock, name: product.name }
          agg.used += item.quantity
          productUsageMap.set(item.productId!, agg)
        }
        for (const agg of productUsageMap.values()) {
          if (agg.used > agg.stock) {
            ElMessage.error(`商品 ${agg.name} 库存不足（可用 ${agg.stock}，已录入 ${agg.used}）`)
            return
          }
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
            // FEFO 模式 batchNo 留空提交（后端按过期日期升序扣减），手动模式填入选中批次号
            batchNo: deductMode.value === 'manual' ? item.batchNo : undefined
          }))
        })
        ElMessage.success('创建调拨单成功')
        dialogVisible.value = false
        loadData()
      } catch (error) {
        ElMessage.error((error as Error).message || '创建失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 详情弹窗
const detailDialogVisible = ref(false)
const detailData = reactive({
  id: '',
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
  } catch (error) {
    ElMessage.error((error as Error).message || '获取详情失败')
  }
}

// 执行调拨（调出门店扣减库存 + 调入门店增加库存，状态转为已调入）
const handleExecute = async (row: StockTransfer) => {
  try {
    await ElMessageBox.confirm(
      `确定要执行调拨单 "${row.transferNo}" 吗？执行后将扣减调出门店库存并增加调入门店库存。`,
      '确认操作',
      { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消' }
    )
    await executeStockTransfer(row.id)
    ElMessage.success('调拨执行成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '操作失败')
    }
  }
}

// 取消调拨单（仅待调出状态可取消）
const handleCancel = async (row: StockTransfer) => {
  try {
    await ElMessageBox.confirm(
      `确定要取消调拨单 "${row.transferNo}" 吗？`,
      '确认操作',
      { type: 'warning', confirmButtonText: '确定', cancelButtonText: '返回' }
    )
    await cancelStockTransfer(row.id)
    ElMessage.success('取消成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '操作失败')
    }
  }
}

// 格式化数字
const formatNumber = (num: number) => {
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
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

/* FEFO 模式批次占位文本 */
.batch-auto-text {
  color: var(--text-tertiary);
  font-size: 13px;
}

/* 详情明细标题（弹窗为白底浮层，使用深色与弹窗表头保持一致） */
.detail-items-title {
  font-weight: 600;
  margin: 20px 0 10px;
  color: #1f2937;
}
</style>
