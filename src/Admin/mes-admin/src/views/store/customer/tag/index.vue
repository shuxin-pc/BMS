<template>
  <div class="tag-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="标签名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入标签名称"
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
    </div>

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增标签
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
        :data="filteredData"
        @selection-change="handleSelectionChange"
        style="width: 100%"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column prop="sort" label="排序" width="80" align="center" />
        <el-table-column prop="name" label="标签名称" min-width="160">
          <template #default="{ row }">
            <el-tag :type="(row.color as any) || 'primary'" size="small" effect="light">
              {{ row.name }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="color" label="颜色" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="(row.color as any) || 'primary'" size="small" effect="dark">
              {{ colorLabel(row.color) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
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
    </div>

    <!-- 新增/编辑弹窗 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑标签' : '新增标签'"
      width="520px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="90px"
      >
        <el-form-item label="标签名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入标签名称" />
        </el-form-item>
        <el-form-item label="标签颜色" prop="color">
          <div class="color-picker">
            <div
              v-for="item in colorOptions"
              :key="item.value"
              class="color-option"
              :class="{ active: formData.color === item.value }"
              @click="formData.color = item.value"
            >
              <el-tag :type="item.value as any" size="default" effect="dark">{{ item.label }}</el-tag>
            </div>
          </div>
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number
            v-model="formData.sort"
            :min="0"
            :step="1"
            :precision="0"
            controls-position="right"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注" />
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
import { Search, Refresh, Plus, Delete, Edit } from '@element-plus/icons-vue'
import { getAllCustomerTags, createCustomerTag, updateCustomerTag, deleteCustomerTag, batchDeleteCustomerTags } from '@/api/customer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { CustomerTag } from '@/api/customer/types'

const systemConfigStore = useSystemConfigStore()

// 颜色选项
const colorOptions = [
  { value: 'primary', label: '主色' },
  { value: 'success', label: '绿色' },
  { value: 'warning', label: '黄色' },
  { value: 'danger', label: '红色' },
  { value: 'info', label: '灰色' }
]

/** 颜色值转中文标签 */
const colorLabel = (color?: string) => {
  const item = colorOptions.find(c => c.value === color)
  return item ? item.label : '主色'
}

// 搜索表单
const searchForm = reactive({
  name: ''
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<CustomerTag[]>([])
const selectedRows = ref<CustomerTag[]>([])

// 前端过滤（标签列表不分页）
const filteredData = computed(() => {
  if (!searchForm.name) return tableData.value
  return tableData.value.filter(item => item.name.includes(searchForm.name))
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    tableData.value = await getAllCustomerTags()
  } catch (error: any) {
    ElMessage.error(error.message || '加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  // 前端过滤，无需重新加载
}

// 重置
const handleReset = () => {
  searchForm.name = ''
}

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  color: 'primary' as string,
  sort: 1,
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '标签名称不能为空', trigger: 'blur' },
    { max: 50, message: '标签名称最多50个字符', trigger: 'blur' }
  ],
  sort: [
    { required: true, message: '排序不能为空', trigger: 'blur' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.color = 'primary'
  formData.sort = 1
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  formData.sort = tableData.value.length + 1
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: CustomerTag) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.color = row.color || 'primary'
  formData.sort = row.sort
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: CustomerTag) => {
  try {
    await ElMessageBox.confirm(`确定要删除标签 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteCustomerTag(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '删除失败')
    }
  }
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个标签吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await batchDeleteCustomerTags(selectedRows.value.map(r => r.id))
    ElMessage.success('批量删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '删除失败')
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
          name: formData.name,
          color: formData.color,
          sort: formData.sort,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateCustomerTag({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createCustomerTag(payload)
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

// 选择行
const handleSelectionChange = (rows: CustomerTag[]) => {
  selectedRows.value = rows
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  loadData()
})
</script>

<style scoped>
.tag-management {
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

/* 颜色选择器 */
.color-picker {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.color-option {
  cursor: pointer;
  padding: 4px 8px;
  border-radius: var(--radius-base, 4px);
  border: 2px solid transparent;
  transition: border-color 0.2s;
}

.color-option.active {
  border-color: var(--el-color-primary);
}
</style>
