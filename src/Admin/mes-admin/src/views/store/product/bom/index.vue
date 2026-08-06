<template>
  <div class="bom-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="服务项目">
            <el-input
              v-model="searchForm.serviceProductName"
              placeholder="请输入服务项目名称"
              clearable
              style="width: 200px"
            />
          </el-form-item>
          <el-form-item label="耗材商品">
            <el-input
              v-model="searchForm.consumableProductName"
              placeholder="请输入耗材商品名称"
              clearable
              style="width: 200px"
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
          新增 BOM
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
        :span-method="handleSpanMethod"
        :row-class-name="rowClassName"
        @cell-mouse-enter="handleCellMouseEnter"
        @cell-mouse-leave="handleCellMouseLeave"
        style="width: 100%"
      >
        <el-table-column prop="serviceProductName" label="服务项目" min-width="160" />
        <el-table-column prop="consumableProductName" label="耗材商品" min-width="160" />
        <el-table-column prop="consumableProductCode" label="耗材编码" width="120" />
        <el-table-column label="消耗数量" width="120" align="center">
          <template #default="{ row }">
            {{ row.quantity }} {{ row.unit || '' }}
          </template>
        </el-table-column>
        <el-table-column label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)">
              <el-icon><Delete /></el-icon>
              删除
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

    <!-- 新增/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑 BOM' : '新增 BOM'"
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="服务项目" prop="serviceProductId">
          <el-select
            v-model="formData.serviceProductId"
            placeholder="请选择服务项目"
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="item in serviceProductOptions"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="耗材商品" prop="consumableProductId">
          <el-select
            v-model="formData.consumableProductId"
            placeholder="请选择耗材商品"
            filterable
            style="width: 100%"
            @change="handleConsumableChange"
          >
            <el-option
              v-for="item in consumableOptions"
              :key="item.id"
              :label="`${item.name}（${item.code}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="消耗数量" prop="quantity">
          <el-input-number
            v-model="formData.quantity"
            :min="0"
            :precision="2"
            :step="1"
            placeholder="请输入单次服务消耗数量"
            style="width: 100%"
          />
          <span v-if="currentUnit" class="unit-suffix">{{ currentUnit }}</span>
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
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Edit, Delete } from '@element-plus/icons-vue'
import {
  getBomList,
  createBom,
  updateBom,
  deleteBom,
  getServiceProductOptions,
  getConsumableOptions
} from '@/api/bom'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { BomItem } from '@/api/bom/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  serviceProductName: '',
  consumableProductName: ''
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<BomItem[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 下拉选项
const serviceProductOptions = ref<{ id: number; name: string }[]>([])
const consumableOptions = ref<{ id: number; name: string; code: string; unit: string }[]>([])

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getBomList({
      serviceProductName: searchForm.serviceProductName || undefined,
      consumableProductName: searchForm.consumableProductName || undefined,
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
    const [services, consumables] = await Promise.all([
      getServiceProductOptions(),
      getConsumableOptions()
    ])
    serviceProductOptions.value = services
    consumableOptions.value = consumables
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
  searchForm.serviceProductName = ''
  searchForm.consumableProductName = ''
  handleSearch()
}

// 同组高亮：hover 某行时，同一服务项目的所有行一起高亮
// 解决合并单元格后，hover 非首行时第一列 td（属于首行 tr）不高亮的视觉断层
const hoveredServiceProductId = ref<number | null>(null)

const handleCellMouseEnter = (row: BomItem) => {
  hoveredServiceProductId.value = row.serviceProductId
}

const handleCellMouseLeave = () => {
  hoveredServiceProductId.value = null
}

const rowClassName = ({ row }: { row: BomItem }) => {
  if (hoveredServiceProductId.value !== null && row.serviceProductId === hoveredServiceProductId.value) {
    return 'group-hover-row'
  }
  return ''
}

// 合并相同服务项目的单元格（视觉上按服务项目分组）
const handleSpanMethod = ({ row, rowIndex, columnIndex }: {
  row: BomItem
  rowIndex: number
  columnIndex: number
}) => {
  // 仅合并第一列（服务项目名称）
  if (columnIndex === 0) {
    const prevRow = rowIndex > 0 ? tableData.value[rowIndex - 1] : null
    if (!prevRow || prevRow.serviceProductId !== row.serviceProductId) {
      // 计算当前服务项目连续出现的行数
      let span = 1
      for (let i = rowIndex + 1; i < tableData.value.length; i++) {
        if (tableData.value[i].serviceProductId === row.serviceProductId) {
          span++
        } else {
          break
        }
      }
      return { rowspan: span, colspan: 1 }
    }
    return { rowspan: 0, colspan: 0 }
  }
}

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  serviceProductId: undefined as number | undefined,
  consumableProductId: undefined as number | undefined,
  quantity: 0
})

const formRules: FormRules = {
  serviceProductId: [
    { required: true, message: '请选择服务项目', trigger: 'change' }
  ],
  consumableProductId: [
    { required: true, message: '请选择耗材商品', trigger: 'change' }
  ],
  quantity: [
    { required: true, message: '请输入消耗数量', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '消耗数量必须大于0', trigger: 'blur' }
  ]
}

// 当前选中耗材的单位
const currentUnit = computed(() => {
  const consumable = consumableOptions.value.find(c => c.id === formData.consumableProductId)
  return consumable?.unit || ''
})

// 耗材选择变化时同步单位展示
const handleConsumableChange = () => {
  // currentUnit 是计算属性，会自动更新
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.serviceProductId = undefined
  formData.consumableProductId = undefined
  formData.quantity = 0
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: BomItem) => {
  isEdit.value = true
  formData.id = row.id
  formData.serviceProductId = row.serviceProductId
  formData.consumableProductId = row.consumableProductId
  formData.quantity = row.quantity
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: BomItem) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除 BOM 项 "${row.serviceProductName} - ${row.consumableProductName}" 吗？`,
      '警告',
      {
        type: 'warning',
        confirmButtonText: '确定删除',
        cancelButtonText: '取消'
      }
    )
    await deleteBom(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        const payload = {
          serviceProductId: formData.serviceProductId!,
          consumableProductId: formData.consumableProductId!,
          quantity: formData.quantity
        }
        if (isEdit.value) {
          await updateBom({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createBom(payload)
          ElMessage.success('创建成功')
        }
        dialogVisible.value = false
        loadData()
      } catch (error: any) {
        ElMessage.error(error.message || '操作失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
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
.bom-management {
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

/* 同组高亮：hover 某行时，同组所有行的单元格一起高亮 */
:deep(.el-table__body tr.group-hover-row > td.el-table__cell) {
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

/* 单位后缀 */
.unit-suffix {
  margin-left: 8px;
  color: var(--text-tertiary);
  font-size: 14px;
}
</style>
