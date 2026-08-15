<template>
  <div class="purchase-order-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="供应商">
            <el-select
              v-model="searchForm.supplierId"
              placeholder="全部供应商"
              clearable
              filterable
              style="width: 160px"
            >
              <el-option
                v-for="item in supplierOptions"
                :key="item.id"
                :label="item.name"
                :value="item.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="商品">
            <el-select
              v-model="searchForm.productId"
              placeholder="全部商品"
              clearable
              filterable
              style="width: 160px"
            >
              <el-option
                v-for="item in productOptions"
                :key="item.id"
                :label="item.name"
                :value="item.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="采购单号">
            <el-input
              v-model="searchForm.orderNo"
              placeholder="请输入采购单号"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="采购类型">
            <el-select v-model="searchForm.purchaseType" placeholder="全部类型" clearable style="width: 140px">
              <el-option label="零售商品采购" :value="1" />
              <el-option label="耗材采购" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item label="采购日期">
            <el-date-picker
              v-model="searchForm.dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD"
              style="width: 220px"
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
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新增采购订单
        </el-button>
        <el-button type="info" plain @click="handleViewSummary">
          <el-icon><DataAnalysis /></el-icon>
          供应商采购统计
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
        <el-table-column prop="orderNo" label="采购单号" width="160" />
        <el-table-column label="供应商" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            {{ getSupplierDisplay(row) }}
          </template>
        </el-table-column>
        <el-table-column label="商品" min-width="180" show-overflow-tooltip>
          <template #default="{ row }">
            {{ getProductsDisplay(row) }}
          </template>
        </el-table-column>
        <el-table-column label="采购日期" width="110">
          <template #default="{ row }">
            {{ formatDate(row.orderDate) }}
          </template>
        </el-table-column>
        <el-table-column label="采购类型" width="120">
          <template #default="{ row }">
            <el-tag :type="row.purchaseType === 1 ? 'primary' : 'warning'" size="small" effect="plain">
              {{ row.purchaseType === 1 ? '零售商品' : '耗材' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="采购总金额" width="120" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥{{ row.totalAmount.toFixed(2) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleViewDetail(row)">
              <el-icon><View /></el-icon>
              查看详情
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

    <!-- 采购单详情弹窗 -->
    <el-dialog
      v-model="detailDialogVisible"
      title="采购单详情"
      width="1100px"
    >
      <div v-if="currentOrder" v-loading="detailLoading">
        <!-- 基本信息 -->
        <el-descriptions :column="2" border class="mb-20">
          <el-descriptions-item label="采购单号">{{ currentOrder.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="供应商">{{ getSupplierDisplay(currentOrder) }}</el-descriptions-item>
          <el-descriptions-item label="采购日期">{{ formatDate(currentOrder.orderDate) }}</el-descriptions-item>
          <el-descriptions-item label="采购类型">
            {{ currentOrder.purchaseType === 1 ? '零售商品采购' : '耗材采购' }}
          </el-descriptions-item>
          <el-descriptions-item label="采购总金额">
            <span class="amount-text">¥{{ currentOrder.totalAmount.toFixed(2) }}</span>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusTagType(currentOrder.status)" size="small" effect="dark">
              {{ getStatusText(currentOrder.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="操作人员">{{ currentOrder.operatorName || '-' }}</el-descriptions-item>
          <el-descriptions-item label="备注" :span="2">{{ currentOrder.remark || '-' }}</el-descriptions-item>
        </el-descriptions>

        <!-- 明细列表 -->
        <div class="detail-section-title">采购明细</div>
        <el-table :data="currentOrder.items || []" style="width: 100%" size="small" border>
          <el-table-column label="供应商" min-width="140">
            <template #default="{ row }">
              {{ getSupplierName(row.supplierId) }}
            </template>
          </el-table-column>
          <el-table-column label="商品" min-width="160">
            <template #default="{ row }">
              {{ getProductName(row.productId) }}
            </template>
          </el-table-column>
          <el-table-column prop="quantity" label="数量" width="80" align="right" />
          <el-table-column label="单价" width="100" align="right">
            <template #default="{ row }">
              ¥{{ row.unitPrice.toFixed(2) }}
            </template>
          </el-table-column>
          <el-table-column label="小计" width="120" align="right">
            <template #default="{ row }">
              <span class="amount-text">¥{{ (row.totalPrice || row.quantity * row.unitPrice).toFixed(2) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="批次号" width="130">
            <template #default="{ row }">
              {{ row.batchNo || '-' }}
            </template>
          </el-table-column>
          <el-table-column label="过期日期" width="110">
            <template #default="{ row }">
              {{ row.expirationDate ? formatDate(row.expirationDate) : '-' }}
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-dialog>

    <!-- 新增采购订单弹窗 -->
    <el-dialog
      v-model="createDialogVisible"
      title="新增采购订单"
      width="1250px"
      :close-on-click-modal="false"
    >
      <el-form :model="createForm" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="采购日期" required>
              <el-date-picker
                v-model="createForm.orderDate"
                type="date"
                placeholder="请选择采购日期"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="采购类型" required>
              <el-select v-model="createForm.purchaseType" style="width: 100%">
                <el-option label="零售商品采购" :value="1" />
                <el-option label="耗材采购" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="采购总金额">
              <span class="amount-text">¥{{ computedTotalAmount.toFixed(2) }}</span>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注" style="max-width: 600px">
          <el-input v-model="createForm.remark" type="textarea" :rows="2" placeholder="请输入备注信息" />
        </el-form-item>

        <!-- 明细列表 -->
        <div class="detail-section-title">
          采购明细
          <el-button type="primary" size="small" @click="addCreateItem" style="margin-left: 12px">
            <el-icon><Plus /></el-icon>
            添加明细
          </el-button>
        </div>
        <el-table :data="createForm.items" style="width: 100%" size="small" border>
          <el-table-column label="供应商" min-width="150">
            <template #default="{ row }">
              <el-select v-model="row.supplierId" placeholder="选择供应商" filterable size="small" style="width: 100%" @change="handleItemSupplierChange(row)">
                <el-option v-for="s in supplierOptions" :key="s.id" :label="s.name" :value="s.id" />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="商品" min-width="170">
            <template #default="{ row }">
              <el-select v-model="row.productId" placeholder="请先选择供应商" filterable size="small" style="width: 100%" :disabled="!row.supplierId" @change="handleItemProductChange(row)">
                <el-option
                  v-for="p in getFilteredProducts(row.supplierId)"
                  :key="p.id"
                  :label="`${p.name}（${p.code}）`"
                  :value="p.id"
                />
              </el-select>
            </template>
          </el-table-column>
          <el-table-column label="数量" width="120">
            <template #default="{ row }">
              <el-input-number v-model="row.quantity" :min="0.01" :precision="2" :step="1" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="单价" width="120">
            <template #default="{ row }">
              <el-input-number v-model="row.unitPrice" :min="0" :precision="2" :step="1" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="小计" width="90" align="right">
            <template #default="{ row }">
              <span class="amount-text">¥{{ computeSubtotal(row).toFixed(2) }}</span>
            </template>
          </el-table-column>
          <el-table-column label="生产日期" width="140">
            <template #default="{ row }">
              <el-date-picker v-model="row.productionDate" type="date" placeholder="生产日期" value-format="YYYY-MM-DD" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="保质期(天)" width="100">
            <template #default="{ row }">
              <el-input-number v-model="row.shelfLifeDays" :min="1" :step="1" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="过期日期" width="140">
            <template #default="{ row }">
              <el-date-picker v-model="row.expirationDate" type="date" placeholder="过期日期" value-format="YYYY-MM-DD" size="small" style="width: 100%" />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="70" fixed="right">
            <template #default="{ $index }">
              <el-button link type="danger" size="small" @click="removeCreateItem($index)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <div v-if="createForm.items.length === 0" class="empty-items-hint">
          请点击"添加明细"添加采购商品
        </div>
      </el-form>
      <template #footer>
        <el-button @click="createDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="createLoading" @click="handleCreate">确定</el-button>
      </template>
    </el-dialog>

    <!-- 供应商采购统计弹窗 -->
    <el-dialog
      v-model="summaryDialogVisible"
      title="供应商累计采购统计"
      width="900px"
    >
      <el-table :data="supplierSummary" style="width: 100%" v-loading="summaryLoading" row-key="supplierId">
        <el-table-column type="expand">
          <template #default="{ row }">
            <div class="top-products-panel">
              <div class="top-products-title">Top 5 采购商品（按采购额降序）</div>
              <el-table :data="row.topProducts" size="small" border>
                <el-table-column type="index" label="排名" width="60" />
                <el-table-column prop="productName" label="商品名称" min-width="180" show-overflow-tooltip />
                <el-table-column prop="quantity" label="采购数量" width="100" align="right" />
                <el-table-column prop="orderCount" label="采购单数" width="100" align="center" />
                <el-table-column label="采购额" width="140" align="right">
                  <template #default="{ row: productRow }">
                    <span class="amount-text">¥{{ Number(productRow.totalAmount).toFixed(2) }}</span>
                  </template>
                </el-table-column>
              </el-table>
            </div>
          </template>
        </el-table-column>
        <el-table-column type="index" label="排名" width="60" />
        <el-table-column prop="supplierName" label="供应商" min-width="180" />
        <el-table-column prop="orderCount" label="采购单数" width="100" align="center" />
        <el-table-column prop="productCount" label="采购商品数" width="110" align="center" />
        <el-table-column label="累计采购额" width="140" align="right">
          <template #default="{ row }">
            <span class="amount-text">¥{{ Number(row.totalAmount).toFixed(2) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="最近采购时间" width="160" align="center">
          <template #default="{ row }">
            {{ row.lastPurchaseTime ? formatDate(row.lastPurchaseTime) : '-' }}
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, View, DataAnalysis, Plus } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import {
  getPurchaseOrders,
  getPurchaseOrder,
  createPurchaseOrder,
  getSupplierPurchaseSummary
} from '@/api/purchase'
import { getSuppliers, getProductsBySupplier } from '@/api/supplier'
import { getProductOptions } from '@/api/inventory-ops'
import type { PurchaseOrder, PurchaseType, SupplierPurchaseSummary } from '@/api/purchase/types'
import type { Supplier } from '@/api/supplier/types'

const systemConfigStore = useSystemConfigStore()

// 供应商和商品下拉选项
const supplierOptions = ref<Supplier[]>([])
const productOptions = ref<{ id: number; name: string; code: string; unit?: string }[]>([])

// 新增弹窗：按供应商ID缓存的商品选项（key=supplierId）
// 切换供应商时按需加载，已缓存则跳过请求
// referencePrice 来自 ProductSupplier 关联表，用于选商品时自动带出建议采购价
// productType 用于按采购类型过滤商品下拉（零售商品采购->实物商品/样品/赠品，耗材采购->耗材/样品/赠品）
const productOptionsBySupplier = ref<Record<number, { id: number; name: string; code: string; referencePrice?: number; productType?: number }[]>>({})

// 采购类型 -> 可选商品类型 映射
// 1 零售商品采购 -> 实物商品(1)/样品(4)/赠品(5)
// 2 耗材采购 -> 耗材(3)/样品(4)/赠品(5)
// 样品/赠品作为通用采购品，在两种采购类型下均放行
const PURCHASE_TYPE_TO_PRODUCT_TYPES: Record<number, number[]> = {
  1: [1, 4, 5],
  2: [3, 4, 5]
}

// 按当前采购类型过滤供应商关联的商品选项
const getFilteredProducts = (supplierId: number | undefined) => {
  if (!supplierId) return []
  const all = productOptionsBySupplier.value[supplierId] || []
  const allowedTypes = PURCHASE_TYPE_TO_PRODUCT_TYPES[createForm.purchaseType] || []
  return all.filter(p => p.productType !== undefined && allowedTypes.includes(p.productType))
}

// 加载指定供应商关联的商品列表（用于新增弹窗明细行的商品下拉筛选）
const loadProductsBySupplier = async (supplierId: number) => {
  // 已缓存则直接复用，避免重复请求
  if (productOptionsBySupplier.value[supplierId]) return
  try {
    const list = await getProductsBySupplier(supplierId)
    // 仅缓存成功结果，失败时不写入以便下次重试
    productOptionsBySupplier.value[supplierId] = list.map(ps => ({
      id: ps.productId,
      name: ps.productName || '',
      code: ps.productCode || '',
      referencePrice: ps.referencePrice ?? undefined,
      productType: ps.productType
    }))
  } catch {
    ElMessage.error('加载该供应商的商品列表失败')
  }
}

// 明细行供应商变更：清空已选商品与单价（原商品可能不属于新供应商）并触发商品列表加载
const handleItemSupplierChange = (row: CreateFormItem) => {
  row.productId = undefined
  row.unitPrice = undefined
  if (row.supplierId) {
    loadProductsBySupplier(row.supplierId)
  }
}

// 明细行商品变更：从供应商-商品关联表带出参考采购价
// 无参考价时清空单价，避免残留上一个商品的价格；用户带出后仍可手动修改
const handleItemProductChange = (row: CreateFormItem) => {
  if (!row.supplierId || !row.productId) {
    row.unitPrice = undefined
    return
  }
  const product = productOptionsBySupplier.value[row.supplierId]?.find(p => p.id === row.productId)
  row.unitPrice = product?.referencePrice
}

// 搜索表单
const searchForm = reactive({
  supplierId: undefined as number | undefined,
  productId: undefined as number | undefined,
  orderNo: '',
  purchaseType: undefined as number | undefined,
  dateRange: [] as string[]
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<PurchaseOrder[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 加载下拉选项
const loadOptions = async () => {
  try {
    const [suppliers, products] = await Promise.all([
      getSuppliers({ pageSize: 100 }),
      getProductOptions()
    ])
    supplierOptions.value = suppliers.list
    productOptions.value = products
  } catch {
    // 选项加载失败不阻断主流程
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getPurchaseOrders({
      supplierId: searchForm.supplierId,
      productId: searchForm.productId,
      orderNo: searchForm.orderNo || undefined,
      purchaseType: searchForm.purchaseType as PurchaseType | undefined,
      orderDateStart: searchForm.dateRange?.[0] || undefined,
      orderDateEnd: searchForm.dateRange?.[1] || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载采购记录失败')
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
  searchForm.supplierId = undefined
  searchForm.productId = undefined
  searchForm.orderNo = ''
  searchForm.purchaseType = undefined
  searchForm.dateRange = []
  handleSearch()
}

// 详情弹窗
const detailDialogVisible = ref(false)
const detailLoading = ref(false)
const currentOrder = ref<PurchaseOrder | null>(null)

// 查看详情
const handleViewDetail = async (row: PurchaseOrder) => {
  detailDialogVisible.value = true
  detailLoading.value = true
  currentOrder.value = null
  try {
    currentOrder.value = await getPurchaseOrder(row.id)
  } catch {
    ElMessage.error('加载详情失败')
    detailDialogVisible.value = false
  } finally {
    detailLoading.value = false
  }
}

// ==================== 新增采购订单 ====================

interface CreateFormItem {
  supplierId: number | undefined
  productId: number | undefined
  quantity: number
  unitPrice: number | undefined
  productionDate: string
  shelfLifeDays: number | undefined
  expirationDate: string
  remark: string
}

const createDialogVisible = ref(false)
const createLoading = ref(false)

const createForm = reactive({
  orderDate: new Date().toISOString().split('T')[0],
  purchaseType: 1 as PurchaseType,
  remark: '',
  items: [] as CreateFormItem[]
})

// 采购类型切换时，已选商品可能不符合新类型（实物<->耗材），清空所有明细的商品选择与单价
// 避免残留不符合新采购类型的商品导致提交脏数据
watch(
  () => createForm.purchaseType,
  () => {
    createForm.items.forEach(item => {
      item.productId = undefined
      item.unitPrice = undefined
    })
  }
)

// 生产日期+保质期天数变化时自动计算过期日期
watch(
  () => createForm.items.map(i => [i.productionDate, i.shelfLifeDays]),
  () => {
    createForm.items.forEach(item => {
      if (item.productionDate && item.shelfLifeDays && item.shelfLifeDays > 0) {
        const date = new Date(item.productionDate)
        date.setDate(date.getDate() + item.shelfLifeDays)
        item.expirationDate = date.toISOString().split('T')[0]
      }
    })
  },
  { deep: true }
)

// 过期日期变更时（保质期已输入）反推计算生产日期
watch(
  () => createForm.items.map(i => i.expirationDate),
  () => {
    createForm.items.forEach(item => {
      if (item.expirationDate && item.shelfLifeDays && item.shelfLifeDays > 0) {
        const date = new Date(item.expirationDate)
        date.setDate(date.getDate() - item.shelfLifeDays)
        item.productionDate = date.toISOString().split('T')[0]
      }
    })
  },
  { deep: true }
)

// 计算明细小计
const computeSubtotal = (item: CreateFormItem): number => {
  const qty = item.quantity || 0
  const price = item.unitPrice || 0
  return Number((qty * price).toFixed(2))
}

// 计算采购总金额
const computedTotalAmount = computed(() => {
  return createForm.items.reduce((sum, item) => sum + computeSubtotal(item), 0)
})

// 添加明细行
const addCreateItem = () => {
  createForm.items.push({
    supplierId: undefined,
    productId: undefined,
    quantity: 1,
    unitPrice: undefined,
    productionDate: '',
    shelfLifeDays: undefined,
    expirationDate: '',
    remark: ''
  })
}

// 删除明细行
const removeCreateItem = (index: number) => {
  createForm.items.splice(index, 1)
}

// 打开新增弹窗
const handleAdd = () => {
  createForm.orderDate = new Date().toISOString().split('T')[0]
  createForm.purchaseType = 1
  createForm.remark = ''
  createForm.items = []
  addCreateItem()
  createDialogVisible.value = true
}

// 提交创建
const handleCreate = async () => {
  if (!createForm.orderDate) {
    ElMessage.warning('请选择采购日期')
    return
  }
  if (createForm.items.length === 0) {
    ElMessage.warning('请至少添加一条采购明细')
    return
  }
  for (const item of createForm.items) {
    if (!item.supplierId) {
      ElMessage.warning('请选择所有明细的供应商')
      return
    }
    if (!item.productId) {
      ElMessage.warning('请选择所有明细的商品')
      return
    }
    if (!item.quantity || item.quantity <= 0) {
      ElMessage.warning('采购数量必须大于0')
      return
    }
    // 样品(4)/赠品(5)允许采购单价为0，其他类型单价必须大于0
    const productType = productOptionsBySupplier.value[item.supplierId]?.find(p => p.id === item.productId)?.productType
    const isSampleOrGift = productType === 4 || productType === 5
    if (item.unitPrice === undefined || item.unitPrice < 0 || (!isSampleOrGift && item.unitPrice === 0)) {
      ElMessage.warning('请输入有效的采购单价')
      return
    }
  }

  createLoading.value = true
  try {
    await createPurchaseOrder({
      orderDate: createForm.orderDate,
      purchaseType: createForm.purchaseType,
      totalAmount: Number(computedTotalAmount.value.toFixed(2)),
      remark: createForm.remark || undefined,
      items: createForm.items.map(item => ({
        supplierId: item.supplierId!,
        productId: item.productId!,
        quantity: item.quantity,
        unitPrice: item.unitPrice!,
        productionDate: item.productionDate || undefined,
        shelfLifeDays: item.shelfLifeDays || undefined,
        expirationDate: item.expirationDate || undefined,
        remark: item.remark || undefined
      }))
    })
    ElMessage.success('创建成功')
    createDialogVisible.value = false
    loadData()
  } catch (error) {
    ElMessage.error((error as Error).message || '创建失败')
  } finally {
    createLoading.value = false
  }
}

// ==================== 供应商采购统计 ====================

const summaryDialogVisible = ref(false)
const summaryLoading = ref(false)
const supplierSummary = ref<SupplierPurchaseSummary[]>([])

const handleViewSummary = async () => {
  summaryDialogVisible.value = true
  summaryLoading.value = true
  try {
    supplierSummary.value = await getSupplierPurchaseSummary({
      supplierId: searchForm.supplierId,
      productId: searchForm.productId,
      orderNo: searchForm.orderNo || undefined,
      purchaseType: searchForm.purchaseType as PurchaseType | undefined,
      orderDateStart: searchForm.dateRange?.[0] || undefined,
      orderDateEnd: searchForm.dateRange?.[1] || undefined
    })
  } catch {
    ElMessage.error('加载统计数据失败')
  } finally {
    summaryLoading.value = false
  }
}

// ==================== 辅助函数 ====================

// 根据供应商ID获取名称
const getSupplierName = (supplierId: number): string => {
  const supplier = supplierOptions.value.find(s => s.id === supplierId)
  return supplier?.name || `供应商${supplierId}`
}

// 根据商品ID获取名称
const getProductName = (productId: number): string => {
  const product = productOptions.value.find(p => p.id === productId)
  return product?.name || `商品${productId}`
}

// 获取订单的供应商显示文本（去重，多个供应商显示"多供应商"标签）
const getSupplierDisplay = (order: PurchaseOrder): string => {
  const items = order.items || []
  if (items.length === 0) return '-'
  const supplierIds = [...new Set(items.map(i => i.supplierId))]
  if (supplierIds.length === 1) return getSupplierName(supplierIds[0])
  return `${getSupplierName(supplierIds[0])} 等${supplierIds.length}家`
}

// 获取订单的商品显示文本（去重，多个商品显示"商品1 等3种"）
const getProductsDisplay = (order: PurchaseOrder): string => {
  const items = order.items || []
  if (items.length === 0) return '-'
  const productIds = [...new Set(items.map(i => i.productId))]
  if (productIds.length === 1) return getProductName(productIds[0])
  return `${getProductName(productIds[0])} 等${productIds.length}种`
}

// 格式化日期（仅日期部分）
const formatDate = (dateStr: string): string => {
  if (!dateStr) return '-'
  return dateStr.split('T')[0]
}

// 获取状态文本
const getStatusText = (status: number): string => {
  const map: Record<number, string> = { 1: '已入库' }
  return map[status] || '未知'
}

// 获取状态标签类型
const getStatusTagType = (status: number): string => {
  const map: Record<number, string> = { 1: 'success' }
  return map[status] || 'info'
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
.purchase-order-management {
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
  padding: 16px 20px;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

/* 去掉 el-form-item 默认 margin-right，间距统一由 gap 控制 */
.search-form-inline :deep(.el-form-item) {
  margin-right: 0;
  margin-bottom: 0;
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
  color: var(--text-primary);
}

/* 详情区域标题 */
.detail-section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 20px 0 12px;
  padding-left: 8px;
  border-left: 3px solid var(--el-color-primary);
  display: flex;
  align-items: center;
}

/* 空明细提示 */
.empty-items-hint {
  text-align: center;
  padding: 24px;
  color: var(--text-tertiary, #909399);
  font-size: 13px;
}

/* 供应商统计弹窗：Top 商品展开面板
   弹窗为白色浅色风格（见 dark-theme.css .el-dialog），此处用浅色色板协调 */
.top-products-panel {
  padding: 12px 16px 16px 48px;
  background: #f9fafb;
}
.top-products-title {
  font-size: 13px;
  color: #4b5563;
  margin-bottom: 8px;
  font-weight: 600;
}
</style>
