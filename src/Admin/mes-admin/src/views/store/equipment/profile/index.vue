<template>
  <div class="equipment-management">
    <!-- 搜索区域 -->
    <div class="card mb-20">
      <div class="search-form">
        <el-form :inline="true" :model="searchForm" class="search-form-inline">
          <el-form-item label="设备名称">
            <el-input
              v-model="searchForm.name"
              placeholder="请输入设备名称"
              clearable
              style="width: 180px"
            />
          </el-form-item>
          <el-form-item label="资产编号">
            <el-input
              v-model="searchForm.code"
              placeholder="请输入资产编号"
              clearable
              style="width: 160px"
            />
          </el-form-item>
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="全部" clearable style="width: 120px">
              <el-option label="正常" :value="1" />
              <el-option label="维修中" :value="2" />
              <el-option label="已停用" :value="3" />
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
          新增设备
        </el-button>
        <el-button @click="loadUpcomingMaintenance">
          <el-icon><Bell /></el-icon>
          即将到期保养
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
      <el-table v-loading="tableLoading" :data="tableData" style="width: 100%">
        <el-table-column prop="code" label="资产编号" width="140" />
        <el-table-column prop="equipmentTypeName" label="设备类型" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.equipmentTypeName || '-' }}</template>
        </el-table-column>
        <el-table-column prop="name" label="设备名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="model" label="型号" width="140" show-overflow-tooltip>
          <template #default="{ row }">{{ row.model || '-' }}</template>
        </el-table-column>
        <el-table-column prop="manufacturer" label="厂商" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.manufacturer || '-' }}</template>
        </el-table-column>
        <el-table-column prop="purchaseDate" label="购入日期" width="120">
          <template #default="{ row }">{{ formatDate(row.purchaseDate) }}</template>
        </el-table-column>
        <el-table-column prop="purchasePrice" label="购买价格" width="120" align="right">
          <template #default="{ row }">
            {{ row.purchasePrice != null ? `¥${row.purchasePrice.toLocaleString()}` : '-' }}
          </template>
        </el-table-column>
        <el-table-column prop="location" label="位置" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.location || '-' }}</template>
        </el-table-column>
        <el-table-column prop="nextMaintenanceDate" label="下次保养" width="120">
          <template #default="{ row }">
            <span :class="{ 'maintenance-warn': isMaintenanceNear(row.nextMaintenanceDate) }">
              {{ formatDate(row.nextMaintenanceDate) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">
              {{ getStatusText(row.status) }}
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
      :title="isEdit ? '编辑设备' : '新增设备'"
      width="720px"
      :close-on-click-modal="false"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="设备类型" prop="equipmentTypeId">
          <el-select
            v-model="formData.equipmentTypeId"
            filterable
            placeholder="请选择设备类型"
            style="width: 100%"
          >
            <el-option
              v-for="item in equipmentTypeOptions"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="设备名称" prop="name">
              <el-input v-model="formData.name" placeholder="请输入设备名称" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="资产编号" prop="code">
              <el-input v-model="formData.code" placeholder="如 EQ-2024-001" :disabled="isEdit" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="型号" prop="model">
              <el-input v-model="formData.model" placeholder="请输入型号" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="厂商" prop="manufacturer">
              <el-input v-model="formData.manufacturer" placeholder="请输入厂商" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="购买日期" prop="purchaseDate">
              <el-date-picker
                v-model="formData.purchaseDate"
                type="date"
                placeholder="选择购买日期"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="购买价格" prop="purchasePrice">
              <el-input-number
                v-model="formData.purchasePrice"
                :min="0"
                :precision="2"
                :step="1000"
                placeholder="请输入购买价格"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="存放位置" prop="location">
              <el-input v-model="formData.location" placeholder="请输入存放位置" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="设备状态" prop="status">
              <el-select v-model="formData.status" placeholder="请选择状态" style="width: 100%">
                <el-option label="正常" :value="1" />
                <el-option label="维修中" :value="2" />
                <el-option label="已停用" :value="3" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="上次保养" prop="lastMaintenanceDate">
              <el-date-picker
                v-model="formData.lastMaintenanceDate"
                type="date"
                placeholder="选择上次保养日期"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="下次保养" prop="nextMaintenanceDate">
              <el-date-picker
                v-model="formData.nextMaintenanceDate"
                type="date"
                placeholder="选择下次保养日期"
                format="YYYY-MM-DD"
                value-format="YYYY-MM-DD"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="保养周期(天)" prop="maintenanceCycleDays">
              <el-input-number
                v-model="formData.maintenanceCycleDays"
                :min="1"
                :step="1"
                placeholder="留空=不定期"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="备注" prop="remark">
          <el-input v-model="formData.remark" type="textarea" :rows="3" placeholder="请输入备注" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitLoading" @click="handleSubmit">确定</el-button>
      </template>
    </el-dialog>

    <!-- 即将到期保养弹窗 -->
    <el-dialog
      v-model="upcomingVisible"
      title="即将到期保养（未来 7 天，含已过期）"
      width="720px"
    >
      <el-table v-loading="upcomingLoading" :data="upcomingList" style="width: 100%">
        <el-table-column prop="code" label="资产编号" width="140" />
        <el-table-column prop="name" label="设备名称" min-width="160" show-overflow-tooltip />
        <el-table-column prop="location" label="位置" width="120" show-overflow-tooltip>
          <template #default="{ row }">{{ row.location || '-' }}</template>
        </el-table-column>
        <el-table-column prop="nextMaintenanceDate" label="下次保养" width="140">
          <template #default="{ row }">
            <span :class="{ 'maintenance-warn': isMaintenanceNear(row.nextMaintenanceDate) }">
              {{ formatDate(row.nextMaintenanceDate) }}
            </span>
          </template>
        </el-table-column>
        <el-table-column label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small" effect="dark">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
      </el-table>
      <template #footer>
        <el-button @click="upcomingVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import { Search, Refresh, Plus, Delete, Edit, Bell } from '@element-plus/icons-vue'
import { useSystemConfigStore } from '@/stores/systemConfig'
import {
  getEquipmentList,
  createEquipment,
  updateEquipment,
  deleteEquipment,
  getUpcomingMaintenance
} from '@/api/equipment'
import type { Equipment, EquipmentStatus } from '@/api/equipment/types'
import { getEquipmentTypeOptions } from '@/api/equipment-type'
import type { EquipmentType } from '@/api/equipment-type'

const systemConfigStore = useSystemConfigStore()

// 搜索表单
const searchForm = reactive({
  name: '',
  code: '',
  status: undefined as EquipmentStatus | undefined
})

// 表格数据
const tableLoading = ref(false)
const tableData = ref<Equipment[]>([])

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

// 即将到期保养弹窗
const upcomingVisible = ref(false)
const upcomingLoading = ref(false)
const upcomingList = ref<Equipment[]>([])

// 设备类型选项（租户级共享）
const equipmentTypeOptions = ref<EquipmentType[]>([])

const formData = reactive({
  id: 0,
  equipmentTypeId: undefined as number | undefined,
  name: '',
  code: '',
  model: '',
  manufacturer: '',
  purchaseDate: '',
  purchasePrice: undefined as number | undefined,
  status: 1 as EquipmentStatus,
  location: '',
  lastMaintenanceDate: '',
  nextMaintenanceDate: '',
  maintenanceCycleDays: undefined as number | undefined,
  remark: ''
})

const formRules: FormRules = {
  equipmentTypeId: [
    { required: true, message: '请选择设备类型', trigger: 'change' }
  ],
  name: [
    { required: true, message: '设备名称不能为空', trigger: 'blur' },
    { max: 100, message: '设备名称最多100个字符', trigger: 'blur' }
  ],
  code: [
    { required: true, message: '资产编号不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '资产编号长度为2-50个字符', trigger: 'blur' }
  ],
  status: [
    { required: true, message: '请选择设备状态', trigger: 'change' }
  ]
}

/**
 * 获取状态文本
 */
const getStatusText = (status: EquipmentStatus): string => {
  const map: Record<EquipmentStatus, string> = {
    1: '正常',
    2: '维修中',
    3: '已停用'
  }
  return map[status] || '未知'
}

/**
 * 获取状态标签类型
 */
const getStatusTagType = (status: EquipmentStatus): 'info' | 'success' | 'warning' | 'danger' => {
  const map: Record<EquipmentStatus, 'info' | 'success' | 'warning' | 'danger'> = {
    1: 'success',
    2: 'warning',
    3: 'danger'
  }
  return map[status] || 'info'
}

/**
 * 格式化日期
 */
const formatDate = (dateStr?: string): string => {
  if (!dateStr) return '-'
  return dateStr.split('T')[0]
}

/**
 * 判断保养日期是否临近（7天内）
 */
const isMaintenanceNear = (dateStr?: string): boolean => {
  if (!dateStr) return false
  const target = new Date(dateStr)
  const now = new Date()
  const diff = target.getTime() - now.getTime()
  const days = diff / (1000 * 60 * 60 * 24)
  return days >= 0 && days <= 7
}

// 加载数据
const loadData = async () => {
  tableLoading.value = true
  try {
    const res = await getEquipmentList({
      name: searchForm.name || undefined,
      code: searchForm.code || undefined,
      status: searchForm.status,
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
  searchForm.code = ''
  searchForm.status = undefined
  handleSearch()
}

// 重置表单
const resetFormData = () => {
  formData.id = 0
  formData.equipmentTypeId = undefined
  formData.name = ''
  formData.code = ''
  formData.model = ''
  formData.manufacturer = ''
  formData.purchaseDate = ''
  formData.purchasePrice = undefined
  formData.status = 1
  formData.location = ''
  formData.lastMaintenanceDate = ''
  formData.nextMaintenanceDate = ''
  formData.maintenanceCycleDays = undefined
  formData.remark = ''
}

// 新增
const handleAdd = () => {
  isEdit.value = false
  resetFormData()
  dialogVisible.value = true
}

// 编辑
const handleEdit = (row: Equipment) => {
  isEdit.value = true
  formData.id = row.id
  formData.equipmentTypeId = row.equipmentTypeId
  formData.name = row.name
  formData.code = row.code
  formData.model = row.model || ''
  formData.manufacturer = row.manufacturer || ''
  formData.purchaseDate = row.purchaseDate || ''
  formData.purchasePrice = row.purchasePrice
  formData.status = row.status
  formData.location = row.location || ''
  formData.lastMaintenanceDate = row.lastMaintenanceDate || ''
  formData.nextMaintenanceDate = row.nextMaintenanceDate || ''
  formData.maintenanceCycleDays = row.maintenanceCycleDays
  formData.remark = row.remark || ''
  dialogVisible.value = true
}

// 删除
const handleDelete = async (row: Equipment) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除设备 "${row.name}" 吗？关联的保养记录将一并删除，此操作不可恢复！`,
      '警告',
      {
        type: 'warning',
        confirmButtonText: '确定删除',
        cancelButtonText: '取消'
      }
    )
    await deleteEquipment(row.id)
    ElMessage.success('删除成功')
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
          equipmentTypeId: formData.equipmentTypeId!,
          name: formData.name,
          code: formData.code,
          model: formData.model || undefined,
          manufacturer: formData.manufacturer || undefined,
          purchaseDate: formData.purchaseDate || undefined,
          purchasePrice: formData.purchasePrice,
          status: formData.status,
          location: formData.location || undefined,
          lastMaintenanceDate: formData.lastMaintenanceDate || undefined,
          nextMaintenanceDate: formData.nextMaintenanceDate || undefined,
          maintenanceCycleDays: formData.maintenanceCycleDays,
          remark: formData.remark || undefined
        }
        if (isEdit.value) {
          await updateEquipment({ ...payload, id: formData.id })
          ElMessage.success('更新成功')
        } else {
          await createEquipment(payload)
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

// 加载即将到期保养设备列表
const loadUpcomingMaintenance = async () => {
  upcomingVisible.value = true
  upcomingLoading.value = true
  try {
    upcomingList.value = await getUpcomingMaintenance(7)
  } catch (error) {
    ElMessage.error((error as Error).message || '加载即将到期保养列表失败')
    upcomingList.value = []
  } finally {
    upcomingLoading.value = false
  }
}

// 加载设备类型选项
const loadEquipmentTypeOptions = async () => {
  try {
    equipmentTypeOptions.value = await getEquipmentTypeOptions()
  } catch {
    equipmentTypeOptions.value = []
  }
}

onMounted(async () => {
  if (!systemConfigStore.loaded) {
    await systemConfigStore.loadSystemConfigs()
  }
  pagination.pageSize = systemConfigStore.defaultPageSize
  loadEquipmentTypeOptions()
  loadData()
})
</script>

<style scoped>
.equipment-management {
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

/* 保养日期临近提醒 */
.maintenance-warn {
  color: var(--warning, #e6a23c);
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
