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
        <el-button type="primary" @click="handleAdd()" v-if="hasPermission('store:equipment:type:add')">
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
      <el-table
        ref="tableRef"
        v-loading="tableLoading"
        :data="displayData"
        row-key="id"
        :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
        style="width: 100%"
      >
        <el-table-column prop="name" label="类型名称" min-width="200">
          <template #default="{ row }">
            <span :class="{ 'row-highlight': isRowHighlighted(row) }">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="code" label="类型编码" width="150" />
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
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleAdd(row)" v-if="hasPermission('store:equipment:type:add')">
              <el-icon><Plus /></el-icon>
              新增子级
            </el-button>
            <el-button link type="primary" size="small" @click="handleEdit(row)" v-if="hasPermission('store:equipment:type:edit')">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)" v-if="hasPermission('store:equipment:type:delete')">
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
        <el-form-item label="上级分类">
          <el-tree-select
            v-model="formData.parentId"
            :data="parentOptions"
            :props="{ label: 'name', children: 'children' }"
            node-key="id"
            placeholder="请选择上级分类（顶级分类请选择）"
            check-strictly
            clearable
            style="width: 100%"
            :disabled="isEdit && formData.parentId === '0'"
          />
        </el-form-item>
        <el-form-item label="类型名称" prop="name">
          <el-input v-model="formData.name" placeholder="如：激光类、飞顿激光、热玛吉" />
        </el-form-item>
        <el-form-item label="类型编码" prop="code">
          <el-input v-model="formData.code" placeholder="如：FEIDUN-LASER，字母数字-_" />
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
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit } from '@element-plus/icons-vue'
import {
  getEquipmentTypeTree,
  createEquipmentType,
  updateEquipmentType,
  deleteEquipmentType
} from '@/api/equipment-type'
import type { EquipmentType } from '@/api/equipment-type'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  isActive: undefined as boolean | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<EquipmentType[]>([])
const tableRef = ref()

// 显示用数据（点击搜索后更新，保留匹配项及其父级路径）
const displayData = ref<EquipmentType[]>([])
// 高亮节点 id 集合（仅真正匹配搜索关键字的节点）
const highlightedIds = ref<Set<string>>(new Set())

// 判断节点是否匹配当前搜索条件（名称/编码模糊 + 状态精确）
const isNodeMatched = (node: EquipmentType): boolean => {
  const keywordMatched =
    (!searchForm.name || node.name.includes(searchForm.name)) &&
    (!searchForm.code || node.code.includes(searchForm.code))
  const statusMatched = searchForm.isActive === undefined || node.isActive === searchForm.isActive
  return keywordMatched && statusMatched
}

// 递归过滤树形数据，同时收集匹配节点 id
const filterTree = (
  nodes: EquipmentType[],
  matchedIds: Set<string>
): EquipmentType[] => {
  const result: EquipmentType[] = []
  for (const node of nodes) {
    const children = node.children ? filterTree(node.children, matchedIds) : []
    const matched = isNodeMatched(node)
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
  const hasFilter = searchForm.name || searchForm.code || searchForm.isActive !== undefined
  if (!hasFilter) {
    displayData.value = tableData.value
    highlightedIds.value = new Set()
    await nextTick(() => collapseAllRows())
    return
  }
  const matchedIds = new Set<string>()
  displayData.value = filterTree(tableData.value, matchedIds)
  highlightedIds.value = matchedIds
  // 展开所有含子节点的行，使匹配结果所在的父级路径可见
  await nextTick(() => expandAllRows())
}

// 展开显示数据中所有含子节点的行
const expandAllRows = () => {
  if (!tableRef.value) return
  const expandRecursive = (data: EquipmentType[]) => {
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
  rows.forEach((row: EquipmentType) => {
    tableRef.value.toggleRowExpansion(row, false)
  })
}

// 判断行是否需要高亮
const isRowHighlighted = (row: EquipmentType): boolean => {
  return highlightedIds.value.has(String(row.id))
}

// 上级分类选项（用于上级分类级联选择，固定包含"顶级分类"选项）
interface ParentOption {
  id: number | string
  name: string
  children: ParentOption[]
}
// 上级分类选项：id 统一转为字符串（顶级哨兵 '0' 与真实节点同型），
// 使 el-tree-select 的 node-key 匹配与 v-model 回显保持一致，同时避免大数精度丢失
const parentOptions = computed(() => {
  const processNode = (node: EquipmentType): ParentOption => ({
    id: String(node.id),
    name: node.name,
    children: node.children && node.children.length > 0
      ? node.children.map(child => processNode(child))
      : []
  })
  return [
    { id: '0', name: '顶级分类', children: [] } as ParentOption,
    ...tableData.value.map(node => processNode(node))
  ]
})

// 递归查找指定 id 的类型节点（用字符串比较避免大数精度丢失）
const findNode = (nodes: EquipmentType[], id: number | string): EquipmentType | null => {
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

// 递归统计节点后代数量（用于停用联动确认文案）
const countDescendants = (node?: EquipmentType | null): number => {
  if (!node?.children || node.children.length === 0) return 0
  return node.children.reduce((acc, child) => acc + 1 + countDescendants(child), 0)
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    tableData.value = await getEquipmentTypeTree()
    // 加载完成后应用当前搜索条件，保持筛选状态一致
    await applySearch()
  } catch (error) {
    ElMessage.error((error as Error).message || '加载数据失败')
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
  searchForm.code = ''
  searchForm.isActive = undefined
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
  parentId: '0' as string | number,
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

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.parentId = '0'
  formData.spec = ''
  formData.description = ''
  formData.isActive = true
}

// 新增（可指定父级节点）
const handleAdd = (row?: EquipmentType) => {
  isEdit.value = false
  resetFormData()
  // 如果指定了父节点，设置为上级分类（保持字符串 id 避免精度丢失）
  if (row) {
    formData.parentId = String(row.id)
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: EquipmentType) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  // 保持字符串 id 避免精度丢失，'0' 表示顶级分类
  formData.parentId = String(row.parentId ?? '0')
  formData.spec = row.spec || ''
  formData.description = row.description || ''
  formData.isActive = row.isActive
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: EquipmentType) => {
  // 从原始数据中查找完整节点，避免搜索过滤导致 children 不完整
  const fullNode = findNode(tableData.value, row.id)
  if (fullNode?.children && fullNode.children.length > 0) {
    ElMessage.warning(`分类"${row.name}"包含 ${fullNode.children.length} 个子类型，请先删除或调整后再删除`)
    return
  }
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
      // 停用联动二次确认：停用含子级的分类将递归停用其全部子级（后端执行；重新启用父级不会自动恢复子级）
      if (isEdit.value && !formData.isActive) {
        const fullNode = findNode(tableData.value, formData.id)
        const descendantCount = countDescendants(fullNode)
        if (descendantCount > 0) {
          try {
            await ElMessageBox.confirm(
              `该分类下有 ${descendantCount} 个子级，停用后将一并停用全部子级（重新启用父级不会自动恢复子级）。确定停用吗？`,
              '停用联动确认',
              { type: 'warning', confirmButtonText: '确定停用', cancelButtonText: '取消' }
            )
          } catch {
            return
          }
        }
      }
      submitLoading.value = true
      try {
        // 上级分类归一：'0'（顶级）/空值统一为 null；其余透传 id（cast 仅满足 TS 类型）。
        // 雪花ID 经后端 LongToStringConverter 以字符串传输，绝不可 Number() 转换（会丢精度导致后端找不到父级）
        const rawParentId = formData.parentId
        const parentId =
          rawParentId === '0' || rawParentId === '' || rawParentId === null || rawParentId === undefined
            ? null
            : (rawParentId as number)
        const payload = {
          name: formData.name,
          code: formData.code,
          parentId,
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
