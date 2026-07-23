<template>
  <div class="role-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="角色名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入角色名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="角色编码">
            <el-input
              v-model="searchForm.code"
              placeholder="请输入角色编码"
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
          <el-form-item label="租户" v-if="isSuperAdmin">
            <el-select v-model="searchForm.tenantId" placeholder="请选择租户" clearable filterable style="width: 150px">
              <el-option
                v-for="tenant in tenantList"
                :key="tenant.id"
                :label="tenant.name"
                :value="tenant.id"
              />
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
        <el-button type="primary" @click="handleAdd" v-if="!isSuperAdmin || searchForm.tenantId != 1">
          <el-icon><Plus /></el-icon>
          新增角色
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
          v-if="hasPermission('system:role:batchDelete')"
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
        :data="tableData"
        @selection-change="handleSelectionChange"
        style="width: 100%"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column prop="code" label="角色编码" min-width="120" />
        <el-table-column prop="name" label="角色名称" min-width="120" />
        <el-table-column prop="level" label="等级" width="80" align="center">
          <template #default="{ row }">
            <span :title="'数字越小权限越大'">{{ row.level ?? '-' }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="200" />
        <el-table-column prop="dataScopeType" label="数据范围" width="120">
          <template #default="{ row }">
            <el-tag :type="getDataScopeType(row.dataScopeType)" size="small" effect="dark">
              {{ getDataScopeName(row.dataScopeType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdTime" label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdTime) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">
              <el-icon><View /></el-icon>
              详情
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
      :title="isEdit ? '编辑角色' : '新增角色'"
      width="700px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="角色编码" prop="code">
              <el-input v-model="formData.code" placeholder="请输入角色编码" :disabled="isEdit" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="角色名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入角色名称" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="数据范围" prop="dataScopeType">
              <el-select v-model="formData.dataScopeType" placeholder="请选择数据范围" style="width: 100%">
                <el-option label="全部数据" :value="1" />
                <el-option label="部门及以下" :value="2" />
                <el-option label="仅本人" :value="3" />
                <el-option label="自定义" :value="4" />
              </el-select>
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
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="角色等级" prop="level">
              <el-input-number v-model="formData.level" :min="minLevel" :max="99" controls-position="right" style="width: 100%" />
              <div style="font-size: 12px; color: #909399; line-height: 1.5; margin-top: 4px;">数字越小权限越大（{{ minLevel }}-99）</div>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="描述">
          <el-input v-model="formData.description" type="textarea" :rows="2" placeholder="请输入描述" />
        </el-form-item>
        <el-form-item label="自定义组织" v-if="formData.dataScopeType === 4">
          <div class="permission-tree">
            <el-tree
              ref="orgTreeRef"
              :data="organizationTree"
              :props="treeProps"
              show-checkbox
              node-key="id"
              :default-checked-keys="formData.customOrganizationIds"
              @check="handleOrgCheck"
            />
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 角色详情弹窗 -->
    <el-dialog
      v-model="detailVisible"
      title="角色详情"
      width="1000px"
      :close-on-click-modal="false"
    >
      <div class="role-detail" v-if="currentRole">
        <el-tabs v-model="activeTab" type="card" :before-leave="handleTabBeforeChange">
          <!-- 基本信息 -->
          <el-tab-pane label="基本信息" name="basic">
            <div class="detail-section">
              <h3 class="section-title">
                <span class="title-text">基本信息</span>
                <span class="title-line"></span>
              </h3>
              <div class="detail-grid">
                <div class="detail-item">
                  <span class="item-label">角色编码</span>
                  <span class="item-value">{{ currentRole.code }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">角色名称</span>
                  <span class="item-value">{{ currentRole.name }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">数据范围</span>
                  <span class="item-value">
                    <el-tag :type="getDataScopeType(currentRole.dataScopeType)" size="small" effect="dark">
                      {{ getDataScopeName(currentRole.dataScopeType) }}
                    </el-tag>
                  </span>
                </div>
                <div class="detail-item">
                  <span class="item-label">状态</span>
                  <span class="item-value">
                    <el-tag :type="currentRole.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
                      {{ currentRole.status === 1 ? '启用' : '禁用' }}
                    </el-tag>
                  </span>
                </div>
                <div class="detail-item full-width">
                  <span class="item-label">描述</span>
                  <span class="item-value">{{ currentRole.description || '-' }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">创建时间</span>
                  <span class="item-value">{{ formatDate(currentRole.createdTime) }}</span>
                </div>
              </div>
            </div>
          </el-tab-pane>

          <!-- 菜单权限 -->
          <el-tab-pane label="菜单权限" name="menus">
            <div class="detail-section">
              <h3 class="section-title">
                <span class="title-text">菜单权限配置</span>
                <span class="title-line"></span>
              </h3>
              <div class="menu-permission-section">
                <div v-if="menuLoading" class="loading-container">
                  <el-skeleton :rows="8" animated />
                </div>
                <div v-else class="subsystem-menu-container">
                  <el-collapse v-model="activeSubsystemIds" accordion>
                    <el-collapse-item
                      v-for="group in groupedMenuData"
                      :key="group.subsystemId"
                      :name="group.subsystemId"
                    >
                      <template #title>
                        <div class="subsystem-collapse-title">
                          <span class="subsystem-name">{{ group.subsystemName }}</span>
                          <span class="subsystem-code">({{ group.subsystemCode }})</span>
                          <el-tag size="small" class="selected-count">
                            {{ getSubsystemSelectedCount(group) }} 项已选
                          </el-tag>
                        </div>
                      </template>
                      <div class="menu-tree-container">
                        <el-tree
                          :ref="(el: any) => setMenuTreeRef(group.subsystemId, el)"
                          :data="group.menus"
                          :props="menuTreeProps"
                          show-checkbox
                          node-key="id"
                          @check="(data: any, info: any) => handleMenuCheck(group, data, info)"
                        />
                      </div>
                    </el-collapse-item>
                  </el-collapse>
                </div>
              </div>
            </div>
          </el-tab-pane>
        </el-tabs>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
        <el-button type="primary" @click="handleEdit(currentRole!)" v-if="isSameTenant(currentRole)">
          编辑
        </el-button>
        <el-button type="success" :loading="menuSaveLoading" @click="handleMenuSave" v-if="activeTab === 'menus' && isSameTenant(currentRole)">
          保存
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, nextTick, computed } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules, type TreeInstance } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, View } from '@element-plus/icons-vue'
import {
  getRoles,
  createRole,
  updateRole,
  deleteRole,
  deleteRoles,
  getOrganizations,
  getRoleMenuAuthsGrouped,
  assignRoleMenuAuths,
  getTenants
} from '@/api/system'
import { useUserStore } from '@/stores/user'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Role, RoleCreate, RoleUpdate, RoleMenuGrouped, Tenant, Menu } from '@/api/system/types'

const userStore = useUserStore()
const systemConfigStore = useSystemConfigStore()

// 判断是否为超级管理员
const isSuperAdmin = computed(() => userStore.isSuperAdmin)
// 判断当前用户是否拥有指定权限（超级管理员不受限制）
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)
// 获取当前登录用户的租户ID
const currentTenantId = computed(() => userStore.currentTenantId)
// 角色等级下限：普通用户只能创建/编辑比自己最高角色等级更低的角色（level 数字更大）
// super_admin(MaxRoleLevel=0) -> min=2；tenant_admin(MaxRoleLevel=1) -> min=2；普通用户(MaxRoleLevel=N) -> min=N+1
// 上限 99：避免 min > max(99) 触发 Element Plus 错误；MaxRoleLevel>=99 时后端兜底拒绝创建
const minLevel = computed(() => Math.min(Math.max(2, (userStore.maxRoleLevel ?? 100) + 1), 99))

// 判断目标角色是否与当前登录用户属于同一租户
const isSameTenant = (row: Role | null): boolean => {
  if (!row) return false
  if (!currentTenantId.value) return false
  if (!row.tenantId) return false
  return String(row.tenantId) === String(currentTenantId.value)
}

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  status: undefined as number | undefined,
  tenantId: undefined as number | string | undefined
})

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
  } catch (error) {
    // 加载租户失败
  }
}

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Role[]>([])
const selectedRows = ref<Role[]>([])

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
const orgTreeRef = ref<TreeInstance>()

const formData = reactive({
  id: 0,
  code: '',
  name: '',
  description: '',
  dataScopeType: 1,
  status: 1,
  level: undefined as number | undefined,
  customOrganizationIds: [] as string[]
})

const formRules: FormRules = {
  code: [
    { required: true, message: '角色编码不能为空', trigger: 'blur' },
    { min: 1, max: 50, message: '角色编码最多50个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '角色编码只能包含字母、数字、下划线', trigger: 'blur' }
  ],
  name: [
    { required: true, message: '角色名称不能为空', trigger: 'blur' },
    { max: 50, message: '角色名称最多50个字符', trigger: 'blur' }
  ],
  description: [
    { max: 500, message: '描述最多500个字符', trigger: 'blur' }
  ],
  level: [
    { required: true, message: '角色等级不能为空', trigger: 'blur' },
    { type: 'number', min: minLevel.value, max: 99, message: `角色等级必须在 ${minLevel.value}-99 之间（数字越小权限越大）`, trigger: 'blur' }
  ],
  dataScopeType: [
    { required: true, message: '请选择数据范围', trigger: 'change' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
}

// 组织树（用于自定义数据范围）
const organizationTree = ref<any[]>([])
const treeProps = {
  children: 'children',
  label: 'name'
}

// 详情弹窗
const detailVisible = ref(false)
const currentRole = ref<Role | null>(null)
const activeTab = ref('basic')
const menuLoading = ref(false)
const menuSaveLoading = ref(false)
const groupedMenuData = ref<RoleMenuGrouped[]>([])
const activeSubsystemIds = ref<number[]>([])

// 菜单树ref管理
const menuTreeRefs = reactive<Record<number, TreeInstance | null>>({})
const setMenuTreeRef = (subsystemId: number, el: any) => {
  if (el) {
    // 仅在 el 实例变化时初始化选中状态
    // 内联 ref 函数每次渲染都会生成新实例并触发回调，若不守卫会反复 setCheckedKeys 重置用户勾选
    if (menuTreeRefs[subsystemId] !== el) {
      menuTreeRefs[subsystemId] = el
      const group = groupedMenuData.value.find(g => g.subsystemId === subsystemId)
      if (group) {
        const leafIds = collectLeafSelectedIds(group.menus, group.selectedMenuIds)
        el.setCheckedKeys(leafIds)
      }
    }
  }
}

// 从 selectedMenuIds 中过滤出叶子节点ID
// 父子联动模式下只需设置叶子节点，父节点会根据子节点状态自动呈现"勾选/半勾选/不勾选"
const collectLeafSelectedIds = (menus: Menu[], selectedIds: number[]): number[] => {
  const result: number[] = []
  const selectedSet = new Set(selectedIds)
  const traverse = (nodes: Menu[]) => {
    nodes.forEach(node => {
      const hasChildren = node.children && node.children.length > 0
      if (!hasChildren) {
        if (selectedSet.has(node.id)) {
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

const menuTreeProps = {
  children: 'children',
  label: 'name'
}

// 数据范围映射
const getDataScopeName = (scope: number) => {
  const map: Record<number, string> = {
    1: '全部数据',
    2: '部门及以下',
    3: '仅本人',
    4: '自定义'
  }
  return map[scope] || '全部数据'
}

const getDataScopeType = (scope: number) => {
  const map: Record<number, string> = {
    1: 'danger',
    2: 'warning',
    3: 'success',
    4: 'info',
    5: 'primary'
  }
  return map[scope] || 'info'
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
    const res = await getRoles({
      name: searchForm.name || undefined,
      code: searchForm.code || undefined,
      status: searchForm.status,
      tenantId: effectiveTenantId,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error('加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 加载组织树（用于自定义数据范围）
const loadOrganizationTree = async () => {
  try {
    const res = await getOrganizations()
    // 后端 /organizations/tree 已返回树形结构，直接使用
    organizationTree.value = res || []
  } catch (error) {
    // 加载组织树失败
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
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.name = ''
  searchForm.code = ''
  searchForm.status = undefined
  // 重置时根据用户角色设置默认租户
  searchForm.tenantId = isSuperAdmin.value ? '1' : currentTenantId.value
  handleSearch()
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetForm()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Role) => {
  isEdit.value = true
  formData.id = row.id
  formData.code = row.code
  formData.name = row.name
  formData.description = row.description || ''
  formData.dataScopeType = row.dataScopeType
  formData.status = row.status
  formData.level = row.level
  // 解析自定义组织ID列表（后端返回逗号分隔字符串，需转换为字符串数组以匹配组织树的node-key类型）
  if (row.customOrganizationIds) {
    if (Array.isArray(row.customOrganizationIds)) {
      formData.customOrganizationIds = row.customOrganizationIds.map(id => String(id))
    } else {
      // 后端返回逗号分隔字符串如 "1,2,3"
      formData.customOrganizationIds = String(row.customOrganizationIds).split(',').map(id => String(id).trim())
    }
  } else {
    formData.customOrganizationIds = []
  }
  dialogVisible.value = true
  detailVisible.value = false
}

// 查看详情
const handleView = async (row: Role) => {
  currentRole.value = row
  detailVisible.value = true
  activeTab.value = 'basic'
  await loadMenuData(row.id)
}

// 加载菜单权限数据
const loadMenuData = async (roleId: number) => {
  menuLoading.value = true
  try {
    const res = await getRoleMenuAuthsGrouped(roleId)
    groupedMenuData.value = res
    // 父子联动模式下，只需设置叶子节点为选中，父节点会根据子节点状态自动呈现"勾选/半勾选/不勾选"
    await nextTick()
    groupedMenuData.value.forEach(group => {
      const treeRef = menuTreeRefs[group.subsystemId]
      if (treeRef) {
        const leafIds = collectLeafSelectedIds(group.menus, group.selectedMenuIds)
        treeRef.setCheckedKeys(leafIds)
      }
    })
  } catch (error) {
    ElMessage.error('加载菜单权限失败')
  } finally {
    menuLoading.value = false
  }
}

// 获取子系统选中的菜单数量
const getSubsystemSelectedCount = (group: RoleMenuGrouped): number => {
  const treeRef = menuTreeRefs[group.subsystemId]
  if (treeRef) {
    const checkedKeys = treeRef.getCheckedKeys(false)
    return checkedKeys.length
  }
  return group.selectedMenuIds.length
}

// 菜单选中事件
const handleMenuCheck = (_group: RoleMenuGrouped, _data: any, _info: any) => {
  // 不需要在这里处理，保存时统一获取
}

// 保存菜单权限
const handleMenuSave = async () => {
  if (!currentRole.value) return

  menuSaveLoading.value = true
  try {
    // 收集所有选中的菜单ID
    const allMenuIds: number[] = []
    groupedMenuData.value.forEach(group => {
      const treeRef = menuTreeRefs[group.subsystemId]
      if (treeRef) {
        const checkedKeys = treeRef.getCheckedKeys(false) as number[]
        allMenuIds.push(...checkedKeys)
      }
    })

    await assignRoleMenuAuths(currentRole.value.id, {
      menuIds: allMenuIds
    })
    ElMessage.success('保存成功')
    // 重新加载数据
    await loadMenuData(currentRole.value.id)
  } catch (error: any) {
    ElMessage.error(error.message || '保存失败')
  } finally {
    menuSaveLoading.value = false
  }
}

// Tab切换前检查
const handleTabBeforeChange = async (newName: string) => {
  // 如果当前是菜单权限Tab且有未保存的更改，提示用户
  if (activeTab.value === 'menus' && newName !== 'menus') {
    // 简单实现，不做复杂的变更检测
  }
  return true
}

// 删除
const handleDelete = async (row: Role) => {
  try {
    await ElMessageBox.confirm(`确定要删除角色 "${row.name}" 吗？`, '提示', {
      type: 'warning'
    })
    await deleteRole(row.id)
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
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个角色吗？`, '提示', {
      type: 'warning'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteRoles(ids)
    ElMessage.success('批量删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error(error.message || '删除失败')
    }
  }
}

// 组织树选择（自定义数据范围）
const handleOrgCheck = () => {
  const checkedNodes = orgTreeRef.value?.getCheckedNodes(false) || []
  formData.customOrganizationIds = checkedNodes.map((n: any) => String(n.id))
}

// 提交表单
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitLoading.value = true
      try {
        if (isEdit.value) {
          const data: RoleUpdate = {
            id: formData.id,
            code: formData.code,
            name: formData.name,
            description: formData.description,
            dataScopeType: formData.dataScopeType,
            status: formData.status,
            level: formData.level!,
            customOrganizationIds: formData.customOrganizationIds,
            permissionIds: []
          }
          await updateRole(data)
          ElMessage.success('更新成功')
        } else {
          const data: RoleCreate = {
            code: formData.code,
            name: formData.name,
            description: formData.description,
            dataScopeType: formData.dataScopeType,
            status: formData.status,
            level: formData.level!,
            customOrganizationIds: formData.customOrganizationIds,
            permissionIds: []
          }
          await createRole(data)
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
const handleSelectionChange = (rows: Role[]) => {
  selectedRows.value = rows
}

// 重置表单
const resetForm = () => {
  formData.id = 0
  formData.code = ''
  formData.name = ''
  formData.description = ''
  formData.dataScopeType = 1
  formData.status = 1
  // 默认设为当前用户可设置的最低权限等级（数字最大），提升体验
  formData.level = minLevel.value
  formData.customOrganizationIds = []
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

onMounted(async () => {
  // 确保系统配置已加载
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  // 应用默认分页大小
  pagination.pageSize = systemConfigStore.defaultPageSize
  // 初始化默认租户：超级管理员默认平台租户，非超级管理员默认当前用户租户
  searchForm.tenantId = isSuperAdmin.value ? '1' : currentTenantId.value
  loadData()
  loadOrganizationTree()
  loadTenants()
})
</script>

<style scoped>
.role-management {
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

/* 权限树 */
.permission-tree {
  max-height: 300px;
  overflow-y: auto;
  padding: 12px;
  background: #f9fafb;
  border-radius: var(--radius-sm);
  border: 1px solid #e5e7eb;
  width: 100%;
  box-sizing: border-box;
}

:deep(.el-tree) {
  background: transparent;
  color: #1f2937;
}

:deep(.el-tree-node__content) {
  background: transparent;
  border-radius: var(--radius-sm);
  height: 32px;
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

/* el-tree 空数据状态 */
:deep(.el-tree__empty-block) {
  min-height: 120px;
  width: 100%;
  background: transparent;
  border-radius: var(--radius-sm);
}

:deep(.el-tree__empty-text) {
  color: #9ca3af;
  font-size: 14px;
  padding: 30px 0;
}

/* 详情样式 - 浅色弹窗 */
.role-detail {
  padding: 24px;
  background: #f9fafb;
  border-radius: 8px;
}

.detail-section {
  margin-bottom: 30px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 12px;
  margin: 0 0 20px;
  padding-bottom: 16px;
  border-bottom: 1px solid #e5e7eb;
}

.title-text {
  font-size: 15px;
  font-weight: 600;
  color: #1f2937;
  letter-spacing: 0.5px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.title-text::before {
  content: '';
  width: 4px;
  height: 16px;
  background: #06b6d4;
  border-radius: 2px;
}

.title-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, #e5e7eb 0%, transparent 100%);
  position: relative;
}

.title-line::after {
  content: '';
  position: absolute;
  right: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #06b6d4;
  opacity: 0.4;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20px 30px;
}

.detail-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 12px 16px;
  background: #ffffff;
  border-radius: 6px;
  border: 1px solid #e5e7eb;
  transition: all 0.2s ease;
}

.detail-item.full-width {
  grid-column: span 2;
}

.detail-item:hover {
  border-color: #06b6d4;
  background: #f9fafb;
}

.item-label {
  font-size: 12px;
  color: #6b7280;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
}

.item-label::before {
  content: '';
  width: 3px;
  height: 3px;
  border-radius: 50%;
  background: #06b6d4;
  opacity: 0.6;
}

.item-value {
  font-size: 14px;
  color: #1f2937;
  font-weight: 500;
}

/* Tabs样式 */
:deep(.el-tabs__item) {
  color: var(--text-secondary);
}

:deep(.el-tabs__item.is-active) {
  color: var(--primary);
}

:deep(.el-tabs__active-bar) {
  background-color: var(--primary);
}

:deep(.el-tabs__nav-wrap::after) {
  border-color: var(--border-primary);
}

/* 菜单权限区域 */
.menu-permission-section {
  margin-top: 20px;
}

.loading-container {
  padding: 20px;
}

.subsystem-menu-container {
  max-height: 600px;
  overflow-y: auto;
  padding: 8px 0;
}

/* 折叠面板样式 - 浅色弹窗 */
:deep(.el-collapse) {
  border: none;
}

:deep(.el-collapse-item) {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  margin-bottom: 12px;
  background: #ffffff;
  overflow: hidden;
}

:deep(.el-collapse-item__header) {
  background: #ffffff;
  color: #1f2937;
  border-bottom: 1px solid #e5e7eb;
  padding: 16px 20px;
}

:deep(.el-collapse-item__wrap) {
  background: #f9fafb;
  border: none;
}

:deep(.el-collapse-item__content) {
  padding: 0;
}

.subsystem-collapse-title {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
}

.subsystem-name {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
}

.subsystem-code {
  font-size: 12px;
  color: #9ca3af;
}

.selected-count {
  margin-left: auto;
  background: rgba(6, 212, 228, 0.15);
  color: #06b6d4;
  border: none;
}

.menu-tree-container {
  padding: 20px;
  max-height: 400px;
  overflow-y: auto;
  background: #f9fafb;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

.mb-20 {
  margin-bottom: 20px;
}
</style>
