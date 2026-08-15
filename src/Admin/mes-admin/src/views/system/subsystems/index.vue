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
        <el-table-column prop="name" label="子系统名称" min-width="120" />
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
              <el-icon><MenuIcon /></el-icon>
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
import { ref, reactive, onMounted, nextTick, computed } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules, type TreeInstance } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Menu as MenuIcon, Picture } from '@element-plus/icons-vue'
import {
  createSubsystem, updateSubsystem, deleteSubsystem,
  getSubsystemMenus, assignSubsystemMenus, getMenus,
  type Subsystem, type SubsystemCreate, type SubsystemUpdate,
  type Menu
} from '@/api/system'
import { useUserStore } from '@/stores/user'
import IconPicker from '@/components/IconPicker/index.vue'
import { useSortAutoFill } from '@/composables/useSortAutoFill'

const userStore = useUserStore()

// 排序自动填充
const { autoSort, calculateAutoSort } = useSortAutoFill(
  () => tableData.value,
  () => null // 子系统无层级，parentId 始终为 null
)

// 菜单类型使用 api/system/types 中定义的 Menu 类型

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
  code: [
    { required: true, message: '请输入子系统编码', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '子系统编码只能包含字母、数字、下划线', trigger: 'blur' }
  ],
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

// 获取授权的子系统列表（从 userStore 获取已过滤的数据）
const authorizedSubsystems = computed(() => userStore.authorizedSubsystems)

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    // 使用权限过滤后的子系统数据
    let filteredData = authorizedSubsystems.value

    // 应用搜索过滤
    if (searchForm.name) {
      filteredData = filteredData.filter(s => s.name?.includes(searchForm.name))
    }
    if (searchForm.code) {
      filteredData = filteredData.filter(s => s.code?.includes(searchForm.code))
    }
    if (searchForm.status !== undefined) {
      filteredData = filteredData.filter(s => s.status === searchForm.status)
    }

    tableData.value = filteredData
  } catch (error) {
    ElMessage.error('加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 加载菜单树
const loadMenuTree = async (checkedIds?: number[]) => {
  try {
    const res = await getMenus({})
    menuTree.value = buildTree(res)
    // 父子联动模式下只需设置叶子节点，父节点会根据子节点状态自动呈现"勾选/半勾选/不勾选"
    // 后端 GetMenusAsync 会合并祖先ID返回，若直接 setCheckedKeys 会让父节点强制勾选所有子节点
    if (checkedIds && menuTreeRef.value) {
      await nextTick()
      const leafIds = collectLeafSelectedIds(menuTree.value, checkedIds)
      menuTreeRef.value.setCheckedKeys(leafIds)
    }
  } catch (error) {
    // 加载菜单树失败
  }
}

// 从 checkedIds 中过滤出叶子节点ID
// 父子联动模式下只需设置叶子节点，父节点会根据子节点状态自动呈现"勾选/半勾选/不勾选"
const collectLeafSelectedIds = (menus: Menu[], checkedIds: number[]): number[] => {
  const result: number[] = []
  const checkedSet = new Set(checkedIds)
  const traverse = (nodes: Menu[]) => {
    nodes.forEach(node => {
      const hasChildren = node.children && node.children.length > 0
      if (!hasChildren) {
        if (checkedSet.has(node.id)) {
          result.push(node.id)
        }
      } else {
        traverse(node.children!)
      }
    })
  }
  traverse(menus)
  return result
}

// 构建树形结构
const buildTree = (list: Menu[]): Menu[] => {
  const map: Record<number, Menu> = {}
  const result: Menu[] = []
  list.forEach(item => {
    map[item.id] = { ...item, children: [] }
  })
  list.forEach(item => {
    if (item.parentId === 0 || !item.parentId) {
      result.push(map[item.id])
    } else if (map[item.parentId]) {
      map[item.parentId!].children!.push(map[item.id])
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
const handleAdd = async () => {
  isEdit.value = false
  resetForm()
  // 计算自动填充排序值
  await calculateAutoSort()
  formData.sort = autoSort.value
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
    // 刷新用户store中的子系统列表
    await userStore.getAuthorizedSubsystems()
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
    menuDialogVisible.value = true
    // 在弹窗显示后加载菜单树并设置选中状态
    await loadMenuTree(menuIds)
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
        // 刷新用户store中的子系统列表
        await userStore.getAuthorizedSubsystems()
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
  // 只保存叶子节点（没有子节点的菜单），不保存半选的父节点
  // 同时去重避免重复键错误
  const leafNodeIds = [...new Set(checkedNodes
    .filter((n: any) => !n.children || n.children.length === 0)
    .map((n: any) => n.id))]

  menuSubmitLoading.value = true
  try {
    await assignSubsystemMenus(currentSubsystem.value.id, { menuIds: leafNodeIds })
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

/* 菜单树 - 浅色弹窗版 */
.menu-tree-container {
  max-height: 400px;
  overflow-y: auto;
  padding: 12px;
  background: #f9fafb;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

:deep(.el-tree) {
  background: transparent;
  color: #1f2937;
}

:deep(.el-tree-node__content) {
  background: transparent;
  border-radius: 4px;
  height: 36px;
  color: #1f2937;
}

:deep(.el-tree-node__content:hover) {
  background: #f3f4f6;
}

:deep(.el-tree-node.is-current > .el-tree-node__content) {
  background: rgba(6, 212, 228, 0.1);
  color: #06b6d4;
}

:deep(.el-tree-node__label) {
  color: #1f2937;
}

:deep(.el-tree-node__expand-icon) {
  color: #9ca3af;
}

:deep(.el-tree-node__expand-icon.expanded) {
  color: #06b6d4;
}

:deep(.el-checkbox__inner) {
  background: #ffffff;
  border-color: #d1d5db;
}

:deep(.el-checkbox__input.is-checked .el-checkbox__inner) {
  background: #06b6d4;
  border-color: #06b6d4;
}

:deep(.el-checkbox__label) {
  color: #1f2937;
}

/* 表单样式 */
:deep(.el-form-item__label) {
  color: #4b5563;
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
