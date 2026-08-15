<template>
  <div class="menu-management">
    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleAdd(null)">
          <el-icon><Plus /></el-icon>
          新增菜单
        </el-button>
        <el-button @click="loadData">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
      </div>
    </div>

    <!-- 表格区域 -->
    <div class="card">
      <el-table
        v-loading="tableLoading"
        :data="tableData"
        row-key="id"
        :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
        style="width: 100%"
      >
        <el-table-column prop="name" label="菜单名称" min-width="180">
          <template #default="{ row }">
            <span :class="{ 'menu-type-icon': true, [`menu-type-${row.type}`]: true }">
              <el-icon v-if="row.type === 0"><FolderOpened /></el-icon>
              <el-icon v-else-if="row.type === 1"><Menu /></el-icon>
              <el-icon v-else-if="row.type === 2"><Link /></el-icon>
            </span>
            <span class="menu-name">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="icon" label="图标" width="80" align="center">
          <template #default="{ row }">
            <el-icon v-if="row.icon" size="20">
              <component :is="row.icon" />
            </el-icon>
            <span v-else class="text-tertiary">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="type" label="类型" width="80" align="center">
          <template #default="{ row }">
            <el-tag :type="getMenuTypeTag(row.type)" size="small">
              {{ getMenuTypeName(row.type) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="code" label="权限标识" min-width="150" />
        <el-table-column prop="path" label="路由路径" min-width="180" />
        <el-table-column prop="component" label="组件路径" min-width="180" />
        <el-table-column prop="sort" label="排序" width="80" align="center" />
        <el-table-column prop="isVisible" label="显示" width="60" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isVisible ? 'success' : 'info'" size="small">
              {{ row.isVisible ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="isCache" label="缓存" width="60" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isCache ? 'success' : 'info'" size="small">
              {{ row.isCache ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleAdd(row)">
              <el-icon><Plus /></el-icon>
              新增
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
      :title="isEdit ? '编辑菜单' : '新增菜单'"
      width="650px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="上级菜单">
          <el-cascader
            v-model="formData.parentId"
            :options="menuTreeOptions"
            :props="{ checkStrictly: true, value: 'id', label: 'name', emitPath: false }"
            placeholder="请选择上级菜单（顶级菜单请选择）"
            clearable
            style="width: 100%"
          />
        </el-form-item>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="菜单类型" prop="type">
              <el-radio-group v-model="formData.type" @change="handleTypeChange">
                <el-radio :value="0">目录</el-radio>
                <el-radio :value="1">菜单</el-radio>
                <el-radio :value="2">按钮</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="菜单图标" v-if="formData.type !== 2">
              <div class="icon-picker-wrapper">
                <div class="selected-icon" @click="openIconPicker">
                  <el-icon v-if="formData.icon" size="18">
                    <component :is="formData.icon" />
                  </el-icon>
                  <span v-else class="placeholder">点击选择图标</span>
                </div>
                <el-button v-if="formData.icon" type="danger" link @click="formData.icon = ''">
                  <el-icon><Close /></el-icon>
                </el-button>
              </div>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="菜单名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入菜单名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="权限标识" prop="code">
              <el-input v-model="formData.code" placeholder="请输入权限标识" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20" v-if="formData.type !== 2">
          <el-col :span="12">
            <el-form-item label="路由路径" prop="path">
              <el-input v-model="formData.path" placeholder="请输入路由路径" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="组件路径" v-if="formData.type === 1">
              <el-input v-model="formData.component" placeholder="请输入组件路径" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="显示" v-if="formData.type !== 2">
              <el-radio-group v-model="formData.isVisible" class="radio-group-inline">
                <el-radio :value="1">是</el-radio>
                <el-radio :value="0">否</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="缓存" v-if="formData.type === 1">
              <el-radio-group v-model="formData.isCache" class="radio-group-inline">
                <el-radio :value="1">是</el-radio>
                <el-radio :value="0">否</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="排序" prop="sort">
          <el-input-number v-model="formData.sort" :min="0" :max="9999" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 图标选择器 -->
    <IconPicker
      v-model:visible="iconPickerVisible"
      v-model="formData.icon"
      :show-upload="false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, watch } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Plus, Refresh, Edit, Delete, Menu, FolderOpened, Link, Close } from '@element-plus/icons-vue'
import IconPicker from '@/components/IconPicker/index.vue'
import { getMenuTree, createMenu, updateMenu, deleteMenu } from '@/api/system'
import { useSortAutoFill } from '@/composables/useSortAutoFill'
import type { Menu as MenuData, MenuCreate, MenuUpdate } from '@/api/system/types'

// 兼容旧代码：MenuType 在此处实际指 MenuData 接口（包含 children）
 
type MenuType = MenuData

// 表格数据
const tableLoading = ref(false)
const tableData = ref<MenuType[]>([])

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const iconPickerVisible = ref(false)

const formData = reactive({
  id: 0,
  parentId: 0,
  name: '',
  path: '',
  component: '',
  code: '',
  icon: '',
  type: 1 as number,
  sort: 0,
  isVisible: 1,
  isCache: 0,
  isAffix: 0,
  permission: ''
})

// 排序自动填充
const { autoSort, calculateAutoSort } = useSortAutoFill(
  () => tableData.value,
  () => formData.parentId
)

// 监听 parentId 变化，切换上级菜单时自动更新排序值（仅新增时有效）
watch(() => formData.parentId, async () => {
  if (!dialogVisible.value || isEdit.value) return
  await calculateAutoSort()
  formData.sort = autoSort.value
})

const formRules: FormRules = {
  name: [{ required: true, message: '请输入菜单名称', trigger: 'blur' }],
  code: [{ required: true, message: '请输入权限标识', trigger: 'blur' }],
  path: [{ required: true, message: '请输入路由路径', trigger: 'blur' }]
}

// 菜单树选项（用于级联选择）- 仅支持3级，禁用第3级作为父级
interface MenuTreeOption {
  id: number
  name: string
  disabled: boolean
  children: MenuTreeOption[]
}
const menuTreeOptions = computed(() => {
  const processMenu = (menu: MenuType, level: number): MenuTreeOption => {
    // 第3级及以上禁用（只能选到第2级作为父级）
    const disabled = level >= 2
    return {
      id: menu.id,
      name: menu.name,
      disabled,
      children: menu.children && menu.children.length > 0
        ? menu.children.map(child => processMenu(child, level + 1))
        : []
    }
  }
  return [
    { id: 0, name: '顶级菜单', children: [], disabled: false } as MenuTreeOption,
    ...tableData.value.map(menu => processMenu(menu, 0))
  ]
})

// 菜单类型映射（与后端 MenuType 枚举一致：0-目录，1-菜单，2-按钮）
const getMenuTypeName = (type: number) => {
  const map: Record<number, string> = {
    0: '目录',
    1: '菜单',
    2: '按钮'
  }
  return map[type] || '未知'
}

const getMenuTypeTag = (type: number) => {
  const map: Record<number, string> = {
    0: 'warning',
    1: 'primary',
    2: 'success'
  }
  return map[type] || 'info'
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getMenuTree()
    tableData.value = res
  } catch {
    ElMessage.error('加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 打开图标选择器
const openIconPicker = () => {
  iconPickerVisible.value = true
}

// 获取当前行的实际层级（从根节点计算）
const getRowLevel = (row: MenuType): number => {
  // 通过查找 row 在树中的位置来计算层级
  const findLevel = (menus: MenuType[], target: MenuType, currentLevel: number): number => {
    for (const menu of menus) {
      if (menu.id === target.id) return currentLevel
      if (menu.children && menu.children.length > 0) {
        const found = findLevel(menu.children, target, currentLevel + 1)
        if (found >= 0) return found
      }
    }
    return -1
  }
  return findLevel(tableData.value, row, 0)
}

// 新增
const handleAdd = async (parent: MenuType | null) => {
  // 检查父级菜单层级，第3级（索引2）及以上不允许新增
  if (parent) {
    const parentLevel = getRowLevel(parent)
    if (parentLevel >= 2) {
      ElMessage.warning('菜单仅支持3级，第3级无法再新增子菜单')
      return
    }
  }

  isEdit.value = false
  resetForm()
  formData.parentId = parent?.id || 0
  // 根据父级类型自动设置子级类型
  // 0-目录，1-菜单，2-按钮
  if (parent) {
    if (parent.type === 0) {
      formData.type = 1 // 目录下的菜单
    } else if (parent.type === 1) {
      formData.type = 2 // 菜单下的按钮
    }
  }
  // 计算自动填充排序值
  await calculateAutoSort()
  formData.sort = autoSort.value
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: MenuType) => {
  isEdit.value = true
  formData.id = row.id
  formData.parentId = row.parentId || 0
  formData.name = row.name || ''
  formData.path = row.path || ''
  formData.component = row.component || ''
  formData.code = row.code || ''
  formData.icon = row.icon || ''
  formData.type = typeof row.type === 'number' ? row.type : parseInt(row.type) || 1
  formData.sort = row.sort || 0
  formData.isVisible = row.isVisible ? 1 : 0
  formData.isCache = row.isCache ? 1 : 0
  formData.isAffix = row.isAffix || 0
  formData.permission = row.permission || ''
  dialogVisible.value = true
}

// 递归查找指定 id 的菜单节点（用字符串比较避免大数精度丢失）
const findNode = (nodes: MenuType[], id: number | string): MenuType | null => {
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

// 删除
const handleDelete = async (row: MenuType) => {
  // 从原始数据中查找完整节点，避免搜索过滤导致 children 不完整
  const fullNode = findNode(tableData.value, row.id)
  if (fullNode?.children && fullNode.children.length > 0) {
    ElMessage.warning(`菜单"${row.name}"包含 ${fullNode.children.length} 个子菜单，请先删除子菜单后再删除`)
    return
  }
  try {
    await ElMessageBox.confirm(`确定要删除菜单 "${row.name}" 吗？此操作不可恢复！`, '提示', {
      type: 'warning'
    })
    await deleteMenu(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '删除失败')
    }
  }
}

// 类型变化
const handleTypeChange = (type: number) => {
  if (type === 2) {
    // 按钮不需要这些属性（0-目录，1-菜单，2-按钮）
    formData.path = ''
    formData.component = ''
    formData.icon = ''
    formData.isVisible = 1
    formData.isCache = 0
    formData.isAffix = 0
  } else {
    formData.permission = ''
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
          const data = {
            id: formData.id,
            parentId: formData.parentId === 0 ? null : formData.parentId,
            name: formData.name,
            path: formData.path || undefined,
            component: formData.component || undefined,
            code: formData.code,
            icon: formData.icon || undefined,
            type: formData.type,
            sort: formData.sort,
            status: 1,
            isVisible: formData.isVisible === 1,
            isCache: formData.isCache === 1,
            permissionCode: formData.permission || undefined
          }
          await updateMenu(data as unknown as MenuUpdate)
          ElMessage.success('更新成功')
        } else {
          const data = {
            parentId: formData.parentId === 0 ? null : formData.parentId,
            name: formData.name,
            path: formData.path || undefined,
            component: formData.component || undefined,
            code: formData.code,
            icon: formData.icon || undefined,
            type: formData.type,
            sort: formData.sort,
            status: 1,
            isVisible: formData.isVisible === 1,
            isCache: formData.isCache === 1,
            permissionCode: formData.permission || undefined
          }
          await createMenu(data as unknown as MenuCreate)
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

// 重置表单
const resetForm = () => {
  formData.id = 0
  formData.parentId = 0
  formData.name = ''
  formData.path = ''
  formData.component = ''
  formData.code = ''
  formData.icon = ''
  formData.type = 1
  formData.sort = 0
  formData.isVisible = 1
  formData.isCache = 0
  formData.isAffix = 0
  formData.permission = ''
}

onMounted(() => {
  loadData()
})
</script>

<style scoped>
.menu-management {
  width: 100%;
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

/* 按钮基础样式 */
:deep(.el-button) {
  transition: all 0.3s ease;
}

:deep(.el-button + .el-button) {
  margin-left: 8px;
}

/* 卡片 - 科技风 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  position: relative;
  overflow: hidden;
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
  background-color: transparent !important;
  color: var(--text-primary);
}

:deep(.el-table th.el-table__cell) {
  background-color: var(--bg-tertiary) !important;
  color: var(--text-tertiary) !important;
  font-weight: 600;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table td.el-table__cell) {
  background-color: var(--bg-tertiary) !important;
  color: var(--text-primary) !important;
  border-bottom: 1px solid var(--border-primary) !important;
}

:deep(.el-table__row) {
  background-color: var(--bg-tertiary) !important;
}

:deep(.el-table__row:hover > td.el-table__cell) {
  background-color: var(--bg-hover) !important;
}

:deep(.el-table::before),
:deep(.el-table__inner-wrapper::before) {
  display: none !important;
}

:deep(.el-table__body-wrapper) {
  background-color: transparent;
}

/* 树形表格展开图标 */
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

/* 菜单类型图标 */
.menu-type-icon {
  margin-right: 8px;
  vertical-align: middle;
}

.menu-type-0 {
  color: var(--warning);
}

.menu-type-1 {
  color: var(--primary);
}

.menu-type-2 {
  color: var(--success);
}

.menu-name {
  vertical-align: middle;
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

:deep(.el-tag--warning) {
  background: rgba(251, 191, 36, 0.15) !important;
  color: var(--warning) !important;
  border: none;
}

:deep(.el-tag--primary) {
  background: rgba(6, 212, 228, 0.15) !important;
  color: var(--primary) !important;
  border: none;
}

:deep(.el-tag--info) {
  background: rgba(91, 155, 255, 0.15) !important;
  color: var(--info) !important;
  border: none;
}

.text-tertiary {
  color: var(--text-tertiary);
}

/* 图标选择器包装器 */
.icon-picker-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
}

.selected-icon {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 12px;
  height: 32px;
  border: 1px solid var(--el-input-border-color, #dcdfe6);
  border-radius: var(--el-input-border-radius, 4px);
  background: var(--el-input-bg-color, #fff);
  cursor: pointer;
  transition: all 0.2s;
}

.selected-icon:hover {
  border-color: var(--el-input-focus-border-color, var(--primary));
}

.selected-icon .placeholder {
  color: var(--el-input-placeholder-color, #909399);
  font-size: 13px;
}

/* 单选按钮横排不换行 */
.radio-group-inline {
  display: flex;
  gap: 16px;
}

.radio-group-inline :deep(.el-radio) {
  margin-right: 0;
}

.radio-group-inline :deep(.el-radio__label) {
  padding-left: 6px;
}

/* 表单样式 - 浅色弹窗 */
:deep(.el-form-item__label) {
  color: #4b5563;
}

:deep(.el-form-item) {
  margin-bottom: 18px;
}
</style>