<template>
  <div class="organization-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="组织名称">
            <el-input v-model="searchForm.name"
                      placeholder="请输入组织名称"
                      clearable
                      style="width: 180px" />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="请选择状态" clearable style="width: 120px">
              <el-option label="启用" :value="1" />
              <el-option label="禁用" :value="0" />
            </el-select>
          </el-form-item>
          <el-form-item label="租户" v-if="isSuperAdmin">
            <el-select v-model="searchForm.tenantId" placeholder="请选择租户" clearable filterable style="width: 150px">
              <el-option v-for="tenant in tenantList" :key="tenant.id" :label="tenant.name" :value="tenant.id" />
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
          新增组织
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
      <el-table ref="tableRef"
                v-loading="tableLoading"
                :data="tableData"
                row-key="id"
                :tree-props="{ children: 'children', hasChildren: 'hasChildren' }"
                style="width: 100%">
        <el-table-column prop="name" label="组织名称" min-width="180">
          <template #default="{ row }">
            <span :class="{ 'row-highlight': isRowHighlighted(row) }">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="code" label="组织编码" width="150" />
        <el-table-column prop="type" label="组织类型" width="100">
          <template #default="{ row }">
            <el-tag :type="getTypeTagType(row.type)" size="small" effect="dark">
              {{ getTypeLabel(row.type) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="managerName" label="负责人" width="120" />
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="createdTime" label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleAdd(row)" :disabled="!isSameTenant(row)">
              <el-icon><Plus /></el-icon>
              新增子级
            </el-button>
            <el-button link type="primary" size="small" @click="handleEdit(row)" :disabled="!isSameTenant(row)">
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
    <el-dialog v-model="dialogVisible"
               :title="isEdit ? '编辑组织' : '新增组织'"
               width="600px"
               :close-on-click-modal="false">
      <el-form ref="formRef"
               :model="formData"
               :rules="formRules"
               label-width="100px">
        <el-form-item label="上级组织">
          <el-cascader
            v-model="formData.parentId"
            :options="orgTreeOptions"
            :props="{ checkStrictly: true, value: 'id', label: 'name', emitPath: false }"
            placeholder="请选择上级组织（顶级组织请选择）"
            clearable
            style="width: 100%"
            :disabled="isEdit && formData.parentId === 0"
          />
        </el-form-item>
        <el-form-item label="组织名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入组织名称" />
        </el-form-item>
        <el-form-item label="组织编码" prop="code">
          <el-input v-model="formData.code" placeholder="请输入组织编码" />
        </el-form-item>
        <el-form-item label="组织类型" prop="type">
          <el-radio-group v-model="formData.type">
            <el-radio value="company">公司</el-radio>
            <el-radio value="department">部门</el-radio>
            <el-radio value="group">班组</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="formData.managerId"
                     placeholder="请选择负责人"
                     clearable
                     filterable
                     remote
                     :remote-method="handleUserSearch"
                     :loading="userLoading"
                     style="width: 100%">
            <el-option v-for="user in allUsers"
                       :key="user.id"
                       :label="user.realName"
                       :value="user.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-radio-group v-model="formData.status">
            <el-radio :value="1">启用</el-radio>
            <el-radio :value="0">禁用</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="formData.sort" :min="0" controls-position="right" />
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
  import { ref, reactive, watch, onMounted, computed, nextTick } from 'vue'
  import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
  import { Search, Refresh, Plus, Delete, Edit } from '@element-plus/icons-vue'
  import { getOrganizations, createOrganization, updateOrganization, deleteOrganization, getUserList, getTenants } from '@/api/system'
  import type { Organization, OrganizationCreate, OrganizationUpdate, User, Tenant } from '@/api/system/types'
  import { useUserStore } from '@/stores/user'
  import { useSortAutoFill } from '@/composables/useSortAutoFill'

  const userStore = useUserStore()
  const isSuperAdmin = computed(() => userStore.isSuperAdmin)
  const currentTenantId = computed(() => userStore.currentTenantId)

  // 判断目标组织是否与当前登录用户属于同一租户
  const isSameTenant = (row: Organization): boolean => {
    if (!currentTenantId.value) return false
    if (!row.tenantId) return false
    return String(row.tenantId) === String(currentTenantId.value)
  }

  // 搜索表单
  const searchForm = reactive({
    name: '',
    status: undefined as number | undefined,
    tenantId: undefined as number | string | undefined
  })

  // 表格数据
  const tableLoading = ref(false)
  const tableData = ref<Organization[]>([])
  const allUsers = ref<User[]>([])
  const userLoading = ref(false)
  const tableRef = ref()

  // 表单弹窗用的组织列表（按当前登录用户租户加载）
  const formOrganizationList = ref<Organization[]>([])

  // 高亮相关
  const highlightedIds = ref<Set<number>>(new Set())

  // 弹窗
  const dialogVisible = ref(false)
  const isEdit = ref(false)
  const submitLoading = ref(false)
  const formRef = ref<FormInstance>()

  const formData = reactive({
    id: 0,
    parentId: 0 as number | undefined,
    name: '',
    code: '',
    type: 'department' as 'company' | 'department' | 'group',
    managerId: undefined as number | undefined,
    status: 1,
    sort: 0
  })

  // 排序自动填充 - 直接从 API 获取完整数据
  const { autoSort, calculateAutoSort } = useSortAutoFill(
    async (options) => {
      const res = await getOrganizations({
        tenantId: options?.tenantId ?? currentTenantId.value
      })
      return res || []
    },
    () => formData.parentId ?? null,
    undefined, // 无分组概念
    () => currentTenantId.value
  )

  // 监听 parentId 变化，切换上级组织时自动更新排序值（仅新增时有效）
  watch(() => formData.parentId, async () => {
    if (!dialogVisible.value || isEdit.value) return
    await calculateAutoSort()
    formData.sort = autoSort.value
  })

  const formRules: FormRules = {
    name: [{ required: true, message: '请输入组织名称', trigger: 'blur' }],
    code: [{ required: true, message: '请输入组织编码', trigger: 'blur' }],
    type: [{ required: true, message: '请选择组织类型', trigger: 'change' }]
  }

  // 租户列表
  const tenantList = ref<Tenant[]>([])

  // 加载租户列表
  const loadTenants = async () => {
    if (!isSuperAdmin.value) return
    try {
      const res = await getTenants({ pageIndex: 1, pageSize: 100 })
      tenantList.value = res.list
      // 确保 tenantList 加载完成后再设置 searchForm.tenantId
      await nextTick()
      if (searchForm.tenantId === undefined) {
        searchForm.tenantId = '1'
      }
    } catch {
      // 加载租户失败
    }
  }

  onMounted(() => {
    // 初始化默认租户：超级管理员默认平台租户，非超级管理员默认当前用户租户
    searchForm.tenantId = isSuperAdmin.value ? '1' : currentTenantId.value
    loadData()
    loadUsers()
    loadTenants()
  })

  // 组织树选项（用于级联选择）- 使用表单专用组织列表
  interface OrgTreeOption {
    id: number
    name: string
    disabled: boolean
    children: OrgTreeOption[]
  }
  const orgTreeOptions = computed(() => {
    const processOrg = (org: Organization): OrgTreeOption => {
      return {
        id: org.id,
        name: org.name,
        disabled: false,
        children: org.children && org.children.length > 0
          ? org.children.map(child => processOrg(child))
          : []
      }
    }
    return [
      { id: 0, name: '顶级组织', children: [], disabled: false } as OrgTreeOption,
      ...formOrganizationList.value.map(org => processOrg(org))
    ]
  })

  // 加载表单用的组织列表（按当前登录用户租户）
  const loadFormOrganizations = async () => {
    try {
      const res = await getOrganizations({ tenantId: currentTenantId.value })
      formOrganizationList.value = res || []
    } catch {
      // 加载组织列表失败
    }
  }

  // 加载数据
  const loadData = async () => {
    tableLoading.value = true
    try {
      // 超级管理员：使用选择的租户（非空则用选择的，否则默认平台租户）
      // 非超级管理员：使用当前登录用户的租户
      const effectiveTenantId = isSuperAdmin.value
        ? (searchForm.tenantId || 1)
        : currentTenantId.value
      const res = await getOrganizations({
        name: searchForm.name || undefined,
        status: searchForm.status,
        tenantId: effectiveTenantId
      })
      tableData.value = res

      // 处理高亮和展开
      if (res.length > 0 && (searchForm.name || searchForm.status !== undefined)) {
        // 收集需要高亮的节点（仅 isMatched 为 true 的节点）
        const highlightSet = new Set<number>()
        // 收集需要展开的节点（所有返回的节点，因为是完整树形）
        const expandSet = new Set<number>()

        const collectIds = (orgs: Organization[], parentIds: number[] = []) => {
          for (const org of orgs) {
            expandSet.add(org.id)
            if (org.isMatched) {
              highlightSet.add(org.id)
              // 匹配节点的父级也需要展开
              parentIds.forEach(id => expandSet.add(id))
            }
            if (org.children) {
              collectIds(org.children, [...parentIds, org.id])
            }
          }
        }

        collectIds(res)
        highlightedIds.value = highlightSet

        // 展开匹配节点及其父级路径
        await nextTick(() => {
          expandRows(expandSet)
        })
      } else {
        highlightedIds.value = new Set()
        // 无数据或取消筛选时折叠所有行
        await nextTick(() => {
          collapseAllRows()
        })
      }
    } catch {
      ElMessage.error('加载数据失败')
    } finally {
      tableLoading.value = false
    }
  }

  // 检查行是否需要高亮
  const isRowHighlighted = (row: Organization): boolean => {
    return highlightedIds.value.has(row.id)
  }

  // 展开所有行
  // 展开指定行
  // 展开指定行
  const expandRows = (expandIds: Set<number>) => {
    if (!tableRef.value) return
    // 递归展开所有层级的行
    const expandRecursive = (data: Organization[]) => {
      data.forEach((row: Organization) => {
        if (expandIds.has(row.id)) {
          tableRef.value!.toggleRowExpansion(row, true)
        }
        // 递归展开子行
        if (row.children && row.children.length > 0) {
          expandRecursive(row.children)
        }
      })
    }
    expandRecursive(tableData.value)
  }

  // 折叠所有行
  const collapseAllRows = () => {
    if (!tableRef.value) return
    const rows = tableRef.value.store.states.data.value
    rows.forEach((row: Organization) => {
      tableRef.value.toggleRowExpansion(row, false)
    })
  }

  // 加载所有用户
  const loadUsers = async (tenantId?: number | string, realName?: string) => {
    try {
      userLoading.value = true
      const users = await getUserList(tenantId, realName)
      allUsers.value = users
    } catch {
      // 加载用户列表失败
    } finally {
      userLoading.value = false
    }
  }

  // 远程搜索用户
  const handleUserSearch = (query: string) => {
    // 按当前登录用户租户筛选
    loadUsers(currentTenantId.value, query)
  }

  // 组织类型映射：字符串 -> 数字
  const typeMap: Record<string, number> = {
    company: 1,
    department: 2,
    group: 3
  }

  // 组织类型映射：数字 -> 字符串
  const typeMapReverse: Record<number, 'company' | 'department' | 'group'> = {
    1: 'company',
    2: 'department',
    3: 'group'
  }

  // 搜索
  const handleSearch = () => {
    loadData()
  }

  // 重置
  const handleReset = () => {
    searchForm.name = ''
    searchForm.status = undefined
    // 重置时根据用户角色设置默认租户
    searchForm.tenantId = isSuperAdmin.value ? '1' : currentTenantId.value
    handleSearch()
  }

  // 新增
  const handleAdd = async (row?: Organization) => {
    isEdit.value = false
    formData.id = 0
    formData.parentId = row?.id || 0
    formData.name = ''
    formData.code = ''
    // 根据父级组织类型设置默认类型
    const parentType = row?.type
    if (!parentType && !row) {
      // 顶级组织默认类型为公司
      formData.type = 'company'
    } else if (parentType === 1) {
      formData.type = 'department'
    } else if (parentType === 2) {
      formData.type = 'group'
    } else {
      formData.type = 'department'
    }
    formData.managerId = undefined
    formData.status = 1
    // 加载当前用户租户的组织列表和用户列表
    await Promise.all([
      loadFormOrganizations(),
      loadUsers(currentTenantId.value)
    ])
    // 计算自动填充排序值
    await calculateAutoSort()
    formData.sort = autoSort.value
    dialogVisible.value = true
  }

  // 编辑
  const handleEdit = async (row: Organization) => {
    isEdit.value = true
    formData.id = row.id
    formData.parentId = row.parentId || 0
    formData.name = row.name
    formData.code = row.code
    formData.type = typeMapReverse[row.type] || 'department'
    formData.managerId = row.managerId
    formData.status = row.status
    formData.sort = row.sort
    // 加载当前用户租户的组织列表和用户列表
    await Promise.all([
      loadFormOrganizations(),
      loadUsers(currentTenantId.value)
    ])
    dialogVisible.value = true
  }

  // 递归查找指定 id 的组织节点（用字符串比较避免大数精度丢失）
  const findNode = (nodes: Organization[], id: number | string): Organization | null => {
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
  const handleDelete = async (row: Organization) => {
    // 从原始数据中查找完整节点，避免搜索过滤导致 children 不完整
    const fullNode = findNode(tableData.value, row.id)
    if (fullNode?.children && fullNode.children.length > 0) {
      ElMessage.warning(`组织"${row.name}"包含 ${fullNode.children.length} 个子组织，请先删除子组织后再删除`)
      return
    }
    try {
      await ElMessageBox.confirm(`确定要删除组织 "${row.name}" 吗？此操作不可恢复！`, '提示', {
        type: 'warning'
      })
      await deleteOrganization(row.id)
      ElMessage.success('删除成功')
      loadData()
    } catch (error) {
      if (error !== 'cancel') {
        ElMessage.error((error as Error).message || '删除失败')
      }
    }
  }

  // 批量删除
  // 提交表单
  const handleSubmit = async () => {
    if (!formRef.value) return
    await formRef.value.validate(async (valid) => {
      if (valid) {
        submitLoading.value = true
        try {
          // 将 parentId 为 0 转换为 null，表示顶级组织
          const parentId = formData.parentId === 0 ? undefined : formData.parentId
          if (isEdit.value) {
            const data: OrganizationUpdate = {
              id: formData.id,
              parentId,
              name: formData.name,
              code: formData.code,
              type: typeMap[formData.type],
              managerId: formData.managerId,
              status: formData.status,
              sort: formData.sort
            }
            await updateOrganization(data)
            ElMessage.success('更新成功')
          } else {
            // 新增组织时，租户ID固定为当前登录用户的租户ID（使用字符串避免精度丢失）
            const data: OrganizationCreate = {
              parentId,
              name: formData.name,
              code: formData.code,
              type: typeMap[formData.type],
              managerId: formData.managerId,
              status: formData.status,
              sort: formData.sort,
              tenantId: String(currentTenantId.value)
            }
            await createOrganization(data)
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

  // 选择行
  // 获取组织类型标签（支持数字和字符串）
  const getTypeLabel = (type: string | number) => {
    const map: Record<string | number, string> = {
      1: '公司',
      2: '部门',
      3: '班组',
      company: '公司',
      department: '部门',
      group: '班组'
    }
    return map[type] || String(type)
  }

  // 获取组织类型标签样式
  const getTypeTagType = (type: string | number) => {
    const map: Record<string | number, string> = {
      1: 'primary',
      2: 'warning',
      3: 'info',
      company: 'primary',
      department: 'warning',
      group: 'info'
    }
    return map[type] || 'info'
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

  // 监听搜索条件变化
  watch([() => searchForm.name, () => searchForm.status], () => {
    loadData()
  })

  onMounted(() => {
    loadData()
    loadUsers()
    loadTenants()
  })
</script>

<style scoped>
  .organization-management {
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

  :deep(.el-button + .el-button) {
    margin-left: 8px;
  }

  /* 分页 - 科技风 */
  .pagination-container {
    display: flex;
    justify-content: flex-end;
    padding: 20px 24px;
    border-top: 1px solid var(--border-primary);
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
