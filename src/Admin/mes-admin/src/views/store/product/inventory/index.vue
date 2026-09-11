<template>
  <div class="inventory-management">
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
          <el-form-item label="商品类型">
            <el-select v-model="searchForm.productType" placeholder="全部" clearable style="width: 140px">
              <el-option label="实物商品" :value="1" />
              <el-option label="耗材" :value="3" />
              <el-option label="样品" :value="4" />
              <el-option label="赠品" :value="5" />
            </el-select>
          </el-form-item>
          <el-form-item label="商品分类">
            <el-tree-select
              v-model="searchForm.categoryId"
              :data="categoryTree"
              :props="{ label: 'name', children: 'children' }"
              node-key="id"
              placeholder="全部"
              check-strictly
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="库存状态">
            <el-select v-model="searchForm.inventoryStatus" placeholder="全部" clearable style="width: 120px">
              <el-option label="充足" :value="1" />
              <el-option label="偏低" :value="2" />
              <el-option label="不足" :value="3" />
              <el-option label="积压" :value="4" />
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
        <el-button v-if="hasPermission('store:purchase-inventory:inventory:export')" type="primary" :loading="exporting" @click="handleExport">
          <el-icon><Download /></el-icon>
          {{ selectedRows.length > 0 ? '导出勾选' : '导出全部' }}
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
        <el-table-column type="selection" width="48" />
        <el-table-column prop="productName" label="商品名称" min-width="130" />
        <el-table-column prop="productCode" label="商品编码" width="110" />
        <el-table-column label="类型" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="productTypeTagType(row.productType)" size="small" effect="dark">
              {{ productTypeText(row.productType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="categoryName" label="分类" width="110" />
        <el-table-column label="当前库存" width="90" align="center">
          <template #default="{ row }">
            <span :class="stockTextClass(row.inventoryStatus)">
              {{ row.quantity }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="alertQuantity" label="预警阈值" width="90" align="center" />
        <el-table-column prop="overstockThreshold" label="积压阈值" width="90" align="center" />
        <el-table-column label="库存状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="inventoryStatusTagType(row.inventoryStatus)" size="small" effect="dark">
              {{ inventoryStatusText(row.inventoryStatus) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="更新时间" width="160">
          <template #default="{ row }">
            {{ formatDate(row.updatedAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" align="center" fixed="right">
          <template #default="{ row }">
            <!-- 服务项目（Type=2）无实物批次，不显示详情按钮 -->
            <el-button v-if="row.productType !== 2" type="primary" link size="small" @click="handleDetail(row)">
              详情
            </el-button>
            <el-button type="primary" link size="small" @click="handleLog(row)">
              流水
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

    <!-- 商品批次详情弹窗 -->
    <el-dialog
      v-model="detailDialog.visible"
      :title="`${detailDialog.productName} - 库存批次详情`"
      width="900px"
      :close-on-click-modal="false"
    >
      <el-table
        v-loading="detailDialog.loading"
        :data="detailDialog.rows"
        border
        size="small"
        style="width: 100%"
      >
        <el-table-column prop="batchNo" label="批次号" width="140" />
        <el-table-column label="入库日期" width="110">
          <template #default="{ row }">
            {{ row.purchaseDate ? row.purchaseDate.split('T')[0] : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="生产日期" width="110">
          <template #default="{ row }">
            {{ row.productionDate ? row.productionDate.split('T')[0] : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="到期日期" width="110">
          <template #default="{ row }">
            {{ row.expirationDate ? row.expirationDate.split('T')[0] : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="quantity" label="库存数" width="90" align="center" />
        <el-table-column prop="unitPrice" label="单价" width="90" align="right" />
        <el-table-column label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="batchStatusTagType(row.status)" size="small" effect="dark">
              {{ batchStatusLabel(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
      </el-table>
    </el-dialog>

    <!-- 库存流水 Drawer -->
    <InventoryLogDrawer
      v-model:visible="logDrawer.visible"
      :product-id="logDrawer.productId"
      :product-name="logDrawer.productName"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh, Download } from '@element-plus/icons-vue'
import { getInventoryList, getInventoryBatchList } from '@/api/inventory'
import { getCategoryTree } from '@/api/product'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { Inventory, InventoryBatch, InventoryBatchStatus, ProductType, InventoryStatus } from '@/api/inventory/types'
import type { ProductCategory } from '@/api/product/types'
import InventoryLogDrawer from './components/InventoryLogDrawer.vue'
import { formatDateTime as formatDate } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 商品分类树（用于分类筛选）
const categoryTree = ref<ProductCategory[]>([])

// 搜索表单
const searchForm = reactive({
  productName: '',
  productType: undefined as ProductType | undefined,
  categoryId: undefined as number | undefined,
  inventoryStatus: undefined as InventoryStatus | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Inventory[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 商品类型文本映射
const productTypeText = (type: ProductType): string => {
  const map: Record<ProductType, string> = { 1: '实物商品', 2: '服务项目', 3: '耗材', 4: '样品', 5: '赠品' }
  return map[type] ?? '-'
}

// 商品类型标签颜色：服务项目/耗材用 info，样品/赠品用 success，实物商品用 primary
const productTypeTagType = (type: ProductType): 'primary' | 'info' | 'success' => {
  if (type === 4 || type === 5) return 'success'
  if (type === 2 || type === 3) return 'info'
  return 'primary'
}

// 库存状态文本映射
const inventoryStatusText = (status: InventoryStatus): string => {
  const map: Record<InventoryStatus, string> = { 1: '充足', 2: '偏低', 3: '不足', 4: '积压' }
  return map[status] ?? '未知'
}

// 库存状态标签颜色
const inventoryStatusTagType = (status: InventoryStatus): 'success' | 'warning' | 'danger' | 'info' => {
  const map: Record<InventoryStatus, 'success' | 'warning' | 'danger' | 'info'> = {
    1: 'success',
    2: 'warning',
    3: 'danger',
    4: 'info'
  }
  return map[status] || 'success'
}

// 库存数字样式：偏低/不足/积压分别高亮
const stockTextClass = (status: InventoryStatus): string => {
  if (status === 3) return 'stock-warn'
  if (status === 2) return 'stock-low'
  if (status === 4) return 'stock-over'
  return ''
}

// 加载分类树
const loadCategoryTree = async () => {
  try {
    categoryTree.value = await getCategoryTree()
  } catch {
    // 分类加载失败不阻塞主流程，静默处理
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getInventoryList({
      productName: searchForm.productName || undefined,
      productType: searchForm.productType,
      categoryId: searchForm.categoryId,
      inventoryStatus: searchForm.inventoryStatus,
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

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.productName = ''
  searchForm.productType = undefined
  searchForm.categoryId = undefined
  searchForm.inventoryStatus = undefined
  handleSearch()
}


// 批次状态映射：1-在库 2-已用完 3-已过期
const batchStatusLabel = (status: InventoryBatchStatus): string => {
  return { 1: '在库', 2: '已用完', 3: '已过期' }[status] ?? '-'
}
const batchStatusTagType = (status: InventoryBatchStatus): 'success' | 'info' | 'danger' => {
  return ({ 1: 'success', 2: 'info', 3: 'danger' } as const)[status] ?? 'info'
}

// 商品批次详情弹窗
const detailDialog = reactive({
  visible: false,
  loading: false,
  productName: '',
  rows: [] as InventoryBatch[]
})

// 打开详情弹窗并加载该商品的所有批次
const handleDetail = async (row: Inventory) => {
  detailDialog.productName = `${row.productName}（${row.productCode}）`
  detailDialog.visible = true
  detailDialog.loading = true
  detailDialog.rows = []
  try {
    const res = await getInventoryBatchList({
      productId: row.productId,
      pageSize: 100
    })
    detailDialog.rows = res.list
  } catch {
    ElMessage.error('加载批次详情失败')
  } finally {
    detailDialog.loading = false
  }
}

// 库存流水 Drawer
const logDrawer = reactive({
  visible: false,
  productId: 0 as number,
  productName: ''
})

// 打开流水 Drawer
const handleLog = (row: Inventory) => {
  logDrawer.productId = row.productId
  logDrawer.productName = `${row.productName}（${row.productCode}）`
  logDrawer.visible = true
}

// ==================== 导出功能 ====================

// 选中行（复选框勾选导出）
const selectedRows = ref<Inventory[]>([])
const handleSelectionChange = (rows: Inventory[]) => {
  selectedRows.value = rows
}

// 导出中状态
const exporting = ref(false)

// 纯日期格式化（yyyy-MM-dd），用于批次日期字段
const formatDateOnly = (dateStr?: string): string => {
  if (!dateStr) return ''
  return dateStr.split('T')[0]
}

// CSV 字段转义：含逗号、换行、双引号的字段用双引号包裹，内部双引号双写转义
const escapeCsvField = (value: unknown): string => {
  const str = value == null ? '' : String(value)
  if (/[",\n\r]/.test(str)) {
    return `"${str.replace(/"/g, '""')}"`
  }
  return str
}

// 导出：未勾选时导出搜索条件下的全量数据，勾选时仅导出勾选商品
// 同一商品的多个批次连续排列，商品维度字段仅在首行显示以体现商品维度划分
const handleExport = async () => {
  if (exporting.value) return
  exporting.value = true
  try {
    // 1. 确定要导出的商品列表
    let inventories: Inventory[]
    if (selectedRows.value.length > 0) {
      inventories = [...selectedRows.value]
    } else {
      // 按当前搜索条件逐页拉取全量数据
      inventories = []
      let pageIndex = 1
      const pageSize = 200
      while (true) {
        const res = await getInventoryList({
          productName: searchForm.productName || undefined,
          productType: searchForm.productType,
          categoryId: searchForm.categoryId,
          inventoryStatus: searchForm.inventoryStatus,
          pageIndex,
          pageSize
        })
        inventories.push(...res.list)
        if (inventories.length >= res.total || res.list.length === 0) break
        pageIndex++
      }
    }

    if (inventories.length === 0) {
      ElMessage.warning('没有可导出的数据')
      return
    }

    // 2. 拉取每个商品的批次详情（服务项目跳过批次拉取），组装 CSV 行
    const headers = [
      '商品名称', '商品编码', '类型', '分类', '当前库存', '预警阈值', '积压阈值', '库存状态', '更新时间',
      '批次号', '入库日期', '生产日期', '到期日期', '批次库存数', '单价', '状态', '备注'
    ]
    const lines: string[] = [headers.join(',')]
    let totalRows = 0

    for (const inv of inventories) {
      // 服务项目（Type=2）无批次，仅输出商品主信息行
      if (inv.productType === 2) {
        lines.push([
          inv.productName, inv.productCode, productTypeText(inv.productType), inv.categoryName ?? '',
          inv.quantity, inv.alertQuantity ?? '', inv.overstockThreshold ?? '',
          inventoryStatusText(inv.inventoryStatus), formatDate(inv.updatedAt ?? inv.createdAt),
          '', '', '', '', '', '', '', ''
        ].map(escapeCsvField).join(','))
        totalRows++
        continue
      }

      const res = await getInventoryBatchList({ productId: inv.productId, pageSize: 500 })
      const batches = res.list

      if (batches.length === 0) {
        // 无批次记录，仅输出商品主信息行（批次字段留空）
        lines.push([
          inv.productName, inv.productCode, productTypeText(inv.productType), inv.categoryName ?? '',
          inv.quantity, inv.alertQuantity ?? '', inv.overstockThreshold ?? '',
          inventoryStatusText(inv.inventoryStatus), formatDate(inv.updatedAt ?? inv.createdAt),
          '', '', '', '', '', '', '', ''
        ].map(escapeCsvField).join(','))
        totalRows++
      } else {
        batches.forEach((b, idx) => {
          lines.push([
            idx === 0 ? inv.productName : '',
            idx === 0 ? inv.productCode : '',
            idx === 0 ? productTypeText(inv.productType) : '',
            idx === 0 ? (inv.categoryName ?? '') : '',
            idx === 0 ? inv.quantity : '',
            idx === 0 ? (inv.alertQuantity ?? '') : '',
            idx === 0 ? (inv.overstockThreshold ?? '') : '',
            idx === 0 ? inventoryStatusText(inv.inventoryStatus) : '',
            idx === 0 ? formatDate(inv.updatedAt ?? inv.createdAt) : '',
            b.batchNo,
            formatDateOnly(b.purchaseDate),
            formatDateOnly(b.productionDate),
            formatDateOnly(b.expirationDate),
            b.quantity,
            b.unitPrice,
            batchStatusLabel(b.status),
            b.remark ?? ''
          ].map(escapeCsvField).join(','))
          totalRows++
        })
      }
    }

    // 3. 生成并下载 CSV（带 UTF-8 BOM 防止 Excel 中文乱码）
    const csv = '\uFEFF' + lines.join('\r\n')
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    const now = new Date()
    const pad = (n: number) => String(n).padStart(2, '0')
    const ts = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}`
    link.href = url
    link.download = `库存导出_${ts}.csv`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)

    ElMessage.success(`导出成功，共 ${totalRows} 条记录`)
  } catch {
    ElMessage.error('导出失败')
  } finally {
    exporting.value = false
  }
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  await loadCategoryTree()
  loadData()
})
</script>

<style scoped>
.inventory-management {
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

/* 库存数字样式 */
.stock-low {
  color: var(--el-color-warning);
  font-weight: 600;
}

.stock-warn {
  color: var(--el-color-danger);
  font-weight: 600;
}

.stock-over {
  color: var(--el-color-info);
  font-weight: 600;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
