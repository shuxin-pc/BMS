<template>
  <div class="tenant-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="租户名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入租户名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="租户编码">
            <el-input
              v-model="searchForm.code"
              placeholder="请输入租户编码"
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
        <el-button type="primary" @click="handleAdd()">
          <el-icon><Plus /></el-icon>
          新增租户
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
          v-if="hasPermission('system:tenant:batchDelete')"
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
        <el-table-column prop="name" label="租户名称" min-width="180">
          <template #default="{ row }">
            <div class="tenant-name">
              <span class="status-indicator" :class="row.status === 1 ? 'success' : 'danger'"></span>
              <span class="name-text">{{ row.name }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="code" label="租户编码" width="150" />
        <el-table-column prop="contactName" label="联系人" width="120" />
        <el-table-column prop="contactPhone" label="联系电话" width="130" />
        <el-table-column prop="status" label="状态" width="80">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
              {{ row.status === 1 ? '启用' : '禁用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="expireTime" label="过期时间" width="170">
          <template #default="{ row }">
            {{ row.expireTime ? row.expireTime.slice(0, 10) : '永不过期' }}
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleView(row)">
              <el-icon><View /></el-icon>
              详情
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
      :title="isEdit ? '编辑租户' : '新增租户'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="租户名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入租户名称" />
        </el-form-item>
        <el-form-item label="租户编码" prop="code">
          <el-input v-model="formData.code" placeholder="请输入租户编码" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="联系人" prop="contactName">
          <el-input v-model="formData.contactName" placeholder="请输入联系人" />
        </el-form-item>
        <el-form-item label="联系电话" prop="contactPhone">
          <el-input v-model="formData.contactPhone" placeholder="请输入联系电话" />
        </el-form-item>
        <el-form-item label="联系邮箱" prop="contactEmail">
          <el-input v-model="formData.contactEmail" placeholder="请输入联系邮箱" />
        </el-form-item>
        <el-form-item label="状态" prop="status">
          <el-radio-group v-model="formData.status">
            <el-radio :value="1">启用</el-radio>
            <el-radio :value="0">禁用</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="过期时间" prop="expireTime">
          <el-date-picker
            v-model="formData.expireTime"
            type="date"
            placeholder="选择过期时间（不设置则永不过期）"
            format="YYYY-MM-DD"
            value-format="YYYY-MM-DD"
            clearable
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 租户详情弹窗 -->
    <el-dialog
      v-model="detailVisible"
      title="租户详情"
      width="1000px"
      :close-on-click-modal="false"
    >
      <div class="tenant-detail" v-if="currentTenant">
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
                  <span class="item-label">租户名称</span>
                  <span class="item-value">{{ currentTenant.name }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">租户编码</span>
                  <span class="item-value">{{ currentTenant.code }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">联系人</span>
                  <span class="item-value">{{ currentTenant.contactName }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">联系电话</span>
                  <span class="item-value">{{ currentTenant.contactPhone }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">联系邮箱</span>
                  <span class="item-value">{{ currentTenant.contactEmail }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">状态</span>
                  <span class="item-value">
                    <el-tag :type="currentTenant.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
                      {{ currentTenant.status === 1 ? '启用' : '禁用' }}
                    </el-tag>
                  </span>
                </div>
                <div class="detail-item">
                  <span class="item-label">过期时间</span>
                  <span class="item-value">{{ currentTenant.expireTime ? currentTenant.expireTime.slice(0, 10) : '永不过期' }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">创建时间</span>
                  <span class="item-value">{{ formatDate(currentTenant.createdAt) }}</span>
                </div>
                <div class="detail-item">
                  <span class="item-label">备注</span>
                  <span class="item-value">{{ currentTenant.remark || '-' }}</span>
                </div>
              </div>
            </div>
          </el-tab-pane>

          <!-- 子系统分配 -->
          <el-tab-pane label="子系统分配" name="subsystems">
            <div class="detail-section">
              <h3 class="section-title">
                <span class="title-text">子系统分配</span>
                <span class="title-line"></span>
              </h3>
              <div class="subsystem-assignment">
                <div class="subsystem-list">
                  <div v-if="subsystemLoading" class="loading-container">
                    <el-skeleton :rows="6" animated />
                  </div>
                  <el-checkbox-group v-if="!subsystemLoading" v-model="selectedSubsystemIds">
                    <div class="subsystem-item" v-for="subsystem in allSubsystems" :key="subsystem.id">
                      <el-checkbox :value="subsystem.id" class="subsystem-checkbox">
                        <div class="subsystem-info">
                          <div class="subsystem-header">
                            <el-icon v-if="subsystem.icon" class="subsystem-icon"><component :is="subsystem.icon" /></el-icon>
                            <span class="subsystem-name">{{ subsystem.name }}</span>
                          </div>
                          <div class="subsystem-meta">
                            <span class="subsystem-code">{{ subsystem.code }}</span>
                            <el-tag :type="subsystem.status === 1 ? 'success' : 'danger'" size="small" class="status-tag">
                              {{ subsystem.status === 1 ? '启用' : '禁用' }}
                            </el-tag>
                          </div>
                        </div>
                      </el-checkbox>
                    </div>
                  </el-checkbox-group>
                </div>
              </div>
            </div>
          </el-tab-pane>
        </el-tabs>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
        <el-button type="primary" @click="handleEdit(currentTenant!)">
          编辑
        </el-button>
        <el-button type="success" :loading="subsystemSaveLoading" @click="handleSubsystemSave" v-if="activeTab === 'subsystems'">
          保存
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, View } from '@element-plus/icons-vue'
import { getTenants, createTenant, updateTenant, deleteTenant, deleteTenants, getSubsystemsAll, getTenantSubsystems, assignTenantSubsystems } from '@/api/system'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { Tenant, Subsystem, TenantCreate, TenantUpdate } from '@/api/system/types'
import { formatDateTime as formatDate } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()

// 判断当前用户是否拥有指定权限（超级管理员不受限制）
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  status: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Tenant[]>([])
const selectedRows = ref<Tenant[]>([])

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
  name: '',
  code: '',
  contactName: '',
  contactPhone: '',
  contactEmail: '',
  status: 1,
  expireTime: '',
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '租户名称不能为空', trigger: 'blur' },
    { max: 100, message: '租户名称最多100个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '租户编码不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '租户编码长度为2-50个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '租户编码只能包含字母、数字、下划线', trigger: 'blur' }
  ],
  contactName: [
    { max: 50, message: '联系人最多50个字符', trigger: 'blur' }
  ],
  contactPhone: [
    { required: true, message: '联系电话不能为空', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确', trigger: 'blur' }
  ],
  contactEmail: [
    { type: 'email', message: '邮箱格式不正确', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
}

// 详情
const detailVisible = ref(false)
const currentTenant = ref<Tenant | null>(null)
const activeTab = ref('basic')
const subsystemLoading = ref(false)
const subsystemSaveLoading = ref(false)
const allSubsystems = ref<Subsystem[]>([])
const selectedSubsystemIds = ref<number[]>([])

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getTenants({
      name: searchForm.name || undefined,
      code: searchForm.code || undefined,
      status: searchForm.status,
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
  handleSearch()
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.contactName = ''
  formData.contactPhone = ''
  formData.contactEmail = ''
  formData.status = 1
  formData.expireTime = ''
  formData.remark = ''
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Tenant) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.contactName = row.contactName
  formData.contactPhone = row.contactPhone
  formData.contactEmail = row.contactEmail
  formData.status = row.status
  formData.expireTime = row.expireTime ? row.expireTime.slice(0, 10) : ''
  formData.remark = row.remark || ''
  dialogVisible.value = true
  detailVisible.value = false
}

// 查看详情
const handleView = async (row: Tenant) => {
  currentTenant.value = row
  detailVisible.value = true
  await loadSubsystemData(row.id)
}

// 加载子系统数据
const loadSubsystemData = async (tenantId: number) => {
  subsystemLoading.value = true
  try {
    // 加载所有子系统（不分启用/禁用）
    const [allSubsystemsRes, tenantSubsystemsRes] = await Promise.all([
      getSubsystemsAll(),
      getTenantSubsystems(tenantId)
    ])
    allSubsystems.value = allSubsystemsRes
    selectedSubsystemIds.value = tenantSubsystemsRes
  } catch {
    ElMessage.error('加载子系统数据失败')
  } finally {
    subsystemLoading.value = false
  }
}

// 保存子系统分配
const handleSubsystemSave = async () => {
  if (!currentTenant.value) return

  subsystemSaveLoading.value = true
  try {
    await assignTenantSubsystems(currentTenant.value.id, {
      SubsystemIds: selectedSubsystemIds.value
    })
    ElMessage.success('保存成功')
  } catch (error) {
    ElMessage.error((error as Error).message || '保存失败')
  } finally {
    subsystemSaveLoading.value = false
  }
}

// Tab切换前检查
const handleTabBeforeChange = async (newName: string) => {
  // 如果当前是子系统分配Tab且有未保存的更改，提示用户
  if (activeTab.value === 'subsystems' && newName !== 'subsystems') {
    // 检查是否有更改
    if (currentTenant.value) {
      try {
        const currentSubsystems = await getTenantSubsystems(currentTenant.value.id)
        if (JSON.stringify(currentSubsystems) !== JSON.stringify(selectedSubsystemIds.value)) {
          try {
            await ElMessageBox.confirm('子系统分配有未保存的更改，是否继续切换？', '提示', {
              type: 'warning',
              distinguishCancelAndClose: true
            })
          } catch {
            return false
          }
        }
      } catch {
        // 检查子系统更改时出错
      }
    }
  }
  return true
}

// 删除
const handleDelete = async (row: Tenant) => {
  try {
    await ElMessageBox.confirm(`确定要删除租户 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteTenant(row.id)
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
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个租户吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteTenants(ids)
    ElMessage.success('批量删除成功')
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
        if (isEdit.value) {
          const data = {
            id: formData.id,
            name: formData.name,
            contactName: formData.contactName,
            contactPhone: formData.contactPhone,
            contactEmail: formData.contactEmail,
            status: formData.status,
            expireTime: formData.expireTime || null,
            remark: formData.remark,
            isolationLevel: 1
          }
          await updateTenant(data as unknown as TenantUpdate)
          ElMessage.success('更新成功')
        } else {
          const data = {
            name: formData.name,
            code: formData.code,
            contactName: formData.contactName,
            contactPhone: formData.contactPhone,
            contactEmail: formData.contactEmail,
            status: formData.status,
            expireTime: formData.expireTime || null,
            remark: formData.remark,
            isolationLevel: 1
          }
          await createTenant(data as unknown as TenantCreate)
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
const handleSelectionChange = (rows: Tenant[]) => {
  selectedRows.value = rows
}


onMounted(async () => {
  // 确保系统配置已加载
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  // 应用默认分页大小
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadData()
})
</script>

<style scoped>
.tenant-management {
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

/* 租户名称显示 */
.tenant-name {
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  position: relative;
}

.status-indicator::after {
  content: '';
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 16px;
  height: 16px;
  border-radius: 50%;
  opacity: 0.3;
}

.status-indicator.success::after {
  background: var(--success);
}

.status-indicator.danger::after {
  background: var(--danger);
}

.status-indicator.success {
  background: var(--success);
  box-shadow: 0 0 8px var(--success);
}

.status-indicator.danger {
  background: var(--danger);
  box-shadow: 0 0 8px var(--danger);
}

.name-text {
  font-weight: 500;
}

/* 详情样式 - 浅色弹窗 */
.tenant-detail {
  padding: 24px;
  background: #f9fafb;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
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
  background: linear-gradient(180deg, var(--primary), var(--success));
  border-radius: 2px;
}

.title-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(90deg, var(--border-primary) 0%, transparent 100%);
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
  background: var(--primary);
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

.detail-item:hover {
  border-color: #06b6d4;
  background: #f9fafb;
}

.item-label {
  font-size: 11px;
  color: #9ca3af;
  text-transform: uppercase;
  letter-spacing: 0.8px;
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
  background: var(--primary);
  opacity: 0.6;
}

.item-value {
  font-size: 16px;
  color: #1f2937;
  font-weight: 600;
  font-family: 'JetBrains Mono', 'SF Mono', monospace;
  letter-spacing: 0.3px;
}

/* 统计卡片网格 */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

.stat-card {
  background: var(--bg-elevated);
  border: 1px solid var(--border-primary);
  border-radius: var(--radius-md);
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  transition: all 0.3s ease;
}

.stat-card:hover {
  border-color: var(--primary);
  box-shadow: var(--shadow-glow-primary);
  transform: translateY(-2px);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.user-icon {
  background: rgba(16, 250, 158, 0.15);
  color: var(--success);
}

.org-icon {
  background: rgba(91, 155, 255, 0.15);
  color: var(--info);
}

.data-icon {
  background: rgba(251, 191, 36, 0.15);
  color: var(--warning);
}

.config-icon {
  background: rgba(6, 212, 228, 0.15);
  color: var(--primary);
}

.stat-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.stat-value {
  font-size: 24px;
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
  color: var(--text-primary);
}

.stat-label {
  font-size: 12px;
  color: var(--text-tertiary);
}

/* 分页 - 科技风 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* Tabs样式 */
:deep(.el-tabs__item) {
  color: #6b7280;
}

:deep(.el-tabs__item.is-active) {
  color: #06b6d4;
  font-weight: 600;
}

:deep(.el-tabs__active-bar) {
  background-color: #06b6d4;
}

:deep(.el-tabs__nav-wrap::after) {
  border-color: #e5e7eb;
}

/* 子系统分配样式 - 浅色弹窗 */
.subsystem-assignment {
  margin-top: 20px;
}

.subsystem-list {
  max-height: 500px;
  overflow-y: auto;
  padding: 8px 0;
}

.loading-container {
  padding: 20px;
}

.subsystem-item {
  padding: 16px 20px;
  background: #ffffff;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
  margin-bottom: 12px;
  transition: all 0.2s ease;
}

.subsystem-item:hover {
  border-color: #06b6d4;
  background: #f9fafb;
}

.subsystem-checkbox {
  width: 100%;
}

.subsystem-info {
  margin-left: 8px;
  flex: 1;
}

.subsystem-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.subsystem-icon {
  color: #06b6d4;
  font-size: 18px;
}

.subsystem-name {
  font-size: 15px;
  font-weight: 600;
  color: #1f2937;
}

.subsystem-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 6px;
}

.subsystem-code {
  font-size: 12px;
  color: #9ca3af;
  font-family: 'JetBrains Mono', monospace;
}

.status-tag {
  margin-left: auto;
}

.subsystem-desc {
  font-size: 13px;
  color: #6b7280;
  line-height: 1.5;
}

/* 自定义checkbox样式 */
:deep(.el-checkbox__label) {
  width: 100%;
}

:deep(.el-checkbox) {
  display: flex;
  align-items: flex-start;
}
</style>
