<template>
  <el-drawer
    :model-value="visible"
    :title="`${productName} - 库存流水`"
    size="70%"
    :close-on-click-modal="true"
    @update:model-value="handleVisibleChange"
  >
    <!-- 搜索区域 -->
    <div class="drawer-search">
      <el-form :inline="true" :model="searchForm">
        <el-form-item label="时间范围">
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
        <el-form-item label="来源类型">
          <el-select
            v-model="searchForm.sourceType"
            placeholder="全部"
            clearable
            style="width: 180px"
          >
            <el-option
              v-for="opt in inventoryLogSourceTypeOptions"
              :key="opt.value"
              :label="opt.label"
              :value="opt.value"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="批次号">
          <el-input
            v-model="searchForm.batchNo"
            placeholder="请输入批次号"
            clearable
            style="width: 180px"
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

    <!-- 表格 -->
    <el-table
      v-loading="loading"
      :data="tableData"
      border
      size="small"
      style="width: 100%"
    >
      <el-table-column label="时间" width="120">
        <template #default="{ row }">
          {{ formatDate(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="类型" width="80" align="center">
        <template #default="{ row }">
          <el-tag :type="row.quantity >= 0 ? 'success' : 'danger'" size="small" effect="dark">
            {{ row.quantity >= 0 ? '入库' : '出库' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="来源" width="100">
        <template #default="{ row }">
          {{ row.sourceType != null ? (inventoryLogSourceTypeMap[row.sourceType as InventoryLogSourceType] || '-') : '-' }}
        </template>
      </el-table-column>
      <el-table-column prop="batchNo" label="批次号" width="100" show-overflow-tooltip />
      <el-table-column label="单价" width="60" align="right">
        <template #default="{ row }">
          {{ row.unitPrice != null ? row.unitPrice : '-' }}
        </template>
      </el-table-column>
      <el-table-column label="数量" width="60" align="center">
        <template #default="{ row }">
          <span :class="row.quantity >= 0 ? 'qty-in' : 'qty-out'">
            {{ row.quantity > 0 ? '+' + row.quantity : row.quantity }}
          </span>
        </template>
      </el-table-column>
      <el-table-column label="操作前库存" width="100" align="center">
        <template #default="{ row }">
          {{ row.batchBeforeQuantity != null ? row.batchBeforeQuantity : '-' }}
        </template>
      </el-table-column>
      <el-table-column label="操作后库存" width="100" align="center">
        <template #default="{ row }">
          {{ row.batchAfterQuantity != null ? row.batchAfterQuantity : '-' }}
        </template>
      </el-table-column>
      <el-table-column label="商品总库存" width="100" align="center">
        <template #default="{ row }">
          {{ row.totalAfterQuantity != null ? row.totalAfterQuantity : '-' }}
        </template>
      </el-table-column>
      <el-table-column prop="operatorName" label="操作人" width="100" show-overflow-tooltip />
      <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
    </el-table>

    <!-- 分页 -->
    <div class="pagination-container">
      <el-pagination
        v-model:current-page="pagination.pageIndex"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="pagination.total"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadData"
        @current-change="loadData"
      />
    </div>
  </el-drawer>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { Search, Refresh } from '@element-plus/icons-vue'
import { getInventoryLogList, inventoryLogSourceTypeMap, inventoryLogSourceTypeOptions } from '@/api/inventory-ops'
import type { InventoryLog, InventoryLogSourceType } from '@/api/inventory-ops'
import { formatDateTime as formatDate } from '@/utils/date'

const props = defineProps<{
  visible: boolean
  productId: string | number
  productName: string
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
}>()

const loading = ref(false)
const tableData = ref<InventoryLog[]>([])

const searchForm = reactive({
  dateRange: [] as string[],
  sourceType: undefined as InventoryLogSourceType | undefined,
  batchNo: ''
})

const pagination = reactive({
  pageIndex: 1,
  pageSize: 20,
  total: 0
})


const loadData = async () => {
  if (!props.productId) return
  loading.value = true
  try {
    const res = await getInventoryLogList({
      productId: props.productId,
      startDate: searchForm.dateRange?.[0] || undefined,
      endDate: searchForm.dateRange?.[1] || undefined,
      sourceType: searchForm.sourceType,
      batchNo: searchForm.batchNo || undefined,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载流水失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

const handleReset = () => {
  searchForm.dateRange = []
  searchForm.sourceType = undefined
  searchForm.batchNo = ''
  handleSearch()
}

const handleVisibleChange = (val: boolean) => {
  emit('update:visible', val)
}

// 打开时重置筛选并加载第一页
watch(() => props.visible, (val) => {
  if (val) {
    searchForm.dateRange = []
    searchForm.sourceType = undefined
    searchForm.batchNo = ''
    pagination.pageIndex = 1
    loadData()
  }
})
</script>

<style scoped>
.drawer-search {
  margin-bottom: 16px;
}

.qty-in {
  color: var(--el-color-success);
  font-weight: 600;
}

.qty-out {
  color: var(--el-color-danger);
  font-weight: 600;
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 16px 0 0;
}
</style>
