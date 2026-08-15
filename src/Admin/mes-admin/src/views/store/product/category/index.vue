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
        :data="displayData"
        row-key="id"
        :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
        style="width: 100%"
      >
        <el-table-column prop="name" label="分类名称" min-width="240">
          <template #default="{ row }">
            <span :class="{ 'row-highlight': isRowHighlighted(row) }">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="code" label="分类编码" width="200">
          <template #default="{ row }">{{ row.code || '-' }}</template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="170">
          <template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
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
          <el-cascader
            v-model="formData.parentId"
            :options="categoryOptions"
            :props="{ checkStrictly: true, value: 'id', label: 'name', emitPath: false }"
            placeholder="请选择父分类（顶级分类请选择）"
            clearable
            style="width: 100%"
            :disabled="isEdit && formData.parentId === '0'"
          />
        </el-form-item>
        <el-form-item label="分类名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入分类名称" />
        </el-form-item>
        <el-form-item label="分类编码" prop="code">
          <el-input v-model="formData.code" placeholder="请输入分类编码" />
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
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
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

// 显示用数据（点击搜索后更新，保留匹配项及其父级路径）
const displayData = ref<ProductCategory[]>([])
// 高亮节点 id 集合（仅真正匹配搜索关键字的节点）
const highlightedIds = ref<Set<string>>(new Set())

// 递归过滤树形数据，同时收集匹配节点 id
const filterTree = (
  nodes: ProductCategory[],
  keyword: string,
  matchedIds: Set<string>
): ProductCategory[] => {
  const result: ProductCategory[] = []
  for (const node of nodes) {
    const children = node.children
      ? filterTree(node.children, keyword, matchedIds)
      : []
    const matched = node.name.includes(keyword)
    if (matched || children.length > 0) {
      result.push({ ...node, children })
      if (matched) {
        matchedIds.add(String(node.id))
      }
    }
  }
  return result
}

// 应用搜索：过滤数据 + 收集高亮 + 展开父级路径
const applySearch = async () => {
  if (!searchForm.name) {
    displayData.value = tableData.value
    highlightedIds.value = new Set()
    await nextTick(() => collapseAllRows())
    return
  }
  const matchedIds = new Set<string>()
  displayData.value = filterTree(tableData.value, searchForm.name, matchedIds)
  highlightedIds.value = matchedIds
  // 展开所有含子节点的行，使匹配结果所在的父级路径可见
  await nextTick(() => expandAllRows())
}

// 展开显示数据中所有含子节点的行
const expandAllRows = () => {
  if (!tableRef.value) return
  const expandRecursive = (data: ProductCategory[]) => {
    data.forEach((row) => {
      if (row.children && row.children.length > 0) {
        tableRef.value!.toggleRowExpansion(row, true)
        expandRecursive(row.children)
      }
    })
  }
  expandRecursive(displayData.value)
}

// 折叠所有行
const collapseAllRows = () => {
  if (!tableRef.value) return
  const rows = tableRef.value.store.states.data.value
  rows.forEach((row: ProductCategory) => {
    tableRef.value.toggleRowExpansion(row, false)
  })
}

// 判断行是否需要高亮
const isRowHighlighted = (row: ProductCategory): boolean => {
  return highlightedIds.value.has(String(row.id))
}

// 分类选项（用于父分类级联选择，固定包含"顶级分类"选项）
interface CategoryOption {
  id: number | string
  name: string
  children: CategoryOption[]
}
const categoryOptions = computed(() => {
  const processCategory = (cat: ProductCategory): CategoryOption => ({
    id: cat.id,
    name: cat.name,
    children: cat.children && cat.children.length > 0
      ? cat.children.map(child => processCategory(child))
      : []
  })
  return [
    { id: '0', name: '顶级分类', children: [] } as CategoryOption,
    ...tableData.value.map(cat => processCategory(cat))
  ]
})

// 递归查找指定 id 的分类节点（用字符串比较避免大数精度丢失）
const findNode = (nodes: ProductCategory[], id: number | string): ProductCategory | null => {
  const targetId = String(id)
  for (const node of nodes) {
    if (String(node.id) === targetId) return node
    if (node.children) {
      const found = findNode(node.children, id)
      if (found) return found
    }
  }
  return null
}

/**
 * 格式化日期时间（标准 ISO 字符串转 YYYY-MM-DD HH:mm:ss）
 */
const formatDateTime = (dateStr?: string): string => {
  if (!dateStr) return '-'
  const dt = new Date(dateStr)
  if (Number.isNaN(dt.getTime())) return dateStr
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())} ${pad(dt.getHours())}:${pad(dt.getMinutes())}:${pad(dt.getSeconds())}`
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    tableData.value = await getCategoryTree()
    // 加载完成后应用当前搜索条件，保持筛选状态一致
    await applySearch()
  } catch {
    ElMessage.error('加载分类数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  applySearch()
}

// 重置
const handleReset = () => {
  searchForm.name = ''
  applySearch()
}

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  code: '',
  parentId: '0' as string | number
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
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.parentId = '0'
}

// 新增
const handleAdd = (row?: ProductCategory) => {
  isEdit.value = false
  resetFormData()
  // 如果指定了父节点，设置为父分类（保持字符串 id 避免精度丢失）
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
  // 保持字符串 id 避免精度丢失，'0' 表示顶级分类
  formData.parentId = row.parentId
  formData.code = row.code || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: ProductCategory) => {
  // 从原始数据中查找完整节点，避免搜索过滤导致 children 不完整
  const fullNode = findNode(tableData.value, row.id)
  if (fullNode?.children && fullNode.children.length > 0) {
    ElMessage.warning(`分类"${row.name}"包含 ${fullNode.children.length} 个子分类，请先删除子分类后再删除`)
    return
  }
  try {
    await ElMessageBox.confirm(
      `确定要删除分类 "${row.name}" 吗？此操作不可恢复！`,
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
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '删除失败')
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
          code: formData.code || undefined,
          parentId: formData.parentId as number
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
      } catch (error) {
        ElMessage.error((error as Error).message || '操作失败')
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

/* 搜索高亮样式 */
.row-highlight {
  color: var(--primary) !important;
  font-weight: 600;
  background: rgba(6, 212, 228, 0.1);
  padding: 2px 6px;
  border-radius: 4px;
}
</style>
