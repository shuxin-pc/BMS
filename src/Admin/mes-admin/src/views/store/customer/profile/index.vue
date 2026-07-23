<template>
  <div class="customer-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户姓名">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入客户姓名"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="手机号">
            <el-input
              v-model="searchForm.phone"
              placeholder="请输入手机号"
              clearable
              style="width: 150px"
            />
          </el-form-item>
          <el-form-item label="客户等级">
            <el-select v-model="searchForm.levelId" placeholder="全部" clearable style="width: 140px">
              <el-option
                v-for="level in customerLevels"
                :key="level.id"
                :label="level.name"
                :value="level.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="性别">
            <el-select v-model="searchForm.gender" placeholder="全部" clearable style="width: 120px">
              <el-option label="男" :value="1" />
              <el-option label="女" :value="2" />
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
          新增客户
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
        <el-table-column label="客户姓名" min-width="120">
          <template #default="{ row }">
            <div class="customer-name">
              <div class="customer-avatar" :class="row.gender === 2 ? 'female' : 'male'">
                {{ row.name.charAt(0) }}
              </div>
              <span class="name-text">{{ row.name }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column prop="phone" label="手机号" width="140" />
        <el-table-column prop="gender" label="性别" width="80">
          <template #default="{ row }">
            {{ genderText(row.gender) }}
          </template>
        </el-table-column>
        <el-table-column prop="birthday" label="生日" width="120">
          <template #default="{ row }">
            {{ formatDate(row.birthday) }}
          </template>
        </el-table-column>
        <el-table-column prop="levelName" label="等级" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.levelName" type="warning" size="small" effect="dark">
              {{ row.levelName }}
            </el-tag>
            <span v-else class="text-tertiary">普通客户</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalPoints" label="积分" width="100" align="right">
          <template #default="{ row }">
            <span class="points-text">{{ row.totalPoints }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="balance" label="余额" width="120" align="right">
          <template #default="{ row }">
            <span class="balance-text">¥{{ formatPrice(row.balance) }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="totalConsume" label="累计消费" width="120" align="right">
          <template #default="{ row }">
            ¥{{ formatPrice(row.totalConsume) }}
          </template>
        </el-table-column>
        <el-table-column prop="lastConsumeTime" label="最后消费" width="170">
          <template #default="{ row }">
            {{ formatDateTime(row.lastConsumeTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="authorizationStatus" label="授权状态" width="100">
          <template #default="{ row }">
            <el-tag v-if="row.authorizationStatus === 1" type="success" size="small">已授权</el-tag>
            <el-tag v-else-if="row.authorizationStatus === 2" type="danger" size="small">已撤回</el-tag>
            <el-tag v-else type="info" size="small">未授权</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="success" size="small" @click="handleViewStat(row)">
              <el-icon><DataAnalysis /></el-icon>
              统计
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
      :title="isEdit ? '编辑客户' : '新增客户'"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="客户姓名" prop="name">
              <el-input v-model="formData.name" placeholder="请输入客户姓名" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="手机号" prop="phone">
              <el-input v-model="formData.phone" placeholder="请输入手机号" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="性别" prop="gender">
              <el-radio-group v-model="formData.gender">
                <el-radio :value="1">男</el-radio>
                <el-radio :value="2">女</el-radio>
                <el-radio :value="0">未知</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="生日" prop="birthday">
              <el-date-picker
                v-model="formData.birthday"
                type="date"
                placeholder="请选择生日"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="客户等级" prop="levelId">
          <el-select v-model="formData.levelId" placeholder="请选择客户等级" clearable style="width: 100%">
            <el-option
              v-for="level in customerLevels"
              :key="level.id"
              :label="`${level.name}（${level.discountRate}折）`"
              :value="level.id"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="授权状态" prop="authorizationStatus">
              <el-select v-model="formData.authorizationStatus" placeholder="请选择授权状态" style="width: 100%">
                <el-option label="未授权" :value="0" />
                <el-option label="已授权" :value="1" />
                <el-option label="已撤回" :value="2" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="授权时间" prop="authorizationTime">
              <el-date-picker
                v-model="formData.authorizationTime"
                type="datetime"
                placeholder="请选择授权时间"
                format="YYYY-MM-DD HH:mm"
                value-format="YYYY-MM-DD HH:mm:ss"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="地址" prop="address">
          <el-input v-model="formData.address" placeholder="请输入地址" />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注信息" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 消费统计弹窗 -->
    <el-dialog v-model="statVisible" title="客户消费统计" width="560px">
      <div v-loading="statLoading">
        <el-descriptions :column="2" border v-if="statData">
          <el-descriptions-item label="消费频次">{{ statData.orderCount }} 次</el-descriptions-item>
          <el-descriptions-item label="累计消费">¥{{ formatPrice(statData.totalConsumption) }}</el-descriptions-item>
          <el-descriptions-item label="客单价">¥{{ formatPrice(statData.averageOrderValue) }}</el-descriptions-item>
          <el-descriptions-item label="最近消费">{{ statData.lastConsumeTime ? formatDate(statData.lastConsumeTime) : '无' }}</el-descriptions-item>
        </el-descriptions>

        <div v-if="statData && statData.preferences.length > 0" class="stat-preferences">
          <div class="stat-section-title">消费偏好</div>
          <div v-for="item in statData.preferences" :key="item.productType" class="pref-item">
            <div class="pref-header">
              <span class="pref-name">{{ item.productTypeName }}</span>
              <span class="pref-amount">¥{{ formatPrice(item.amount) }}（{{ (item.percentage * 100).toFixed(1) }}%）</span>
            </div>
            <el-progress
              :percentage="Math.round(item.percentage * 100)"
              :show-text="false"
              :stroke-width="8"
              :color="getPrefColor(item.productType)"
            />
          </div>
        </div>
        <el-empty v-else-if="statData" description="暂无消费偏好数据" />
      </div>
      <template #footer>
        <el-button @click="statVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, DataAnalysis } from '@element-plus/icons-vue'
import {
  getCustomers,
  createCustomer,
  updateCustomer,
  deleteCustomer,
  deleteCustomers,
  getCustomerLevels,
  getCustomerConsumptionStat
} from '@/api/customer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { Customer, CustomerLevel, AuthorizationStatus, CustomerConsumptionStat } from '@/api/customer/types'

const systemConfigStore = useSystemConfigStore()

// 客户等级列表
const customerLevels = ref<CustomerLevel[]>([])

// 搜索表单
const searchForm = reactive({
  name: '',
  phone: '',
  levelId: undefined as number | undefined,
  gender: undefined as number | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Customer[]>([])
const selectedRows = ref<Customer[]>([])

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
  phone: '',
  gender: 0 as number,
  birthday: '',
  levelId: undefined as number | undefined,
  authorizationStatus: 0 as AuthorizationStatus,
  authorizationTime: '',
  address: '',
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '客户姓名不能为空', trigger: 'blur' },
    { max: 50, message: '客户姓名最多50个字符', trigger: 'blur' }
  ],
  phone: [
    { required: true, message: '手机号不能为空', trigger: 'blur' },
    { pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确', trigger: 'blur' }
  ],
  gender: [
    { required: true, message: '请选择性别', trigger: 'change' }
  ]
}

// 加载客户等级列表
const loadCustomerLevels = async () => {
  try {
    customerLevels.value = await getCustomerLevels()
  } catch (error) {
    customerLevels.value = []
  }
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getCustomers({
      name: searchForm.name || undefined,
      phone: searchForm.phone || undefined,
      levelId: searchForm.levelId,
      gender: searchForm.gender,
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

// 搜索
const handleSearch = () => {
  pagination.pageIndex = 1
  loadData()
}

// 重置
const handleReset = () => {
  searchForm.name = ''
  searchForm.phone = ''
  searchForm.levelId = undefined
  searchForm.gender = undefined
  handleSearch()
}

// 重置表单数据
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.phone = ''
  formData.gender = 0
  formData.birthday = ''
  formData.levelId = undefined
  formData.authorizationStatus = 0
  formData.authorizationTime = ''
  formData.address = ''
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Customer) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.phone = row.phone
  formData.gender = row.gender
  formData.birthday = row.birthday || ''
  formData.levelId = row.levelId
  formData.authorizationStatus = row.authorizationStatus ?? 0
  formData.authorizationTime = row.authorizationTime || ''
  formData.address = row.address || ''
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Customer) => {
  try {
    await ElMessageBox.confirm(`确定要删除客户 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteCustomer(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

// ==================== 消费统计 ====================
const statVisible = ref(false)
const statLoading = ref(false)
const statData = ref<CustomerConsumptionStat | null>(null)

const handleViewStat = async (row: Customer) => {
  statVisible.value = true
  statLoading.value = true
  statData.value = null
  try {
    statData.value = await getCustomerConsumptionStat(row.id)
  } catch (error: any) {
    ElMessage.error(error.message || '加载消费统计失败')
  } finally {
    statLoading.value = false
  }
}

const getPrefColor = (productType: number) => {
  const colors: Record<number, string> = { 1: '#409eff', 2: '#67c23a', 4: '#e6a23c' }
  return colors[productType] || '#909399'
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个客户吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    const ids = selectedRows.value.map(row => row.id)
    await deleteCustomers(ids)
    ElMessage.success('批量删除成功')
    loadData()
  } catch (error: any) {
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
        const payload = {
          name: formData.name,
          phone: formData.phone,
          gender: formData.gender,
          birthday: formData.birthday || undefined,
          levelId: formData.levelId || undefined,
          authorizationStatus: formData.authorizationStatus,
          authorizationTime: formData.authorizationTime || undefined,
          address: formData.address || undefined,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateCustomer({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createCustomer(payload)
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
const handleSelectionChange = (rows: Customer[]) => {
  selectedRows.value = rows
}

// 性别文本
const genderText = (gender: number) => {
  const map: Record<number, string> = { 0: '未知', 1: '男', 2: '女' }
  return map[gender] || '未知'
}

// 格式化价格
const formatPrice = (price: number) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

// 格式化日期（仅日期）
const formatDate = (dateStr?: string) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
}

// 格式化日期时间
const formatDateTime = (dateStr?: string) => {
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
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadCustomerLevels()
  loadData()
})
</script>

<style scoped>
.customer-management {
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

/* 客户姓名显示 */
.customer-name {
  display: flex;
  align-items: center;
  gap: 10px;
}

.customer-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 13px;
  font-weight: 600;
  flex-shrink: 0;
}

.customer-avatar.male {
  background: linear-gradient(135deg, #5b9bff, #3b82f6);
  color: #fff;
}

.customer-avatar.female {
  background: linear-gradient(135deg, #f97373, #ec4899);
  color: #fff;
}

.name-text {
  font-weight: 500;
  color: var(--text-primary);
}

.text-tertiary {
  color: var(--text-tertiary);
}

.points-text {
  color: var(--warning);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
}

.balance-text {
  color: var(--success);
  font-weight: 600;
  font-family: 'JetBrains Mono', monospace;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}

/* 消费统计弹窗 */
.stat-preferences {
  margin-top: 20px;
}

.stat-section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 12px;
}

.pref-item {
  margin-bottom: 12px;
}

.pref-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
  font-size: 13px;
}

.pref-name {
  color: var(--text-primary);
}

.pref-amount {
  color: var(--text-tertiary);
}
</style>
