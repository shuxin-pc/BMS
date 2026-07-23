<template>
  <div class="skill-category-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="分类名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入分类名称或编码"
              clearable
              style="width: 200px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="启用" :value="1" />
              <el-option label="禁用" :value="0" />
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
          新增分类
        </el-button>
        <el-button @click="expandAll">
          <el-icon><Expand /></el-icon>
          展开全部
        </el-button>
        <el-button @click="collapseAll">
          <el-icon><Fold /></el-icon>
          折叠全部
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
        ref="tableRef"
        v-loading="tableLoading"
        :data="tableData"
        row-key="id"
        :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
        :default-expand-all="false"
        style="width: 100%"
      >
        <el-table-column prop="name" label="分类名称" min-width="240" />
        <el-table-column prop="code" label="分类编码" width="200" />
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">{{ row.remark || '-' }}</template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="操作" width="260" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleAdd(row)">
              <el-icon><Plus /></el-icon>
              新增子级
            </el-button>
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
      :title="isEdit ? '编辑技能分类' : '新增技能分类'"
      width="560px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="父分类">
          <el-tree-select
            v-model="formData.parentId"
            :data="categoryOptions"
            :props="{ label: 'name', children: 'children' }"
            node-key="id"
            placeholder="不选为顶级分类"
            check-strictly
            clearable
            style="width: 100%"
          />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="分类名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入分类名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="分类编码" prop="code">
              <el-input v-model="formData.code" placeholder="如 SKIN-CARE" :disabled="isEdit" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="状态" prop="status">
          <el-radio-group v-model="formData.status">
            <el-radio :value="1">启用</el-radio>
            <el-radio :value="0">禁用</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注" />
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
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Expand, Fold } from '@element-plus/icons-vue'
import {
  getSkillCategoryTree,
  createSkillCategory,
  updateSkillCategory,
  deleteSkillCategory
} from '@/api/skill'
import type { SkillCategory, SkillCategoryStatus } from '@/api/skill/types'

// 搜索表单
const searchForm = reactive({
  name: '',
  status: undefined as SkillCategoryStatus | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<SkillCategory[]>([])
const tableRef = ref()

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  code: '',
  parentId: 0,
  status: 1 as SkillCategoryStatus,
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '分类名称不能为空', trigger: 'blur' },
    { max: 50, message: '分类名称最多50个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '分类编码不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '分类编码长度为2-50个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_-]+$/, message: '分类编码只能包含字母、数字、下划线、横线', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
}

/**
 * 父分类选项（添加顶级选项）
 */
const categoryOptions = computed<SkillCategory[]>(() => {
  return [
    { id: 0, name: '顶级分类', code: '', parentId: 0, status: 1 },
    ...tableData.value
  ]
})

/**
 * 格式化日期时间
 */
const formatDateTime = (dateStr?: string): string => {
  if (!dateStr) return '-'
  return dateStr.replace('T', ' ')
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    tableData.value = await getSkillCategoryTree({
      name: searchForm.name || undefined,
      status: searchForm.status
    })
  } catch (error: any) {
    ElMessage.error(error.message || '加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.name = ''
  searchForm.status = undefined
  loadData()
}

// 展开全部
const expandAll = () => {
  toggleAllExpansion(true)
}

// 折叠全部
const collapseAll = () => {
  toggleAllExpansion(false)
}

/**
 * 递归切换所有节点展开状态
 * @param expanded 是否展开
 */
const toggleAllExpansion = (expanded: boolean) => {
  const traverse = (nodes: SkillCategory[]) => {
    nodes.forEach(node => {
      tableRef.value?.toggleRowExpansion(node, expanded)
      if (node.children && node.children.length > 0) {
        traverse(node.children)
      }
    })
  }
  traverse(tableData.value)
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.parentId = 0
  formData.status = 1
  formData.remark = ''
}

// 新增（可指定父节点）
const handleAdd = (row?: SkillCategory) => {
  isEdit.value = false
  resetFormData()
  if (row) {
    formData.parentId = row.id
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: SkillCategory) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.parentId = row.parentId
  formData.status = row.status
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: SkillCategory) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除分类 "${row.name}" 吗？如果包含子分类将一并删除，此操作不可恢复！`,
      '警告',
      {
        type: 'warning',
        confirmButtonText: '确定删除',
        cancelButtonText: '取消'
      }
    )
    await deleteSkillCategory(row.id)
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
          parentId: formData.parentId || 0,
          status: formData.status,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateSkillCategory({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createSkillCategory(payload)
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

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.skill-category-management {
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

/* 树形展开图标 */
:deep(.el-table__expand-icon) {
  color: var(--text-tertiary) !important;
  transition: all 0.3s;
}

:deep(.el-table__expand-icon:hover) {
  color: var(--primary) !important;
}

:deep(.el-table__expand-icon--expanded) {
  transform: rotate(90deg);
  color: var(--primary) !important;
}

:deep(.el-table::before),
:deep(.el-table__inner-wrapper::before) {
  display: none !important;
}

:deep(.el-table__body-wrapper) {
  background-color: transparent;
}
</style>
