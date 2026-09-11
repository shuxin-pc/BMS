<template>
  <div class="user-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="用户名">
          <el-input v-model="searchForm.username" placeholder="请输入用户名" clearable style="width: 160px" />
        </el-form-item>
        <el-form-item label="姓名">
          <el-input v-model="searchForm.realName" placeholder="请输入姓名" clearable style="width: 140px" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.status" placeholder="请选择状态" clearable style="width: 110px">
            <el-option label="启用" :value="1" />
            <el-option label="禁用" :value="0" />
          </el-select>
        </el-form-item>
        <el-form-item label="租户" v-if="isSuperAdmin">
          <el-select v-model="searchForm.tenantId" placeholder="请选择租户" clearable filterable style="width: 140px" @change="handleTenantChange">
            <el-option v-for="tenant in tenantList" :key="tenant.id" :label="tenant.name" :value="tenant.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="组织">
          <el-cascader
            v-model="searchForm.organizationId"
            :options="searchOrganizations"
            :props="{ checkStrictly: true, value: 'id', label: 'name', emitPath: false }"
            placeholder="请选择组织"
            clearable
            style="width: 140px"
          />
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="searchForm.roleId" placeholder="请选择角色" clearable style="width: 130px">
            <el-option v-for="role in searchFilteredRoles" :key="role.id" :label="role.name" :value="role.id" />
          </el-select>
        </el-form-item>
        <el-form-item class="search-buttons">
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

    <!-- 操作栏 -->
    <div class="table-toolbar">
      <div class="toolbar-left">
        <el-button type="primary" @click="handleAdd" v-if="hasPermission('system:user:add')">
          <el-icon><Plus /></el-icon>
          新增用户
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
          v-if="hasPermission('system:user:batchDelete')"
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
        <el-table-column prop="userName" label="用户名" min-width="120" />
        <el-table-column prop="realName" label="姓名" min-width="100" />
        <el-table-column prop="email" label="邮箱" min-width="180" />
        <el-table-column prop="phone" label="手机号" width="130" />
        <el-table-column prop="organizationName" label="组织" width="150" />
        <el-table-column prop="roleNames" label="角色" width="150" />
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
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)" :disabled="!isSameTenant(row)" v-if="hasPermission('system:user:edit')">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="primary" size="small" @click="handleResetPwd(row)" v-if="hasPermission('system:user:resetPwd')">
              <el-icon><Key /></el-icon>
              重置密码
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)" v-if="hasPermission('system:user:delete')">
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
      :title="isEdit ? '编辑用户' : '新增用户'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="用户名" prop="username">
          <el-input v-model="formData.username" placeholder="请输入用户名" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="密码" prop="password" v-if="!isEdit">
          <el-input v-model="formData.password" type="password" placeholder="请输入密码" show-password />
        </el-form-item>
        <el-form-item label="姓名" prop="realName">
          <el-input v-model="formData.realName" placeholder="请输入姓名" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="formData.email" placeholder="请输入邮箱" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="formData.phone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="组织" prop="organizationId" :required="!isSuperAdmin">
          <el-cascader
            v-model="formData.organizationId"
            :options="orgTreeOptions"
            :props="{ checkStrictly: true, value: 'id', label: 'name', emitPath: false }"
            placeholder="请选择组织"
            clearable
            style="width: 100%"
            :disabled="isEditingSelf"
          />
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-radio-group v-model="formData.status" :disabled="isEditingSelf">
            <el-radio :value="1">启用</el-radio>
            <el-radio :value="0">禁用</el-radio>
          </el-radio-group>
        </el-form-item>
        <!-- 角色选择：单选 -->
        <el-form-item label="角色" prop="roleId">
          <!-- 角色在当前租户范围内：正常显示下拉框 -->
          <el-select
            v-if="!isRoleOutOfTenant"
            v-model="formData.roleId"
            placeholder="请选择角色"
            style="width: 100%"
            :disabled="isRoleSelectDisabled"
            @change="handleRoleChange"
          >
            <el-option
              v-for="role in filteredRoles"
              :key="role.id"
              :label="role.name"
              :value="role.id"
            />
          </el-select>
          <!-- 角色不在当前租户范围内（跨租户用户）：以文本形式显示 -->
          <div v-else>
            {{ getRoleNameById(formData.roleId) }}
          </div>
        </el-form-item>
        <!-- 租户选择：选择管理员角色时显示 -->
        <el-form-item v-if="showTenantSelect" label="租户" prop="tenantId">
          <el-select v-model="formData.tenantId" placeholder="请选择租户" style="width: 100%">
            <el-option
              v-for="tenant in tenantList"
              :key="tenant.id"
              :label="tenant.name"
              :value="tenant.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 重置密码弹窗 -->
    <el-dialog v-model="resetPwdVisible" title="重置密码" width="400px">
      <el-form ref="resetPwdRef" :model="resetPwdForm" :rules="resetPwdRules" label-width="80px">
        <el-form-item label="新密码" prop="password">
          <el-input v-model="resetPwdForm.password" type="password" placeholder="请输入新密码" show-password />
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input v-model="resetPwdForm.confirmPassword" type="password" placeholder="请确认新密码" show-password />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="resetPwdVisible = false">取消</el-button>
        <el-button type="primary" :loading="resetLoading" @click="handleResetPwdSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, nextTick } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Key } from '@element-plus/icons-vue'
import { getUsers, getUser, createUser, updateUser, deleteUser, deleteUsers, resetPassword, getAllRoles, getAllRolesWithoutFilter, getTenants, getOrganizationOptions } from '@/api/system'
import { useUserStore } from '@/stores/user'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { User, UserCreate, UserUpdate, Role, Tenant, Organization } from '@/api/system/types'
import { formatDateTime as formatDate } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()

const userStore = useUserStore()

// 判断是否为超级管理员
// 直接从 userStore 获取 isSuperAdmin
const isSuperAdmin = computed(() => userStore.isSuperAdmin)
// 获取当前登录用户的租户ID
const currentTenantId = computed(() => userStore.currentTenantId)
// 判断当前用户是否拥有指定权限（超级管理员不受限制）
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 被编辑用户的角色代码
const editingUserRoleCode = ref<string | undefined>(undefined)

// 根据当前用户角色过滤可选角色
// 统一规则：只显示当前登录用户所属租户的角色
const filteredRoles = computed(() => {
  if (!allRoles.value || allRoles.value.length === 0) {
    return []
  }
  // 如果当前用户没有租户ID，返回空
  if (!currentTenantId.value) {
    return []
  }
  // 统一按当前登录用户的租户ID过滤角色
  // 超级管理员也按租户ID过滤（包括平台租户的角色）
  // 使用宽松比较处理数字和字符串类型不一致的问题
  return allRoles.value.filter(role => String(role.tenantId) === String(currentTenantId.value))
})

// 搜索下拉 - 角色列表
// 如果当前租户的角色列表中没有管理员角色(tenant_admin)，则从 allRolesWithoutFilter 中获取并添加到底部
const searchFilteredRoles = computed(() => {
  const roles = searchRoles.value || []
  // 检查是否已有管理员角色
  const hasAdminRole = roles.some(role => role.code === 'tenant_admin')
  if (!hasAdminRole && allRolesWithoutFilter.value.length > 0) {
    // 从不过滤的角色列表中找管理员角色
    const adminRole = allRolesWithoutFilter.value.find(role => role.code === 'tenant_admin')
    if (adminRole) {
      return [...roles, adminRole]
    }
  }
  return roles
})

// 租户变更处理 - 超级管理员选择租户后清空组织和角色筛选，并重新加载搜索用的数据
const handleTenantChange = async () => {
  searchForm.organizationId = undefined
  searchForm.roleId = undefined
  // 清空租户时恢复平台租户的初始数据，否则加载选中租户的数据
  const targetTenantId = searchForm.tenantId || 1
  await loadOrganizations(targetTenantId, 'search')
  await loadRoles(targetTenantId, 'search')
}

// 是否显示租户选择
// 规则：仅超级管理员登录时考虑租户下拉框显示/隐藏，其他角色一律隐藏
const showTenantSelect = computed(() => {
  // 非超级管理员登录：一律隐藏租户下拉
  if (!isSuperAdmin.value) {
    return false
  }

  // 超级管理员：选中管理员角色时显示租户下拉
  const selectedRole = allRoles.value.find(r => String(r.id) === String(formData.roleId))
  if (selectedRole?.code === 'tenant_admin') {
    return true
  }

  return false
})

// 角色下拉是否禁用
// 规则：仅非超级管理员账号在编辑模式下，需要防止将管理员降权
// 是否在编辑自己（编辑自己时禁用角色/组织/状态字段，UX 优化，后端已独立校验）
const isEditingSelf = computed(() => {
  return isEdit.value && formData.id === userStore.userInfo.id
})

const isRoleSelectDisabled = computed(() => {
  // 编辑自己：禁用角色选择（防止误改自己的角色导致失权）
  if (isEditingSelf.value) {
    return true
  }

  // 超级管理员登录：不禁用
  if (isSuperAdmin.value) {
    return false
  }

  // 非超级管理员 + 新增模式：不禁用
  if (!isEdit.value) {
    return false
  }

  // 非超级管理员 + 编辑模式：仅选中管理员角色时禁用（防止降权）
  return editingUserRoleCode.value === 'tenant_admin'
})

// 判断被编辑用户的角色是否在当前登录用户所属租户的角色列表中
// 如果不在，说明是跨租户用户，需要以文本形式显示
const isRoleOutOfTenant = computed(() => {
  // 只有编辑模式才需要判断
  if (!isEdit.value) {
    return false
  }
  // 如果 filteredRoles 为空，说明当前用户没有角色可选
  if (!filteredRoles.value || filteredRoles.value.length === 0) {
    return false
  }
  // 如果没有选中角色，返回false
  if (!formData.roleId) {
    return false
  }
  // 判断当前选中的角色是否在 filteredRoles 中
  const roleExists = filteredRoles.value.some(role => String(role.id) === String(formData.roleId))
  // 如果角色不在列表中，返回 true（需要以文本显示）
  return !roleExists
})

// 角色变更处理
const handleRoleChange = () => {
  // 切换角色时清空租户选择，重新选择
  formData.tenantId = undefined
}

// 根据角色ID获取角色名称（用于跨租户用户显示，使用不过滤的角色列表）
const getRoleNameById = (roleId: number | string | undefined): string => {
  if (!roleId) return '-'
  const role = allRolesWithoutFilter.value.find(r => String(r.id) === String(roleId))
  return role?.name || '-'
}

// 搜索表单
const searchForm = reactive({
  username: '',
  realName: '',
  status: undefined as number | undefined,
  organizationId: undefined as number | undefined,
  tenantId: undefined as number | string | undefined,
  roleId: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<User[]>([])
const selectedRows = ref<User[]>([])

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
// 角色列表（用于表单编辑下拉框，按当前用户租户加载）
const allRoles = ref<Role[]>([])
// 角色列表（不过滤，用于跨租户用户显示角色名称）
const allRolesWithoutFilter = ref<Role[]>([])
// 搜索用的角色列表（按选中的租户加载）
const searchRoles = ref<Role[]>([])

const formData = reactive({
  id: 0,
  username: '',
  password: '',
  realName: '',
  email: '',
  phone: '',
  status: 1,
  roleId: undefined as number | undefined,
  tenantId: undefined as number | string | undefined,
  organizationId: undefined as number | undefined
})

// 租户列表
const tenantList = ref<Tenant[]>([])

// 组织列表（树形结构，用于表单编辑）
const organizationList = ref<Organization[]>([])
// 搜索用的组织列表（按选中的租户加载）
const searchOrganizations = ref<Organization[]>([])

// 组织树形选项（用于级联选择器）
const orgTreeOptions = computed(() => {
  return organizationList.value
})

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

// 加载组织列表
// targetRef: 'form' 更新表单编辑用的 organizationList，'search' 更新搜索用的 searchOrganizations
// 使用下拉专用接口 getOrganizationOptions，避免依赖组织架构页面权限
const loadOrganizations = async (tenantId?: number | string, targetRef: 'form' | 'search' = 'form') => {
  try {
    const res = await getOrganizationOptions({ tenantId })
    if (targetRef === 'form') {
      organizationList.value = res || []
    } else {
      searchOrganizations.value = res || []
    }
  } catch {
    // 加载组织失败
  }
}

const formRules: FormRules = {
  username: [
    { required: true, message: '用户名不能为空', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度为3-50个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '用户名只能包含字母、数字、下划线', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '密码不能为空', trigger: 'blur' },
    { min: 6, max: 100, message: '密码长度为6-100个字符', trigger: 'blur' }
  ],
  realName: [
    { required: true, message: '真实姓名不能为空', trigger: 'blur' },
    { max: 50, message: '真实姓名最多50个字符', trigger: 'blur' }
  ],
  email: [
    { type: 'email', message: '邮箱格式不正确', trigger: 'blur' }
  ],
  phone: [
    { pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ],
  roleId: [
    { required: true, message: '请选择角色', trigger: 'change' }
  ],
  organizationId: [
    {
      validator: (_rule, value, callback) => {
        // 超管可选（允许创建平台管理员等无组织用户）
        if (isSuperAdmin.value) {
          callback()
          return
        }
        // 非超管必填
        if (!value) {
          callback(new Error('请选择组织'))
          return
        }
        callback()
      },
      trigger: 'change'
    }
  ]
}

// 重置密码
const resetPwdVisible = ref(false)
const resetLoading = ref(false)
const resetPwdRef = ref<FormInstance>()
const resetPwdRow = ref<User | null>(null)
const resetPwdForm = reactive({
  password: '',
  confirmPassword: ''
})

const resetPwdRules: FormRules = {
  password: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度至少6位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    {
      validator: (_rule, value, callback) => {
        if (value !== resetPwdForm.password) {
          callback(new Error('两次输入的密码不一致'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ]
}

// 递归获取组织及其所有子组织的ID列表（基于树形结构）
const getChildOrganizationIds = (organizationId: number, organizations: Organization[]): number[] => {
  const result: number[] = [organizationId]
  // 获取子节点
  const getChildren = (node: Organization): Organization[] => {
    return node.children || (node as Organization & { Children?: Organization[] }).Children || []
  }
  // 递归查找子节点
  const findChildren = (node: Organization) => {
    const children = getChildren(node)
    if (children.length > 0) {
      children.forEach((child: Organization) => {
        result.push(child.id)
        findChildren(child)
      })
    }
  }
  // 查找匹配的节点
  const findNode = (orgs: Organization[], id: number): Organization | null => {
    for (const org of orgs) {
      if (org.id == id) {
        return org
      }
      const children = getChildren(org)
      if (children.length > 0) {
        const found = findNode(children, id)
        if (found) return found
      }
    }
    return null
  }
  const targetNode = findNode(organizations, organizationId)
  if (targetNode) {
    findChildren(targetNode)
  }
  return result
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    // 如果选择了组织，展开为包含所有子组织的列表
    let organizationIds: number[] | undefined
    if (searchForm.organizationId) {
      organizationIds = getChildOrganizationIds(searchForm.organizationId, searchOrganizations.value)
    }
    const res = await getUsers({
      username: searchForm.username || undefined,
      realName: searchForm.realName || undefined,
      status: searchForm.status,
      organizationIds,
      // 超级管理员：使用选择的租户（非空则用选择的，否则默认平台租户）
      // 非超级管理员：使用当前登录用户的租户
      tenantId: isSuperAdmin.value
        ? (searchForm.tenantId || 1)
        : currentTenantId.value,
      roleId: searchForm.roleId,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch {
    ElMessage.error('加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 加载所有角色
// targetRef: 'form' 更新表单编辑用的 allRoles，'search' 更新搜索用的 searchRoles
const loadRoles = async (tenantId?: number | string, targetRef: 'form' | 'search' = 'form') => {
  try {
    // 获取按租户过滤的角色列表
    const roles = await getAllRoles(tenantId)
    if (targetRef === 'form') {
      allRoles.value = roles || []
    } else {
      searchRoles.value = roles || []
    }
  } catch (error) {
    // 加载角色失败
    ElMessage.error('加载角色失败: ' + ((error as Error).message || '未知错误'))
    return
  }
  // 获取不过滤的角色列表（仅 super_admin 跨租户场景需要，失败不阻塞用户管理主流程）
  if (isSuperAdmin.value) {
    try {
      const allRolesData = await getAllRolesWithoutFilter()
      allRolesWithoutFilter.value = allRolesData || []
    } catch (e) {
      console.warn('[loadRoles] getAllRolesWithoutFilter 失败:', (e as Error)?.message)
    }
  }
}

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = async () => {
  searchForm.username = ''
  searchForm.realName = ''
  searchForm.status = undefined
  searchForm.organizationId = undefined
  searchForm.roleId = undefined
  // 重置时根据用户角色设置默认租户
  searchForm.tenantId = isSuperAdmin.value ? '1' : currentTenantId.value
  const resetTenantId = isSuperAdmin.value ? '1' : currentTenantId.value
  await loadOrganizations(resetTenantId, 'search')
  await loadRoles(resetTenantId, 'search')
  handleSearch()
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetForm()
  dialogVisible.value = true
}

// 编辑
const handleEdit = async (row: User) => {
  // 确保角色和租户数据已加载
  if (allRoles.value.length === 0) {
    await loadRoles()
  }
  if (tenantList.value.length === 0 && isSuperAdmin.value) {
    await loadTenants()
  }

  // 获取用户详情（列表数据不包含 roles 和完整 tenantId）
  let userDetail = row
  try {
    const detailRes = await getUser(row.id)
    if (detailRes) {
      userDetail = detailRes
    }
  } catch {
    // 获取用户详情失败，使用列表数据
  }

  isEdit.value = true
  formData.id = userDetail.id
  formData.username = userDetail.userName
  formData.realName = userDetail.realName
  formData.email = userDetail.email
  formData.phone = userDetail.phone
  formData.status = userDetail.status

  // 取第一个角色（单角色模式）
  formData.roleId = userDetail.roles && userDetail.roles.length > 0 ? userDetail.roles[0].id : undefined
  // 保存被编辑用户的角色代码，用于角色下拉框过滤
  editingUserRoleCode.value = userDetail.roles && userDetail.roles.length > 0 ? userDetail.roles[0].code : undefined
  // 租户ID可能返回字符串，需要转换为与下拉选项相同的类型
  if (userDetail.tenantId && tenantList.value.length > 0) {
    const firstTenantIdType = typeof tenantList.value[0].id
    formData.tenantId = firstTenantIdType === 'string'
      ? String(userDetail.tenantId)
      : userDetail.tenantId
  } else {
    formData.tenantId = undefined
  }

  // 组织ID
  formData.organizationId = userDetail.organizationId

  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: User) => {
  try {
    await ElMessageBox.confirm(`确定要删除用户 "${row.realName}" 吗？`, '提示', {
      type: 'warning'
    })
    await deleteUser(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个用户吗？`, '提示', {
      type: 'warning'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteUsers(ids)
    ElMessage.success('批量删除成功')
    selectedRows.value = []
    loadData()
  } catch (error) {
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
        // 获取选中角色的代码
        const selectedRole = allRoles.value.find(r => String(r.id) === String(formData.roleId))
        const isSelectedTenantAdmin = selectedRole?.code === 'tenant_admin'

        // 计算提交的 tenantId
        // 超级管理员 + 选中管理员角色 → tenantId = 选中的租户ID
        // 超级管理员 + 选中其他角色 → tenantId = 当前登录的 tenantId
        // 非超级管理员 → tenantId = 当前登录的 tenantId
        let submitTenantId: number | string | undefined
        if (isSuperAdmin.value) {
          submitTenantId = isSelectedTenantAdmin && formData.tenantId ? formData.tenantId : currentTenantId.value
        } else {
          submitTenantId = currentTenantId.value
        }

        if (isEdit.value) {
          const data: UserUpdate = {
            id: formData.id,
            userName: formData.username,
            realName: formData.realName,
            email: formData.email,
            phone: formData.phone,
            status: formData.status,
            roleIds: formData.roleId ? [formData.roleId] : [],
            tenantId: submitTenantId != null ? String(submitTenantId) : undefined,
            organizationId: formData.organizationId
          }
          await updateUser(data)
          ElMessage.success('更新成功')
        } else {
          const data: UserCreate = {
            userName: formData.username,
            password: formData.password,
            realName: formData.realName,
            email: formData.email,
            phone: formData.phone,
            status: formData.status,
            roleIds: formData.roleId ? [formData.roleId] : [],
            tenantId: submitTenantId != null ? String(submitTenantId) : undefined,
            organizationId: formData.organizationId
          }
          await createUser(data)
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

// 重置密码
const handleResetPwd = (row: User) => {
  resetPwdRow.value = row
  resetPwdForm.password = ''
  resetPwdForm.confirmPassword = ''
  resetPwdVisible.value = true
}

// 提交重置密码
const handleResetPwdSubmit = async () => {
  if (!resetPwdRef.value || !resetPwdRow.value) return
  const row = resetPwdRow.value
  await resetPwdRef.value.validate(async (valid) => {
    if (valid) {
      resetLoading.value = true
      try {
        await resetPassword(row.id, resetPwdForm.password)
        ElMessage.success('密码重置成功')
        resetPwdVisible.value = false
      } catch (error) {
        ElMessage.error((error as Error).message || '操作失败')
      } finally {
        resetLoading.value = false
      }
    }
  })
}

// 选择行
const handleSelectionChange = (rows: User[]) => {
  selectedRows.value = rows
}

// 重置表单
const resetForm = () => {
  formData.id = 0
  formData.username = ''
  formData.password = ''
  formData.realName = ''
  formData.email = ''
  formData.phone = ''
  formData.status = 1
  formData.roleId = undefined
  formData.tenantId = undefined
  formData.organizationId = undefined
  editingUserRoleCode.value = undefined
}


// 判断目标用户是否与当前登录用户属于同一租户
const isSameTenant = (row: User): boolean => {
  if (!currentTenantId.value) return false
  if (!row.tenantId) return false
  return String(row.tenantId) === String(currentTenantId.value)
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
  // 按当前登录用户的租户加载表单编辑用的组织和角色数据
  const initTenantId = isSuperAdmin.value ? '1' : currentTenantId.value
  loadRoles(initTenantId, 'form')
  loadOrganizations(initTenantId, 'form')
  // 同时加载搜索用的初始数据（与表单编辑数据相同）
  loadRoles(initTenantId, 'search')
  loadOrganizations(initTenantId, 'search')
  if (isSuperAdmin.value) {
    loadTenants()
  }
})
</script>

<style scoped>
.user-management {
  width: 100%;
}

/* 搜索区域 */
.search-form {
  padding: 16px 20px;
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
}

.search-form :deep(.el-form-item) {
  margin-right: 16px;
  margin-bottom: 0;
}

.search-form :deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-weight: 500;
}

.search-buttons {
  margin-left: auto;
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

/* 弹窗样式已在全局 dark-theme.css 中定义 */
</style>