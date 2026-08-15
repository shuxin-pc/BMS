<template>
  <div class="treatment-card-config">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="卡名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入卡名称"
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
        <el-button type="primary" @click="handleAdd">
          <el-icon><Plus /></el-icon>
          新增疗程卡
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
        <el-table-column prop="code" label="卡编码" width="100" />
        <el-table-column prop="name" label="卡名称" min-width="140" />
        <el-table-column label="项目明细" min-width="200" show-overflow-tooltip>
          <template #default="{ row }">
            {{ formatItems(row.items) }}
          </template>
        </el-table-column>
        <el-table-column prop="totalTimes" label="总次数" width="80" align="center" />
        <el-table-column label="原价合计" width="100" align="right">
          <template #default="{ row }">
            <span class="price-text">¥{{ formatPrice(calculateOriginalTotal(row)) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="卡价" width="100" align="right">
          <template #default="{ row }">
            <span class="price-text">¥{{ formatPrice(row.price) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="优惠" width="90" align="center">
          <template #default="{ row }">
            <span class="discount-text">{{ calculateDiscount(row) }}折</span>
          </template>
        </el-table-column>
        <el-table-column label="有效期" width="100" align="center">
          <template #default="{ row }">
            {{ row.validityDays }}天
          </template>
        </el-table-column>
        <el-table-column label="状态" width="90">
          <template #default="{ row }">
            <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small" effect="dark">
              {{ row.isEnabled ? '启用' : '停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="160" fixed="right">
          <template #default="{ row }">
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
      :title="isEdit ? '编辑疗程卡' : '新增疗程卡'"
      width="720px"
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
            <el-form-item label="卡名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入卡名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="卡编码" prop="code">
              <el-input v-model="formData.code" placeholder="请输入卡编码" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="卡价" prop="price">
              <el-input-number
                v-model="formData.price"
                :min="0"
                :precision="2"
                :step="1"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="有效期(天)" prop="validityDays">
              <el-input-number
                v-model="formData.validityDays"
                :min="1"
                :step="1"
                controls-position="right"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="状态" prop="isEnabled">
          <el-radio-group v-model="formData.isEnabled">
            <el-radio :value="true">启用</el-radio>
            <el-radio :value="false">停用</el-radio>
          </el-radio-group>
        </el-form-item>

        <!-- 项目明细 -->
        <el-form-item label="项目明细" prop="items">
          <div class="items-editor">
            <el-table :data="formData.items" border size="small" style="width: 100%">
              <el-table-column label="商品ID" width="120">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.productId"
                    :min="1"
                    :controls="false"
                    style="width: 100%"
                    placeholder="商品ID"
                  />
                </template>
              </el-table-column>
              <el-table-column label="商品名称" min-width="140">
                <template #default="{ row }">
                  <el-input v-model="row.productName" placeholder="商品名称（仅显示）" />
                </template>
              </el-table-column>
              <el-table-column label="次数" width="100">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.quantity"
                    :min="1"
                    :step="1"
                    :controls="false"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="原价" width="120">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.originalPrice"
                    :min="0"
                    :precision="2"
                    :controls="false"
                    style="width: 100%"
                  />
                </template>
              </el-table-column>
              <el-table-column label="操作" width="70" align="center">
                <template #default="{ $index }">
                  <el-button link type="danger" size="small" @click="removeItem($index)">
                    <el-icon><Delete /></el-icon>
                  </el-button>
                </template>
              </el-table-column>
            </el-table>
            <div class="items-summary">
              <span>总次数：{{ itemsTotalCount }} 次</span>
              <span>原价合计：¥{{ formatPrice(itemsTotalOriginal) }}</span>
              <el-button link type="primary" size="small" @click="addItem">
                <el-icon><Plus /></el-icon>
                添加项目
              </el-button>
            </div>
          </div>
        </el-form-item>

        <el-form-item label="描述">
          <el-input v-model="formData.description" type="textarea" :rows="2" placeholder="请输入描述" />
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
import { Search, Refresh, Plus, Edit, Delete } from '@element-plus/icons-vue'
import {
  getTreatmentCardConfigs,
  createTreatmentCardConfig,
  updateTreatmentCardConfig,
  deleteTreatmentCardConfig
} from '@/api/treatment-card'
import { useSystemConfigStore } from '@/stores/systemConfig'
import type { TreatmentCardConfig, CourseCardItemInput } from '@/api/treatment-card/types'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  isEnabled: undefined as boolean | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<TreatmentCardConfig[]>([])

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
    const res = await getTreatmentCardConfigs({
      name: searchForm.name || undefined,
      isEnabled: searchForm.isEnabled,
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
  searchForm.isEnabled = undefined
  handleSearch()
}

// 格式化价格
const formatPrice = (price: number) => {
  return (price || 0).toFixed(2)
}

// 格式化项目明细摘要
const formatItems = (items: TreatmentCardConfig['items']): string => {
  if (!items || items.length === 0) return '-'
  return items.map(i => i.productName || `商品${i.productId}`).join('、')
}

// 计算原价合计
const calculateOriginalTotal = (row: TreatmentCardConfig): number => {
  if (!row.items || row.items.length === 0) return 0
  return row.items.reduce((sum, item) => sum + item.originalPrice * item.quantity, 0)
}

// 计算折扣
const calculateDiscount = (row: TreatmentCardConfig): string => {
  const originalTotal = calculateOriginalTotal(row)
  if (originalTotal <= 0 || row.price <= 0) return '-'
  const discount = (row.price / originalTotal * 10).toFixed(1)
  return discount
}

// 弹窗
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitLoading = ref(false)
const formRef = ref<FormInstance>()

// 表单中的项目明细行（含 productName 用于显示）
interface ItemFormRow extends CourseCardItemInput {
  productName?: string
}

const formData = reactive({
  id: 0,
  name: '',
  code: '',
  price: 0,
  validityDays: 30,
  isEnabled: true,
  items: [] as ItemFormRow[],
  description: ''
})

// 表单中的计算属性
const itemsTotalCount = computed(() => {
  return formData.items.reduce((sum, item) => sum + (item.quantity || 0), 0)
})

const itemsTotalOriginal = computed(() => {
  return formData.items.reduce((sum, item) => sum + (item.originalPrice || 0) * (item.quantity || 0), 0)
})

const formRules: FormRules = {
  name: [
    { required: true, message: '卡名称不能为空', trigger: 'blur' },
    { max: 100, message: '卡名称最多100个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '卡编码不能为空', trigger: 'blur' },
    { max: 50, message: '卡编码最多50个字符', trigger: 'blur' }
  ],
  price: [
    { required: true, message: '卡价不能为空', trigger: 'blur' },
    { type: 'number', min: 0, message: '卡价必须大于等于0', trigger: 'blur' }
  ],
  validityDays: [
    { required: true, message: '有效期不能为空', trigger: 'blur' },
    { type: 'number', min: 1, message: '有效期必须大于0', trigger: 'blur' }
  ],
  isEnabled: [{ required: true, message: '请选择状态', trigger: 'change' }]
}

// 添加项目行
const addItem = () => {
  formData.items.push({
    productId: 0,
    quantity: 1,
    originalPrice: 0,
    productName: ''
  })
}

// 删除项目行
const removeItem = (index: number) => {
  formData.items.splice(index, 1)
}

// 重置表单
const resetForm = () => {
  formData.id = 0
  formData.name = ''
  formData.code = ''
  formData.price = 0
  formData.validityDays = 30
  formData.isEnabled = true
  formData.items = []
  formData.description = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetForm()
  addItem() // 默认添加一行空项目
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: TreatmentCardConfig) => {
  isEdit.value = true
  formData.id = row.id
  formData.name = row.name
  formData.code = row.code
  formData.price = row.price
  formData.validityDays = row.validityDays
  formData.isEnabled = row.isEnabled
  formData.items = row.items.map(item => ({
    productId: item.productId,
    quantity: item.quantity,
    originalPrice: item.originalPrice,
    productName: item.productName
  }))
  formData.description = row.description || row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: TreatmentCardConfig) => {
  try {
    await ElMessageBox.confirm(`确定要删除疗程卡 "${row.name}" 吗？此操作不可恢复！`, '警告', {
      type: 'warning',
      confirmButtonText: '确定删除',
      cancelButtonText: '取消'
    })
    await deleteTreatmentCardConfig(row.id)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') ElMessage.error('删除失败')
  }
}

// 提交
const handleSubmit = async () => {
  if (!formRef.value) return
  await formRef.value.validate(async (valid) => {
    if (valid) {
      // 校验项目明细
      if (formData.items.length === 0) {
        ElMessage.warning('请至少添加一个项目明细')
        return
      }
      for (const item of formData.items) {
        if (!item.productId || item.productId <= 0) {
          ElMessage.warning('项目明细中的商品ID必须大于0')
          return
        }
        if (!item.quantity || item.quantity <= 0) {
          ElMessage.warning('项目明细中的次数必须大于0')
          return
        }
      }

      submitLoading.value = true
      try {
        // 构造提交数据，移除 productName（仅用于显示）
        const items: CourseCardItemInput[] = formData.items.map(item => ({
          productId: item.productId,
          quantity: item.quantity,
          originalPrice: item.originalPrice
        }))
        const payload = {
          name: formData.name,
          code: formData.code,
          totalTimes: itemsTotalCount.value,
          price: formData.price,
          validityDays: formData.validityDays,
          isEnabled: formData.isEnabled,
          items,
          remark: formData.description || undefined
        }
        if (isEdit.value) {
          await updateTreatmentCardConfig({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createTreatmentCardConfig(payload)
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

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadData()
})
</script>

<style scoped>
.treatment-card-config {
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

/* 项目明细编辑器 */
.items-editor {
  width: 100%;
}

.items-summary {
  display: flex;
  align-items: center;
  gap: 20px;
  margin-top: 8px;
  font-size: 13px;
  color: var(--text-tertiary);
}

/* 价格样式 */
.price-text {
  color: var(--primary);
  font-weight: 600;
}

.discount-text {
  color: #e6a23c;
  font-weight: 600;
}

/* 分页 */
.pagination-container {
  display: flex;
  justify-content: flex-end;
  padding: 20px 24px;
  border-top: 1px solid var(--border-primary);
}
</style>
