<template>
  <div class="subsystem-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="子系统名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入子系统名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="子系统编码">
            <el-input
              v-model="searchForm.code"
              placeholder="请输入子系统编码"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="请选择状态" clearable style="width: 120px">
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
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新增子系统
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
        <el-table-column prop="code" label="子系统编码" min-width="120" />
        <el-table-column prop="name" label="子系统名称" min-width="120">
          <template #default="{ row }">
            <el-icon v-if="row.icon && !row.icon.startsWith('data:')" class="subsystem-icon">
              <component :is="row.icon" />
            </el-icon>
            <img
              v-else-if="row.icon && row.icon.startsWith('data:')"
              :src="row.icon"
              class="subsystem-icon-img"
            />
            <span>{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="icon" label="图标" width="80" align="center">
          <template #default="{ row }">
            <el-icon v-if="row.icon && !row.icon.startsWith('data:')" size="20">
              <component :is="row.icon" />
            </el-icon>
            <img
              v-else-if="row.icon && row.icon.startsWith('data:')"
              :src="row.icon"
              class="table-icon-img"
            />
            <span v-else class="text-tertiary">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
        <el-table-column prop="sort" label="排序" width="80" align="center" />
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleAssignMenus(row)">
              <el-icon><Menu /></el-icon>
              分配菜单
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
      :title="isEdit ? '编辑子系统' : '新增子系统'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="子系统编码" prop="code">
          <el-input v-model="formData.code" placeholder="请输入子系统编码" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="子系统名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入子系统名称" />
        </el-form-item>
        <el-form-item label="图标">
          <div class="icon-input-wrapper" @click="iconPickerVisible = true">
            <el-input
              v-model="formData.icon"
              placeholder="点击选择图标"
              readonly
              class="icon-input"
            >
              <template #prefix>
                <el-icon v-if="formData.icon && !formData.icon.startsWith('data:')">
                  <component :is="formData.icon" />
                </el-icon>
                <img
                  v-else-if="formData.icon && formData.icon.startsWith('data:')"
                  :src="formData.icon"
                  class="icon-preview-img"
                />
              </template>
            </el-input>
            <el-button type="primary" link @click.stop="iconPickerVisible = true">
              <el-icon><Picture /></el-icon>
              选择图标
            </el-button>
          </div>
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="formData.description" type="textarea" :rows="2" placeholder="请输入描述" />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="排序" prop="sort">
              <el-input-number v-model="formData.sort" :min="0" :max="9999" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态" prop="status">
              <el-radio-group v-model="formData.status">
                <el-radio :value="1">启用</el-radio>
                <el-radio :value="0">禁用</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 分配菜单弹窗 -->
    <el-dialog
      v-model="menuDialogVisible"
      title="分配菜单"
      width="600px"
      :close-on-click-modal="false"
    >
      <div class="menu-tree-container">
        <el-tree
          ref="menuTreeRef"
          :data="menuTree"
          :props="treeProps"
          show-checkbox
          node-key="id"
          :default-checked-keys="checkedMenuIds"
          :default-expand-all="true"
        />
      </div>
      <template #footer>
        <el-button @click="menuDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="menuSubmitLoading" @click="handleMenuSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 图标选择器 -->
    <IconPicker
      v-model="formData.icon"
      v-model:visible="iconPickerVisible"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules, type TreeInstance } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Menu, Picture } from '@element-plus/icons-vue'
import {
  getSubsystems, createSubsystem, updateSubsystem, deleteSubsystem,
  getSubsystemMenus, assignSubsystemMenus, getMenus,
  type Subsystem, type SubsystemCreate, type SubsystemUpdate
} from '@/api/system'
import IconPicker from '@/components/IconPicker/index.vue'

// 菜单类型定义
type Menu = {
  id: number
  parentId?: number
  name: string
  code: string
  type: number
  sort?: number
  status?: number
  icon?: string
  children?: Menu[]
}

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  status: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Subsystem[]>([])

// 弹窗 - 编辑/新增
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  code: '',
  name: '',
  icon: '',
  description: '',
  sort: 0,
  status: 1
})

const formRules: FormRules = {
  code: [{ required: true, message: '请输入子系统编码', trigger: 'blur' }],
  name: [{ required: true, message: '请输入子系统名称', trigger: 'blur' }]
}

// 弹窗 - 分配菜单
const menuDialogVisible = ref(false)
const menuSubmitLoading = ref(false)
const menuTreeRef = ref<TreeInstance>()
const menuTree = ref<Menu[]>([])
const checkedMenuIds = ref<number[]>([])
const currentSubsystem = ref<Subsystem | null>(null)

// 图标选择器
const iconPickerVisible = ref(false)

const treeProps = {
  children: 'children',
  label: 'name'
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

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getSubsystems({
      name: searchForm.name || undefined,
      code: searchForm.code || undefined,
      status: searchForm.status
    })
    tableData.value = res
  } catch (error) {
    ElMessage.error('加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 加载菜单树
const loadMenuTree = async () => {
  try {
    const res = await getMenus({})
    menuTree.value = buildTree(res)
  } catch (error) {
    console.error('加载菜单树失败', error)
  }
}

// 构建树形结构
const buildTree = (list: any[]): any[] => {
  const map: Record<number, any> = {}
  const result: any[] = []
  list.forEach(item => {
    map[item.id] = { ...item, children: [] }
  })
  list.forEach(item => {
    if (item.parentId === 0 || !item.parentId) {
      result.push(map[item.id])
    } else if (map[item.parentId]) {
      map[item.parentId].children.push(map[item.id])
    }
  })
  return result
}

// 搜索
const handleSearch = () => {
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.name = ''
  searchForm.code = ''
  searchForm.status = undefined
  handleSearch()
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetForm()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Subsystem) => {
  isEdit.value = true
  formData.id = row.id
  formData.code = row.code
  formData.name = row.name
  formData.icon = row.icon || ''
  formData.description = row.description || ''
  formData.sort = row.sort || 0
  formData.status = row.status
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Subsystem) => {
  try {
    await ElMessageBox.confirm(`确定要删除子系统 "${row.name}" 吗？`, '提示', {
      type: 'warning'
    })
    await deleteSubsystem(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '删除失败')
    }
  }
}

// 分配菜单
const handleAssignMenus = async (row: Subsystem) => {
  currentSubsystem.value = row
  try {
    const menuIds = await getSubsystemMenus(row.id)
    checkedMenuIds.value = menuIds
    await loadMenuTree()
    menuDialogVisible.value = true
  } catch (error) {
    ElMessage.error('加载菜单失败')
  }
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        if (isEdit.value) {
          const data: SubsystemUpdate = {
            id: formData.id,
            name: formData.name,
            icon: formData.icon || undefined,
            description: formData.description || undefined,
            sort: formData.sort,
            status: formData.status
          }
          await updateSubsystem(data)
          ElMessage.success('更新成功')
        } else {
          const data: SubsystemCreate = {
            code: formData.code,
            name: formData.name,
            icon: formData.icon || undefined,
            description: formData.description || undefined,
            sort: formData.sort,
            status: formData.status
          }
          await createSubsystem(data)
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

// 提交菜单分配
const handleMenuSubmit = async () => {
  if (!menuTreeRef.value || !currentSubsystem.value) return
  const checkedNodes = menuTreeRef.value.getCheckedNodes(false)
  const halfCheckedNodes = menuTreeRef.value.getHalfCheckedNodes()
  const allCheckedIds = [
    ...checkedNodes.map((n: any) => n.id),
    ...halfCheckedNodes.map((n: any) => n.id)
  ]

  menuSubmitLoading.value = true
  try {
    await assignSubsystemMenus(currentSubsystem.value.id, { menuIds: allCheckedIds })
    ElMessage.success('分配成功')
    menuDialogVisible.value = false
  } catch (error: any) {
    ElMessage.error(error.message || '操作失败')
  } finally {
    menuSubmitLoading.value = false
  }
}

// 重置表单
const resetForm = () => {
  formData.id = 0
  formData.code = ''
  formData.name = ''
  formData.icon = ''
  formData.description = ''
  formData.sort = 0
  formData.status = 1
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.subsystem-management {
  width: 100%;
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

/* 卡片 - 科技风 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  position: relative;
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

.card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--primary), transparent);
  opacity: 0;
  transition: opacity 0.3s;
}

.card:hover::before {
  opacity: 1;
}

/* 表格 - 科技风 */
:deep(.el-table) {
  border-radius: var(--radius-lg);
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
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table td.el-table__cell) {
  background-color: var(--bg-tertiary) !important;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table__row:hover > td.el-table__cell) {
  background-color: var(--bg-hover) !important;
}

/* 按钮基础样式 */
:deep(.el-button) {
  transition: all 0.3s ease;
}

/* 标签 - 科技风 */
:deep(.el-tag--success) {
  background: rgba(16, 250, 158, 0.15) !important;
  color: var(--success) !important;
  border: none;
}

:deep(.el-tag--danger) {
  background: rgba(255, 87, 87, 0.15) !important;
  color: var(--danger) !important;
  border: none;
}

.subsystem-icon {
  margin-right: 8px;
  vertical-align: middle;
  color: var(--primary);
}

.subsystem-icon-img {
  width: 16px;
  height: 16px;
  object-fit: contain;
  margin-right: 8px;
  vertical-align: middle;
}

.text-tertiary {
  color: var(--text-tertiary);
}

/* 菜单树 */
.menu-tree-container {
  max-height: 400px;
  overflow-y: auto;
  padding: 12px;
  background: var(--bg-secondary);
  border-radius: 8px;
  border: 1px solid var(--border-primary);
}

:deep(.el-tree) {
  background: transparent;
  color: var(--text-primary);
}

:deep(.el-tree-node__content) {
  background: transparent;
  border-radius: 4px;
  height: 32px;
  color: var(--text-primary);
}

:deep(.el-tree-node__content:hover) {
  background: var(--bg-hover);
}

:deep(.el-tree-node.is-current > .el-tree-node__content) {
  background: rgba(6, 212, 228, 0.15);
  color: var(--primary);
}

:deep(.el-checkbox__inner) {
  background: var(--bg-tertiary);
  border-color: var(--border-primary);
}

:deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
  background: var(--primary);
  border-color: var(--primary);
}

:deep(.el-checkbox__label) {
  color: var(--text-primary);
}

/* 表单样式 */
:deep(.el-form-item__label) {
  color: var(--text-secondary);
}

:deep(.el-form-item) {
  margin-bottom: 18px;
}

/* 图标输入包装器 */
.icon-input-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}

.icon-input {
  flex: 1;
}

.icon-input :deep(.el-input__wrapper) {
  cursor: pointer;
}

.icon-preview-img {
  width: 16px;
  height: 16px;
  object-fit: contain;
  margin-left: 4px;
}

.table-icon-img {
  width: 20px;
  height: 20px;
  object-fit: contain;
}
</style>
