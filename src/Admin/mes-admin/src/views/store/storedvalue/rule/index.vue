<template>
  <div class="rule-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="规则名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入规则名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.isEnabled" placeholder="全部" clearable style="width: 120px">
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
        <el-button type="primary" @click="handleAdd()" v-if="hasPermission('store:storedvalue:rule:add')">
          <el-icon><Plus /></el-icon>
          新增规则
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
          v-if="hasPermission('store:storedvalue:rule:batchDelete')"
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
        <el-table-column prop="name" label="规则名称" min-width="160">
          <template #default="{ row }">
            <span class="rule-name">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column label="充值金额" width="120" align="right">
          <template #default="{ row }">
            <span>¥{{ formatPrice(row.amount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="赠送金额" width="120" align="right">
          <template #default="{ row }">
            <span class="bonus-text">¥{{ formatPrice(row.giftAmount) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="赠送比例" width="100" align="center">
          <template #default="{ row }">
            <el-tag type="success" size="small" effect="plain">
              {{ formatBonusRate(row.amount, row.giftAmount) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90" align="center">
          <template #default="{ row }">
            <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small" effect="dark">
              {{ row.isEnabled ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="生效时间" width="120">
          <template #default="{ row }">
            <span>{{ formatDate(row.startDate) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="失效时间" width="120">
          <template #default="{ row }">
            <span v-if="row.endDate">{{ formatDate(row.endDate) }}</span>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="150" show-overflow-tooltip />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)" v-if="hasPermission('store:storedvalue:rule:edit')">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)" v-if="hasPermission('store:storedvalue:rule:delete')">
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
      :title="isEdit ? '编辑规则' : '新增规则'"
      width="560px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="规则名称" prop="name">
          <el-input v-model="formData.name" placeholder="如：充值500送50" />
        </el-form-item>
        <el-form-item label="充值金额" prop="amount">
          <el-input-number
            v-model="formData.amount"
            :min="0.01"
            :precision="2"
            :step="100"
            controls-position="right"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="赠送金额" prop="giftAmount">
          <el-input-number
            v-model="formData.giftAmount"
            :min="0"
            :precision="2"
            :step="10"
            controls-position="right"
            style="width: 100%"
          />
          <div class="form-tip">赠送比例：{{ computedBonusRate }}</div>
        </el-form-item>
        <el-form-item label="生效状态" prop="isEnabled">
          <el-radio-group v-model="formData.isEnabled">
            <el-radio :value="true">启用</el-radio>
            <el-radio :value="false">停用</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="生效时间" prop="startDate">
          <el-date-picker
            v-model="formData.startDate"
            type="date"
            placeholder="选择生效日期"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="失效时间" prop="endDate">
          <el-date-picker
            v-model="formData.endDate"
            type="date"
            placeholder="留空表示长期有效"
            value-format="YYYY-MM-DD"
            style="width: 100%"
          />
        </el-form-item>
        <el-form-item label="备注" prop="remark">
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit } from '@element-plus/icons-vue'
import { getRechargeRules, createRechargeRule, updateRechargeRule, deleteRechargeRule } from '@/api/member'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { RechargeRule } from '@/api/member/types'
import { formatDate } from '@/utils/date'

const systemConfigStore = useSystemConfigStore()
const userStore = useUserStore()
const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  name: '',
  isEnabled: undefined as boolean | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<RechargeRule[]>([])
const selectedRows = ref<RechargeRule[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getRechargeRules({
      name: searchForm.name || undefined,
      isEnabled: searchForm.isEnabled,
      pageIndex: pagination.pageIndex,
      pageSize: pagination.pageSize
    })
    tableData.value = res.list
    pagination.total = res.total
  } catch (error) {
    ElMessage.error((error as Error).message || '加载数据失败')
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
  searchForm.isEnabled = undefined
  handleSearch()
}

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

const formData = reactive({
  id: 0,
  name: '',
  amount: 500,
  giftAmount: 50,
  isEnabled: true,
  sort: 0,
  startDate: '',
  endDate: '',
  remark: ''
})

const computedBonusRate = computed(() => {
  if (formData.amount <= 0) return '0%'
  return ((formData.giftAmount / formData.amount) * 100).toFixed(1) + '%'
})

const formRules: FormRules = {
  name: [
    { required: true, message: '规则名称不能为空', trigger: 'blur' },
    { max: 100, message: '规则名称最多100个字符', trigger: 'blur' }
  ],
  amount: [
    { required: true, message: '充值金额不能为空', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '充值金额必须大于0', trigger: 'blur' }
  ],
  giftAmount: [
    { type: 'number', min: 0, message: '赠送金额不能小于0', trigger: 'blur' }
  ],
  isEnabled: [
    { required: true, message: '请选择状态', trigger: 'change' }
  ],
  startDate: [
    { required: true, message: '请选择生效时间', trigger: 'change' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.amount = 500
  formData.giftAmount = 50
  formData.isEnabled = true
  formData.sort = 0
  formData.startDate = formatDate(new Date())
  formData.endDate = ''
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: RechargeRule) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.amount = row.amount
  formData.giftAmount = row.giftAmount
  formData.isEnabled = row.isEnabled
  formData.sort = row.sort
  formData.startDate = formatDate(row.startDate) || ''
  formData.endDate = formatDate(row.endDate) || ''
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: RechargeRule) => {
  try {
    await ElMessageBox.confirm(`确定要删除规则 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteRechargeRule(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error((error as Error).message || '删除失败')
    }
  }
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) return
  try {
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个规则吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    for (const row of selectedRows.value) {
      await deleteRechargeRule(row.id)
    }
    ElMessage.success('批量删除成功')
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
      submitLoading.value = true
      try {
        const payload = {
          name: formData.name,
          amount: formData.amount,
          giftAmount: formData.giftAmount,
          isEnabled: formData.isEnabled,
          sort: formData.sort,
          startDate: formData.startDate,
          // 留空表示长期有效
          endDate: formData.endDate || undefined,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateRechargeRule({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createRechargeRule(payload)
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
const handleSelectionChange = (rows: RechargeRule[]) => {
  selectedRows.value = rows
}

/** 格式化价格 */
const formatPrice = (price: number | undefined) => {
  if (price === null || price === undefined) return '0.00'
  return price.toFixed(2)
}


/** 赠送比例由充值金额与赠送金额实时换算（后端不存储比例字段） */
const formatBonusRate = (amount: number | undefined, giftAmount: number | undefined) => {
  if (!amount || amount <= 0) return '0%'
  return (((giftAmount || 0) / amount) * 100).toFixed(1) + '%'
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadData()
})
</script>

<style scoped>
.rule-management {
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

/* 规则名称 */
.rule-name {
  font-weight: 500;
  color: var(--text-primary);
}

/* 金额样式 */
.bonus-text {
  color: var(--el-color-success);
  font-weight: 500;
}

.text-muted {
  color: var(--text-tertiary);
}

/* 表单提示 */
.form-tip {
  font-size: 12px;
  color: var(--text-tertiary);
  line-height: 1.5;
  margin-top: 4px;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
