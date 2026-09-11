<template>
  <div class="level-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="等级名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入等级名称"
              clearable
              style="width: 180px"
            />
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
        <el-button type="primary" @click="handleAdd()" v-if="hasPermission('store:customer:level:add')">
          <el-icon><Plus /></el-icon>
          新增等级
        </el-button>
        <el-button
          type="danger"
          :disabled="selectedRows.length === 0"
          @click="handleBatchDelete"
          v-if="hasPermission('store:customer:level:batchDelete')"
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
        :data="filteredData"
        @selection-change="handleSelectionChange"
        style="width: 100%"
      >
        <el-table-column type="selection" width="50" />
        <el-table-column prop="level" label="等级值" width="100" align="center" />
        <el-table-column prop="name" label="等级名称" min-width="140">
          <template #default="{ row }">
            <span class="level-name">{{ row.name }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="code" label="等级编码" width="140" />
        <el-table-column label="折扣率" width="120" align="center">
          <template #default="{ row }">
            <span v-if="row.discountRate >= 1">-</span>
            <el-tag v-else :type="discountTagType(row.discountRate)" size="small" effect="plain">
              {{ (row.discountRate * 10).toFixed(1) }}折
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)" v-if="hasPermission('store:customer:level:edit')">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>
            <el-button link type="danger" size="small" @click="handleDelete(row)" v-if="hasPermission('store:customer:level:delete')">
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
      :title="isEdit ? '编辑等级' : '新增等级'"
      width="520px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="90px"
      >
        <el-form-item label="等级名称" prop="name">
          <el-input v-model="formData.name" placeholder="请输入等级名称" />
        </el-form-item>
        <el-form-item label="等级编码" prop="code">
          <el-input v-model="formData.code" placeholder="如 NORMAL、SILVER、level1" />
        </el-form-item>
        <el-form-item label="等级值" prop="level">
          <el-input-number
            v-model="formData.level"
            :min="1"
            :step="1"
            :precision="0"
            controls-position="right"
            style="width: 100%"
          />
          <div class="form-tip">等级值不可重复，用于标识等级高低</div>
        </el-form-item>
        <el-form-item label="折扣率" prop="discountRate">
          <el-input-number
            v-model="formData.discountRate"
            :min="0.01"
            :max="1"
            :step="0.05"
            :precision="2"
            controls-position="right"
            style="width: 100%"
          />
          <div class="form-tip">1.00 表示原价，0.90 表示9折</div>
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
import { getCustomerLevels, createCustomerLevel, updateCustomerLevel, deleteCustomerLevel, batchDeleteCustomerLevels } from '@/api/customer'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { useUserStore } from '@/stores/user'
import type { CustomerLevel } from '@/api/customer/types'

const systemConfigStore = useSystemConfigStore()

const userStore = useUserStore()

const hasPermission = (permissionCode: string) => userStore.hasPermission(permissionCode)

// 搜索表单
const searchForm = reactive({
  name: ''
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<CustomerLevel[]>([])
const selectedRows = ref<CustomerLevel[]>([])

// 前端过滤（等级列表不分页），按等级值升序排序
const filteredData = computed(() => {
  const data = searchForm.name
    ? tableData.value.filter(item => item.name.includes(searchForm.name))
    : tableData.value
  return [...data].sort((a, b) => a.level - b.level)
})

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    tableData.value = await getCustomerLevels()
  } catch (error) {
    ElMessage.error((error as Error).message || '加载数据失败')
  } finally {
    tableLoading.value = false
  }
}

// 搜索
const handleSearch = () => {
  // 前端过滤，无需重新加载
}

// 重置
const handleReset = () => {
  searchForm.name = ''
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
  level: 1,
  discountRate: 1,
  remark: ''
})

const formRules: FormRules = {
  name: [
    { required: true, message: '等级名称不能为空', trigger: 'blur' },
    { max: 50, message: '等级名称最多50个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '等级编码不能为空', trigger: 'blur' },
    { pattern: /^[A-Za-z0-9_]+$/, message: '等级编码只能包含字母、数字和下划线', trigger: 'blur' }
  ],
  level: [
    { required: true, message: '等级值不能为空', trigger: 'blur' },
    { type: 'number', min: 1, message: '等级值必须为正整数', trigger: 'blur' }
  ],
  discountRate: [
    { required: true, message: '折扣率不能为空', trigger: 'blur' },
    { type: 'number', min: 0.01, max: 1, message: '折扣率范围 0.01-1', trigger: 'blur' }
  ]
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.level = 1
  formData.discountRate = 1
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  // 默认等级值取当前最大值 + 1，避免与已有等级冲突
  formData.level = tableData.value.length > 0
    ? Math.max(...tableData.value.map(l => l.level)) + 1
    : 1
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: CustomerLevel) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.level = row.level
  formData.discountRate = row.discountRate
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: CustomerLevel) => {
  try {
    await ElMessageBox.confirm(`确定要删除等级 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteCustomerLevel(row.id)
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
    await ElMessageBox.confirm(`确定要删除选中的 ${selectedRows.value.length} 个等级吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await batchDeleteCustomerLevels(selectedRows.value.map(r => r.id))
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
          code: formData.code,
          level: formData.level,
          discountRate: formData.discountRate,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateCustomerLevel({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createCustomerLevel(payload)
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
const handleSelectionChange = (rows: CustomerLevel[]) => {
  selectedRows.value = rows
}

/** 折扣率标签类型 */
const discountTagType = (rate: number) => {
  if (rate >= 1) return 'info'
  if (rate >= 0.9) return ''
  if (rate >= 0.8) return 'success'
  return 'warning'
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  loadData()
})
</script>

<style scoped>
.level-management {
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

/* 等级名称 */
.level-name {
  font-weight: 500;
  color: var(--text-primary);
}

/* 表单提示 */
.form-tip {
  font-size: 12px;
  color: var(--text-tertiary);
  line-height: 1.5;
  margin-top: 4px;
}
</style>
