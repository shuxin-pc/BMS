<template>
  <div class="system-config-management">
    <!-- 配置分组标签 -->
    <div class="config-tabs">
      <div
        v-for="tab in configTabs"
        :key="tab.key"
        class="tab-item"
        :class="{ active: activeTab === tab.key }"
        @click="activeTab = tab.key"
      >
        <span class="tab-indicator"></span>
        <span class="tab-label">{{ tab.label }}</span>
        <span class="tab-count">{{ tab.count }}</span>
      </div>
    </div>

    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="配置键">
            <el-input
              v-model="searchForm.configKey"
              placeholder="请输入配置键"
              clearable
              style="width: 200px"
            />
          </el-form-item>
          <el-form-item v-if="activeTab === ''" label="配置分组">
            <el-select v-model="searchForm.configGroup" placeholder="请选择配置分组" clearable style="width: 140px">
              <el-option label="全部" value="" />
              <el-option label="系统设置" value="System" />
              <el-option label="安全策略" value="Security" />
              <el-option label="审计日志" value="AuditLog" />
              <el-option label="文件上传" value="Upload" />
            </el-select>
          </el-form-item>
          <el-form-item v-if="isSuperAdmin" label="租户">
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
        <el-button v-if="isSuperAdmin" type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增配置
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
        <el-table-column prop="configKey" label="配置键" min-width="180">
          <template #default="{ row }">
            <code class="config-key-code">{{ row.configKey }}</code>
          </template>
        </el-table-column>
        <el-table-column prop="configValue" label="配置值" min-width="200">
          <template #default="{ row }">
            <div class="config-value-wrapper">
              <code v-if="!isJson(row.configValue)" class="config-value-code">{{ row.configValue }}</code>
              <code v-else class="config-value-json">{{ formatJsonValue(row.configValue) }}</code>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="configGroup" label="配置分组" width="120">
          <template #default="{ row }">
            <el-tag :type="getGroupTagType(row.configGroup)" size="small" effect="dark">
              {{ getGroupLabel(row.configGroup) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述" min-width="150" show-overflow-tooltip />
        <el-table-column prop="isPublic" label="是否公开" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isPublic ? 'success' : 'info'" size="small" effect="dark">
              {{ row.isPublic ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="isEditable" label="可修改" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isEditable ? 'success' : 'danger'" size="small" effect="dark">
              {{ row.isEditable ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="isSystem" label="系统配置" width="100">
          <template #default="{ row }">
            <el-tag :type="row.isSystem ? 'warning' : 'info'" size="small" effect="dark">
              {{ row.isSystem ? '是' : '否' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="sort" label="排序" width="80" />
        <el-table-column prop="createdAt" label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button
              link
              type="primary"
              size="small"
              :disabled="!canEdit(row)"
              @click="handleEdit(row)"
              v-if="hasPermission('system:config:edit')"
            >
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button
              link
              type="danger"
              size="small"
              :disabled="!canDelete(row)"
              @click="handleDelete(row)"
              v-if="hasPermission('system:config:delete')"
            >
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
      :title="isEdit ? '编辑配置' : '新增配置'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="配置键" prop="configKey" :disabled="isEdit">
          <el-input v-model="formData.configKey" placeholder="请输入配置键" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="配置值" prop="configValue">
          <div class="config-value-editor">
            <el-input v-model="formData.configValue" type="textarea" :rows="4" placeholder="请输入配置值，支持JSON格式" />
            <el-upload
              class="logo-upload"
              :show-file-list="false"
              :before-upload="handleLogoUpload"
              accept="image/*"
              action="#"
            >
              <el-button type="primary" size="small">
                <el-icon><Upload /></el-icon>
                上传图片
              </el-button>
            </el-upload>
          </div>
        </el-form-item>
        <el-form-item label="配置分组" prop="configGroup">
          <el-select v-model="formData.configGroup" placeholder="请选择配置分组" style="width: 100%" :disabled="isEdit && !isPlatformTenantUser">
            <el-option label="系统设置" value="System" />
            <el-option label="安全策略" value="Security" />
            <el-option label="审计日志" value="AuditLog" />
            <el-option label="文件上传" value="Upload" />
          </el-select>
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="formData.description" placeholder="请输入描述" :disabled="isEdit && !isPlatformTenantUser" />
        </el-form-item>
        <el-form-item label="是否公开">
          <el-switch
            v-model="formData.isPublic"
            active-text="公开"
            inactive-text="私有"
            :disabled="!isSuperAdmin"
          />
        </el-form-item>
        <el-form-item label="可修改">
          <el-switch
            v-model="formData.isEditable"
            active-text="是"
            inactive-text="否"
            :disabled="!isSuperAdmin"
          />
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
import { ref, reactive, onMounted, watch, computed, nextTick } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Upload } from '@element-plus/icons-vue'
import type { SystemConfig, SystemConfigCreate, SystemConfigUpdate, Tenant } from '@/api/system/types'
import { getSystemConfigs, createSystemConfig, updateSystemConfig, deleteSystemConfig, getTenants } from '@/api/system'
import { useUserStore } from '@/stores/user'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useSortAutoFill } from '@/composables/useSortAutoFill'

const userStore = useUserStore()
const systemConfigStore = useSystemConfigStore()

// 判断是否为超级管理员
const isSuperAdmin = computed(() => userStore.isSuperAdmin)
// 判断当前用户是否为平台租户用户
const isPlatformTenantUser = computed(() => String(userStore.currentTenantId) === '1' || userStore.currentTenantId === 1)
// 判断当前用户是否拥有指定权限（超级管理员不受限制）
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 租户列表
const tenantList = ref<Tenant[]>([])

// 配置分组标签
const configTabs = ref([
  { key: '', label: '全部', count: 0 },
  { key: 'System', label: '系统设置', count: 0 },
  { key: 'Security', label: '安全策略', count: 0 },
  { key: 'AuditLog', label: '审计日志', count: 0 },
  { key: 'Upload', label: '文件上传', count: 0 }
])

const activeTab = ref('')

// 搜索表单
const searchForm = reactive({
  configKey: '',
  configGroup: '',
  tenantId: undefined as number | string | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<SystemConfig[]>([])

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

const formData = reactive({
  id: 0,
  configKey: '',
  configValue: '',
  configGroup: 'System',
  description: '',
  isPublic: false,
  isEditable: true,
  sort: 0
})

// 排序自动填充 - 直接从 API 获取完整数据
const { autoSort, calculateAutoSort } = useSortAutoFill(
  async (options) => {
    const effectiveTenantId = isSuperAdmin.value
      ? Number(searchForm.tenantId || 1)
      : undefined
    const res = await getSystemConfigs({
      configGroup: options?.group ?? undefined,
      tenantId: Number(options?.tenantId) || effectiveTenantId,
      pageIndex: 1,
      pageSize: 9999
    })
    return res.list
  },
  () => null, // 系统配置无层级
  () => formData.configGroup,
  () => isSuperAdmin.value ? (searchForm.tenantId || 1) : undefined
)

// 监听 configGroup 变化，自动更新排序值（仅新增时有效）
watch(() => formData.configGroup, async () => {
  if (!dialogVisible.value || isEdit.value) return
  await calculateAutoSort()
  formData.sort = autoSort.value
})

const formRules: FormRules = {
  configKey: [{ required: true, message: '请输入配置键', trigger: 'blur' }],
  configValue: [{ required: true, message: '请输入配置值', trigger: 'blur' }],
  configGroup: [{ required: true, message: '请选择配置分组', trigger: 'change' }]
}

// 加载租户列表
const loadTenants = async () => {
  if (!isSuperAdmin.value) return
  try {
    const res = await getTenants({ pageIndex: 1, pageSize: 100 })
    tenantList.value = res.list
    // 确保 tenantList 加载完成后再设置 searchForm.tenantId
    await nextTick()
    if (searchForm.tenantId === undefined) {
      searchForm.tenantId = '1'  // 默认平台租户
    }
  } catch {
    // 加载租户失败
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    // 超级管理员：使用选择的租户（非空则用选择的，否则默认平台租户）
    // 非超级管理员：不传 tenantId，后端使用当前登录用户的租户
    const effectiveTenantId = isSuperAdmin.value
      ? Number(searchForm.tenantId || 1)
      : undefined
    // 优先使用搜索框的分组筛选，如果没有则使用页签筛选
    const groupFilter = searchForm.configGroup || activeTab.value || undefined
    const res = await getSystemConfigs({
      configKey: searchForm.configKey || undefined,
      configGroup: groupFilter === '' ? undefined : groupFilter,
      tenantId: effectiveTenantId,
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

// 加载分组计数
const loadGroupCounts = async () => {
  try {
    const groups = ['System', 'Security', 'AuditLog', 'Upload']
    const counts: Record<string, number> = {}
    // 超级管理员：使用选择的租户；非超级管理员：不传（后端使用当前用户租户）
    const effectiveTenantId = isSuperAdmin.value
      ? Number(searchForm.tenantId || 1)
      : undefined

    for (const group of groups) {
      const res = await getSystemConfigs({
        configGroup: group,
        tenantId: effectiveTenantId,
        pageIndex: 1,
        pageSize: 1
      })
      counts[group] = res.total
    }

    // 更新全部计数
    const totalRes = await getSystemConfigs({
      tenantId: effectiveTenantId,
      pageIndex: 1,
      pageSize: 1
    })
    counts[''] = totalRes.total

    // 更新标签数量
    configTabs.value.forEach(tab => {
      tab.count = counts[tab.key] ?? 0
    })
  } catch {
    // 加载分组计数失败
  }
}

// 判断是否可以编辑
const canEdit = (row: SystemConfig): boolean => {
  // 超级管理员可以编辑所有数据
  if (isSuperAdmin.value) return true
  // 其他用户仅可编辑 isEditable=true 的数据
  return row.isEditable
}

// 判断是否可以删除
const canDelete = (row: SystemConfig): boolean => {
  // 系统配置禁止任何用户删除
  if (row.isSystem) return false
  // 超级管理员可删除所有非系统配置
  if (isSuperAdmin.value) return true
  // 平台租户普通用户仅可删除 isEditable=true 的数据
  if (isPlatformTenantUser.value) return row.isEditable
  // 其他租户用户仅可删除自己租户的私有配置（IsPublic=false 且 TenantId 匹配）
  return row.tenantId === userStore.currentTenantId && !row.isPublic
}

// 监听标签切换
watch(activeTab, () => {
  // 切换页签时，清空搜索框的分组选择
  searchForm.configGroup = ''
  pagination.pageIndex = 1
  loadData()
})

// 监听租户切换
watch(() => searchForm.tenantId, () => {
  pagination.pageIndex = 1
  loadData()
  loadGroupCounts()
})

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.configKey = ''
  searchForm.configGroup = ''
  // 重置时根据用户角色设置默认租户
  searchForm.tenantId = isSuperAdmin.value ? '1' : userStore.currentTenantId
  pagination.pageIndex = 1
  loadData()
}

// 新增
const handleAdd = async () => {
  isEdit.value = false
  formData.id = 0
  formData.configKey = ''
  formData.configValue = ''
  formData.configGroup = activeTab.value || 'System'
  formData.description = ''
  formData.isPublic = false
  formData.isEditable = true
  // 计算自动填充排序值
  await calculateAutoSort()
  formData.sort = autoSort.value
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: SystemConfig) => {
  isEdit.value = true
  formData.id = row.id
  formData.configKey = row.configKey
  formData.configValue = row.configValue
  formData.configGroup = row.configGroup
  formData.description = row.description || ''
  formData.isPublic = row.isPublic
  formData.isEditable = row.isEditable
  formData.sort = row.sort
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: SystemConfig) => {
  try {
    await ElMessageBox.confirm(`确定要删除配置 "${row.configKey}" 吗？`, '提示', {
      type: 'warning'
    })
    await deleteSystemConfig(row.id)
    ElMessage.success('删除成功')
    loadData()
    loadGroupCounts()
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
        if (isEdit.value) {
          const data: SystemConfigUpdate = {
            id: formData.id,
            configKey: formData.configKey,
            configValue: formData.configValue,
            configGroup: formData.configGroup,
            description: formData.description,
            isPublic: formData.isPublic,
            isEditable: formData.isEditable,
            sort: formData.sort
          }
          await updateSystemConfig(data)
          ElMessage.success('更新成功')
        } else {
          const data: SystemConfigCreate = {
            configKey: formData.configKey,
            configValue: formData.configValue,
            configGroup: formData.configGroup,
            description: formData.description,
            isPublic: formData.isPublic,
            isEditable: formData.isEditable,
            sort: formData.sort
          }
          await createSystemConfig(data)
          ElMessage.success('创建成功')
        }
        dialogVisible.value = false
        loadData()
        loadGroupCounts()
      } catch (error) {
        ElMessage.error((error as Error).message || '操作失败')
      } finally {
        submitLoading.value = false
      }
    }
  })
}

// 获取分组标签
const getGroupLabel = (group: string) => {
  const map: Record<string, string> = {
    system: '系统设置',
    security: '安全策略',
    auditlog: '审计日志',
    upload: '文件上传'
  }
  return map[group] || group
}

// 获取分组标签类型
const getGroupTagType = (group: string) => {
  const map: Record<string, string> = {
    system: 'primary',
    security: 'danger',
    auditlog: 'warning',
    upload: 'success'
  }
  return map[group] || 'info'
}

// 判断是否为JSON
const isJson = (str: string) => {
  try {
    JSON.parse(str)
    return true
  } catch {
    return false
  }
}

// 格式化JSON值
const formatJsonValue = (str: string) => {
  try {
    const obj = JSON.parse(str)
    return JSON.stringify(obj, null, 2)
  } catch {
    return str
  }
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

// Logo上传 - 将图片转换为Base64
const handleLogoUpload = (file: File) => {
  const reader = new FileReader()
  reader.onload = (e) => {
    const result = e.target?.result as string
    formData.configValue = result
    ElMessage.success('图片上传成功，已转为Base64')
  }
  reader.onerror = () => {
    ElMessage.error('图片读取失败')
  }
  reader.readAsDataURL(file)
  return false // 阻止默认上传行为
}

onMounted(async () => {
  // 确保系统配置已加载
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  // 应用默认分页大小
  pagination.pageSize = systemConfigStore.defaultPageSize
  // 初始化默认租户：超级管理员默认平台租户，非超级管理员默认当前用户租户
  searchForm.tenantId = isSuperAdmin.value ? '1' : userStore.currentTenantId
  loadData()
  loadGroupCounts()
  loadTenants()
})
</script>

<style scoped>
.system-config-management {
  width: 100%;
}

/* 卡片样式 */
.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
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

/* 配置分组标签 */
.config-tabs {
  display: flex;
  gap: 0;
  margin-bottom: 20px;
  background: var(--bg-tertiary);
  border-radius: var(--radius-md);
  overflow: hidden;
}

.tab-item {
  position: relative;
  padding: 12px 24px;
  cursor: pointer;
  transition: all 0.3s ease;
  background: transparent;
}

.tab-item:hover {
  background: var(--bg-hover);
}

.tab-item.active {
  background: var(--bg-elevated);
}

.tab-indicator {
  display: none;
}

.tab-item.active .tab-indicator {
  display: block;
  position: absolute;
  bottom: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 0;
  height: 2px;
  background: var(--primary);
  box-shadow: 0 0 10px var(--primary);
  animation: tabIndicator 0.3s ease-out forwards;
}

@keyframes tabIndicator {
  to {
    width: 60%;
  }
}

.tab-label {
  font-size: 14px;
  font-weight: 500;
  color: var(--text-secondary);
  transition: color 0.3s ease;
}

.tab-item.active .tab-label {
  color: var(--primary);
}

.tab-count {
  margin-left: 8px;
  padding: 2px 8px;
  background: var(--bg-secondary);
  border-radius: 10px;
  font-size: 12px;
  color: var(--text-tertiary);
  font-family: 'JetBrains Mono', monospace;
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

/* 配置键样式 */
.config-key-code {
  font-family: 'JetBrains Mono', monospace;
  font-size: 13px;
  background: rgba(6, 212, 228, 0.1);
  padding: 4px 8px;
  border-radius: 4px;
  color: var(--primary);
  border: 1px solid rgba(6, 212, 228, 0.2);
}

/* 配置值样式 */
.config-value-wrapper {
  max-width: 300px;
}

.config-value-code {
  font-family: 'JetBrains Mono', monospace;
  font-size: 12px;
  background: var(--bg-secondary);
  padding: 4px 8px;
  border-radius: 4px;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.config-value-json {
  font-family: 'JetBrains Mono', monospace;
  font-size: 11px;
  background: var(--bg-secondary);
  padding: 4px 8px;
  border-radius: 4px;
  color: var(--success);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 开关样式 - 科技风 */
:deep(.el-switch) {
  --el-switch-on-color: var(--primary);
}

:deep(.el-switch__core) {
  border: 1px solid var(--border-primary);
}

:deep(.el-switch__core.is-checked) {
  background: var(--primary);
  border-color: var(--primary);
  box-shadow: var(--shadow-glow-primary);
}

:deep(.el-switch__action) {
  background: var(--bg-tertiary);
}

:deep(.el-switch__action .el-switch__action-text) {
  color: var(--text-secondary);
}

/* 分页 - 科技风 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 配置值编辑器 */
.config-value-editor {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
}

/* Logo上传按钮 */
.logo-upload {
  display: flex;
  align-items: center;
}

.logo-upload :deep(.el-button) {
  background: var(--bg-secondary);
  border-color: var(--border-primary);
  color: var(--text-secondary);
  transition: all 0.3s ease;
}

.logo-upload :deep(.el-button:hover) {
  background: var(--bg-hover);
  border-color: var(--primary);
  color: var(--primary);
}
</style>
