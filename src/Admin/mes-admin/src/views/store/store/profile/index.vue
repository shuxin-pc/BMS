<template>
  <div class="store-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="门店名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入门店名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="门店编号">
            <el-input
              v-model="searchForm.code"
              placeholder="请输入门店编号"
              clearable
              style="width: 150px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="营业" :value="1" />
              <el-option label="歇业" :value="2" />
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
          新增门店
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
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
        <el-table-column prop="code" label="门店编号" width="110" />
        <el-table-column prop="name" label="门店名称" min-width="180">
          <template #default="{ row }">
            <div class="store-name">
              <span class="status-indicator" :class="row.status === 1 ? 'success' : 'danger'"></span>
              <span class="name-text">{{ row.name }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="联系电话" width="140" />
        <el-table-column prop="address" label="地址" min-width="220" show-overflow-tooltip />
        <el-table-column prop="managerName" label="店长" width="100" />
        <el-table-column prop="businessHours" label="营业时间" width="140" />
        <el-table-column prop="status" label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'danger'" size="small" effect="dark">
              {{ row.status === 1 ? '营业' : '歇业' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="170">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="primary" size="small" @click="handleAssignUser(row)">
              <el-icon><User /></el-icon>
              分配用户
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
      :title="isEdit ? '编辑门店' : '新增门店'"
      width="900px"
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
            <el-form-item label="门店名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入门店名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="门店编号" prop="code">
              <el-input v-model="formData.code" placeholder="请输入门店编号" :disabled="isEdit" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="门店简称" prop="shortName">
              <el-input v-model="formData.shortName" placeholder="请输入门店简称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="联系电话" prop="phone">
              <el-input v-model="formData.phone" placeholder="请输入联系电话" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="店长" prop="managerName">
              <el-input v-model="formData.managerName" placeholder="请输入店长姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="门店面积" prop="area">
              <el-input-number
                v-model="formData.area"
                :min="0"
                :precision="2"
                placeholder="请输入门店面积"
                style="width: 200px"
              />
              <span style="margin-left: 8px; color: var(--text-tertiary)">平方米</span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="营业开始时间" prop="businessStart">
              <el-time-picker
                v-model="formData.businessStart"
                placeholder="09:00"
                format="HH:mm"
                value-format="HH:mm"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="营业结束时间" prop="businessEnd">
              <el-time-picker
                v-model="formData.businessEnd"
                placeholder="22:00"
                format="HH:mm"
                value-format="HH:mm"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="状态" prop="status">
              <el-radio-group v-model="formData.status">
                <el-radio :value="1">营业</el-radio>
                <el-radio :value="2">歇业</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="详细地址" prop="address">
              <el-input v-model="formData.address" placeholder="请输入详细地址" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="门店Logo" prop="logoUrl">
              <el-input v-model="formData.logoUrl" placeholder="请输入Logo图片URL" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="营业执照" prop="businessLicenseUrl">
              <el-input v-model="formData.businessLicenseUrl" placeholder="请输入营业执照图片URL" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="门店简介" prop="remark">
              <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入门店简介" />
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

    <!-- 分配用户弹窗 -->
    <el-dialog
      v-model="userDialogVisible"
      :title="`分配用户 - ${currentStoreForUser?.name ?? ''}`"
      width="700px"
      :close-on-click-modal="false"
    >
      <el-transfer
        v-model="transferValue"
        :data="transferData"
        :titles="['可分配用户', '已分配用户']"
        filterable
        filter-placeholder="搜索用户名/姓名"
        :left-default-checked="[]"
        :right-default-checked="[]"
      />
      <template #footer>
        <el-button @click="userDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="userSubmitLoading" @click="handleUserSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, User } from '@element-plus/icons-vue'
import { getStores, createStore, updateStore, deleteStore, deleteStores, getStoreAvailableUsers, assignStoreUsers } from '@/api/store'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Store } from '@/api/store/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  status: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Store[]>([])
const selectedRows = ref<Store[]>([])

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
  id: '' as string,
  name: '',
  code: '',
  shortName: '',
  phone: '',
  managerName: '',
  businessStart: '09:00',
  businessEnd: '22:00',
  address: '',
  area: undefined as number | undefined,
  logoUrl: '',
  businessLicenseUrl: '',
  remark: '',
  status: 1 as number
})

const formRules: FormRules = {
  name: [
    { required: true, message: '门店名称不能为空', trigger: 'blur' },
    { max: 100, message: '门店名称最多100个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '门店编号不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '门店编号长度为2-50个字符', trigger: 'blur' },
    { pattern: /^[a-zA-Z0-9_]+$/, message: '门店编号只能包含字母、数字、下划线', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '联系电话不能为空', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确', trigger: 'blur' }
  ],
  managerName: [
    { required: true, message: '店长姓名不能为空', trigger: 'blur' },
    { max: 50, message: '店长姓名最多50个字符', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ]
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getStores({
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

// 重置表单数据
const resetFormData = () => {
  formData.id = ''
  formData.name = ''
  formData.code = ''
  formData.shortName = ''
  formData.phone = ''
  formData.managerName = ''
  formData.businessStart = '09:00'
  formData.businessEnd = '22:00'
  formData.address = ''
  formData.area = undefined
  formData.logoUrl = ''
  formData.businessLicenseUrl = ''
  formData.remark = ''
  formData.status = 1
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Store) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.shortName = row.shortName || ''
  formData.phone = row.phone || ''
  formData.managerName = row.managerName || ''
  // 解析营业时间 "09:00 - 22:00" -> 开始/结束
  const hours = row.businessHours || ''
  const parts = hours.split('-').map(s => s.trim())
  formData.businessStart = parts[0] || '09:00'
  formData.businessEnd = parts[1] || '22:00'
  formData.address = row.address || ''
  formData.area = row.area
  formData.logoUrl = row.logoUrl || ''
  formData.businessLicenseUrl = row.businessLicenseUrl || ''
  formData.remark = row.remark || ''
  formData.status = row.status
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Store) => {
  try {
    await ElMessageBox.confirm(`确定要删除门店 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteStore(row.id)
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
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个门店吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteStores(ids)
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
        // 合并营业时间
        const businessHours = `${formData.businessStart} - ${formData.businessEnd}`
        if (isEdit.value) {
          await updateStore({
            id: formData.id,
            name: formData.name,
            code: formData.code,
            shortName: formData.shortName || undefined,
            phone: formData.phone,
            managerName: formData.managerName,
            businessHours,
            address: formData.address,
            area: formData.area,
            logoUrl: formData.logoUrl || undefined,
            businessLicenseUrl: formData.businessLicenseUrl || undefined,
            remark: formData.remark,
            status: formData.status
          })
          ElMessage.success('更新成功')
        } else {
          await createStore({
            name: formData.name,
            code: formData.code,
            shortName: formData.shortName || undefined,
            phone: formData.phone,
            managerName: formData.managerName,
            businessHours,
            address: formData.address,
            area: formData.area,
            logoUrl: formData.logoUrl || undefined,
            businessLicenseUrl: formData.businessLicenseUrl || undefined,
            remark: formData.remark,
            status: formData.status
          })
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
const handleSelectionChange = (rows: Store[]) => {
  selectedRows.value = rows
}

// ==================== 分配用户 ====================

interface TransferItem {
  key: string
  label: string
}

const userDialogVisible = ref(false)
const currentStoreForUser = ref<Store | null>(null)
const transferData = ref<TransferItem[]>([])
const transferValue = ref<string[]>([])
const userSubmitLoading = ref(false)

// 打开分配用户弹窗
const handleAssignUser = async (row: Store) => {
  currentStoreForUser.value = row
  userDialogVisible.value = true
  transferData.value = []
  transferValue.value = []
  try {
    const users = await getStoreAvailableUsers(row.id)
    transferData.value = users.map(u => ({
      key: u.id,
      label: u.realName ? `${u.realName}（${u.userName}）` : u.userName
    }))
    // 右侧默认显示已分配用户
    transferValue.value = users.filter(u => u.assigned).map(u => u.id)
  } catch (error) {
    ElMessage.error((error as Error).message || '加载用户列表失败')
  }
}

// 提交分配（全量替换）
const handleUserSubmit = async () => {
  if (!currentStoreForUser.value) return
  userSubmitLoading.value = true
  try {
    await assignStoreUsers(currentStoreForUser.value.id, transferValue.value)
    ElMessage.success('分配成功')
    userDialogVisible.value = false
  } catch (error) {
    ElMessage.error((error as Error).message || '分配失败')
  } finally {
    userSubmitLoading.value = false
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
.store-management {
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

/* 门店名称显示 */
.store-name {
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

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
