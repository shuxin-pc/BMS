<template>
  <div class="equipment-type-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="类型名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入类型名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="类型编码">
            <el-input
              v-model="searchForm.code"
              placeholder="请输入类型编码"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.isActive" placeholder="全部" clearable style="width: 120px">
              <el-option label="启用" :value="true" />
              <el-option label="停用" :value="false" />
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
          新增设备类型
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
      <el-table v-loading="tableLoading" :data="tableData" style="width: 100%">
        <el-table-column prop="name" label="类型名称" min-width="140" show-overflow-tooltip />
        <el-table-column prop="code" label="类型编码" width="140" />
        <el-table-column prop="category" label="分类" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.category || '-' }}</template>
        </el-table-column>
        <el-table-column prop="spec" label="规格/型号" width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.spec || '-' }}</template>
        </el-table-column>
        <el-table-column prop="description" label="说明" min-width="160" show-overflow-tooltip>
          <template #default="{ row }">{{ row.description || '-' }}</template>
        </el-table-column>
        <el-table-column prop="equipmentCount" label="关联设备" width="100" align="center">
          <template #default="{ row }">{{ row.equipmentCount }}</template>
        </el-table-column>
        <el-table-column prop="isActive" label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isActive ? 'success' : 'info'" size="small" effect="dark">
              {{ row.isActive ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" fixed="right">
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
      :title="isEdit ? '编辑设备类型' : '新增设备类型'"
      width="560px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="类型名称" prop="name">
          <el-input v-model="formData.name" placeholder="如：飞顿激光、热玛吉" />
        </el-form-item>
        <el-form-item label="类型编码" prop="code">
          <el-input v-model="formData.code" placeholder="如：FEIDUN-LASER，字母数字-_" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="分类" prop="category">
          <el-input v-model="formData.category" placeholder="如：激光类、射频类、注射类" />
        </el-form-item>
        <el-form-item label="规格/型号" prop="spec">
          <el-input v-model="formData.spec" placeholder="请输入规格/型号" />
        </el-form-item>
        <el-form-item label="说明" prop="description">
          <el-input v-model="formData.description" type="textarea" :rows="3" placeholder="请输入备注说明" />
        </el-form-item>
        <el-form-item label="启用状态" prop="isActive">
          <el-switch
            v-model="formData.isActive"
            active-text="启用"
            inactive-text="停用"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import {
  getEquipmentTypeList,
  createEquipmentType,
  updateEquipmentType,
  deleteEquipmentType
} from '@/api/equipment-type'
import type { EquipmentType } from '@/api/equipment-type'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  isActive: undefined as boolean | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<EquipmentType[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  code: '',
  category: '',
  spec: '',
  description: '',
  isActive: true
})

const formRules: FormRules = {
  name: [
    { required: true, message: '类型名称不能为空', trigger: 'blur' },
    { max: 100, message: '类型名称最多100个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '类型编码不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '类型编码长度为2-50个字符', trigger: 'blur' },
    { pattern: /^[A-Za-z0-9_-]+$/, message: '只能包含字母、数字、下划线和中划线', trigger: 'blur' }
  ]
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getEquipmentTypeList({
      name: searchForm.name || undefined,
      code: searchForm.code || undefined,
      isActive: searchForm.isActive,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error: any) {
    ElMessage.error(error.message || '加载数据失败')
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
  searchForm.name = ''
  searchForm.code = ''
  searchForm.isActive = undefined
  handleSearch()
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.category = ''
  formData.spec = ''
  formData.description = ''
  formData.isActive = true
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: EquipmentType) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.category = row.category || ''
  formData.spec = row.spec || ''
  formData.description = row.description || ''
  formData.isActive = row.isActive
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: EquipmentType) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除设备类型 "${row.name}" 吗？被设备或服务项目引用的类型无法删除。`,
      '警告',
      {
        type: 'warning',
        confirmButtonText: '确定删除',
        cancelButtonText: '取消'
      }
    )
    await deleteEquipmentType(row.id)
    ElMessage.success('删除成功')
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
          code: formData.code,
          category: formData.category || undefined,
          spec: formData.spec || undefined,
          description: formData.description || undefined,
          isActive: formData.isActive
        }
        if (isEdit.value) {
          await updateEquipmentType({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createEquipmentType(payload)
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

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadData()
})
</script>

<style scoped>
.equipment-type-management {
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

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
