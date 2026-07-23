<template>
  <div class="beauty-profile">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="客户名称">
            <el-input
              v-model="searchForm.customerName"
              placeholder="请输入客户名称"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="肤质类型">
            <el-select v-model="searchForm.skinType" placeholder="全部" clearable style="width: 120px">
              <el-option label="干性" value="dry" />
              <el-option label="油性" value="oily" />
              <el-option label="混合性" value="combination" />
              <el-option label="敏感性" value="sensitive" />
              <el-option label="中性" value="normal" />
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
        <span class="toolbar-hint">肤质档案列表</span>
      </div>
      <div class="toolbar-right">
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新建档案
        </el-button>
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
        <el-table-column prop="customerName" label="客户名称" width="100" />
        <el-table-column prop="customerPhone" label="手机号" width="130" />
        <el-table-column label="肤质类型" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.skinType" :type="getSkinTypeTagType(row.skinType)" size="small">
              {{ getSkinTypeText(row.skinType) }}
            </el-tag>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column label="敏感程度" width="100" align="center">
          <template #default="{ row }">
            <el-tag v-if="row.sensitivity" :type="getSensitivityTagType(row.sensitivity)" size="small" effect="plain">
              {{ getSensitivityText(row.sensitivity) }}
            </el-tag>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="hairType" label="发质情况" min-width="120" show-overflow-tooltip />
        <el-table-column prop="allergyHistory" label="过敏史/禁忌" min-width="160" show-overflow-tooltip />
        <el-table-column prop="remark" label="备注" min-width="120" show-overflow-tooltip />
        <el-table-column prop="updatedAt" label="更新时间" width="170">
          <template #default="{ row }">
            {{ row.updatedAt || row.createdAt }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button link type="primary" size="small" @click="handleEdit(row)">
              <el-icon><Edit /></el-icon>
              编辑
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

    <!-- 新建/编辑弹窗 -->
    <el-dialog v-model="dialogVisible" :title="dialogTitle" width="560px" @closed="handleDialogClosed">
      <el-form
        ref="formRef"
        :model="profileForm"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="客户" prop="customerId">
          <el-select
            v-model="profileForm.customerId"
            placeholder="请选择客户"
            filterable
            style="width: 100%"
            :disabled="isEdit"
          >
            <el-option
              v-for="item in customerOptions"
              :key="item.id"
              :label="`${item.name}（${item.phone}）`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="肤质类型">
          <el-select v-model="profileForm.skinType" placeholder="请选择" clearable style="width: 100%">
            <el-option label="干性" value="dry" />
            <el-option label="油性" value="oily" />
            <el-option label="混合性" value="combination" />
            <el-option label="敏感性" value="sensitive" />
            <el-option label="中性" value="normal" />
          </el-select>
        </el-form-item>

        <el-form-item label="敏感程度">
          <el-select v-model="profileForm.sensitivity" placeholder="请选择" clearable style="width: 100%">
            <el-option label="低" value="low" />
            <el-option label="中" value="medium" />
            <el-option label="高" value="high" />
          </el-select>
        </el-form-item>

        <el-form-item label="发质情况">
          <el-input
            v-model="profileForm.hairType"
            placeholder="请输入发质情况"
            maxlength="100"
          />
        </el-form-item>

        <el-form-item label="过敏史">
          <el-input
            v-model="profileForm.allergyHistory"
            type="textarea"
            :rows="3"
            placeholder="请输入过敏史和禁忌成分"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>

        <el-form-item label="备注">
          <el-input
            v-model="profileForm.remark"
            type="textarea"
            :rows="2"
            placeholder="请输入备注"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">
          保存
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Edit } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import { getBeautyProfiles, saveBeautyProfile, getCustomerOptions } from '@/api/customer-profile'
import type {
  CustomerBeautyProfile,
  BeautyProfileSave,
  CustomerOption,
  SkinType,
  SensitivityLevel
} from '@/api/customer-profile/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  customerName: '',
  skinType: undefined as SkinType | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<CustomerBeautyProfile[]>([])

// 分页
const pagination = reactive({
  pageIndex: 1,
  pageSize: systemConfigStore.defaultPageSize,
  total: 0
})

// 肤质类型文本
const getSkinTypeText = (type: SkinType): string => {
  const map: Record<string, string> = {
    dry: '干性',
    oily: '油性',
    combination: '混合性',
    sensitive: '敏感性',
    normal: '中性'
  }
  return map[type] || type
}

// 肤质类型标签颜色
const getSkinTypeTagType = (type: SkinType): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<string, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    dry: 'warning',
    oily: 'info',
    combination: '',
    sensitive: 'danger',
    normal: 'success'
  }
  return map[type] || 'info'
}

// 敏感程度文本
const getSensitivityText = (level: SensitivityLevel): string => {
  const map: Record<string, string> = { low: '低', medium: '中', high: '高' }
  return map[level] || level
}

// 敏感程度标签颜色
const getSensitivityTagType = (level: SensitivityLevel): '' | 'success' | 'info' | 'warning' | 'danger' => {
  const map: Record<string, '' | 'success' | 'info' | 'warning' | 'danger'> = {
    low: 'success',
    medium: 'warning',
    high: 'danger'
  }
  return map[level] || 'info'
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getBeautyProfiles({
      customerName: searchForm.customerName || undefined,
      skinType: searchForm.skinType,
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
  searchForm.customerName = ''
  searchForm.skinType = undefined
  handleSearch()
}

// ==================== 弹窗 ====================

const dialogVisible = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()
const isEdit = ref(false)
const customerOptions = ref<CustomerOption[]>([])

const dialogTitle = computed(() => isEdit.value ? '编辑肤质档案' : '新建肤质档案')

const profileForm = reactive<BeautyProfileSave>({
  customerId: 0,
  skinType: undefined,
  sensitivity: undefined,
  hairType: '',
  allergyHistory: '',
  remark: ''
})

const formRules: FormRules = {
  customerId: [{ required: true, message: '请选择客户', trigger: 'change' }]
}

// 新建
const handleAdd = async () => {
  isEdit.value = false
  if (customerOptions.value.length === 0) {
    customerOptions.value = await getCustomerOptions()
  }
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: CustomerBeautyProfile) => {
  isEdit.value = true
  profileForm.customerId = row.customerId
  profileForm.skinType = row.skinType
  profileForm.sensitivity = row.sensitivity
  profileForm.hairType = row.hairType || ''
  profileForm.allergyHistory = row.allergyHistory || ''
  profileForm.remark = row.remark || ''
  dialogVisible.value = true
}

// 弹窗关闭后重置
const handleDialogClosed = () => {
  formRef.value?.resetFields()
  isEdit.value = false
  profileForm.customerId = 0
  profileForm.skinType = undefined
  profileForm.sensitivity = undefined
  profileForm.hairType = ''
  profileForm.allergyHistory = ''
  profileForm.remark = ''
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    submitLoading.value = true
    try {
      await saveBeautyProfile({
        customerId: profileForm.customerId,
        skinType: profileForm.skinType,
        sensitivity: profileForm.sensitivity,
        hairType: profileForm.hairType || undefined,
        allergyHistory: profileForm.allergyHistory || undefined,
        remark: profileForm.remark || undefined
      })
      ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
      dialogVisible.value = false
      loadData()
    } catch (error: any) {
      ElMessage.error(error.message || '保存失败')
    } finally {
      submitLoading.value = false
    }
  })
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
.beauty-profile {
  width: 100%;
}

.card {
  background: var(--bg-tertiary);
  border-radius: var(--radius-lg);
  border: 1px solid var(--border-primary);
  overflow: hidden;
}

.mb-20 {
  margin-bottom: 20px;
}

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

.search-form {
  padding: 20px 24px 0;
}

.search-form-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

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
  align-items: center;
}

.toolbar-right {
  display: flex;
  gap: 8px;
}

.toolbar-hint {
  font-size: 13px;
  color: var(--text-tertiary);
}

.text-muted {
  color: var(--text-tertiary);
}

.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
