<template>
  <div class="category-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="分类名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入分类名称"
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
          新增分类
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
        :data="filteredTreeData"
        row-key="id"
        :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
        style="width: 100%"
      >
        <el-table-column prop="name" label="分类名称" min-width="240" />
        <el-table-column prop="sort" label="排序" width="100" align="center" />
        <el-table-column label="操作" width="220" fixed="right">
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
      :title="isEdit ? '编辑分类' : '新增分类'"
      width="500px"
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
            placeholder="请选择父分类（不选为顶级分类）"
            check-strictly
            clearable
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="分类名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入分类名称" />
        </el-form-item>
        <el-form-item label="排序" prop="sort">
          <el-input-number
            v-model="formData.sort"
            :min="0"
            :step="1"
            controls-position="right"
            style="width: 100%"
          />
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
import {
  getCategoryTree,
  createCategory,
  updateCategory,
  deleteCategory
} from '@/api/product'
import type { ProductCategory } from '@/api/product/types'

// 搜索表单
const searchForm = reactive({
  name: ''
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<ProductCategory[]>([])
const tableRef = ref()

// 根据搜索关键字过滤树形数据
const filteredTreeData = computed(() => {
  if (!searchForm.name) return tableData.value
  const filterTree = (nodes: ProductCategory[]): ProductCategory[] => {
    const result: ProductCategory[] = []
    for (const node of nodes) {
      const children = node.children ? filterTree(node.children) : []
      const matched = node.name.includes(searchForm.name)
      if (matched || children.length > 0) {
        result.push({ ...node, children })
      }
    }
    return result
  }
  return filterTree(tableData.value)
})

// 分类选项（用于父分类选择，添加顶级选项）
const categoryOptions = computed<ProductCategory[]>(() => {
  return [
    { id: 0, name: '顶级分类', parentId: 0, sort: 0 },
    ...tableData.value
  ]
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    tableData.value = await getCategoryTree()
  } catch (error) {
    ElMessage.error('加载分类数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  // computed 自动响应搜索关键字
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
  parentId: 0,
  sort: 0
})

const formRules: FormRules = {
  name: [
    { required: true, message: '分类名称不能为空', trigger: 'blur' },
    { max: 50, message: '分类名称最多50个字符', trigger: 'blur' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.parentId = 0
  formData.sort = 0
}

// 新增
const handleAdd = (row?: ProductCategory) => {
  isEdit.value = false
  resetFormData()
  // 如果指定了父节点，设置为父分类
  if (row) {
    formData.parentId = row.id
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: ProductCategory) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.parentId = row.parentId
  formData.sort = row.sort
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: ProductCategory) => {
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
    await deleteCategory(row.id)
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
          name: formData.name,
          parentId: formData.parentId,
          sort: formData.sort
        }
        if (isEdit.value) {
          await updateCategory({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createCategory(payload)
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
.category-management {
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
